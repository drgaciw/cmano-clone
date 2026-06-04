namespace ProjectAegis.Sim.Policy;

/// <summary>Effective policy plus inheritance provenance for UI/MCP (req 13 P0).</summary>
public readonly record struct ResolvedUnitPolicy(
    EffectivePolicy Effective,
    bool RoeInheritedFromMission)
{
    public bool HasInheritedDoctrineFromMission => RoeInheritedFromMission;
}