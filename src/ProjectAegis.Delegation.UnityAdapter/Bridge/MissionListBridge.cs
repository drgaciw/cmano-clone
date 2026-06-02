namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

public static class MissionListBridge
{
    public static IReadOnlyList<MissionListEntry> ProjectFrom(ScenarioMissionTimeline? timeline) =>
        MissionListProjection.Project(timeline);
}