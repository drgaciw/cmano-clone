namespace ProjectAegis.Sim.Cec;

/// <summary>Deterministic mesh membership transition kinds (join / leave / degrade).</summary>
public enum CecMeshEventKind
{
    Join = 0,
    Degrade = 1,
    Leave = 2,
}
