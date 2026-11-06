namespace ProjectAegis.Delegation.PlatformDegrade;

/// <summary>
/// Closed severity band for own-unit platform degrade (deterministic from injected facts).
/// None = healthy; Light / Heavy map to injected per-system severity.
/// </summary>
public enum PlatformDegradeSeverityBand
{
    /// <summary>No degrade or healthy subsystem.</summary>
    None = 0,

    /// <summary>Subsystem impaired but partially operational.</summary>
    Light = 1,

    /// <summary>Subsystem offline or critically impaired.</summary>
    Heavy = 2,
}
