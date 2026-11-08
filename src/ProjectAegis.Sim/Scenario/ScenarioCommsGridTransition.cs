namespace ProjectAegis.Sim.Scenario;

/// <summary>
/// C3-01 / DRG-390: authored per-unit comms-grid membership change.
/// <paramref name="Membership"/> is "OnGrid" or "OffGrid" (case-insensitive).
/// </summary>
public sealed record ScenarioCommsGridTransition(
    ulong AtTick,
    string UnitId,
    string Membership,
    string Reason = "");
