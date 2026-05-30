namespace ProjectAegis.Sim.Time;

public sealed class SimClock
{
    public SimClock(double fixedDeltaSeconds = 1.0 / 60.0)
    {
        FixedDeltaSeconds = fixedDeltaSeconds;
    }

    public double FixedDeltaSeconds { get; }
    public ulong SimTick { get; private set; }
    public double SimTime => SimTick * FixedDeltaSeconds;

    public void AdvanceOneTick() => SimTick++;

    public void Reset(ulong startTick = 0) => SimTick = startTick;
}
