namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

/// <summary>
/// Host chrome policy for the Scenario Map Authoring Editor window.
/// Keeps place/draw staging consistent when UXML is recloned from defaults
/// (CreateGUI / OpenWindow RebuildUi), and gates which chrome stays interactive
/// before a scenario session is open.
/// </summary>
public static class ScenarioMapAuthoringHostPolicy
{
    /// <summary>
    /// User-visible status when a session-gated write action is invoked with no open scenario.
    /// </summary>
    public const string NoSessionMessage = "Open a scenario.json first (Browse… then Open).";

    /// <summary>
    /// Repo-relative candidates preferred for UI-dev auto-seed (first existing wins).
    /// </summary>
    public static IReadOnlyList<string> DefaultScenarioRelativeCandidates { get; } = new[]
    {
        "assets/data/scenarios/authoring/russia-vs-nato-baltic-ui-dev.json",
        "assets/data/scenarios/validation/golden_clean.json",
    };

    /// <summary>
    /// Whether catalog, domain nav, and place-unit form chrome should remain enabled.
    /// Always true so authors can prep domain/catalog/form fields before Open; only
    /// session write actions (Save, Place, Commit, RP upsert, etc.) require a session.
    /// </summary>
    /// <param name="hasSession">True when a map surface/session is open (unused; kept for call-site symmetry).</param>
    public static bool ShouldEnableCatalogAndFormChrome(bool hasSession) => true;

    /// <summary>
    /// Whether write actions that need an open session should be enabled:
    /// Save, Rebuild, Refresh Findings, Place/Commit, Cancel, RP upsert.
    /// </summary>
    public static bool ShouldEnableSessionWriteActions(bool hasSession) => hasSession;

    /// <summary>
    /// Given a repo root directory, return the full path of the first existing
    /// <see cref="DefaultScenarioRelativeCandidates"/> entry, or null.
    /// </summary>
    public static string? ResolveDefaultScenarioPath(string? repoRoot)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return null;
        }

        try
        {
            if (!Directory.Exists(repoRoot))
            {
                return null;
            }
        }
        catch
        {
            return null;
        }

        foreach (var rel in DefaultScenarioRelativeCandidates)
        {
            if (string.IsNullOrWhiteSpace(rel))
            {
                continue;
            }

            var full = Path.GetFullPath(Path.Combine(repoRoot, rel));
            if (File.Exists(full))
            {
                return full;
            }
        }

        return null;
    }

    /// <summary>
    /// Host chrome rebuild reclones form controls from UXML defaults while the
    /// session <see cref="MapAuthoringSurface"/> may still hold a staged
    /// <see cref="MapAuthoringSurface.TentativeUnit"/>. Cancel the gesture so
    /// Commit cannot upsert an invisible stale payload that no longer matches
    /// the visible form.
    /// </summary>
    /// <param name="surface">Active map surface, or null when no session is open.</param>
    public static void InvalidateStagedGesturesForChromeRebuild(MapAuthoringSurface? surface)
    {
        surface?.CancelGesture();
    }

    /// <summary>
    /// Domain switch, catalog preset click, or place-form field changes leave the visible form
    /// out of sync with a previously staged <see cref="MapAuthoringSurface.TentativeUnit"/>.
    /// Cancel so Commit cannot write the pre-edit payload.
    /// </summary>
    public static void InvalidateStagedGesturesForFormOrDomainChange(MapAuthoringSurface? surface)
    {
        surface?.CancelGesture();
    }
}
