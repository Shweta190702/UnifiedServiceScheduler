using UnifiedServiceScheduler.Application.Appointments;
using UnifiedServiceScheduler.Application.Common;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Services;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Tests.Fakes;
using Xunit;

namespace UnifiedServiceScheduler.Tests.Application;

/// <summary>
/// Application Layer Tests adhering to the Anti-Mocking principle:
/// Uses genuine in-memory repositories and authentic Domain Aggregates (Technician, ServiceBay,
/// Dealership, Appointment, Customer, Vehicle) to test actual business logic without mocking domain rules.
/// </summary>
public sealed class SchedulingEngineApplicationTests
{
    private static readonly DateTimeOffset FixedMonday8AmUtc = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemoryDealershipRepository  _dealershipRepo  = new();
    private readonly InMemoryCustomerRepository    _customerRepo    = new();
    private readonly InMemoryVehicleRepository     _vehicleRepo     = new();
    private readonly InMemoryServiceTypeRepository _serviceTypeRepo = new();
    private readonly InMemoryTechnicianRepository  _technicianRepo  = new();
    private readonly InMemoryServiceBayRepository  _serviceBayRepo  = new();
    private readonly InMemoryAppointmentRepository _appointmentRepo = new();
    private readonly InMemoryUnitOfWork            _unitOfWork      = new();

    private readonly AvailabilityService _availabilityService;
    private readonly BookAppointmentCommandHandler _handler;

    public SchedulingEngineApplicationTests()
    {
        _availabilityService = new AvailabilityService(
            _dealershipRepo,
            _serviceBayRepo,
            _technicianRepo);

        _handler = new BookAppointmentCommandHandler(
            _dealershipRepo,
            _customerRepo,
            _vehicleRepo,
            _serviceTypeRepo,
            _technicianRepo,
            _serviceBayRepo,
            _appointmentRepo,
            _availabilityService,
            _unitOfWork);
    }

    /// <summary>
    /// Requirement 3: Schedule_BayOccupied_FailsWithoutAllocatingTechnician.
    /// Real Domain Test: Technician is free and qualified, but the only ServiceBay is already booked.
    /// Asserts that no partial booking occurs and the technician remains unreserved.
    /// </summary>
    [Fact]
    public async Task Schedule_BayOccupied_FailsWithoutAllocatingTechnician()
    {
        // Arrange: Real Domain Entities
        var dealership = TestDomainDataBuilder.CreateDealership();
        _dealershipRepo.Seed(dealership);

        var customer = TestDomainDataBuilder.CreateCustomer();
        _customerRepo.Seed(customer);

        var vehicle = TestDomainDataBuilder.CreateVehicle(customer.Id);
        _vehicleRepo.Seed(vehicle);

        var serviceType = TestDomainDataBuilder.CreateStandardMaintenance();
        _serviceTypeRepo.Seed(serviceType);

        // Real Technician with required Master certification
        var cert = Certification.Create("Brakes", SkillLevel.Master, new DateOnly(2025, 1, 1));
        var technician = TestDomainDataBuilder.CreateTechnician(dealership.Id, "TECH-FREE", certifications: [cert]);
        _technicianRepo.Seed(technician);

        // Real ServiceBay with Lift capability
        var bay = TestDomainDataBuilder.CreateBay(dealership.Id, "BAY-OCCUPIED", capability: BayCapability.Lift);
        _serviceBayRepo.Seed(bay);

        var targetStartTime = FixedMonday8AmUtc.AddHours(2); // 10:00 UTC Monday
        var targetSlot = TimeSlot.From(targetStartTime, serviceType.BaseDuration); // [10:00, 11:00)

        // Pre-condition: Occupy the bay with another appointment
        bay.Reserve(AppointmentId.New(), serviceType, targetSlot);
        Assert.Single(bay.Bookings); // Verified pre-condition

        var command = new BookAppointmentCommand(
            dealership.Id.Value,
            customer.Id.Value,
            vehicle.Id.Value,
            serviceType.Id.Value,
            targetStartTime);

        // Act: Run actual command handler with real domain logic
        var result = await _handler.HandleAsync(command);

        // Assert 1: Clean typed failure
        Assert.True(result.IsFailure);
        Assert.Equal(SchedulingErrorKind.ResourceUnavailable, result.Error.Kind);
        Assert.Equal(AvailabilityFailureCode.AllCompatibleBaysBooked.ToString(), result.Error.Code);

        // Assert 2: Invariant Guard - Free technician was NEVER allocated or locked
        Assert.Empty(technician.Bookings);

        // Assert 3: Invariant Guard - Zero appointments saved, zero unit of work commits
        Assert.Empty(_appointmentRepo.All);
        Assert.Equal(0, _unitOfWork.CommitCount);
    }

    /// <summary>
    /// Requirement 5: Schedule_OutsideOperatingHours_ReturnsValidationError.
    /// Tested with deterministic timestamps: 17:30 UTC start spills past 18:00 UTC closing.
    /// </summary>
    [Fact]
    public async Task Schedule_OutsideOperatingHours_ReturnsValidationError()
    {
        // Arrange
        var dealership = TestDomainDataBuilder.CreateDealership(); // Closes at 18:00 UTC
        _dealershipRepo.Seed(dealership);

        var customer = TestDomainDataBuilder.CreateCustomer();
        _customerRepo.Seed(customer);

        var vehicle = TestDomainDataBuilder.CreateVehicle(customer.Id);
        _vehicleRepo.Seed(vehicle);

        var serviceType = TestDomainDataBuilder.CreateStandardMaintenance(); // 1-hour duration
        _serviceTypeRepo.Seed(serviceType);

        var pastClosingStartTime = new DateTimeOffset(2026, 11, 2, 17, 30, 0, TimeSpan.Zero); // Ends 18:30 UTC
        var command = new BookAppointmentCommand(
            dealership.Id.Value,
            customer.Id.Value,
            vehicle.Id.Value,
            serviceType.Id.Value,
            pastClosingStartTime);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(SchedulingErrorKind.OutsideWorkingHours, result.Error.Kind);
        Assert.Contains("falls outside dealership working hours", result.Error.Message);
        Assert.Equal(0, _unitOfWork.CommitCount);
    }

    /// <summary>
    /// Requirement 2: Real domain certification evaluation.
    /// Technician lacks required skill -> Handled as typed SkillMismatch failure without raw exceptions.
    /// </summary>
    [Fact]
    public async Task Schedule_TechnicianMissingRequiredSkill_ReturnsSkillMismatchError()
    {
        // Arrange
        var dealership = TestDomainDataBuilder.CreateDealership();
        _dealershipRepo.Seed(dealership);

        var customer = TestDomainDataBuilder.CreateCustomer();
        _customerRepo.Seed(customer);

        var vehicle = TestDomainDataBuilder.CreateVehicle(customer.Id);
        _vehicleRepo.Seed(vehicle);

        var evServiceType = TestDomainDataBuilder.CreateHighVoltageService(); // Requires Specialist in EV Powertrain
        _serviceTypeRepo.Seed(evServiceType);

        // Real bay exists
        var bay = TestDomainDataBuilder.CreateBay(dealership.Id, capability: BayCapability.EvCharging);
        _serviceBayRepo.Seed(bay);

        // Real technician exists, but only holds Apprentice in Brakes
        var lowCert = Certification.Create("Brakes", SkillLevel.Apprentice, new DateOnly(2025, 1, 1));
        var technician = TestDomainDataBuilder.CreateTechnician(dealership.Id, "TECH-NO-EV", certifications: [lowCert]);
        _technicianRepo.Seed(technician);

        var command = new BookAppointmentCommand(
            dealership.Id.Value,
            customer.Id.Value,
            vehicle.Id.Value,
            evServiceType.Id.Value,
            FixedMonday8AmUtc.AddHours(2));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert: Evaluated by real domain code
        Assert.True(result.IsFailure);
        Assert.Equal(SchedulingErrorKind.ResourceUnavailable, result.Error.Kind);
        Assert.Equal(AvailabilityFailureCode.NoQualifiedTechnicians.ToString(), result.Error.Code);
        Assert.Contains(evServiceType.ServiceCategory, result.Error.Message);
    }
}
