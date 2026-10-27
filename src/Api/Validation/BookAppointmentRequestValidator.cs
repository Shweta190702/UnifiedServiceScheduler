using FluentValidation;
using UnifiedServiceScheduler.Api.Requests;

namespace UnifiedServiceScheduler.Api.Validation;

/// <summary>
/// FluentValidation rules for incoming BookAppointmentRequest DTO.
/// Enforces time-zone normalization, past-date rejection, and operating window constraints.
/// </summary>
public sealed class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    private static readonly TimeSpan OpeningTimeUtc = new(8, 0, 0);   // 08:00 UTC
    private static readonly TimeSpan ClosingTimeUtc = new(18, 0, 0);  // 18:00 UTC

    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId must be a valid non-empty GUID.");

        RuleFor(x => x.VehicleId)
            .NotEmpty()
            .WithMessage("VehicleId must be a valid non-empty GUID.");

        RuleFor(x => x.ServiceTypeId)
            .NotEmpty()
            .WithMessage("ServiceTypeId must be a valid non-empty GUID.");

        RuleFor(x => x.ScheduledStartTime)
            .NotEmpty()
            .WithMessage("ScheduledStartTime must be specified.")
            // 1. Explicit past-date & minimum 15-minute lead time threshold
            .Must(BeAtLeastFifteenMinutesInFutureUtc)
            .WithErrorCode("StartTimeMustBeInFuture")
            .WithMessage("ScheduledStartTime must be in the future and at least 15 minutes from now.")
            // 2. Closed days (e.g. Sunday)
            .Must(BeOnOperatingDayUtc)
            .WithErrorCode("OutsideOperatingSchedule")
            .WithMessage("Dealership is closed on Sundays and weekends.")
            // 3. Operating hours boundary in UTC
            .Must(BeWithinStandardOpeningTimeUtc)
            .WithErrorCode("OutsideOperatingSchedule")
            .WithMessage("ScheduledStartTime is before dealership opening hours (opens at 08:00 UTC).");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithMessage("Notes cannot exceed 1000 characters.");
    }

    private static bool BeAtLeastFifteenMinutesInFutureUtc(DateTimeOffset startTime)
    {
        return startTime.ToUniversalTime() >= DateTimeOffset.UtcNow.AddMinutes(15);
    }

    private static bool BeOnOperatingDayUtc(DateTimeOffset startTime)
    {
        var dayOfWeek = startTime.ToUniversalTime().DayOfWeek;
        return dayOfWeek != DayOfWeek.Sunday && dayOfWeek != DayOfWeek.Saturday;
    }

    private static bool BeWithinStandardOpeningTimeUtc(DateTimeOffset startTime)
    {
        var utcTime = startTime.ToUniversalTime().TimeOfDay;
        return utcTime >= OpeningTimeUtc;
    }
}
