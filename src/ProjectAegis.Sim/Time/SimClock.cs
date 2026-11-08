namespace ProjectAegis.Sim.Time;

/// <summary>
/// Fixed-step simulation clock with optional pause and acceleration factor.
/// Acceleration is applied by the tick runner (multiple full steps per call in
/// <see cref="TimeCompressionMode.Accelerated"/>), not by stretching FixedDeltaSeconds.
/// </summary>
public sealed class SimClock
{
    public const int MinAccelerationFactor = 1;
    public const int MaxAccelerationFactor = 256;

    public SimClock(double fixedDeltaSeconds = 1.0 / 60.0)
    {
        FixedDeltaSeconds = fixedDeltaSeconds;
    }

    public double FixedDeltaSeconds { get; }

    public ulong SimTick { get; private set; }

    public double SimTime => SimTick * FixedDeltaSeconds;

    /// <summary>When true, interactive tick modes do not advance. Default false.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>Steps per <see cref="TimeCompressionMode.Accelerated"/> TickOnce. Default 1; range 1..256.</summary>
    public int AccelerationFactor { get; private set; } = 1;

    public void Pause() => IsPaused = true;

    public void Resume() => IsPaused = false;

    /// <summary>Sets acceleration factor, clamped to [1, 256].</summary>
    public void SetAccelerationFactor(int factor) =>
        AccelerationFactor = Math.Clamp(factor, MinAccelerationFactor, MaxAccelerationFactor);

    public void AdvanceOneTick() => SimTick++;

    public void Reset(ulong startTick = 0) => SimTick = startTick;
}
