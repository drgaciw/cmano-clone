namespace ProjectAegis.Delegation.Core;

public enum OrderKind
{
    Move,
    Hold,
    Engage,
    SetEwPosture,
    ReturnToBase,
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
