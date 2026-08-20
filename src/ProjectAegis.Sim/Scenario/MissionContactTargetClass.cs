namespace ProjectAegis.Sim.Scenario;

public enum MissionContactTargetClass
{
    Any = 0,
    Surface = 1,
    Air = 2,
    Subsurface = 3,
}

public static class MissionContactTargetClassifier
{
    // Catalog platform ids (gauntlet/baltic) encode class in the slug; ucav* remains Air
    // for golden Baltic v3 fixtures. Checked before the Surface default.
    private static readonly string[] SubsurfaceMarkers =
    [
        "ssn", "ssk", "ssbn", "ssgn", "kilo", "gotland", "yasen", "virginia",
        "akula", "oscar", "astute", "collins", "type-212", "type-214", "uuv",
    ];

    private static readonly string[] AirMarkers =
    [
        "ucav", "jas-39", "eurofighter", "tu-160", "tu-22", "tu-95",
        "blackjack", "mig-", "su-27", "su-30", "su-33", "su-34", "su-35", "su-57",
        "flanker", "felon", "fullback", "f-16", "f-15", "f-18", "f-22", "f-35",
        "ka-27", "ka-52", "helix", "ah-64", "e-3a", "e-3-", "gripen", "falcon",
        "foxhound", "fulcrum", "sentry",
    ];

    public static MissionContactTargetClass Classify(string targetId)
    {
        if (ContainsMarker(targetId, SubsurfaceMarkers))
        {
            return MissionContactTargetClass.Subsurface;
        }

        if (ContainsMarker(targetId, AirMarkers))
        {
            return MissionContactTargetClass.Air;
        }

        return MissionContactTargetClass.Surface;
    }

    public static bool Matches(MissionContactTargetClass required, string targetId) =>
        required switch
        {
            MissionContactTargetClass.Any => true,
            MissionContactTargetClass.Surface => Classify(targetId) == MissionContactTargetClass.Surface,
            MissionContactTargetClass.Air => Classify(targetId) == MissionContactTargetClass.Air,
            MissionContactTargetClass.Subsurface => Classify(targetId) == MissionContactTargetClass.Subsurface,
            _ => false,
        };

    private static bool ContainsMarker(string targetId, string[] markers)
    {
        foreach (var marker in markers)
        {
            if (targetId.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
