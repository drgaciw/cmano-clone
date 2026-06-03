namespace ProjectAegis.Data.Validation;

/// <summary>Strike fuel reachability per GDD §4.1 (round-trip combat radius).</summary>
public static class ReachabilityCalculator
{
    private const double EarthRadiusNm = 3440.065;

    public static double HaversineNm(double lat1Deg, double lon1Deg, double lat2Deg, double lon2Deg)
    {
        static double ToRad(double d) => d * Math.PI / 180.0;
        var lat1 = ToRad(lat1Deg);
        var lat2 = ToRad(lat2Deg);
        var dLat = lat2 - lat1;
        var dLon = ToRad(lon2Deg - lon1Deg);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusNm * c;
    }

    public static bool IsReachable(
        double distanceNm,
        double combatRadiusNm,
        double ingressEgressPadNm,
        double fuelFraction,
        out double excessNm)
    {
        excessNm = 0;
        if (combatRadiusNm <= 0)
        {
            return false;
        }

        var budget = combatRadiusNm * fuelFraction - ingressEgressPadNm;
        if (distanceNm <= budget)
        {
            return true;
        }

        excessNm = distanceNm - budget;
        return false;
    }
}