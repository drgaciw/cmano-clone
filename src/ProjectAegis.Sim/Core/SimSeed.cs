namespace ProjectAegis.Sim.Core;

/// <summary>Global scenario seed; subsystem RNG derives via domain + entity id.</summary>
public readonly record struct SimSeed(ulong Value)
{
    public static SimSeed FromScenario(ulong scenarioSeed) => new(scenarioSeed);
}
