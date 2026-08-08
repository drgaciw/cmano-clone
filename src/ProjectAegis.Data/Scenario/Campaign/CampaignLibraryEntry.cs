namespace ProjectAegis.Data.Scenario.Campaign;

/// <summary>
/// One campaign-library browse row (CMD-27.12): membership summary + pre-load feasibility.
/// Campaigns are an artifact class listed separately from flat scenarios.
/// </summary>
public sealed record CampaignLibraryEntry(
    string CampaignId,
    string Title,
    int MemberCount,
    int CompletedCount,
    string? NextScenarioId,
    bool Available,
    string? UnavailableReason,
    string SourcePath);

/// <summary>Stable pre-load feasibility reason codes for campaigns (CMD-27.12).</summary>
public static class CampaignLibraryReasons
{
    public const string FileUnreadable = "FILE_UNREADABLE";
    public const string SchemaError = "SCHEMA_ERROR";

    /// <summary>At least one member scenario document is missing from the scenarios tree.</summary>
    public const string MemberMissing = "MEMBER_MISSING";
}
