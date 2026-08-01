namespace ProjectAegis.Delegation.Core;

public enum OrderKind
{
    Move,
    Hold,
    Engage,
    SetEwPosture,
    ReturnToBase,
    // CMD-31 append-only (do not reorder above)
    SetEmcon,
    SetSensors,
    // LOG-08 / CMD-24 Phase N append-only (do not reorder above)
    LaunchAircraft,
    AbortLaunchAircraft,
}

public enum RiskLevel
{
    Low,
    High,
}

public sealed record Order(
    OrderId Id,
    TargetId Target,
    double SimTime,
    OrderKind Kind,
    RiskLevel Risk);
