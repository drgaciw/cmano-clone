namespace ProjectAegis.Delegation.Projection;

/// <summary>Resolved enabled flag and tooltip for one Scenario Editor action button.</summary>
public readonly struct ScenarioEditorActionAvailabilityResult
{
    public ScenarioEditorActionAvailabilityResult(bool enabled, string tooltip)
    {
        Enabled = enabled;
        Tooltip = tooltip;
    }

    /// <summary>Whether the button should be enabled.</summary>
    public bool Enabled { get; }

    /// <summary>Tooltip text; empty when there is nothing to explain.</summary>
    public string Tooltip { get; }
}

/// <summary>
/// UAT D26: an action whose event has no subscriber is disabled with a <c>NOT_AVAILABLE:</c>
/// reason instead of silently doing nothing. Otherwise the projection's enable rule applies.
/// </summary>
public static class ScenarioEditorActionAvailability
{
    /// <summary>Exact prefix of the tooltip shown for unwired actions.</summary>
    public const string NotAvailablePrefix = "NOT_AVAILABLE:";

    /// <summary>Resolve enabled state and tooltip for an action button.</summary>
    /// <param name="actionName">Display name, e.g. "Load".</param>
    /// <param name="hasSubscriber">True when the action's event has at least one subscriber.</param>
    /// <param name="stateEnabled">Enabled flag from <see cref="ScenarioEditorShellState"/>.</param>
    /// <param name="stateDisabledReason">Existing reason when the state disables the action, if any.</param>
    public static ScenarioEditorActionAvailabilityResult Resolve(
        string actionName,
        bool hasSubscriber,
        bool stateEnabled,
        string? stateDisabledReason = null)
    {
        if (!hasSubscriber)
        {
            return new ScenarioEditorActionAvailabilityResult(
                false,
                $"{NotAvailablePrefix} {actionName} is not wired in this build");
        }

        return new ScenarioEditorActionAvailabilityResult(
            stateEnabled,
            stateEnabled ? string.Empty : stateDisabledReason ?? string.Empty);
    }
}
