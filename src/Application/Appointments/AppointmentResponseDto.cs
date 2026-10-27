namespace UnifiedServiceScheduler.Application.Appointments;

public sealed record AppointmentResponseDto(
    Guid           AppointmentId,
    Guid           DealershipId,
    Guid           CustomerId,
    Guid           VehicleId,
    string         VehicleVin,
    string         VehicleDescription,
    Guid           TechnicianId,
    string         TechnicianName,
    Guid           ServiceBayId,
    string         BayCode,
    Guid           ServiceTypeId,
    string         ServiceTypeName,
    DateTimeOffset ScheduledStartTime,
    DateTimeOffset ScheduledEndTime,
    TimeSpan       Duration,
    string         Status,
    string?        Notes
);
