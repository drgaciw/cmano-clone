namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

public sealed record ScoredIntent(OrderKind Kind, double Score, RiskLevel Risk);
