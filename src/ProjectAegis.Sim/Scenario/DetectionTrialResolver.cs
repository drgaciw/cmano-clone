namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Sensors;

/// <summary>Builds sorted detection trials from scenario JSON and/or catalog basePd.</summary>
public static class DetectionTrialResolver
{
    public static IReadOnlyList<ScenarioDetectionTrial> Resolve(
        ScenarioPolicyProfile profile,
        ICatalogReader catalog)
    {
        // Prefer profile-authored trials as-is (no modality rewrite).
        if (profile.DetectionTrials.Count > 0)
        {
            return profile.DetectionTrials;
        }

        if (profile.CatalogDetectionTargets.Count == 0)
        {
            return Array.Empty<ScenarioDetectionTrial>();
        }

        // S112-C / DRG-10 residual: map catalog sensor Modality onto trials.
        var bindingByKey = new Dictionary<(string PlatformId, string SensorId), CatalogSensorBinding>();
        foreach (var binding in catalog.GetSortedSensorBindings())
        {
            bindingByKey[(binding.PlatformId, binding.SensorId)] = binding;
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

            var modality = SensorModality.Radar;
            if (bindingByKey.TryGetValue((target.ObserverId, target.SensorId), out var sensorBinding))
            {
                modality = ParseSensorModality(sensorBinding.Modality);
            }

            // Optical / IR sensors do not require active radar emission.
            var requiresActiveRadar = modality is SensorModality.Infrared or SensorModality.Visual
                ? false
                : target.RequiresActiveRadar;

            trials.Add(new ScenarioDetectionTrial(
                target.ObserverId,
                target.SensorId,
                target.TargetId,
                target.ContactId,
                effectiveBasePd,
                effectiveEnvMask,
                target.JamStrength,
                RequiresActiveRadar: requiresActiveRadar,
                Modality: modality));
        }

        return trials;
    }

    /// <summary>
    /// Maps catalog modality strings (case-insensitive) to <see cref="SensorModality"/>.
    /// Unknown / empty → <see cref="SensorModality.Radar"/>.
    /// </summary>
    public static SensorModality ParseSensorModality(string? modality)
    {
        if (string.IsNullOrWhiteSpace(modality))
        {
            return SensorModality.Radar;
        }

        if (string.Equals(modality, CatalogSensorModalities.Infrared, StringComparison.OrdinalIgnoreCase))
        {
            return SensorModality.Infrared;
        }

        if (string.Equals(modality, CatalogSensorModalities.Visual, StringComparison.OrdinalIgnoreCase))
        {
            return SensorModality.Visual;
        }

        // Radar and any unknown token default to Radar.
        return SensorModality.Radar;
    }
}
