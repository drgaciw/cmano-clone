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

    public void TickOnce(TimeCompressionMode mode)
    {
        _ = mode;
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
