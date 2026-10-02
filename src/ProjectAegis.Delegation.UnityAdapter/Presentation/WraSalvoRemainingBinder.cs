using System.Globalization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.EmploymentLedger;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-259: WRA salvo remaining vs doctrine max on weapon-panel chrome.
/// Binds employment ledger + policy facts only — hosts must not re-derive sim truth (ADR-010 §2–3).
/// </summary>
public sealed record WraSalvoRemainingState(
    string StateLine,
    int RemainingSalvo,
    int MaxSalvo,
    string? AbortReasonCode,
    bool IsExhausted,
    string DeclutterToken,
    string CueClass,
    bool IsFireOrder)
{
    /// <summary>Cleared weapon-panel salvo row when selection or facts are unavailable.</summary>
    public static WraSalvoRemainingState Empty { get; } = new(
        "WRA SALVO: —",
        0,
        0,
        null,
        true,
        string.Empty,
        WraSalvoCueClasses.Unknown,
        IsFireOrder: false);
}

/// <summary>Non-color USS cue tokens for WRA salvo rows (text + border class).</summary>
public static class WraSalvoCueClasses
{
    public const string Unknown = "wra-salvo-cue--unknown";
    public const string Nominal = "wra-salvo-cue--nominal";
    public const string Exhausted = "wra-salvo-cue--exhausted";

    /// <summary>All cue classes hosts must clear before applying the active row cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Unknown, Nominal, Exhausted };
}

/// <summary>Headless text declutter tokens paired with cue classes — never color-only state.</summary>
public static class WraSalvoDeclutterTokens
{
    public const string Unknown = "[WRA-SALVO:UNKNOWN]";
    public const string Ready = "[WRA-SALVO:READY]";
    public const string Exhausted = "[WRA-SALVO:EXHAUSTED]";
}

/// <summary>One live-surface WRA salvo label row with its USS cue class token.</summary>
public sealed record WraSalvoRemainingRow(string ElementName, string Text, string CueClass);

/// <summary>Headless row binder for weapon-panel WRA salvo labels and cue classes.</summary>
public static class WraSalvoRemainingPanelBinder
{
    /// <summary>Maps bound state into element names, text, and cue classes for UI Toolkit classList swaps.</summary>
    public static IReadOnlyList<WraSalvoRemainingRow> BindRows(WraSalvoRemainingState state) =>
    [
        new("wra-salvo-line", state.StateLine, state.CueClass),
    ];
}

/// <summary>Maps employment + policy + engage-preview facts into WRA salvo chrome.</summary>
public static class WraSalvoRemainingBinder
{
    /// <summary>
    /// Projects remaining vs max WRA salvo for the selected shooter. Advisory only — never a fire order.
    /// </summary>
    public static WraSalvoRemainingState Bind(
        string? shooterUnitId,
        in EngageContext engageContext,
        EffectivePolicy policy,
        EngagePreview? engagePreview = null)
    {
        if (string.IsNullOrWhiteSpace(shooterUnitId))
        {
            return WraSalvoRemainingState.Empty;
        }

        var requestedSalvo = Math.Max(1, engageContext.SalvoSize);
        var maxSalvo = Math.Max(0, policy.MaxSalvo);
        var ledger = EmploymentLedgerProjection.Project(
            new EmploymentLedgerMagazineFacts(
                shooterUnitId,
                CatalogWeaponIds.MvpDefault,
                engageContext.RoundsRemaining,
                requestedSalvo,
                LastEmploymentTick: 0));

        if (ledger.IsFireOrder)
        {
            throw new InvalidOperationException("Employment ledger must remain advisory for presentation bind.");
        }

        var withhold = ledger.Rows.Count > 0 ? ledger.Rows[0].WithholdReason : null;
        string? abort = withhold;
        int remaining;

        if (withhold != null)
        {
            remaining = 0;
        }
        else if (maxSalvo <= 0 || requestedSalvo > maxSalvo)
        {
            remaining = 0;
            abort = AbortReasonCatalog.Doctrine.WRA_SALVO;
        }
        else
        {
            remaining = Math.Min(requestedSalvo, maxSalvo);
            abort = null;
        }

        if (remaining <= 0 && abort == null && engagePreview?.AbortPreviewCode != null)
        {
            abort = engagePreview.AbortPreviewCode;
        }

        var exhausted = remaining <= 0;
        var cue = exhausted ? WraSalvoCueClasses.Exhausted : WraSalvoCueClasses.Nominal;
        var declutter = exhausted
            ? WraSalvoDeclutterTokens.Exhausted
            : (maxSalvo <= 0 ? WraSalvoDeclutterTokens.Unknown : WraSalvoDeclutterTokens.Ready);

        var line = string.Format(
            CultureInfo.InvariantCulture,
            "WRA SALVO: {0}/{1}",
            remaining,
            maxSalvo);
        if (exhausted && !string.IsNullOrEmpty(abort))
        {
            line += $" | {abort}";
        }

        return new WraSalvoRemainingState(
            line,
            remaining,
            maxSalvo,
            abort,
            exhausted,
            declutter,
            cue,
            IsFireOrder: false);
    }
}
