namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;

/// <summary>Builds sorted detection trials from scenario JSON and/or catalog basePd.</summary>
public static class DetectionTrialResolver
{
    public static IReadOnlyList<ScenarioDetectionTrial> Resolve(
        ScenarioPolicyProfile profile,
        ICatalogReader catalog)
    {
        if (profile.DetectionTrials.Count > 0)
        {
            return profile.DetectionTrials;
        }

        if (profile.CatalogDetectionTargets.Count == 0)
        {
            return Array.Empty<ScenarioDetectionTrial>();
        }

        var trials = new List<ScenarioDetectionTrial>(profile.CatalogDetectionTargets.Count);
        foreach (var target in profile.CatalogDetectionTargets
                     .OrderBy(t => t.ObserverId, StringComparer.Ordinal)
                     .ThenBy(t => t.SensorId, StringComparer.Ordinal)
                     .ThenBy(t => t.TargetId, StringComparer.Ordinal))
        {
            if (!catalog.TryGetBasePd(target.ObserverId, target.SensorId, out var basePd))
            {
                throw new InvalidOperationException(
                    $"Catalog missing basePd for platform '{target.ObserverId}' sensor '{target.SensorId}'.");
            }

            var (effectiveBasePd, effectiveEnvMask) = PhaseBCatalogDetectionModifier.Apply(
                basePd,
                target.EnvMask,
                catalog,
                target.ObserverId);

            trials.Add(new ScenarioDetectionTrial(
                target.ObserverId,
                target.SensorId,
                target.TargetId,
                target.ContactId,
                effectiveBasePd,
                effectiveEnvMask,
                target.JamStrength,
                target.RequiresActiveRadar));
        }

        return trials;
    }
}