namespace ProjectAegis.Delegation.Projection;

/// <summary>PE-UX-W6: Standard-tier accessibility scale / reduced-motion stubs for C2 + Platform Editor.</summary>
public enum C2AccessibilityScalePercent
{
    OneHundred = 100,
    OneTwentyFive = 125,
    OneFifty = 150,
}

/// <summary>Bindable a11y settings applied as USS classes on UIDocument roots.</summary>
public sealed record C2AccessibilitySettings(
    C2AccessibilityScalePercent ScalePercent = C2AccessibilityScalePercent.OneHundred,
    bool ReducedMotion = false)
{
    public static C2AccessibilitySettings Default { get; } = new();

    public float FontScaleMultiplier => (int)ScalePercent / 100f;

    public string ScaleUssClass => ScalePercent switch
    {
        C2AccessibilityScalePercent.OneTwentyFive => "aegis-scale-125",
        C2AccessibilityScalePercent.OneFifty => "aegis-scale-150",
        _ => "aegis-scale-100",
    };

    public string RootUssClasses =>
        ReducedMotion ? "reduced-motion" : string.Empty;
}
