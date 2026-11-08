namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Data.Catalog;
using Scenario;

/// <summary>Builds a <see cref="DetectionEnvironment"/> from scenario policy + catalog (DRG-379 / DRG-386).</summary>
public static class DetectionEnvironmentFactory
{
    /// <summary>
    /// Radar ECCM profiles come from catalog sensor rows first (sorted platform → sensor; the first
    /// row for a sensor id wins), then scenario <c>radarEccm</c> entries override by sensor id.
    /// Returns <see cref="DetectionEnvironment.None"/> when nothing is authored.
    /// </summary>
    public static DetectionEnvironment Build(ScenarioPolicyProfile? profile, ICatalogReader? catalog)
    {
        var eccm = new SortedDictionary<string, RadarEccmProfile>(StringComparer.Ordinal);
        if (catalog != null)
        {
            foreach (var binding in catalog.GetSortedSensorBindings())
            {
                if (eccm.ContainsKey(binding.SensorId))
                {
                    continue;
                }

                var p = FromBinding(binding);
                if (p != RadarEccmProfile.Unspecified)
                {
                    eccm[binding.SensorId] = p;
                }
            }
        }

        if (profile != null)
        {
            foreach (var pair in profile.RadarEccm)
            {
                eccm[pair.Key] = pair.Value;
            }
        }

        var los = profile is { LineOfSight.Count: > 0 }
            ? new LosGeometryTable(profile.LineOfSight)
            : LosGeometryTable.Empty;
        return los.Count == 0 && eccm.Count == 0
            ? DetectionEnvironment.None
            : new DetectionEnvironment(los, eccm);
    }

    public static RadarEccmProfile FromBinding(CatalogSensorBinding binding)
    {
        if (!string.Equals(binding.Modality, CatalogSensorModalities.Radar, StringComparison.Ordinal))
        {
            return RadarEccmProfile.Unspecified;
        }

        return new RadarEccmProfile(
            RadarEccmProfile.ParseScanType(binding.RadarScanType),
            binding.FrequencyAgile,
            Math.Max(0, binding.RadarTechGeneration));
    }
}
