namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for <see cref="UnitDetailPanelState"/> (ASSET-008 / S107).
/// Unity hosts map presentation fields onto labels without re-formatting.
/// </summary>
public static class UnitDetailApplyState
{
    public static UnitDetailPresentation Apply(UnitDetailPanelState? state)
    {
        if (state is null)
        {
            return UnitDetailPresentation.Empty;
        }

        return new UnitDetailPresentation(
            UnitIdLine: state.UnitIdLine ?? string.Empty,
            StatusLine: state.StatusLine ?? string.Empty,
            MagazineLine: state.MagazineLine ?? string.Empty,
            EmconLine: state.EmconLine ?? string.Empty,
            DoctrineLine: state.DoctrineLine ?? string.Empty,
            FuelLine: state.FuelLine ?? string.Empty,
            EngagePreviewLine: state.EngagePreviewLine ?? string.Empty,
            AttackOptionsLine: state.AttackOptionsLine ?? string.Empty,
            ContactLine: state.ContactLine ?? string.Empty,
            AttackOptionCount: state.AttackMenu?.Count ?? 0);
    }

    public static UnitDetailPresentation BindAndApply(UnitDetailEntry? entry, string? contactLine = null)
        => Apply(UnitDetailPanelBinder.Bind(entry, contactLine));
}

public sealed record UnitDetailPresentation(
    string UnitIdLine,
    string StatusLine,
    string MagazineLine,
    string EmconLine,
    string DoctrineLine,
    string FuelLine,
    string EngagePreviewLine,
    string AttackOptionsLine,
    string ContactLine,
    int AttackOptionCount)
{
    public static UnitDetailPresentation Empty { get; } = new(
        "UNIT: —",
        "STATUS: —",
        "MAGAZINE: —",
        "EMCON: —",
        "DOCTRINE: —",
        "FUEL: —",
        "ENGAGE: —",
        "ATTACK: —",
        "CONTACT: —",
        0);
}
