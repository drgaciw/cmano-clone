namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless binder for ASSET-011 Delegation badge overlay (Specced / S107).
/// Presentation-only — no sim hot-path.
/// </summary>
public static class DelegationBadgePanelBinder
{
    public static DelegationBadgePanelState BindIdle()
        => new("DELEGATION: —", "delegation-badge", "delegation-badge--idle");

    public static DelegationBadgePanelState Bind(string? modeLabel, bool isHumanControlled)
    {
        if (string.IsNullOrWhiteSpace(modeLabel))
        {
            return BindIdle();
        }

        var label = $"DELEGATION: {modeLabel.Trim()}";
        var css = isHumanControlled
            ? "delegation-badge--human"
            : "delegation-badge--agent";
        return new DelegationBadgePanelState(label, "delegation-badge", css);
    }
}

public sealed record DelegationBadgePanelState(
    string BadgeLabel,
    string BaseCssClass,
    string StateCssClass);

/// <summary>Apply path for hosts / tests.</summary>
public static class DelegationBadgeApplyState
{
    public static DelegationBadgePresentation Apply(DelegationBadgePanelState? state)
    {
        if (state is null)
        {
            return DelegationBadgePresentation.Empty;
        }

        return new DelegationBadgePresentation(
            state.BadgeLabel ?? string.Empty,
            state.BaseCssClass ?? "delegation-badge",
            state.StateCssClass ?? "delegation-badge--idle");
    }
}

public sealed record DelegationBadgePresentation(
    string BadgeLabel,
    string BaseCssClass,
    string StateCssClass)
{
    public static DelegationBadgePresentation Empty { get; } =
        new("DELEGATION: —", "delegation-badge", "delegation-badge--idle");
}
