using ProjectAegis.Ai.Contracts;
using ProjectAegis.Delegation.AfterAction;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>Projection output: frozen evidence DTO plus host-computed quantitative fields.</summary>
public sealed record EvidenceProjectionResult(
    EvidenceDocument Document,
    EvidenceQuantitativeSummary Quantitative,
    AgentPerformanceSection? AarSection,
    IReadOnlyDictionary<string, string>? AuthoritativeFacts);
