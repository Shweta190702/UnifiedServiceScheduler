using UnifiedServiceScheduler.Application.Common;

namespace UnifiedServiceScheduler.Application.Appointments;

/// <summary>
/// CQRS Command to book a new automotive service appointment.
/// </summary>
public sealed record BookAppointmentCommand(
    Guid           DealershipId,
    Guid           CustomerId,
    Guid           VehicleId,
    Guid           ServiceTypeId,
    DateTimeOffset DesiredStartTime,
    string?        Notes = null
) : ICommand<Result<AppointmentResponseDto, SchedulingError>>;
