using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Services;

/// <summary>
/// Domain service responsible for determining resource availability (Technicians and ServiceBays)
/// at a specific dealership for a given ServiceType and TimeSlot.
/// </summary>
public interface IAvailabilityService
{
    Task<AvailabilityResult> CheckAvailabilityAsync(
        DealershipId      dealershipId,
        TimeSlot          targetSlot,
        ServiceType       serviceType,
        CancellationToken ct = default);
}
