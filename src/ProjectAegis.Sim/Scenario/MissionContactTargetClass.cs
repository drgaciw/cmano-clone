namespace ProjectAegis.Sim.Scenario;

public enum MissionContactTargetClass
{
    Any = 0,
    Surface = 1,
    Air = 2,
}

public static class MissionContactTargetClassifier
{
    public static MissionContactTargetClass Classify(string targetId) =>
        targetId.StartsWith("ucav", StringComparison.Ordinal)
            ? MissionContactTargetClass.Air
            : MissionContactTargetClass.Surface;

    public static bool Matches(MissionContactTargetClass required, string targetId) =>
        required switch
        {
            MissionContactTargetClass.Any => true,
            MissionContactTargetClass.Surface => Classify(targetId) == MissionContactTargetClass.Surface,
            MissionContactTargetClass.Air => Classify(targetId) == MissionContactTargetClass.Air,
            _ => false,
        };
}
