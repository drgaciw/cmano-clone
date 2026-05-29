namespace ProjectAegis.Delegation.Trust;

public sealed class AgentExperienceBlob
{
    public Dictionary<string, double> Metrics { get; } = new();
}

public sealed record TrustSignal(string AgentId, string Metric, double Value);
