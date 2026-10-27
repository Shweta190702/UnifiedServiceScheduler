namespace UnifiedServiceScheduler.Domain.ValueObjects;

/// <summary>
/// Vehicle identity snapshot captured at booking time.
/// Using a value object here keeps the Appointment self-describing
/// even if the Vehicle entity is later modified or archived.
/// </summary>
public sealed record VehicleSnapshot
{
    public VehicleId Id           { get; }
    public string    Make         { get; }
    public string    Model        { get; }
    public int       Year         { get; }
    public string    Vin          { get; }  // Vehicle Identification Number
    public string    LicensePlate { get; }

    private VehicleSnapshot()
    {
        Id           = null!;
        Make         = string.Empty;
        Model        = string.Empty;
        Vin          = string.Empty;
        LicensePlate = string.Empty;
    }

    private VehicleSnapshot(
        VehicleId id, string make, string model,
        int year, string vin, string licensePlate)
    {
        Id           = id;
        Make         = make;
        Model        = model;
        Year         = year;
        Vin          = vin;
        LicensePlate = licensePlate;
    }

    public static VehicleSnapshot From(
        VehicleId id, string make, string model,
        int year, string vin, string licensePlate)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(make);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(vin);
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);

        if (year < 1886 || year > DateTimeOffset.UtcNow.Year + 2)
            throw new ArgumentOutOfRangeException(nameof(year), "Vehicle year is implausible.");

        if (vin.Length != 17)
            throw new ArgumentException("VIN must be exactly 17 characters.", nameof(vin));

        return new VehicleSnapshot(id, make.Trim(), model.Trim(), year,
            vin.ToUpperInvariant().Trim(), licensePlate.ToUpperInvariant().Trim());
    }

    public override string ToString() => $"{Year} {Make} {Model} (VIN: {Vin})";
}
