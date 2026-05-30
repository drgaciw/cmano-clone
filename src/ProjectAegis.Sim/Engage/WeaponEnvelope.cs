namespace ProjectAegis.Sim.Engage;

public readonly record struct WeaponEnvelope(double MinRangeMeters, double MaxRangeMeters)
{
    public bool Contains(double rangeMeters) =>
        rangeMeters >= MinRangeMeters && rangeMeters <= MaxRangeMeters;
}
