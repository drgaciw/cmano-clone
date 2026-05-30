namespace ProjectAegis.Sim.Engage;

public static class DlzEvaluator
{
    public static DlzState Evaluate(double rangeMeters, WeaponEnvelope envelope)
    {
        if (rangeMeters < envelope.MinRangeMeters * 0.9)
        {
            return DlzState.OutOfZone;
        }

        if (rangeMeters > envelope.MaxRangeMeters)
        {
            return DlzState.OutOfZone;
        }

        if (rangeMeters > envelope.MaxRangeMeters * 0.85)
        {
            return DlzState.Approaching;
        }

        return DlzState.InZone;
    }
}
