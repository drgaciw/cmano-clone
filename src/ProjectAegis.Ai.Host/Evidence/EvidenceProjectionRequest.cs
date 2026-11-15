using ProjectAegis.Ai.Contracts;
using ProjectAegis.Delegation.AfterAction;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>Inputs for projecting a read-only order log into an <see cref="EvidenceDocument"/>.</summary>
public sealed record EvidenceProjectionRequest(
    string BuildIdentity,
    string ScenarioId,
    string CatalogRevision,
    ViewingSide ViewingSide,
    bool DebriefMode,
    string FingerprintPrefix,
    AgentPerformanceSection? AarSection = null,
    IReadOnlyDictionary<string, string>? AuthoritativeFacts = null);
