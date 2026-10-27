using UnifiedServiceScheduler.Domain.Services;

namespace UnifiedServiceScheduler.Application.Common;

public enum SchedulingErrorKind
{
    Validation,
    NotFound,
    DealershipClosed,
    OutsideWorkingHours,
    ResourceUnavailable,
    Conflict,
    InvariantViolation
}

public sealed record SchedulingError(
    SchedulingErrorKind Kind,
    string Code,
    string Message,
    AvailabilityFailure? AvailabilityDetails = null)
{
    public static SchedulingError Validation(string message) =>
        new(SchedulingErrorKind.Validation, "VALIDATION_FAILED", message);

    public static SchedulingError NotFound(string entityName, string id) =>
        new(SchedulingErrorKind.NotFound, $"{entityName.ToUpperInvariant()}_NOT_FOUND", $"{entityName} with ID '{id}' was not found.");

    public static SchedulingError OutsideWorkingHours(string message, string code = "ExceedsOperatingHours") =>
        new(SchedulingErrorKind.OutsideWorkingHours, code, message);

    public static SchedulingError ResourceUnavailable(AvailabilityFailure failure) =>
        new(SchedulingErrorKind.ResourceUnavailable, failure.Code.ToString(), failure.Message, failure);

    public static SchedulingError Conflict(string message) =>
        new(SchedulingErrorKind.Conflict, "SCHEDULING_CONFLICT", message);

    public static SchedulingError InvariantViolation(string message) =>
        new(SchedulingErrorKind.InvariantViolation, "INVARIANT_VIOLATION", message);
}
