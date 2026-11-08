namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for chrome collapse title / affordance labels (CMD-23 adjacent).
/// Hosts map strings onto collapse toggle buttons without re-formatting.
/// </summary>
public static class C2ChromeCollapseApplyState
{
    public static C2ChromeCollapsePresentation Apply(C2ChromeCollapseState? state)
    {
        if (state is null)
        {
            return C2ChromeCollapsePresentation.Empty;
        }

        return new C2ChromeCollapsePresentation(
            MessageLogCollapsed: state.MessageLogCollapsed,
            LeftDrawerCollapsed: state.LeftDrawerCollapsed,
            MessageLogAffordanceLabel: C2ChromeCollapseProjection.MessageLogAffordanceLabel(
                state.MessageLogCollapsed),
            LeftDrawerAffordanceLabel: C2ChromeCollapseProjection.LeftDrawerAffordanceLabel(
                state.LeftDrawerCollapsed));
    }

    public static C2ChromeCollapsePresentation ProjectAndApply(
        bool messageLogCollapsed,
        bool leftDrawerCollapsed) =>
        Apply(C2ChromeCollapseProjection.Project(messageLogCollapsed, leftDrawerCollapsed));
}

/// <summary>Applied chrome collapse presentation fields.</summary>
public sealed record C2ChromeCollapsePresentation(
    bool MessageLogCollapsed,
    bool LeftDrawerCollapsed,
    string MessageLogAffordanceLabel,
    string LeftDrawerAffordanceLabel)
{
    public static C2ChromeCollapsePresentation Empty { get; } = new(
        MessageLogCollapsed: false,
        LeftDrawerCollapsed: false,
        MessageLogAffordanceLabel: C2ChromeCollapseProjection.CollapseMessageLogLabel,
        LeftDrawerAffordanceLabel: C2ChromeCollapseProjection.CollapseLeftDrawerLabel);
}
