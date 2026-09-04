namespace ProjectAegis.Delegation.C2Network;

using ProjectAegis.Delegation.Comms;

/// <summary>
/// Presentation-only TopBar text for <see cref="C2NetworkHealthLevel"/> (S122-06 / DRG-190).
/// Stable <c>NET:</c> prefix; no hex color encoding.
/// </summary>
public static class C2NetworkHealthHudFormat
{
    /// <summary>Stable HUD prefix for network-health text.</summary>
    public const string Prefix = "NET:";

    /// <summary>Empty / unknown network health (graceful fail).</summary>
    public const string Unavailable = "NET: —";

    /// <summary>Format a known aggregate health level for the TopBar.</summary>
    public static string Format(C2NetworkHealthLevel level) =>
        level switch
        {
            C2NetworkHealthLevel.Healthy => "NET: HEALTHY",
            C2NetworkHealthLevel.Degraded => "NET: DEGRADED",
            C2NetworkHealthLevel.Partitioned => "NET: PARTITIONED",
            _ => Unavailable,
        };

    /// <summary>Null level → <see cref="Unavailable"/> (graceful fail).</summary>
    public static string Format(C2NetworkHealthLevel? level) =>
        level is { } value ? Format(value) : Unavailable;

    /// <summary>Null snapshot → <see cref="Unavailable"/>; otherwise format <see cref="C2NetworkHealthSnapshot.NetworkHealth"/>.</summary>
    public static string Format(C2NetworkHealthSnapshot? snapshot) =>
        snapshot is null ? Unavailable : Format(snapshot.NetworkHealth);

    /// <summary>
    /// Map last projected comms quality onto network-health HUD text when a full mesh
    /// snapshot is not available on the host (Denied→PARTITIONED, Degraded→DEGRADED, Nominal→HEALTHY).
    /// </summary>
    public static string FromCommsState(CommsState? state) => Format(MapFromComms(state));

    /// <summary>Parse already-applied TopBar comms text when LastCommsState is not yet set.</summary>
    public static string FromCommsLabel(string? commsLabel)
    {
        if (string.IsNullOrEmpty(commsLabel))
        {
            return Unavailable;
        }

        if (commsLabel.Contains("DENIED", StringComparison.OrdinalIgnoreCase))
        {
            return Format(C2NetworkHealthLevel.Partitioned);
        }

        if (commsLabel.Contains("DEGRADED", StringComparison.OrdinalIgnoreCase))
        {
            return Format(C2NetworkHealthLevel.Degraded);
        }

        if (commsLabel.Contains("NOMINAL", StringComparison.OrdinalIgnoreCase))
        {
            return Format(C2NetworkHealthLevel.Healthy);
        }

        return Unavailable;
    }

    /// <summary>Comms fallback mapping used by TopBar hosts that cannot run the mesh projector.</summary>
    public static C2NetworkHealthLevel? MapFromComms(CommsState? state) =>
        state switch
        {
            null => null,
            CommsState.Denied => C2NetworkHealthLevel.Partitioned,
            CommsState.Degraded => C2NetworkHealthLevel.Degraded,
            CommsState.Nominal => C2NetworkHealthLevel.Healthy,
            _ => null,
        };
}
