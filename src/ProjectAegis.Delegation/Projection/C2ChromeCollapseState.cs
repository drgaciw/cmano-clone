namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// UI-local chrome collapse flags for message log and left drawer (CMD-23 adjacent / CMD-28 polish).
/// Pure presentation state — no host file I/O required.
/// </summary>
public sealed record C2ChromeCollapseState(
    bool MessageLogCollapsed,
    bool LeftDrawerCollapsed)
{
    public static C2ChromeCollapseState Expanded { get; } = new(
        MessageLogCollapsed: false,
        LeftDrawerCollapsed: false);

    public C2ChromeCollapseState ToggleMessageLog() =>
        this with { MessageLogCollapsed = !MessageLogCollapsed };

    public C2ChromeCollapseState ToggleLeftDrawer() =>
        this with { LeftDrawerCollapsed = !LeftDrawerCollapsed };

    public C2ChromeCollapseState SetMessageLogCollapsed(bool collapsed) =>
        this with { MessageLogCollapsed = collapsed };

    public C2ChromeCollapseState SetLeftDrawerCollapsed(bool collapsed) =>
        this with { LeftDrawerCollapsed = collapsed };
}
