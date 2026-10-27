namespace UnifiedServiceScheduler.Domain.ValueObjects;

// ─────────────────────────────────────────────────────────────────────────────
// Strongly-typed IDs  (avoids primitive obsession; records give free equality)
// ─────────────────────────────────────────────────────────────────────────────

public sealed record AppointmentId(Guid Value)
{
    public static AppointmentId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed record TechnicianId(Guid Value)
{
    public static TechnicianId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed record ServiceBayId(Guid Value)
{
    public static ServiceBayId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed record CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed record VehicleId(Guid Value)
{
    public static VehicleId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed record DealershipId(Guid Value)
{
    public static DealershipId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed record ServiceTypeId(Guid Value)
{
    public static ServiceTypeId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
