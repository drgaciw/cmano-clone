namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// Optional, scenario-authored detection environment: LOS geometry (ENV-02 / DRG-379) and radar
/// ECCM profiles keyed by sensor id (EW-01 / DRG-386). <see cref="None"/> preserves legacy behaviour.
/// </summary>
public sealed class DetectionEnvironment
{
    public static readonly DetectionEnvironment None = new(LosGeometryTable.Empty, new Dictionary<string, RadarEccmProfile>());

    public DetectionEnvironment(
        LosGeometryTable lineOfSight,
        IReadOnlyDictionary<string, RadarEccmProfile> radarEccmBySensorId)
    {
        LineOfSight = lineOfSight;
        RadarEccmBySensorId = radarEccmBySensorId;
    }

    public LosGeometryTable LineOfSight { get; }

    public IReadOnlyDictionary<string, RadarEccmProfile> RadarEccmBySensorId { get; }

    public bool IsEmpty => LineOfSight.Count == 0 && RadarEccmBySensorId.Count == 0;

    public RadarEccmProfile ResolveEccm(string sensorId) =>
        RadarEccmBySensorId.TryGetValue(sensorId, out var p) ? p : RadarEccmProfile.Unspecified;
}
