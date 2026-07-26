namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for projected <see cref="C2TopBarState"/> into a presentation field bag
/// (S106). Unity hosts map these strings onto UI Toolkit labels without re-formatting.
/// </summary>
public static class C2TopBarApplyState
{
    /// <summary>
    /// Apply projected top-bar state into presentation fields. Null state → empty labels.
    /// Also derives comms CSS modifier from the projected comms label (presentation-only).
    /// </summary>
    public static C2TopBarPresentation Apply(C2TopBarState? state)
    {
        if (state is null)
        {
            return C2TopBarPresentation.Empty;
        }

        return new C2TopBarPresentation(
            SimTimeLabel: state.SimTimeLabel ?? string.Empty,
            PhaseLabel: state.PhaseLabel ?? string.Empty,
            CompressionLabel: state.CompressionLabel ?? string.Empty,
            ModeLabel: state.ModeLabel ?? string.Empty,
            CommsLabel: state.CommsLabel ?? string.Empty,
            ScoreLabel: state.ScoreLabel ?? string.Empty,
            CommsCssClass: ResolveCommsCssClass(state.CommsLabel));
    }

    /// <summary>
    /// Project then apply in one call — the real product path for headless top-bar refresh tests.
    /// </summary>
    public static C2TopBarPresentation ProjectAndApply(
        double simTimeSeconds,
        Orchestration.SimulationPhase phase,
        string compressionLabel,
        string simulationModeLabel,
        Decision.DecisionLog log,
        int baseScore = 0)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var projected = C2TopBarProjection.Project(
            simTimeSeconds,
            phase,
            compressionLabel,
            simulationModeLabel,
            log,
            baseScore);
        return Apply(projected);
    }

    public static string ResolveCommsCssClass(string? commsLabel)
    {
        if (string.IsNullOrEmpty(commsLabel))
        {
            return "c2-topbar-item--comms-nominal";
        }

        if (commsLabel.Contains("DENIED", StringComparison.Ordinal))
        {
            return "c2-topbar-item--comms-denied";
        }

        if (commsLabel.Contains("DEGRADED", StringComparison.Ordinal))
        {
            return "c2-topbar-item--comms-degraded";
        }

        return "c2-topbar-item--comms-nominal";
    }
}

/// <summary>Applied top-bar presentation fields (label text + comms CSS class).</summary>
public sealed record C2TopBarPresentation(
    string SimTimeLabel,
    string PhaseLabel,
    string CompressionLabel,
    string ModeLabel,
    string CommsLabel,
    string ScoreLabel,
    string CommsCssClass)
{
    public static C2TopBarPresentation Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        "c2-topbar-item--comms-nominal");
}
