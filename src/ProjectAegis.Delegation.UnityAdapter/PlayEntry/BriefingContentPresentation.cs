namespace ProjectAegis.Delegation.UnityAdapter.PlayEntry;

using Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;

public enum BriefingContentState
{
    NoPackage,
    Available,
    MissingContent,
}

/// <summary>
/// Briefing content panel model (S123-03 W2-CORE-01). Binds only what the loaded package already carries:
/// metadata title / description and mission rows. An absent description is an explicit
/// <see cref="BriefingContentState.MissingContent"/> state, never placeholder narrative.
/// </summary>
public sealed record BriefingContentPresentation(
    BriefingContentState State,
    string Title,
    string StatusLabel,
    string? Narrative,
    string PolicyLabel,
    IReadOnlyList<string> MissionLines)
{
    public const string NoPackageLabel = "No scenario loaded";
    public const string AvailableLabel = "Briefing";
    public const string MissingContentLabel = "Briefing content not authored for this scenario package";

    public static BriefingContentPresentation None { get; } = new(
        BriefingContentState.NoPackage,
        string.Empty,
        NoPackageLabel,
        null,
        string.Empty,
        Array.Empty<string>());

    public static BriefingContentPresentation Bind(PlayEntrySession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        return Bind(session.Package, session.Document);
    }

    public static BriefingContentPresentation Bind(ScenarioPackage? package, ScenarioDocumentDto? document)
    {
        if (package == null || document == null)
        {
            return None;
        }

        var meta = document.Metadata;
        var narrative = string.IsNullOrWhiteSpace(meta.Description) ? null : meta.Description!.Trim();
        var missions = document.Missions;
        var lines = new string[missions.Count];
        for (var i = 0; i < missions.Count; i++)
        {
            var mission = missions[i];
            var units = mission.AssignedUnitIds is { Count: > 0 }
                ? string.Join(", ", mission.AssignedUnitIds)
                : "none";
            lines[i] = $"{mission.Id} · {mission.Type} · units: {units}";
        }

        return new BriefingContentPresentation(
            narrative == null ? BriefingContentState.MissingContent : BriefingContentState.Available,
            ScenarioLibraryProjection.PreferTitle(meta.Title, package.ScenarioId),
            narrative == null ? MissingContentLabel : AvailableLabel,
            narrative,
            $"POLICY: {package.PolicyId} · {package.TlBranch} · SEED {package.Seed}",
            lines);
    }
}
