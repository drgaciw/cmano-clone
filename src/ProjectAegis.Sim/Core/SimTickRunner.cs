namespace ProjectAegis.Sim.Core;

using ProjectAegis.Sim.Time;

/// <summary>MVP tick runner: advances clock and placeholder world hash until subsystems wire in.</summary>
public sealed class SimTickRunner : ISimTickRunner
{
    public SimTickRunner(SimSeed seed, double fixedDeltaSeconds = 1.0 / 60.0)
    {
        Seed = seed;
        Clock = new SimClock(fixedDeltaSeconds);
    }

    public SimClock Clock { get; }
    public SimSeed Seed { get; }
    public ulong LastWorldHash { get; private set; }

    /// <summary>
    /// Runs one or more deterministic pipeline ticks depending on <paramref name="mode"/>.
    /// When <see cref="SimClock.IsPaused"/> is true and mode is not
    /// <see cref="TimeCompressionMode.HeadlessBatch"/>, this is a no-op
    /// (SimTick and LastWorldHash unchanged). HeadlessBatch overrides pause so CI/batch
    /// runners can advance deterministically without an explicit Resume.
    /// Accelerated mode advances <see cref="SimClock.AccelerationFactor"/> full steps;
    /// RealTime and HeadlessBatch advance one step per call.
    /// </summary>
    public void TickOnce(TimeCompressionMode mode)
    {
        if (Clock.IsPaused && mode != TimeCompressionMode.HeadlessBatch)
        {
            return;
        }

        var steps = mode == TimeCompressionMode.Accelerated ? Clock.AccelerationFactor : 1;
        for (var i = 0; i < steps; i++)
        {
            AdvanceOneStep();
        }
    }

    /// <summary>Advances exactly one sim step (no pause/acceleration checks). Used by pipelines that own the outer loop.</summary>
    internal void AdvanceOneStep()
    {
        Clock.AdvanceOneTick();
        LastWorldHash = MixWorldHash(Seed.Value, Clock.SimTick, LastWorldHash);
    }

    private static ulong MixWorldHash(ulong seed, ulong tick, ulong previous)
    {
        ulong x = seed ^ (tick << 7) ^ previous;
        x ^= x >> 33;
        x *= 0xff51_afd7_ed55_8ccdUL;
        x ^= x >> 33;
        return x;
    }
}
