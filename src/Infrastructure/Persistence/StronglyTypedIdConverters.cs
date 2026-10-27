using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Infrastructure.Persistence;

/// <summary>
/// EF Core Value Converters for Domain Strongly-Typed IDs.
/// Seamlessly maps domain ID records to database primitive GUIDs.
/// </summary>
public static class StronglyTypedIdConverters
{
    public static ValueConverter<AppointmentId, Guid> AppointmentIdConverter =>
        new(id => id.Value, value => new AppointmentId(value));

    public static ValueConverter<TechnicianId, Guid> TechnicianIdConverter =>
        new(id => id.Value, value => new TechnicianId(value));

    public static ValueConverter<ServiceBayId, Guid> ServiceBayIdConverter =>
        new(id => id.Value, value => new ServiceBayId(value));

    public static ValueConverter<CustomerId, Guid> CustomerIdConverter =>
        new(id => id.Value, value => new CustomerId(value));

    public static ValueConverter<VehicleId, Guid> VehicleIdConverter =>
        new(id => id.Value, value => new VehicleId(value));

    public static ValueConverter<DealershipId, Guid> DealershipIdConverter =>
        new(id => id.Value, value => new DealershipId(value));

    public static ValueConverter<ServiceTypeId, Guid> ServiceTypeIdConverter =>
        new(id => id.Value, value => new ServiceTypeId(value));
}
