namespace UnifiedServiceScheduler.Domain.Enums;

/// <summary>
/// Represents the required certification tier for a service type.
/// Ordinal ordering enforces "higher is stricter" comparisons.
/// </summary>
public enum SkillLevel
{
    Apprentice = 1,
    Journeyman = 2,
    Senior      = 3,
    Master      = 4,
    Specialist  = 5,   // e.g., EV high-voltage, transmission overhaul
}
