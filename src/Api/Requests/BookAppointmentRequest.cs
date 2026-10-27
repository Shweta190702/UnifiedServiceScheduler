namespace UnifiedServiceScheduler.Api.Requests;

/// <summary>
/// HTTP request contract for booking a service appointment.
/// The DealershipId is supplied via route parameter.
/// </summary>
public sealed record BookAppointmentRequest(
    Guid           CustomerId,
    Guid           VehicleId,
    Guid           ServiceTypeId,
    DateTimeOffset ScheduledStartTime,
    string?        Notes = null
);
