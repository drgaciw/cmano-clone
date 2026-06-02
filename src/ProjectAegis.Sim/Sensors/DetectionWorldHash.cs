namespace ProjectAegis.Sim.Sensors;

/// <summary>Deterministic mix of detection roll outcomes for replay gates.</summary>
public static class DetectionWorldHash
{
    public static ulong MixTick(ulong previous, IReadOnlyList<DetectionRollResult> rolls)
    {
        ulong x = previous;
        foreach (var roll in rolls)
        {
            var entity = DetectionEntityId.FromTrial(
                roll.Trial.ObserverId,
                roll.Trial.SensorId,
                roll.Trial.TargetId);
            x ^= entity;
            x ^= roll.Detected ? 0x9e37_79b9_7f4a_7c15UL : 0;
            x ^= (ulong)(uint)(roll.Pd * 10_000);
            x ^= (ulong)(uint)(roll.Draw * 10_000);
        }

        x ^= (ulong)rolls.Count << 32;
        return x;
    }
}