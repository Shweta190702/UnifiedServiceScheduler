using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Exceptions;

namespace UnifiedServiceScheduler.Domain.ValueObjects;

/// <summary>
/// Represents a technician's certified skill for a service category.
/// Equality is structural — two certifications are identical if
/// category and level match, regardless of when they were issued.
/// </summary>
public sealed record Certification
{
    public string     ServiceCategory { get; }  // e.g. "Transmission", "EV Powertrain"
    public SkillLevel Level           { get; }
    public DateOnly   IssuedOn        { get; }
    public DateOnly?  ExpiresOn       { get; }   // null = permanent

    private Certification()
    {
        ServiceCategory = string.Empty;
    }

    private Certification(
        string serviceCategory, SkillLevel level,
        DateOnly issuedOn, DateOnly? expiresOn)
    {
        ServiceCategory = serviceCategory;
        Level           = level;
        IssuedOn        = issuedOn;
        ExpiresOn       = expiresOn;
    }

    public static Certification Create(
        string serviceCategory, SkillLevel level,
        DateOnly issuedOn, DateOnly? expiresOn = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceCategory);

        if (expiresOn.HasValue && expiresOn.Value <= issuedOn)
            throw new InvalidTimeSlotException(
                $"Certification expiry ({expiresOn}) must be after issue date ({issuedOn})");

        return new Certification(serviceCategory.Trim(), level, issuedOn, expiresOn);
    }

    /// <summary>
    /// Is this certification valid on the given date AND does it satisfy
    /// the minimum skill level required?
    /// </summary>
    public bool SatisfiesRequirement(
        string requiredCategory, SkillLevel requiredLevel, DateOnly asOf)
    {
        if (!ServiceCategory.Equals(requiredCategory, StringComparison.OrdinalIgnoreCase))
            return false;

        if (Level < requiredLevel)
            return false;

        if (ExpiresOn.HasValue && asOf > ExpiresOn.Value)
            return false;   // Expired

        return true;
    }

    public override string ToString()
        => $"{ServiceCategory} / {Level}" +
           (ExpiresOn.HasValue ? $" (expires {ExpiresOn})" : " (permanent)");
}
