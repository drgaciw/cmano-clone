namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Host mode for scenario authoring vs play preview.
/// </summary>
public enum ScenarioHostMode
{
    /// <summary>Authoring: mutations allowed via the command bus.</summary>
    Edit,

    /// <summary>Play preview: entered only when live findings allow (or force-confirm).</summary>
    Play,
}

/// <summary>
/// Play ↔ Edit FSM for the headless authoring host.
/// Does not mutate ORBAT or other document state — only switches <see cref="Mode"/>
/// after consulting <see cref="LiveFindingsPresenter"/> for error-severity findings.
/// </summary>
public sealed class EditModeController
{
    private readonly ScenarioAuthoringSession _session;
    private readonly LiveFindingsPresenter _findings;

    /// <summary>
    /// Creates a controller bound to the given session and findings presenter.
    /// Default mode is <see cref="ScenarioHostMode.Edit"/>.
    /// </summary>
    public EditModeController(ScenarioAuthoringSession session, LiveFindingsPresenter findings)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _findings = findings ?? throw new ArgumentNullException(nameof(findings));
        Mode = ScenarioHostMode.Edit;
    }

    /// <summary>Current host mode (default <see cref="ScenarioHostMode.Edit"/>).</summary>
    public ScenarioHostMode Mode { get; private set; }

    /// <summary>
    /// Why the most recent <see cref="TryEnterPlay"/> was rejected, or <c>null</c> when it succeeded
    /// (or has not been called).
    /// </summary>
    public string? LastPlayBlockReason { get; private set; }

    /// <summary>
    /// Attempts to enter play mode. Refreshes live findings first; if any finding has
    /// <see cref="ProjectAegis.Data.Validation.ValidationSeverity.Error"/> and
    /// <paramref name="forceConfirmInvalid"/> is false, stays in the current mode and returns false.
    /// Unsupported mission roles (DRG-345 capability findings) reject Play even when force-confirmed.
    /// Does not mutate the scenario document / ORBAT.
    /// </summary>
    /// <param name="forceConfirmInvalid">
    /// When true, allows play even if non-capability error-severity findings are present.
    /// </param>
    /// <returns>True when mode is now <see cref="ScenarioHostMode.Play"/>.</returns>
    public bool TryEnterPlay(bool forceConfirmInvalid = false)
    {
        // Session is the document owner; mode switch never writes through it.
        _ = _session;
        _findings.RefreshImmediate();
        if (_findings.Gate.HasUnsupportedExecution)
        {
            LastPlayBlockReason = _findings.Gate.PlayBlockingReason;
            return false;
        }

        if (_findings.HasErrorSeverity && !forceConfirmInvalid)
        {
            LastPlayBlockReason = _findings.ErrorCount == 1
                ? "Play blocked — 1 error must be resolved or force-confirmed."
                : $"Play blocked — {_findings.ErrorCount} errors must be resolved or force-confirmed.";
            return false;
        }

        LastPlayBlockReason = null;
        Mode = ScenarioHostMode.Play;
        return true;
    }

    /// <summary>Returns the host to <see cref="ScenarioHostMode.Edit"/>. Does not mutate ORBAT.</summary>
    public void EnterEdit() => Mode = ScenarioHostMode.Edit;
}
