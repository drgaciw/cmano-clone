namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Skills;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Headless apply path for <see cref="UnitDetailPanelState"/> (ASSET-008 / S107).
/// Unity hosts map presentation fields onto labels without re-formatting.
/// CMD-17: additive <see cref="UnitDetailPresentation.CommsLine"/>.
/// S122-07 / DRG-182: additive <see cref="UnitDetailPresentation.AuthorityLine"/>;
/// doctrine-line is ROE chrome via <see cref="RoeProjection.FormatRoeLabel"/>.
/// </summary>
public static class UnitDetailApplyState
{
    public const string EmptyRoeLine = "ROE: —";
    public const string EmptyAuthorityLine = "AUTH: —";

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
            DoctrineLine: FormatRoeLine(state.DoctrineLine),
            FuelLine: state.FuelLine ?? string.Empty,
            EngagePreviewLine: state.EngagePreviewLine ?? string.Empty,
            AttackOptionsLine: state.AttackOptionsLine ?? string.Empty,
            ContactLine: state.ContactLine ?? string.Empty,
            AttackOptionCount: state.AttackMenu?.Count ?? 0,
            CommsLine: state.CommsLine ?? "COMMS: —",
            AuthorityLine: FormatAuthorityLine(state.AuthorityLine));
    }

    public static UnitDetailPresentation BindAndApply(UnitDetailEntry? entry, string? contactLine = null)
        => Apply(UnitDetailPanelBinder.Bind(entry, contactLine));

    /// <summary>Right-panel doctrine-line: <c>ROE: WEAPONS_FREE</c> / HOLD_FIRE / WEAPONS_TIGHT.</summary>
    public static string FormatRoeLine(RoeLevel? roe) =>
        roe is null ? EmptyRoeLine : $"ROE: {RoeProjection.FormatRoeLabel(roe.Value)}";

    /// <summary>
    /// Format from an existing unit-detail doctrine/ROE label
    /// (<c>DOCTRINE: WeaponsFree</c>, <c>ROE: WEAPONS_FREE</c>, or empty placeholder).
    /// </summary>
    public static string FormatRoeLine(string? doctrineOrRoeLabel)
    {
        var value = ExtractLabelValue(doctrineOrRoeLabel);
        if (IsEmptyPlaceholder(value))
        {
            return EmptyRoeLine;
        }

        return FormatRoeLine(C2AuthorityProjector.ParseRoeLabel(value));
    }

    /// <summary>Right-panel authority-line from track source: <c>AUTH: ORGANIC</c>.</summary>
    public static string FormatAuthorityLine(TrackSource? source) =>
        source is null || source == TrackSource.Unknown
            ? EmptyAuthorityLine
            : $"AUTH: {source.Value.ToString().ToUpperInvariant()}";

    /// <summary>Right-panel authority-line from projector disposition: <c>AUTH: Permitted</c> / Withheld.</summary>
    public static string FormatAuthorityLine(C2AuthorityDisposition? disposition) =>
        disposition is null ? EmptyAuthorityLine : $"AUTH: {disposition.Value}";

    /// <summary>Pass through or normalize an existing <c>AUTH:</c> label.</summary>
    public static string FormatAuthorityLine(string? authorityLabel)
    {
        var value = ExtractLabelValue(authorityLabel);
        if (IsEmptyPlaceholder(value))
        {
            return EmptyAuthorityLine;
        }

        if (Enum.TryParse<TrackSource>(value, ignoreCase: true, out var source) &&
            source != TrackSource.Unknown)
        {
            return FormatAuthorityLine((TrackSource?)source);
        }

        if (Enum.TryParse<C2AuthorityDisposition>(value, ignoreCase: true, out var disposition))
        {
            return FormatAuthorityLine((C2AuthorityDisposition?)disposition);
        }

        return $"AUTH: {value}";
    }

    private static string ExtractLabelValue(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        var trimmed = label.Trim();
        var colon = trimmed.IndexOf(':');
        var value = colon >= 0 ? trimmed[(colon + 1)..] : trimmed;
        value = value.Trim();
        var paren = value.IndexOf('(');
        return paren >= 0 ? value[..paren].Trim() : value;
    }

    private static bool IsEmptyPlaceholder(string value) =>
        value.Length == 0 || value == "—" || value == "-" || value == "–";
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
    int AttackOptionCount,
    string CommsLine = "COMMS: —",
    string AuthorityLine = "AUTH: —")
{
    public static UnitDetailPresentation Empty { get; } = new(
        "UNIT: —",
        "STATUS: —",
        "MAGAZINE: —",
        "EMCON: —",
        "ROE: —",
        "FUEL: —",
        "ENGAGE: —",
        "ATTACK: —",
        "CONTACT: —",
        0,
        "COMMS: —",
        "AUTH: —");
}
