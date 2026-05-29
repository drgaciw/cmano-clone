namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Delegation.Core;

public static class DefaultRiskClassifier
{
    public static RiskLevel Classify(OrderKind kind) =>
        kind switch
        {
            OrderKind.Engage => RiskLevel.High,
            _ => RiskLevel.Low,
        };
}
