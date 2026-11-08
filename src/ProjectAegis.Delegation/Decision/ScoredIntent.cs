namespace ProjectAegis.Delegation.Decision;

using Core;

public sealed record ScoredIntent(OrderKind Kind, double Score, RiskLevel Risk);
