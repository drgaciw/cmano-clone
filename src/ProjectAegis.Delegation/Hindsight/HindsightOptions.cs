namespace ProjectAegis.Delegation.Hindsight;

/// <summary>Configuration for the optional Hindsight sidecar (default off for CI/replay).</summary>
public sealed class HindsightOptions
{
    public bool Enabled { get; init; } = true;

    public string BaseUrl { get; init; } = "http://localhost:8888";

    public string? ApiKey { get; init; }

    /// <summary>Scenario slug used for AAR banks, e.g. <c>baltic</c>.</summary>
    public string ScenarioSlug { get; init; } = "session";

    /// <summary>Run identifier; when null, derived from order-log fingerprint at finalize.</summary>
    public string? RunId { get; init; }

    public bool RetainAgentDecisions { get; init; } = true;

    public bool FinalizeAarBank { get; init; } = true;

    public bool FinalizeCampaignExperience { get; init; } = true;

    /// <summary>Optional reflect query after AAR retain (tooling only; not used in tick loop).</summary>
    public string? AarReflectQuery { get; init; }
}
