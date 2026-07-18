namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless binder for ASSET-012 Policy denial + EMCON HUD (Specced / S107).
/// </summary>
public static class PolicyEmconHudPanelBinder
{
    public static PolicyEmconHudPanelState BindIdle()
        => new("POLICY: —", "EMCON: —", "policy-emcon-hud", "policy-emcon-hud--idle");

    public static PolicyEmconHudPanelState Bind(string? policyLabel, string? emconLabel, bool isDenied)
    {
        var policy = string.IsNullOrWhiteSpace(policyLabel)
            ? "POLICY: —"
            : policyLabel.StartsWith("POLICY:", StringComparison.Ordinal)
                ? policyLabel
                : $"POLICY: {policyLabel.Trim()}";
        var emcon = string.IsNullOrWhiteSpace(emconLabel)
            ? "EMCON: —"
            : emconLabel.StartsWith("EMCON:", StringComparison.Ordinal)
                ? emconLabel
                : $"EMCON: {emconLabel.Trim()}";
        var css = isDenied ? "policy-emcon-hud--denied" : "policy-emcon-hud--nominal";
        return new PolicyEmconHudPanelState(policy, emcon, "policy-emcon-hud", css);
    }
}

public sealed record PolicyEmconHudPanelState(
    string PolicyLine,
    string EmconLine,
    string BaseCssClass,
    string StateCssClass);

public static class PolicyEmconHudApplyState
{
    public static PolicyEmconHudPresentation Apply(PolicyEmconHudPanelState? state)
    {
        if (state is null)
        {
            return PolicyEmconHudPresentation.Empty;
        }

        return new PolicyEmconHudPresentation(
            state.PolicyLine ?? string.Empty,
            state.EmconLine ?? string.Empty,
            state.BaseCssClass ?? "policy-emcon-hud",
            state.StateCssClass ?? "policy-emcon-hud--idle");
    }
}

public sealed record PolicyEmconHudPresentation(
    string PolicyLine,
    string EmconLine,
    string BaseCssClass,
    string StateCssClass)
{
    public static PolicyEmconHudPresentation Empty { get; } =
        new("POLICY: —", "EMCON: —", "policy-emcon-hud", "policy-emcon-hud--idle");
}
