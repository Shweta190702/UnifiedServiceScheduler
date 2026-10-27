using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UnifiedServiceScheduler.Api.Mapping;
using UnifiedServiceScheduler.Api.Requests;
using UnifiedServiceScheduler.Api.Validation;
using UnifiedServiceScheduler.Application.Appointments;
using UnifiedServiceScheduler.Application.Common;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var dealershipGroup = app.MapGroup("/api/v1/dealerships/{dealershipId:guid}/appointments")
            .WithTags("Dealership Appointments");

        // 1. POST /api/v1/dealerships/{dealershipId}/appointments
        dealershipGroup.MapPost("/", HandleBookAppointmentAsync)
            .AddEndpointFilter<ValidationFilter<BookAppointmentRequest>>()
            .WithName("BookAppointment")
            .WithSummary("Books a new automotive service appointment")
            .WithDescription("Coordinates availability across Customer, Vehicle, Technician, and ServiceBay aggregates with UTC normalization and race prevention.")
            .Produces<AppointmentResponseDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 2. GET /api/v1/appointments/{appointmentId:guid} (Primary canonical REST resource location)
        app.MapGet("/api/v1/appointments/{appointmentId:guid}", HandleGetAppointmentByCanonicalIdAsync)
            .WithTags("Appointments")
            .WithName("GetAppointmentById")
            .WithSummary("Retrieves an appointment by its canonical ID")
            .Produces<AppointmentResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 3. GET /api/v1/dealerships/{dealershipId}/appointments/{appointmentId} (Scoped subresource location)
        dealershipGroup.MapGet("/{appointmentId:guid}", HandleGetAppointmentByIdScopedAsync)
            .WithName("GetAppointmentByIdScoped")
            .WithSummary("Retrieves an appointment scoped by dealership ID")
            .Produces<AppointmentResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    public static async Task<IResult> HandleBookAppointmentAsync(
        [FromRoute] Guid dealershipId,
        [FromBody] BookAppointmentRequest request,
        [FromServices] ICommandHandler<BookAppointmentCommand, Result<AppointmentResponseDto, SchedulingError>> handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        // 1. Guard route parameter
        if (dealershipId == Guid.Empty)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Dealership ID",
                Detail = "Dealership ID in route must be a valid non-empty GUID.",
                Instance = httpContext.Request.Path.Value
            });
        }

        // 2. TimeZone Normalization: explicitly convert incoming DateTimeOffset to UTC before domain ingestion
        var normalizedUtcStartTime = request.ScheduledStartTime.ToUniversalTime();

        // 3. Construct application CQRS command with normalized parameters
        var command = new BookAppointmentCommand(
            DealershipId: dealershipId,
            CustomerId: request.CustomerId,
            VehicleId: request.VehicleId,
            ServiceTypeId: request.ServiceTypeId,
            DesiredStartTime: normalizedUtcStartTime,
            Notes: request.Notes);

        // 4. Dispatch to Command Handler
        var result = await handler.HandleAsync(command, ct);

        // 5. RESTful Success Response: 201 Created with canonical Location header
        if (result.IsSuccess)
        {
            var dto = result.Value;
            var locationUri = $"/api/v1/appointments/{dto.AppointmentId}";

            return Results.Created(locationUri, dto);
        }

        // 6. Precise Error Mapping: 400 Bad Request, 404 Not Found, 409 Conflict, 422 Unprocessable Entity
        return result.Error.ToHttpResult(httpContext);
    }

    public static async Task<IResult> HandleGetAppointmentByCanonicalIdAsync(
        [FromRoute] Guid appointmentId,
        [FromServices] IAppointmentRepository appointmentRepository,
        [FromServices] ITechnicianRepository technicianRepository,
        [FromServices] IServiceBayRepository serviceBayRepository,
        [FromServices] IServiceTypeRepository serviceTypeRepository,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var appt = await appointmentRepository.GetByIdAsync(new AppointmentId(appointmentId), ct);
        if (appt is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Appointment Not Found",
                Detail = $"Appointment '{appointmentId}' was not found.",
                Instance = httpContext.Request.Path.Value
            });
        }

        var tech = await technicianRepository.GetByIdAsync(appt.TechnicianId, ct);
        var bay = await serviceBayRepository.GetByIdAsync(appt.ServiceBayId, ct);
        var serviceType = await serviceTypeRepository.GetByIdAsync(appt.ServiceTypeId, ct);

        var dto = new AppointmentResponseDto(
            appt.Id.Value,
            appt.DealershipId.Value,
            appt.CustomerId.Value,
            appt.VehicleSnapshot.Id.Value,
            appt.VehicleSnapshot.Vin,
            $"{appt.VehicleSnapshot.Year} {appt.VehicleSnapshot.Make} {appt.VehicleSnapshot.Model}",
            appt.TechnicianId.Value,
            tech?.Contact.FullName ?? "Unassigned",
            appt.ServiceBayId.Value,
            bay?.BayCode ?? "Unassigned",
            appt.ServiceTypeId.Value,
            serviceType?.Name ?? "General Service",
            appt.Slot.Start,
            appt.Slot.End,
            appt.Slot.Duration,
            appt.Status.ToString(),
            appt.Notes);

        return Results.Ok(dto);
    }

    public static async Task<IResult> HandleGetAppointmentByIdScopedAsync(
        [FromRoute] Guid dealershipId,
        [FromRoute] Guid appointmentId,
        [FromServices] IAppointmentRepository appointmentRepository,
        [FromServices] ITechnicianRepository technicianRepository,
        [FromServices] IServiceBayRepository serviceBayRepository,
        [FromServices] IServiceTypeRepository serviceTypeRepository,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var appt = await appointmentRepository.GetByIdAsync(new AppointmentId(appointmentId), ct);
        if (appt is null || appt.DealershipId.Value != dealershipId)
        {
            return Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Appointment Not Found",
                Detail = $"Appointment '{appointmentId}' was not found at dealership '{dealershipId}'.",
                Instance = httpContext.Request.Path.Value
            });
        }

        var tech = await technicianRepository.GetByIdAsync(appt.TechnicianId, ct);
        var bay = await serviceBayRepository.GetByIdAsync(appt.ServiceBayId, ct);
        var serviceType = await serviceTypeRepository.GetByIdAsync(appt.ServiceTypeId, ct);

        var dto = new AppointmentResponseDto(
            appt.Id.Value,
            appt.DealershipId.Value,
            appt.CustomerId.Value,
            appt.VehicleSnapshot.Id.Value,
            appt.VehicleSnapshot.Vin,
            $"{appt.VehicleSnapshot.Year} {appt.VehicleSnapshot.Make} {appt.VehicleSnapshot.Model}",
            appt.TechnicianId.Value,
            tech?.Contact.FullName ?? "Unassigned",
            appt.ServiceBayId.Value,
            bay?.BayCode ?? "Unassigned",
            appt.ServiceTypeId.Value,
            serviceType?.Name ?? "General Service",
            appt.Slot.Start,
            appt.Slot.End,
            appt.Slot.Duration,
            appt.Status.ToString(),
            appt.Notes);

        return Results.Ok(dto);
    }
}
