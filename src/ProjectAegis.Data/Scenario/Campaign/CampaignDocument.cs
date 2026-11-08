namespace ProjectAegis.Data.Scenario.Campaign;

/// <summary>
/// First-class campaign artifact (CMD-27.12): ordered scenario membership with completion state.
/// Sequence and in-fiction progression live as metadata fields — not filename encoding.
/// </summary>
public sealed record CampaignDocument(
    string CampaignId,
    string Title,
    string Synopsis,
    IReadOnlyList<CampaignScenarioMember> Members);

/// <summary>One ordered campaign member. <see cref="Sequence"/> is 1-based.</summary>
public sealed record CampaignScenarioMember(
    string ScenarioId,
    int Sequence,
    bool Completed,
    string? DisplayTitle = null);
