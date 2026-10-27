using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="ServiceBay"/> aggregate root.
/// </summary>
public interface IServiceBayRepository
{
    /// <summary>Returns null when no bay with that ID exists.</summary>
    Task<ServiceBay?> GetByIdAsync(ServiceBayId id, CancellationToken ct = default);

    /// <summary>
    /// Returns active (not under maintenance) bays at <paramref name="dealershipId"/> whose
    /// <see cref="ServiceBay.Capability"/> equals <paramref name="requiredCapability"/>
    /// AND that have NO booking overlapping <paramref name="requestedSlot"/>.
    ///
    /// <para>
    /// The returned aggregates are fully hydrated with their <c>Bookings</c>
    /// so that <see cref="ServiceBay.Reserve"/> can re-verify as the final guard.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<ServiceBay>> FindAvailableAsync(
        DealershipId      dealershipId,
        BayCapability     requiredCapability,
        TimeSlot          requestedSlot,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if any bay at the dealership possesses the required capability, regardless of schedule.
    /// </summary>
    Task<bool> HasCompatibleBayAsync(
        DealershipId      dealershipId,
        BayCapability     requiredCapability,
        CancellationToken ct = default);

    Task AddAsync(ServiceBay bay, CancellationToken ct = default);
    Task UpdateAsync(ServiceBay bay, CancellationToken ct = default);
}
