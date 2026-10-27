using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Entities;

/// <summary>
/// ServiceType entity — defines what work will be performed, how long it takes,
/// and what skill + bay it requires. Owned by the Catalog bounded context;
/// the Scheduling context holds a local copy of the fields needed to enforce
/// scheduling invariants without a cross-context call at booking time.
/// </summary>
public sealed class ServiceType
{
    // Private constructor required by EF Core for materialization.
    private ServiceType()
    {
        Id              = null!;
        Name            = string.Empty;
        ServiceCategory = string.Empty;
        Description     = string.Empty;
    }

    private ServiceType(
        ServiceTypeId id,
        string        name,
        string        serviceCategory,
        TimeSpan      baseDuration,
        SkillLevel    requiredSkillLevel,
        BayCapability requiredBayCapability,
        string        description)
    {
        Id                    = id;
        Name                  = name;
        ServiceCategory       = serviceCategory;
        BaseDuration          = baseDuration;
        RequiredSkillLevel    = requiredSkillLevel;
        RequiredBayCapability = requiredBayCapability;
        Description           = description;
    }

    // Fully init-only: ServiceType definitions are immutable once published.
    public ServiceTypeId  Id                    { get; }
    public string         Name                  { get; }

    /// <summary>
    /// The service category string matched case-insensitively against
    /// <see cref="Certification.ServiceCategory"/> when assigning a technician.
    /// </summary>
    public string         ServiceCategory       { get; }

    /// <summary>Nominal duration before any technician-specific buffers.</summary>
    public TimeSpan       BaseDuration          { get; }

    /// <summary>Minimum certification level a technician must hold.</summary>
    public SkillLevel     RequiredSkillLevel    { get; }

    /// <summary>The physical bay capability required to perform this service.</summary>
    public BayCapability  RequiredBayCapability { get; }

    public string         Description           { get; }

    public static ServiceType Define(
        ServiceTypeId id,
        string        name,
        string        serviceCategory,
        TimeSpan      baseDuration,
        SkillLevel    requiredSkillLevel,
        BayCapability requiredBayCapability,
        string        description = "")
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceCategory);

        if (baseDuration <= TimeSpan.Zero)
            throw new ArgumentException("Base duration must be positive.", nameof(baseDuration));

        return new ServiceType(
            id, name.Trim(), serviceCategory.Trim(),
            baseDuration, requiredSkillLevel, requiredBayCapability,
            description.Trim());
    }

    public override string ToString()
        => $"{Name} ({BaseDuration.TotalMinutes:0}min, {RequiredSkillLevel}+)";
}
