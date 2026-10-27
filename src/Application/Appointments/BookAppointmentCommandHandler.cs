using UnifiedServiceScheduler.Application.Common;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Exceptions;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.Services;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Application.Appointments;

/// <summary>
/// Orchestrates appointment booking across the Customer, Vehicle, Technician,
/// ServiceBay, Dealership, and Appointment aggregate boundaries.
///
/// Ensures:
/// 1. All entities exist and belong to the correct boundaries.
/// 2. Dealership operating hours are respected.
/// 3. Resource availability is checked via <see cref="IAvailabilityService"/>.
/// 4. Resource bookings are acquired via domain methods to generate valid evidence tokens.
/// 5. The Appointment aggregate is constructed with invariant validation.
/// 6. All participating aggregates are persisted atomically via <see cref="IUnitOfWork"/>.
/// 7. Errors are cleanly mapped to <see cref="Result{TValue, TError}"/> without throwing raw business exceptions.
/// </summary>
public sealed class BookAppointmentCommandHandler : ICommandHandler<BookAppointmentCommand, Result<AppointmentResponseDto, SchedulingError>>
{
    private readonly IDealershipRepository  _dealershipRepository;
    private readonly ICustomerRepository    _customerRepository;
    private readonly IVehicleRepository     _vehicleRepository;
    private readonly IServiceTypeRepository _serviceTypeRepository;
    private readonly ITechnicianRepository  _technicianRepository;
    private readonly IServiceBayRepository  _serviceBayRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityService   _availabilityService;
    private readonly IUnitOfWork            _unitOfWork;

    public BookAppointmentCommandHandler(
        IDealershipRepository  dealershipRepository,
        ICustomerRepository    customerRepository,
        IVehicleRepository     vehicleRepository,
        IServiceTypeRepository serviceTypeRepository,
        ITechnicianRepository  technicianRepository,
        IServiceBayRepository  serviceBayRepository,
        IAppointmentRepository appointmentRepository,
        IAvailabilityService   availabilityService,
        IUnitOfWork            unitOfWork)
    {
        _dealershipRepository  = dealershipRepository  ?? throw new ArgumentNullException(nameof(dealershipRepository));
        _customerRepository    = customerRepository    ?? throw new ArgumentNullException(nameof(customerRepository));
        _vehicleRepository     = vehicleRepository     ?? throw new ArgumentNullException(nameof(vehicleRepository));
        _serviceTypeRepository = serviceTypeRepository ?? throw new ArgumentNullException(nameof(serviceTypeRepository));
        _technicianRepository  = technicianRepository  ?? throw new ArgumentNullException(nameof(technicianRepository));
        _serviceBayRepository  = serviceBayRepository  ?? throw new ArgumentNullException(nameof(serviceBayRepository));
        _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
        _availabilityService   = availabilityService   ?? throw new ArgumentNullException(nameof(availabilityService));
        _unitOfWork            = unitOfWork            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<AppointmentResponseDto, SchedulingError>> HandleAsync(
        BookAppointmentCommand command,
        CancellationToken      ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. Basic validation
        if (command.DealershipId == Guid.Empty)
            return SchedulingError.Validation("Dealership ID cannot be empty.");
        if (command.CustomerId == Guid.Empty)
            return SchedulingError.Validation("Customer ID cannot be empty.");
        if (command.VehicleId == Guid.Empty)
            return SchedulingError.Validation("Vehicle ID cannot be empty.");
        if (command.ServiceTypeId == Guid.Empty)
            return SchedulingError.Validation("ServiceType ID cannot be empty.");
        if (command.DesiredStartTime <= DateTimeOffset.UtcNow)
            return SchedulingError.Validation("Desired appointment time must be in the future.");

        var dealershipId  = new DealershipId(command.DealershipId);
        var customerId    = new CustomerId(command.CustomerId);
        var vehicleId     = new VehicleId(command.VehicleId);
        var serviceTypeId = new ServiceTypeId(command.ServiceTypeId);

        // 2. Fetch prerequisite entities sequentially (EF DbContext is single-threaded)
        var dealership  = await _dealershipRepository.GetByIdAsync(dealershipId, ct);
        var customer    = await _customerRepository.GetByIdAsync(customerId, ct);
        var vehicle     = await _vehicleRepository.GetByIdAsync(vehicleId, ct);
        var serviceType = await _serviceTypeRepository.GetByIdAsync(serviceTypeId, ct);

        if (dealership is null)
            return SchedulingError.NotFound("Dealership", command.DealershipId.ToString());
        if (customer is null)
            return SchedulingError.NotFound("Customer", command.CustomerId.ToString());
        if (vehicle is null)
            return SchedulingError.NotFound("Vehicle", command.VehicleId.ToString());
        if (serviceType is null)
            return SchedulingError.NotFound("ServiceType", command.ServiceTypeId.ToString());

        // Invariant: Vehicle must belong to the specified Customer
        if (vehicle.OwnerId != customerId)
            return SchedulingError.InvariantViolation($"Vehicle '{vehicleId}' is not owned by customer '{customerId}'.");

        // 3. Compute target TimeSlot based on ServiceType.BaseDuration
        TimeSlot targetSlot;
        try
        {
            targetSlot = TimeSlot.From(command.DesiredStartTime, serviceType.BaseDuration);
        }
        catch (InvalidTimeSlotException ex)
        {
            return SchedulingError.Validation(ex.Message);
        }

        // 4. Verify Dealership working hours
        try
        {
            dealership.AssertWithinWorkingHours(targetSlot);
        }
        catch (OutsideWorkingHoursException ex)
        {
            var code = ex.Message.Contains("closed")
                ? "OutsideOperatingSchedule"
                : "ExceedsOperatingHours";

            return SchedulingError.OutsideWorkingHours(ex.Message, code);
        }

        // 4.5. Idempotency & Overlap Guard: Client double-click or conflicting vehicle schedule
        var existingAppointment = await _appointmentRepository.FindDuplicateAsync(dealershipId, customerId, vehicleId, targetSlot, ct);
        if (existingAppointment is not null)
        {
            // Case 7: If the requested start time is identical, it's an accidental client double-click resubmission!
            if (existingAppointment.Slot.Start == targetSlot.Start)
            {
                var bookedTech = await _technicianRepository.GetByIdAsync(existingAppointment.TechnicianId, ct);
                var bookedBay  = await _serviceBayRepository.GetByIdAsync(existingAppointment.ServiceBayId, ct);

                var idempotentDto = new AppointmentResponseDto(
                    existingAppointment.Id.Value,
                    existingAppointment.DealershipId.Value,
                    existingAppointment.CustomerId.Value,
                    existingAppointment.VehicleSnapshot.Id.Value,
                    existingAppointment.VehicleSnapshot.Vin,
                    $"{existingAppointment.VehicleSnapshot.Year} {existingAppointment.VehicleSnapshot.Make} {existingAppointment.VehicleSnapshot.Model}",
                    existingAppointment.TechnicianId.Value,
                    bookedTech?.Contact.FullName ?? "Assigned Technician",
                    existingAppointment.ServiceBayId.Value,
                    bookedBay?.BayCode ?? "Assigned Bay",
                    serviceType.Id.Value,
                    serviceType.Name,
                    existingAppointment.Slot.Start,
                    existingAppointment.Slot.End,
                    existingAppointment.Slot.Duration,
                    existingAppointment.Status.ToString(),
                    existingAppointment.Notes);

                return Result<AppointmentResponseDto, SchedulingError>.Success(idempotentDto);
            }

            // Head, Tail, or Enclosing Overlap Collision
            return SchedulingError.Conflict(
                $"Vehicle '{vehicleId}' already has an appointment booked for slot [{existingAppointment.Slot.Start:HH:mm}, {existingAppointment.Slot.End:HH:mm}) which overlaps with the requested slot [{targetSlot.Start:HH:mm}, {targetSlot.End:HH:mm}).");
        }

        // 5. Query candidate resources via Domain Service
        var availability = await _availabilityService.CheckAvailabilityAsync(
            dealershipId,
            targetSlot,
            serviceType,
            ct);

        if (!availability.IsAvailable)
        {
            return SchedulingError.ResourceUnavailable(availability.Failure!);
        }

        // 6. Select candidate pair (Technician + ServiceBay)
        var selectedPair = availability.CandidatePairs[0];
        var technician   = selectedPair.Technician;
        var bay          = selectedPair.ServiceBay;
        var appointmentId = AppointmentId.New();

        // 7. Domain execution: Reserve resources to produce booking evidence tokens
        TechnicianBooking techEvidence;
        BayBooking        bayEvidence;
        try
        {
            techEvidence = technician.Reserve(appointmentId, serviceType, targetSlot);
            bayEvidence  = bay.Reserve(appointmentId, serviceType, targetSlot);
        }
        catch (SchedulingConflictException ex)
        {
            return SchedulingError.Conflict(ex.Message);
        }
        catch (InsufficientSkillException ex)
        {
            return SchedulingError.InvariantViolation(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return SchedulingError.InvariantViolation(ex.Message);
        }

        // 8. Construct Appointment aggregate root with reservation evidence
        Appointment appointment;
        try
        {
            appointment = Appointment.Schedule(
                appointmentId,
                dealershipId,
                customerId,
                vehicle.TakeSnapshot(),
                techEvidence,
                bayEvidence,
                serviceTypeId,
                targetSlot,
                command.Notes);
        }
        catch (Exception ex)
        {
            // Compensate if aggregate creation failed
            technician.Release(appointmentId);
            bay.Release(appointmentId);
            return SchedulingError.InvariantViolation($"Failed to construct appointment aggregate: {ex.Message}");
        }

        // 9. Atomic persistence across aggregates
        try
        {
            await _appointmentRepository.AddAsync(appointment, ct);
            await _technicianRepository.UpdateAsync(technician, ct);
            await _serviceBayRepository.UpdateAsync(bay, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            // If another racing request just committed the exact same appointment:
            var concurrentAppt = await _appointmentRepository.FindDuplicateAsync(dealershipId, customerId, vehicleId, targetSlot, ct);
            if (concurrentAppt is not null)
            {
                var bookedTech = await _technicianRepository.GetByIdAsync(concurrentAppt.TechnicianId, ct);
                var bookedBay  = await _serviceBayRepository.GetByIdAsync(concurrentAppt.ServiceBayId, ct);

                var idempotentDto = new AppointmentResponseDto(
                    concurrentAppt.Id.Value,
                    concurrentAppt.DealershipId.Value,
                    concurrentAppt.CustomerId.Value,
                    concurrentAppt.VehicleSnapshot.Id.Value,
                    concurrentAppt.VehicleSnapshot.Vin,
                    $"{concurrentAppt.VehicleSnapshot.Year} {concurrentAppt.VehicleSnapshot.Make} {concurrentAppt.VehicleSnapshot.Model}",
                    concurrentAppt.TechnicianId.Value,
                    bookedTech?.Contact.FullName ?? "Assigned Technician",
                    concurrentAppt.ServiceBayId.Value,
                    bookedBay?.BayCode ?? "Assigned Bay",
                    serviceType.Id.Value,
                    serviceType.Name,
                    concurrentAppt.Slot.Start,
                    concurrentAppt.Slot.End,
                    concurrentAppt.Slot.Duration,
                    concurrentAppt.Status.ToString(),
                    concurrentAppt.Notes);

                return Result<AppointmentResponseDto, SchedulingError>.Success(idempotentDto);
            }

            // In a real environment, transaction rollback occurs here
            return SchedulingError.Conflict($"Transaction failed during atomic commit: {ex.Message}");
        }

        // 10. Map to response DTO
        var response = new AppointmentResponseDto(
            appointment.Id.Value,
            appointment.DealershipId.Value,
            appointment.CustomerId.Value,
            appointment.VehicleSnapshot.Id.Value,
            appointment.VehicleSnapshot.Vin,
            $"{appointment.VehicleSnapshot.Year} {appointment.VehicleSnapshot.Make} {appointment.VehicleSnapshot.Model}",
            technician.Id.Value,
            technician.Contact.FullName,
            bay.Id.Value,
            bay.BayCode,
            serviceType.Id.Value,
            serviceType.Name,
            appointment.Slot.Start,
            appointment.Slot.End,
            appointment.Slot.Duration,
            appointment.Status.ToString(),
            appointment.Notes);

        return Result<AppointmentResponseDto, SchedulingError>.Success(response);
    }
}
