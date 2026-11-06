using ProjectAegis.Data.Catalog;

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
    public static MissionContactTargetClass FromCatalogDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return MissionContactTargetClass.Surface;
        }

        return domain.Trim().ToLowerInvariant() switch
        {
            "air" or "aircraft" => MissionContactTargetClass.Air,
            "subsurface" or "submarine" or "sub" or "subs" => MissionContactTargetClass.Subsurface,
            _ => MissionContactTargetClass.Surface,
        };
    }

    public static MissionContactTargetClass Classify(string targetId, ICatalogReader? catalogReader = null)
    {
        if (catalogReader?.TryGetPlatformDomain(targetId, out var domain) == true)
        {
            return FromCatalogDomain(domain);
        }

        return ClassifyCatalogMiss(targetId);
    }

    public static bool Matches(
        MissionContactTargetClass required,
        string targetId,
        ICatalogReader? catalogReader = null) =>
        required switch
        {
            MissionContactTargetClass.Any => true,
            MissionContactTargetClass.Surface => Classify(targetId, catalogReader) == MissionContactTargetClass.Surface,
            MissionContactTargetClass.Air => Classify(targetId, catalogReader) == MissionContactTargetClass.Air,
            MissionContactTargetClass.Subsurface => Classify(targetId, catalogReader) == MissionContactTargetClass.Subsurface,
            _ => false,
        };

    private static MissionContactTargetClass ClassifyCatalogMiss(string targetId) =>
        targetId.StartsWith("ucav", StringComparison.Ordinal)
            ? MissionContactTargetClass.Air
            : MissionContactTargetClass.Surface;
}
