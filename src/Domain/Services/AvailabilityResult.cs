using UnifiedServiceScheduler.Domain.Aggregates;

namespace UnifiedServiceScheduler.Domain.Services;

/// <summary>
/// A compatible pairing of an available technician and an available service bay
/// ready to be reserved for a specific appointment slot.
/// </summary>
public sealed record CandidateResourcePair(Technician Technician, ServiceBay ServiceBay);

public enum AvailabilityFailureCode
{
    DealershipClosed,
    OutsideWorkingHours,
    NoCompatibleBays,
    NoQualifiedTechnicians,
    AllCompatibleBaysBooked,
    AllQualifiedTechniciansBooked,
    NoConcurrentBayAndTechnicianAvailable
}

public sealed record AvailabilityFailure(AvailabilityFailureCode Code, string Message);

public sealed class AvailabilityResult
{
    private AvailabilityResult(
        bool isAvailable,
        IReadOnlyList<CandidateResourcePair> candidatePairs,
        AvailabilityFailure? failure)
    {
        IsAvailable = isAvailable;
        CandidatePairs = candidatePairs;
        Failure = failure;
    }

    public bool IsAvailable { get; }
    public IReadOnlyList<CandidateResourcePair> CandidatePairs { get; }
    public AvailabilityFailure? Failure { get; }

    public static AvailabilityResult Success(IReadOnlyList<CandidateResourcePair> candidatePairs)
    {
        ArgumentNullException.ThrowIfNull(candidatePairs);
        if (candidatePairs.Count == 0)
            throw new ArgumentException("Candidate pairs cannot be empty for a successful availability result.", nameof(candidatePairs));

        return new AvailabilityResult(true, candidatePairs, null);
    }

    public static AvailabilityResult Failed(AvailabilityFailureCode code, string message)
    {
        return new AvailabilityResult(false, Array.Empty<CandidateResourcePair>(), new AvailabilityFailure(code, message));
    }
}
