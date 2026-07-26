namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

/// <summary>
/// Host chrome policy for the Scenario Map Authoring Editor window.
/// Keeps place/draw staging consistent when UXML is recloned from defaults
/// (CreateGUI / OpenWindow RebuildUi).
/// </summary>
public static class ScenarioMapAuthoringHostPolicy
{
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
