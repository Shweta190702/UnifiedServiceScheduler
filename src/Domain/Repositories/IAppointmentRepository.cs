using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="Appointment"/> aggregate root.
/// Lives in the Domain layer — the infrastructure project provides the implementation.
/// </summary>
public interface IAppointmentRepository
{
    /// <summary>Returns null when no appointment with that ID exists.</summary>
    Task<Appointment?> GetByIdAsync(AppointmentId id, CancellationToken ct = default);

    /// <summary>Returns all confirmed/in-progress appointments for a dealership within a time window.</summary>
    Task<IReadOnlyList<Appointment>> FindByDealershipAndSlotAsync(
        DealershipId dealershipId,
        TimeSlot     window,
        CancellationToken ct = default);

    /// <summary>Finds an existing appointment for the same customer/vehicle and slot to guarantee idempotency.</summary>
    Task<Appointment?> FindDuplicateAsync(
        DealershipId dealershipId,
        CustomerId   customerId,
        VehicleId    vehicleId,
        TimeSlot     slot,
        CancellationToken ct = default);

    Task AddAsync(Appointment appointment, CancellationToken ct = default);

    /// <summary>Persists state changes made to an already-tracked aggregate.</summary>
    Task UpdateAsync(Appointment appointment, CancellationToken ct = default);
}
