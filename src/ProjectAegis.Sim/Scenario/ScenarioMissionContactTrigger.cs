namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Policy;

public enum MissionContactPolicySide
{
    Friendly = 0,
    Opposing = 1,
}

/// <summary>Contact-detected mission + ROE escalation authored in scenario mission JSON.</summary>
public sealed record ScenarioMissionContactTrigger(
    string TriggerId,
    string ObserverId,
    MissionContactTargetClass TargetClass,
    MissionContactPolicySide PolicySide,
    string MissionCode,
    RoeLevel Roe,
    IReadOnlyList<string> UnitIds);
