using Microsoft.AspNetCore.Mvc;
using UnifiedServiceScheduler.Application.Common;
using UnifiedServiceScheduler.Domain.Services;

namespace UnifiedServiceScheduler.Api.Mapping;

/// <summary>
/// Maps domain/application <see cref="SchedulingError"/> models into standardized RFC 7807/9110 ProblemDetails
/// with precise HTTP status codes:
/// - 400 Bad Request: Validation failures & outside working hours
/// - 404 Not Found: Entity lookup failures
/// - 409 Conflict: Resource slot taken (code: SlotUnavailable)
/// - 422 Unprocessable Entity: Unqualified technician or missing capability (code: SkillMismatch)
/// </summary>
public static class SchedulingErrorResultMapper
{
    public static IResult ToHttpResult(this SchedulingError error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);

        var path = httpContext.Request.Path.Value ?? "/api/v1/appointments";

        return error.Kind switch
        {
            SchedulingErrorKind.Validation =>
                Results.BadRequest(CreateProblemDetails(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Failure",
                    type: "https://errors.unifiedscheduler.com/validation-failed",
                    detail: error.Message,
                    errorCode: "VALIDATION_FAILED",
                    instance: path)),

            SchedulingErrorKind.OutsideWorkingHours =>
                Results.BadRequest(CreateProblemDetails(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Exceeds Operating Hours",
                    type: "https://errors.unifiedscheduler.com/exceeds-operating-hours",
                    detail: error.Message,
                    errorCode: error.Code,
                    instance: path)),

            SchedulingErrorKind.NotFound =>
                Results.NotFound(CreateProblemDetails(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Resource Not Found",
                    type: "https://errors.unifiedscheduler.com/not-found",
                    detail: error.Message,
                    errorCode: error.Code,
                    instance: path)),

            SchedulingErrorKind.ResourceUnavailable =>
                MapResourceUnavailable(error, path),

            // Concurrent race collision on resource slots -> 409 Conflict (code: SlotUnavailable)
            SchedulingErrorKind.Conflict =>
                Results.Conflict(CreateProblemDetails(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Slot Unavailable",
                    type: "https://errors.unifiedscheduler.com/slot-unavailable",
                    detail: error.Message,
                    errorCode: "SlotUnavailable",
                    instance: path)),

            // Invariant / Skill requirement mismatch -> 422 Unprocessable Entity (code: SkillMismatch)
            SchedulingErrorKind.InvariantViolation =>
                Results.UnprocessableEntity(CreateProblemDetails(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Skill Mismatch",
                    type: "https://errors.unifiedscheduler.com/skill-mismatch",
                    detail: error.Message,
                    errorCode: "SkillMismatch",
                    instance: path)),

            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Scheduling Error",
                detail: error.Message,
                instance: path)
        };
    }

    private static IResult MapResourceUnavailable(SchedulingError error, string instance)
    {
        var failure = error.AvailabilityDetails;
        var failureCode = failure?.Code ?? AvailabilityFailureCode.NoConcurrentBayAndTechnicianAvailable;

        return failureCode switch
        {
            // 409 Conflict: Desired slot is taken by existing appointments (code: BayUnavailable)
            AvailabilityFailureCode.AllCompatibleBaysBooked =>
                Results.Conflict(CreateProblemDetails(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Bay Unavailable",
                    type: "https://errors.unifiedscheduler.com/bay-unavailable",
                    detail: error.Message,
                    errorCode: "BayUnavailable",
                    instance: instance)),

            AvailabilityFailureCode.AllQualifiedTechniciansBooked =>
                Results.Conflict(CreateProblemDetails(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Technician Unavailable",
                    type: "https://errors.unifiedscheduler.com/technician-unavailable",
                    detail: error.Message,
                    errorCode: "TechnicianUnavailable",
                    instance: instance)),

            AvailabilityFailureCode.NoConcurrentBayAndTechnicianAvailable =>
                Results.Conflict(CreateProblemDetails(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Slot Unavailable",
                    type: "https://errors.unifiedscheduler.com/slot-unavailable",
                    detail: error.Message,
                    errorCode: "SlotUnavailable",
                    instance: instance)),

            // 422 Unprocessable Entity: Unqualified technician or missing bay capability (code: SkillMismatch)
            AvailabilityFailureCode.NoQualifiedTechnicians =>
                Results.UnprocessableEntity(CreateProblemDetails(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Skill Mismatch",
                    type: "https://errors.unifiedscheduler.com/skill-mismatch",
                    detail: error.Message,
                    errorCode: "SkillMismatch",
                    instance: instance)),

            AvailabilityFailureCode.NoCompatibleBays =>
                Results.UnprocessableEntity(CreateProblemDetails(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Skill Mismatch",
                    type: "https://errors.unifiedscheduler.com/skill-mismatch",
                    detail: error.Message,
                    errorCode: "SkillMismatch",
                    instance: instance)),

            _ => Results.Conflict(CreateProblemDetails(
                statusCode: StatusCodes.Status409Conflict,
                title: "Slot Unavailable",
                type: "https://errors.unifiedscheduler.com/slot-unavailable",
                detail: error.Message,
                errorCode: "SlotUnavailable",
                instance: instance))
        };
    }

    private static ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string type,
        string detail,
        string errorCode,
        string instance)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance
        };

        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        return problem;
    }
}
