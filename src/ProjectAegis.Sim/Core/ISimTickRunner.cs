namespace ProjectAegis.Sim.Core;

using ProjectAegis.Sim.Time;

public interface ISimTickRunner
{
    SimClock Clock { get; }
    SimSeed Seed { get; }
    ulong LastWorldHash { get; }

    /// <summary>
    /// Runs one full deterministic pipeline tick (ADR-004), or multiple steps when
    /// <paramref name="mode"/> is <see cref="TimeCompressionMode.Accelerated"/>.
    /// Paused clocks no-op unless mode is HeadlessBatch (CI override).
    /// </summary>
    void TickOnce(TimeCompressionMode mode);
}
