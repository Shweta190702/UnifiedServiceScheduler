using FluentValidation;

namespace UnifiedServiceScheduler.Api.Validation;

/// <summary>
/// Minimal API Endpoint Filter that automatically runs registered FluentValidators
/// and returns an RFC 7807/9110 ValidationProblem (HTTP 400 Bad Request) on failure.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T>? _validator;

    public ValidationFilter(IValidator<T>? validator = null)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_validator is null)
            return await next(context);

        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return Results.BadRequest(new { error = $"Missing or invalid request payload of type {typeof(T).Name}." });

        var validationResult = await _validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First();
            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failure",
                Type = "https://errors.unifiedscheduler.com/validation-failed",
                Detail = firstError.ErrorMessage,
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["errorCode"] = firstError.ErrorCode ?? "VALIDATION_FAILED";
            problem.Extensions["errors"] = validationResult.ToDictionary();
            return Results.BadRequest(problem);
        }

        return await next(context);
    }
}
