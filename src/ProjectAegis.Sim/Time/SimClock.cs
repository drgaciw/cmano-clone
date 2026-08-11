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

    /// <summary>
    /// S117 / PRD P0-8: weapons-release forced 1× is active.
    /// Precedence: pause &gt; forced-1x &gt; player compression.
    /// </summary>
    public bool IsWeaponsReleaseForced1x { get; private set; }

    public void Pause() => IsPaused = true;

    public void Resume() => IsPaused = false;

    /// <summary>
    /// Sets acceleration factor, clamped to [1, 256].
    /// While <see cref="IsWeaponsReleaseForced1x"/> is true, values &gt; 1 are ignored (forced 1× holds).
    /// </summary>
    public void SetAccelerationFactor(int factor)
    {
        if (IsWeaponsReleaseForced1x && factor > MinAccelerationFactor)
        {
            AccelerationFactor = MinAccelerationFactor;
            return;
        }

        AccelerationFactor = Math.Clamp(factor, MinAccelerationFactor, MaxAccelerationFactor);
    }

    /// <summary>S117 / P0-8: force compression to 1× on weapons-release.</summary>
    public void ForceRealTimeForWeaponsRelease()
    {
        AccelerationFactor = MinAccelerationFactor;
        IsWeaponsReleaseForced1x = true;
    }

    /// <summary>Clears weapons-release forced 1× (approve/deny/expiry residual path).</summary>
    public void ClearWeaponsReleaseForced1x()
    {
        IsWeaponsReleaseForced1x = false;
    }

    public void AdvanceOneTick() => SimTick++;

    public void Reset(ulong startTick = 0) => SimTick = startTick;
}
