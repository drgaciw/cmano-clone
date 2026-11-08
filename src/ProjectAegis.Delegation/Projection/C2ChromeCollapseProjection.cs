namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Projects chrome collapse affordance labels (CMD-23 adjacent / CMD-28 polish).
/// </summary>
public static class C2ChromeCollapseProjection
{
    public const string ExpandMessageLogLabel = "Expand Message Log";
    public const string CollapseMessageLogLabel = "Collapse Message Log";
    public const string ExpandLeftDrawerLabel = "Expand Left Drawer";
    public const string CollapseLeftDrawerLabel = "Collapse Left Drawer";

    public static C2ChromeCollapseState Project(
        bool messageLogCollapsed,
        bool leftDrawerCollapsed) =>
        new(messageLogCollapsed, leftDrawerCollapsed);

    public static string MessageLogAffordanceLabel(bool collapsed) =>
        collapsed ? ExpandMessageLogLabel : CollapseMessageLogLabel;

    public static string LeftDrawerAffordanceLabel(bool collapsed) =>
        collapsed ? ExpandLeftDrawerLabel : CollapseLeftDrawerLabel;
}
