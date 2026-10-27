using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Exceptions;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Services;

/// <summary>
/// Domain service implementation for resource availability checks.
/// Thread-safe and free from any ORM/infrastructure dependencies.
/// </summary>
public sealed class AvailabilityService : IAvailabilityService
{
    private readonly IDealershipRepository _dealershipRepository;
    private readonly IServiceBayRepository _serviceBayRepository;
    private readonly ITechnicianRepository _technicianRepository;

    public AvailabilityService(
        IDealershipRepository dealershipRepository,
        IServiceBayRepository serviceBayRepository,
        ITechnicianRepository technicianRepository)
    {
        _dealershipRepository = dealershipRepository ?? throw new ArgumentNullException(nameof(dealershipRepository));
        _serviceBayRepository = serviceBayRepository ?? throw new ArgumentNullException(nameof(serviceBayRepository));
        _technicianRepository = technicianRepository ?? throw new ArgumentNullException(nameof(technicianRepository));
    }

    public async Task<AvailabilityResult> CheckAvailabilityAsync(
        DealershipId      dealershipId,
        TimeSlot          targetSlot,
        ServiceType       serviceType,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentNullException.ThrowIfNull(targetSlot);
        ArgumentNullException.ThrowIfNull(serviceType);

        // 1. Verify Dealership existence & operating hours
        var dealership = await _dealershipRepository.GetByIdAsync(dealershipId, ct);
        if (dealership is null)
        {
            return AvailabilityResult.Failed(
                AvailabilityFailureCode.DealershipClosed,
                $"Dealership {dealershipId} could not be found.");
        }

        try
        {
            dealership.AssertWithinWorkingHours(targetSlot);
        }
        catch (OutsideWorkingHoursException ex)
        {
            return AvailabilityResult.Failed(
                AvailabilityFailureCode.OutsideWorkingHours,
                ex.Message);
        }

        // 2. Query candidates from repositories sequentially (EF DbContext is single-threaded)
        var asOf = DateOnly.FromDateTime(targetSlot.Start.UtcDateTime);

        var availableBays = await _serviceBayRepository.FindAvailableAsync(
            dealershipId,
            serviceType.RequiredBayCapability,
            targetSlot,
            ct);

        var availableTechs = await _technicianRepository.FindAvailableAsync(
            dealershipId,
            serviceType.ServiceCategory,
            serviceType.RequiredSkillLevel,
            asOf,
            targetSlot,
            ct);

        // 3. Domain invariant filtering (defense in depth)
        var qualifiedBays = availableBays
            .Where(b => !b.IsUnderMaintenance &&
                        b.Supports(serviceType) &&
                        !b.Bookings.Any(booking => booking.Slot.OverlapsWith(targetSlot)))
            .ToList();

        var qualifiedTechs = availableTechs
            .Where(t => t.IsActive &&
                        t.CanPerform(serviceType, asOf) &&
                        !t.Bookings.Any(booking => booking.Slot.OverlapsWith(targetSlot)))
            .ToList();

        // 4. Evaluate availability
        if (qualifiedBays.Count == 0 && qualifiedTechs.Count == 0)
        {
            return AvailabilityResult.Failed(
                AvailabilityFailureCode.NoConcurrentBayAndTechnicianAvailable,
                $"No service bays with capability '{serviceType.RequiredBayCapability}' and no technicians qualified for '{serviceType.ServiceCategory}' ({serviceType.RequiredSkillLevel}) are available during {targetSlot}.");
        }

        if (qualifiedBays.Count == 0)
        {
            var hasCompatibleBay = await _serviceBayRepository.HasCompatibleBayAsync(
                dealershipId, serviceType.RequiredBayCapability, ct);

            if (!hasCompatibleBay)
            {
                return AvailabilityResult.Failed(
                    AvailabilityFailureCode.NoCompatibleBays,
                    $"No service bays at dealership '{dealership.Name}' possess the required capability '{serviceType.RequiredBayCapability}' for '{serviceType.Name}'.");
            }

            return AvailabilityResult.Failed(
                AvailabilityFailureCode.AllCompatibleBaysBooked,
                $"No service bays with capability '{serviceType.RequiredBayCapability}' are available during {targetSlot}.");
        }

        if (qualifiedTechs.Count == 0)
        {
            var hasQualifiedTech = await _technicianRepository.HasQualifiedTechniciansAsync(
                dealershipId, serviceType.ServiceCategory, serviceType.RequiredSkillLevel, asOf, ct);

            if (!hasQualifiedTech)
            {
                return AvailabilityResult.Failed(
                    AvailabilityFailureCode.NoQualifiedTechnicians,
                    $"No technicians at dealership '{dealership.Name}' possess the required skill level '{serviceType.RequiredSkillLevel}' in category '{serviceType.ServiceCategory}' for '{serviceType.Name}'.");
            }

            return AvailabilityResult.Failed(
                AvailabilityFailureCode.AllQualifiedTechniciansBooked,
                $"No technicians certified for '{serviceType.ServiceCategory}' at or above level {serviceType.RequiredSkillLevel} are available during {targetSlot}.");
        }

        // 5. Construct candidate pairings (e.g. prioritized by pairing order)
        var candidatePairs = new List<CandidateResourcePair>();
        foreach (var bay in qualifiedBays)
        {
            foreach (var tech in qualifiedTechs)
            {
                candidatePairs.Add(new CandidateResourcePair(tech, bay));
            }
        }

        return AvailabilityResult.Success(candidatePairs);
    }
}
