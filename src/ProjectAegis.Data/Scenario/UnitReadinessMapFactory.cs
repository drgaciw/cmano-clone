namespace ProjectAegis.Data.Scenario;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Builds readiness dictionaries from scenario metadata for harness/CLI.</summary>
public static class UnitReadinessMapFactory
{
    public static IReadOnlyDictionary<string, bool>? FromMetadata(ScenarioMetadataDto? metadata)
    {
        if (metadata?.UnitReadiness == null || metadata.UnitReadiness.Count == 0)
        {
            return null;
        }

        var map = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var (unitId, dto) in metadata.UnitReadiness)
        {
            map[unitId] = dto.ReadyForLaunch;
        }

        return map;
    }
}