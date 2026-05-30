namespace ProjectAegis.Sim.Core;

using ProjectAegis.Sim.Time;

public interface ISimTickRunner
{
    SimClock Clock { get; }
    SimSeed Seed { get; }
    ulong LastWorldHash { get; }

    /// <summary>Runs one full deterministic pipeline tick (ADR-004).</summary>
    void TickOnce(TimeCompressionMode mode);
}
