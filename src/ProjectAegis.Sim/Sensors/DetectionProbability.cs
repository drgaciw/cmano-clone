namespace ProjectAegis.Sim.Sensors;

/// <summary>MVP detection probability (sensor GDD § Formulas).</summary>
public static class DetectionProbability
{
    public static double ComputePd(
        double basePd,
        double envMask = 1.0,
        double eccmFactor = 1.0,
        double jamStrength = 0.0)
    {
        var jam = Math.Clamp(jamStrength, 0, 1);
        return Math.Clamp(basePd * envMask * eccmFactor * (1.0 - jam), 0, 1);
    }
}