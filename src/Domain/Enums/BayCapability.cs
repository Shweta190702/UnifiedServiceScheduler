namespace UnifiedServiceScheduler.Domain.Enums;

/// <summary>
/// Physical capability classification of a service bay.
/// </summary>
public enum BayCapability
{
    Standard,       // General maintenance — oil, tyres, brakes
    Lift,           // Vehicle lift required for undercarriage work
    Alignment,      // 4-wheel alignment rack
    Diagnostic,     // Dedicated ECU / scan-tool station
    EvCharging,     // EV-specific: HV battery, charging systems
    BodyShop,       // Collision repair / paint
}
