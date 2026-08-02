namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// CMD-20: map scale bar (NM) and camera altitude readout.
/// Pure math helpers — hosts bind labels only.
/// </summary>
public static class MapScaleProjection
{
    public const double MetersPerNauticalMile = 1852.0;

    public static MapScaleState Project(double cameraAltitudeMeters, double metersPerScreenUnit)
    {
        var alt = Math.Max(0, cameraAltitudeMeters);
        var mpu = Math.Max(0, metersPerScreenUnit);
        var scaleNm = mpu <= 0 ? 0 : mpu / MetersPerNauticalMile;
        return new MapScaleState(
            ScaleBarLabel: FormatScaleBar(scaleNm),
            CameraAltitudeLabel: FormatAltitude(alt),
            ScaleNauticalMiles: scaleNm,
            CameraAltitudeMeters: alt);
    }

    public static string FormatScaleBar(double nauticalMiles)
    {
        if (nauticalMiles <= 0)
        {
            return "SCALE —";
        }

        if (nauticalMiles >= 100)
        {
            return $"SCALE {nauticalMiles:0} NM";
        }

        if (nauticalMiles >= 10)
        {
            return $"SCALE {nauticalMiles:0.0} NM";
        }

        return $"SCALE {nauticalMiles:0.00} NM";
    }

    public static string FormatAltitude(double meters)
    {
        if (meters <= 0)
        {
            return "CAM ALT —";
        }

        if (meters >= 1_000_000)
        {
            return $"CAM ALT {meters / 1000:0} km";
        }

        return $"CAM ALT {meters:0} m";
    }
}

public sealed record MapScaleState(
    string ScaleBarLabel,
    string CameraAltitudeLabel,
    double ScaleNauticalMiles,
    double CameraAltitudeMeters);

/// <summary>
/// CMD-28.4: range / bearing measurement tool (pure geometry).
/// </summary>
public static class MapMeasureProjection
{
    public static MapMeasureResult Measure(
        double fromX,
        double fromY,
        double toX,
        double toY,
        double metersPerUnit)
    {
        var dx = toX - fromX;
        var dy = toY - fromY;
        var distUnits = Math.Sqrt(dx * dx + dy * dy);
        var meters = distUnits * Math.Max(0, metersPerUnit);
        var nm = meters / MapScaleProjection.MetersPerNauticalMile;
        var bearingDeg = (Math.Atan2(dx, dy) * 180.0 / Math.PI + 360.0) % 360.0;
        return new MapMeasureResult(
            RangeMeters: meters,
            RangeNauticalMiles: nm,
            BearingDegrees: bearingDeg,
            Label: $"RNG {nm:0.00} NM  BRG {bearingDeg:000.0}°");
    }
}

public sealed record MapMeasureResult(
    double RangeMeters,
    double RangeNauticalMiles,
    double BearingDegrees,
    string Label);

/// <summary>
/// CMD-28.5: next/prev unit cycle over ordered id list.
/// </summary>
public static class UnitCycleProjection
{
    public static string? Next(IReadOnlyList<string>? unitIds, string? currentId)
    {
        if (unitIds is null || unitIds.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrEmpty(currentId))
        {
            return unitIds[0];
        }

        var idx = -1;
        for (var i = 0; i < unitIds.Count; i++)
        {
            if (string.Equals(unitIds[i], currentId, StringComparison.Ordinal))
            {
                idx = i;
                break;
            }
        }

        if (idx < 0)
        {
            return unitIds[0];
        }

        return unitIds[(idx + 1) % unitIds.Count];
    }

    public static string? Previous(IReadOnlyList<string>? unitIds, string? currentId)
    {
        if (unitIds is null || unitIds.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrEmpty(currentId))
        {
            return unitIds[^1];
        }

        var idx = -1;
        for (var i = 0; i < unitIds.Count; i++)
        {
            if (string.Equals(unitIds[i], currentId, StringComparison.Ordinal))
            {
                idx = i;
                break;
            }
        }

        if (idx < 0)
        {
            return unitIds[^1];
        }

        return unitIds[(idx - 1 + unitIds.Count) % unitIds.Count];
    }
}
