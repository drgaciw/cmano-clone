namespace ProjectAegis.Sim.Engage;

/// <summary>Unified engagement pipeline (doc 14 / ADR architecture step 8).</summary>
public interface IEngagementResolver
{
    EngageResult Resolve(in EngageRequest request);
}
