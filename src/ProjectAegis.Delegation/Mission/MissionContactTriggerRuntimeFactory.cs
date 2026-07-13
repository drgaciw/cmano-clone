namespace ProjectAegis.Delegation.Mission;

using ProjectAegis.Sim.Scenario;

public static class MissionContactTriggerRuntimeFactory
{
    public static MissionContactTriggerRuntime? TryCreate(ScenarioMissionTimeline? timeline)
    {
        if (timeline?.ContactTriggers == null || timeline.ContactTriggers.Count == 0)
        {
            return null;
        }

        return new MissionContactTriggerRuntime(timeline.ContactTriggers);
    }
}
