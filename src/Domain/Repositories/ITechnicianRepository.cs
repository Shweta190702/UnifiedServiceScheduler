using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="Technician"/> aggregate root.
/// </summary>
public interface ITechnicianRepository
{
    /// <summary>Returns null when no technician with that ID exists.</summary>
    Task<Technician?> GetByIdAsync(TechnicianId id, CancellationToken ct = default);

    /// <summary>
    /// Returns active technicians at <paramref name="dealershipId"/> whose certifications
    /// satisfy <paramref name="minimumSkillLevel"/> in <paramref name="serviceCategory"/>
    /// as of <paramref name="certificationAsOf"/>, AND who have NO booking that overlaps
    /// <paramref name="requestedSlot"/>.
    ///
    /// <para>
    /// The implementation (infrastructure layer) performs this as a single efficient
    /// query rather than loading all technicians and filtering in memory.
    /// The returned aggregates are fully hydrated with their <c>Bookings</c>
    /// so that <see cref="Technician.Reserve"/> can re-verify as the final guard.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Technician>> FindAvailableAsync(
        DealershipId      dealershipId,
        string            serviceCategory,
        SkillLevel        minimumSkillLevel,
        DateOnly          certificationAsOf,
        TimeSlot          requestedSlot,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if any active technician at the dealership possesses the required qualification, regardless of schedule.
    /// </summary>
    Task<bool> HasQualifiedTechniciansAsync(
        DealershipId      dealershipId,
        string            serviceCategory,
        SkillLevel        minimumSkillLevel,
        DateOnly          certificationAsOf,
        CancellationToken ct = default);

    Task AddAsync(Technician technician, CancellationToken ct = default);
    Task UpdateAsync(Technician technician, CancellationToken ct = default);
}
