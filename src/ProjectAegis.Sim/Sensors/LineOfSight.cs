namespace ProjectAegis.Sim.Sensors;

using Glossary;

/// <summary>One terrain elevation sample along the observer → target great-circle path (ENV-02 / DRG-379).</summary>
/// <param name="DistanceMeters">Ground distance from the observer, in metres (0 &lt; d &lt; range).</param>
/// <param name="ElevationMslMeters">Terrain (or obstruction) height above mean sea level, in metres.</param>
public readonly record struct TerrainProfileSample(double DistanceMeters, double ElevationMslMeters);

/// <summary>
/// Authored geometry for one observer → target pair. Heights are antenna / target heights above
/// mean sea level (ground elevation + mast or flight altitude). Terrain samples are optional;
/// without them only the earth-curvature horizon is applied.
/// </summary>
public sealed record ScenarioLosGeometry(
    string ObserverId,
    string TargetId,
    double ObserverHeightMslMeters,
    double TargetHeightMslMeters,
    double RangeMeters,
    IReadOnlyList<TerrainProfileSample>? Terrain = null);

/// <summary>Outcome of a line-of-sight check. <see cref="BlockCode"/> is a stable Sensor-family log code.</summary>
public readonly record struct LosResult(bool Clear, string? BlockCode)
{
    public static LosResult ClearPath => new(true, null);

    public static LosResult Blocked(string code) => new(false, code);
}

/// <summary>A detection trial skipped this tick because LOS was blocked (order-log / AAR evidence).</summary>
public readonly record struct DetectionLosBlock(
    ulong SimTick,
    string ObserverId,
    string SensorId,
    string TargetId,
    string BlockCode);

/// <summary>
/// Deterministic radar-horizon and terrain-masking evaluator (ENV-02 / DRG-379).
/// Pure arithmetic in double precision over ordered inputs — no RNG, no allocation.
/// </summary>
public static class LineOfSightEvaluator
{
    /// <summary>Mean earth radius (m).</summary>
    public const double EarthRadiusMeters = 6_371_000.0;

    /// <summary>Standard-atmosphere effective-earth factor for RF propagation (4/3 earth).</summary>
    public const double RadarKFactor = 4.0 / 3.0;

    /// <summary>Optical / IR refraction factor (≈7/6 earth).</summary>
    public const double OpticalKFactor = 7.0 / 6.0;

    /// <summary>
    /// Per-pair performance budget: at most this many terrain samples are evaluated. Samples beyond
    /// the cap are ignored (authoring validation should resample long profiles).
    /// </summary>
    public const int MaxTerrainSamplesPerPair = 256;

    public static double KFactorFor(SensorModality modality) =>
        modality == SensorModality.Radar ? RadarKFactor : OpticalKFactor;

    /// <summary>Maximum mutual-visibility range over a smooth earth for two heights above MSL.</summary>
    public static double HorizonRangeMeters(double observerHeightMsl, double targetHeightMsl, double kFactor)
    {
        var re = EarthRadiusMeters * kFactor;
        return Math.Sqrt(2.0 * re * Math.Max(0, observerHeightMsl))
            + Math.Sqrt(2.0 * re * Math.Max(0, targetHeightMsl));
    }

    public static LosResult Evaluate(ScenarioLosGeometry geometry, SensorModality modality)
    {
        var k = KFactorFor(modality);
        var range = Math.Max(0, geometry.RangeMeters);
        if (range > HorizonRangeMeters(geometry.ObserverHeightMslMeters, geometry.TargetHeightMslMeters, k))
        {
            return LosResult.Blocked(AbortReasonCatalog.Sensor.LOS_RADAR_HORIZON);
        }

        var terrain = geometry.Terrain;
        if (terrain == null || terrain.Count == 0 || range <= 0)
        {
            return LosResult.ClearPath;
        }

        var re = EarthRadiusMeters * k;
        var h1 = geometry.ObserverHeightMslMeters;
        var h2 = geometry.TargetHeightMslMeters;
        var count = Math.Min(terrain.Count, MaxTerrainSamplesPerPair);
        for (var i = 0; i < count; i++)
        {
            var s = terrain[i];
            var d = s.DistanceMeters;
            if (d <= 0 || d >= range)
            {
                continue;
            }

            // Straight ray height over a flat reference, then add earth bulge to the obstruction.
            var rayHeight = h1 + ((h2 - h1) * d / range);
            var bulge = d * (range - d) / (2.0 * re);
            if (s.ElevationMslMeters + bulge >= rayHeight)
            {
                return LosResult.Blocked(AbortReasonCatalog.Sensor.LOS_TERRAIN_MASK);
            }
        }

        return LosResult.ClearPath;
    }
}

/// <summary>Ordinal lookup of authored LOS geometry by (observer, target).</summary>
public sealed class LosGeometryTable
{
    public static readonly LosGeometryTable Empty = new(Array.Empty<ScenarioLosGeometry>());

    private readonly Dictionary<string, ScenarioLosGeometry> _byPair;

    public LosGeometryTable(IReadOnlyList<ScenarioLosGeometry> entries)
    {
        _byPair = new Dictionary<string, ScenarioLosGeometry>(entries.Count, StringComparer.Ordinal);
        foreach (var e in entries)
        {
            // Last authored entry wins; callers pass entries in authored order.
            _byPair[Key(e.ObserverId, e.TargetId)] = e;
        }
    }

    public int Count => _byPair.Count;

    public bool TryGet(string observerId, string targetId, out ScenarioLosGeometry geometry) =>
        _byPair.TryGetValue(Key(observerId, targetId), out geometry!);

    private static string Key(string observerId, string targetId) => observerId + "\u001f" + targetId;
}
