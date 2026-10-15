namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Skill-unavailable disclosure for <c>skill_unavailable_disclosure</c>.
/// Discloses unavailable assistance or local heuristic scores. Never an engage or order card.
/// </summary>
public sealed record JevDisclosurePresentation(
    bool Visible,
    string BannerText,
    string CssClass,
    string ModelPin,
    string OpenRouterModel)
{
    /// <summary>Pinned Jev line. Not a floating <c>latest</c>.</summary>
    public const string PinnedModel = "jev-1.13.x";

    /// <summary>Pinned OpenRouter model id.</summary>
    public const string PinnedOpenRouterModel = "typesafe/jev-1.13";

    /// <summary>USS class while disclosure is showing.</summary>
    public const string VisibleCssClass = "jev-disclosure--visible";

    /// <summary>USS class while the decide path is healthy.</summary>
    public const string HiddenCssClass = "jev-disclosure--hidden";

    /// <summary>Player-visible offline banner. Advisory only.</summary>
    public const string UnavailableBanner =
        "ASSIST UNAVAILABLE — Jev decide is offline. Advisory chrome only; no engage suggestion.";

    /// <summary>Hidden disclosure.</summary>
    public static JevDisclosurePresentation Hidden { get; } = new(
        false,
        string.Empty,
        HiddenCssClass,
        PinnedModel,
        PinnedOpenRouterModel);

    /// <summary>Persistent offline disclosure.</summary>
    public static JevDisclosurePresentation Shown { get; } = new(
        true,
        UnavailableBanner,
        VisibleCssClass,
        PinnedModel,
        PinnedOpenRouterModel);

    /// <summary>Available skill without a received decision: priorities are local heuristics.</summary>
    public static JevDisclosurePresentation LocalHeuristic { get; } = new(
        true,
        "LOCAL HEURISTIC — No Jev decision received. Priorities use local advisory rules.",
        VisibleCssClass,
        PinnedModel,
        PinnedOpenRouterModel);
}
