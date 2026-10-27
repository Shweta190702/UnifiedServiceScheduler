using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Entities;

/// <summary>
/// Vehicle entity — the source of truth for what is being serviced.
/// Appointments capture a <see cref="VehicleSnapshot"/> at booking time so
/// that historical records are never affected by subsequent Vehicle mutations.
/// </summary>
public sealed class Vehicle
{
    // Private constructor required by EF Core for materialization.
    private Vehicle()
    {
        Id           = null!;
        OwnerId      = null!;
        Make         = string.Empty;
        Model        = string.Empty;
        Vin          = string.Empty;
        LicensePlate = string.Empty;
    }

    private Vehicle(
        VehicleId  id,
        CustomerId ownerId,
        string     make,
        string     model,
        int        year,
        string     vin,
        string     licensePlate)
    {
        Id           = id;
        OwnerId      = ownerId;
        Make         = make;
        Model        = model;
        Year         = year;
        Vin          = vin;
        LicensePlate = licensePlate;
    }

    // All identity / immutable fields use { get; } — no setters.
    public VehicleId  Id      { get; }
    public CustomerId OwnerId { get; }          // cross-aggregate reference by ID only

    // Correctable fields — typo in plate or make requires an explicit domain action.
    public string Make         { get; private set; }
    public string Model        { get; private set; }
    public int    Year         { get; private set; }
    public string Vin          { get; private set; }
    public string LicensePlate { get; private set; }

    public static Vehicle Register(
        VehicleId  id,
        CustomerId ownerId,
        string     make,
        string     model,
        int        year,
        string     vin,
        string     licensePlate)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(make);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(vin);
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);

        if (vin.Length != 17)
            throw new ArgumentException("VIN must be exactly 17 characters.", nameof(vin));

        if (year < 1886 || year > DateTimeOffset.UtcNow.Year + 2)
            throw new ArgumentOutOfRangeException(nameof(year), "Vehicle year is implausible.");

        return new Vehicle(
            id, ownerId,
            make.Trim(), model.Trim(), year,
            vin.ToUpperInvariant().Trim(),
            licensePlate.ToUpperInvariant().Trim());
    }

    /// <summary>Produces an immutable snapshot for embedding inside an Appointment.</summary>
    public VehicleSnapshot TakeSnapshot()
        => VehicleSnapshot.From(Id, Make, Model, Year, Vin, LicensePlate);

    public override string ToString() => $"Vehicle({Id}) {Year} {Make} {Model}";
}
