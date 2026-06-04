namespace ProjectAegis.Sim.Engage;

/// <summary>DLZ launch gate with personality timing (req 14).</summary>
public static class DlzEngageGate
{
    public static bool AllowsLaunch(
        double rangeMeters,
        WeaponEnvelope envelope,
        DlzPersonality personality = DlzPersonality.Normal)
    {
        var state = DlzEvaluator.Evaluate(rangeMeters, envelope);
        return personality switch
        {
            DlzPersonality.Early => state is DlzState.InZone or DlzState.Approaching,
            DlzPersonality.Late => state == DlzState.InZone &&
                                   rangeMeters <= envelope.MaxRangeMeters * 0.7,
            _ => state == DlzState.InZone,
        };
    }

    public static DlzState EvaluateState(double rangeMeters, WeaponEnvelope envelope) =>
        DlzEvaluator.Evaluate(rangeMeters, envelope);
}