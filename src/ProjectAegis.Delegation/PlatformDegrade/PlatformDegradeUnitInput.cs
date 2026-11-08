namespace ProjectAegis.Delegation.PlatformDegrade;

/// <summary>Read-only per-unit facts for headless platform degrade projection (injected only).</summary>
public sealed record PlatformDegradeUnitInput(
    string UnitId,
    bool MobilityDegraded = false,
    PlatformDegradeSeverityBand MobilitySeverity = PlatformDegradeSeverityBand.Light,
    bool SensorDegraded = false,
    PlatformDegradeSeverityBand SensorSeverity = PlatformDegradeSeverityBand.Light,
    bool WeaponDegraded = false,
    PlatformDegradeSeverityBand WeaponSeverity = PlatformDegradeSeverityBand.Light,
    bool CommsDegraded = false,
    PlatformDegradeSeverityBand CommsSeverity = PlatformDegradeSeverityBand.Light);
