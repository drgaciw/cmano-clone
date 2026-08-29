namespace ProjectAegis.Delegation.PlatformDegrade;

/// <summary>Named own-unit degrade codes for damage-control posture (never silent drops).</summary>
public static class PlatformDegradeCode
{
    /// <summary>Unit has no active platform degrade.</summary>
    public const string None = "NONE";

    /// <summary>Mobility / propulsion subsystem degraded or offline.</summary>
    public const string Mobility = "MOBILITY";

    /// <summary>Sensor subsystem degraded or offline.</summary>
    public const string Sensor = "SENSOR";

    /// <summary>Weapon / magazine subsystem degraded or offline.</summary>
    public const string Weapon = "WEAPON";

    /// <summary>Comms / datalink subsystem degraded or offline.</summary>
    public const string Comms = "COMMS";
}
