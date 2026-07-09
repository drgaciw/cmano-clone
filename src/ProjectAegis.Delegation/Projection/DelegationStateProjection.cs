namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;

/// <summary>
/// Req 20 P0 / ADR-019: per-unit delegation overlay state (read projection only).
/// Built by T4 from controller ownership; UI never mutates these fields directly.
/// </summary>
public sealed record DelegationStateProjection(
    string UnitId,
    DelegationOwnerKind Owner,
    AutonomyLevel AutonomyLevel,
    string PersonalityId,
    bool Paused);

/// <summary>Who currently owns tactical decisions for a unit.</summary>
public enum DelegationOwnerKind
{
    Human = 0,
    Agent = 1,
    Mixed = 2,
}

/// <summary>ADR-019 command: pause agent decisions for a unit (logged intent).</summary>
public sealed record AgentPauseRequested(string UnitId, double SimTime);

/// <summary>ADR-019 command: resume agent decisions for a unit (logged intent).</summary>
public sealed record AgentResumeRequested(string UnitId, double SimTime);

/// <summary>ADR-019 command: change autonomy level for a unit (logged intent).</summary>
public sealed record AutonomyLevelChangeRequested(
    string UnitId,
    AutonomyLevel AutonomyLevel,
    double SimTime);
