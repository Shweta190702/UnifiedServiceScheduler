using System.Data;
using Microsoft.EntityFrameworkCore;
using UnifiedServiceScheduler.Application.Appointments;
using UnifiedServiceScheduler.Application.Common;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Exceptions;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;
using UnifiedServiceScheduler.Infrastructure.Repositories;

namespace UnifiedServiceScheduler.Infrastructure.Persistence;

/// <summary>
/// Infrastructure service that coordinates the atomic reservation and persistence of
/// Appointment, Technician, and ServiceBay aggregates using pessimistic row locking
/// (SELECT ... FOR UPDATE) and EF Core execution strategy to prevent race conditions.
/// </summary>
public sealed class AtomicSchedulingCoordinator
{
    private readonly UnifiedServiceSchedulerDbContext _context;
    private readonly TechnicianRepository _technicianRepository;
    private readonly ServiceBayRepository _serviceBayRepository;

    public AtomicSchedulingCoordinator(
        UnifiedServiceSchedulerDbContext context,
        TechnicianRepository technicianRepository,
        ServiceBayRepository serviceBayRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _technicianRepository = technicianRepository ?? throw new ArgumentNullException(nameof(technicianRepository));
        _serviceBayRepository = serviceBayRepository ?? throw new ArgumentNullException(nameof(serviceBayRepository));
    }

    /// <summary>
    /// Atomically locks the candidate Technician and ServiceBay rows at the DB engine level,
    /// re-evaluates conflict-freedom under exclusive lock, records the bookings, constructs the
    /// Appointment aggregate, and commits the transaction.
    ///
    /// If another customer attempts to book the same technician or bay at the exact same millisecond,
    /// their transaction will block on the row-lock, unblock only after this transaction commits,
    /// and immediately detect the newly inserted overlapping slot, failing cleanly without double-booking.
    /// </summary>
    public async Task<Result<Appointment, SchedulingError>> ExecuteAtomicBookingAsync(
        DealershipId    dealershipId,
        CustomerId      customerId,
        VehicleSnapshot vehicleSnapshot,
        TechnicianId    candidateTechId,
        ServiceBayId    candidateBayId,
        ServiceType     serviceType,
        TimeSlot        targetSlot,
        string?         notes,
        CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Begin explicit transaction with ReadCommitted (row-level locks take care of isolation)
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, ct);

            try
            {
                // 1. Acquire exclusive pessimistic row locks on both the Technician and ServiceBay records
                var lockedTechnician = await _technicianRepository.GetByIdWithLockAsync(candidateTechId, ct);
                if (lockedTechnician is null)
                {
                    await transaction.RollbackAsync(ct);
                    return SchedulingError.NotFound("Technician", candidateTechId.ToString());
                }

                var lockedBay = await _serviceBayRepository.GetByIdWithLockAsync(candidateBayId, ct);
                if (lockedBay is null)
                {
                    await transaction.RollbackAsync(ct);
                    return SchedulingError.NotFound("ServiceBay", candidateBayId.ToString());
                }

                // 2. Critical section: Re-verify invariants under exclusive row locks
                // (Guarantees no race condition occurred between candidate search and lock acquisition)
                if (lockedTechnician.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
                {
                    await transaction.RollbackAsync(ct);
                    return SchedulingError.Conflict(
                        $"Technician '{lockedTechnician.Contact.FullName}' was just booked by another customer for {targetSlot}.");
                }

                if (lockedBay.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
                {
                    await transaction.RollbackAsync(ct);
                    return SchedulingError.Conflict(
                        $"ServiceBay '{lockedBay.BayCode}' was just booked by another customer for {targetSlot}.");
                }

                var appointmentId = AppointmentId.New();

                // 3. Domain reservation (produces mandatory evidence tokens)
                var techBooking = lockedTechnician.Reserve(appointmentId, serviceType, targetSlot);
                var bayBooking  = lockedBay.Reserve(appointmentId, serviceType, targetSlot);

                // 4. Instantiate Appointment aggregate root with evidence
                var appointment = Appointment.Schedule(
                    appointmentId,
                    dealershipId,
                    customerId,
                    vehicleSnapshot,
                    techBooking,
                    bayBooking,
                    serviceType.Id,
                    targetSlot,
                    notes);

                // 5. Stage entities in EF Core change tracker
                await _context.Appointments.AddAsync(appointment, ct);
                _context.Technicians.Update(lockedTechnician);
                _context.ServiceBays.Update(lockedBay);

                // 6. Persist & commit atomically
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return Result<Appointment, SchedulingError>.Success(appointment);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(ct);
                _context.ChangeTracker.Clear();
                return SchedulingError.Conflict(
                    $"Concurrency token mismatch detected while reserving resources: {ex.Message}");
            }
            catch (SchedulingConflictException ex)
            {
                await transaction.RollbackAsync(ct);
                _context.ChangeTracker.Clear();
                return SchedulingError.Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _context.ChangeTracker.Clear();
                return SchedulingError.Conflict(
                    $"Unexpected error during atomic booking transaction: {ex.Message}");
            }
        });
    }
}
