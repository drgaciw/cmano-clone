using System.Globalization;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// S124-01 W2-C2-01: pre-commit constraint + magazine cost strip shown at fire commit.
/// Binds already-computed engage preview, attack-option, WRA salvo (DRG-259) and abort tooltip
/// (DRG-258) projections only — hosts must not re-derive sim truth (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record CommitConstraintStripState(
    string OptionId,
    bool CanCommit,
    string? PrimaryReasonCode,
    IReadOnlyList<CommitConstraintEntry> Constraints,
    int MagazineCost,
    int RoundsBefore,
    int RoundsAfter,
    string ConstraintLine,
    string CostLine,
    string CueClass,
    string DeclutterToken,
    bool IsFireOrder)
{
    /// <summary>Cleared strip when no commit option is armed.</summary>
    public static CommitConstraintStripState Empty { get; } = new(
        string.Empty,
        false,
        null,
        Array.Empty<CommitConstraintEntry>(),
        0,
        0,
        0,
        "COMMIT: —",
        "COST: —",
        CommitConstraintStripCueClasses.Empty,
        CommitConstraintStripDeclutterTokens.Empty,
        IsFireOrder: false);
}

/// <summary>One ranked constraint row (1 = most important) with the projection it came from.</summary>
public sealed record CommitConstraintEntry(int Rank, string Code, string Source);

/// <summary>Projection sources a strip constraint may come from, in precedence order.</summary>
public static class CommitConstraintSources
{
    /// <summary>Attack-option disabled reason — the code the command façade refuses with.</summary>
    public const string AttackOption = "attack-option";

    /// <summary><see cref="EngagePreview.AbortPreviewCode"/>.</summary>
    public const string EngagePreview = "engage-preview";

    /// <summary>DRG-259 <see cref="WraSalvoRemainingState.AbortReasonCode"/>.</summary>
    public const string WraSalvo = "wra-salvo";

    /// <summary>DRG-258 <see cref="WeaponAbortTooltipState.Reasons"/>.</summary>
    public const string AbortTooltip = "abort-tooltip";
}

/// <summary>Non-color USS cue tokens for the commit strip (text + border class).</summary>
public static class CommitConstraintStripCueClasses
{
    public const string Empty = "commit-strip-cue--empty";
    public const string Ready = "commit-strip-cue--ready";
    public const string Blocked = "commit-strip-cue--blocked";

    /// <summary>All cue classes hosts must clear before applying the active cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Empty, Ready, Blocked };
}

/// <summary>Headless text declutter tokens paired with cue classes — never color-only state.</summary>
public static class CommitConstraintStripDeclutterTokens
{
    public const string Empty = "[COMMIT:NONE]";
    public const string Ready = "[COMMIT:READY]";
    public const string Blocked = "[COMMIT:BLOCKED]";
}

/// <summary>One live-surface strip label row with its USS cue class token.</summary>
public sealed record CommitConstraintStripRow(string ElementName, string Text, string CueClass);

/// <summary>Merges existing commit-time projections into a top-N constraint + cost strip.</summary>
public static class CommitConstraintStripBinder
{
    /// <summary>Top-N cap: the strip lists at most this many constraints.</summary>
    public const int MaxConstraints = 3;

    /// <summary>UXML name for the constraint line.</summary>
    public const string ConstraintElementName = "commit-strip-constraints";

    /// <summary>UXML name for the magazine cost line.</summary>
    public const string CostElementName = "commit-strip-cost";

    /// <summary>Code shown for an option id absent from the attack menu (matches the order resolver).</summary>
    public const string UnknownOptionCode = "UNKNOWN_OPTION";

    private const string FireSingleOptionId = "fire-single";
    private const string FireSalvoOptionId = "fire-salvo";
    private const string HoldFireOptionId = "hold-fire";

    /// <summary>
    /// Binds the strip for <paramref name="optionId"/>. Constraints merge in fixed source order
    /// (attack option, engage preview, WRA salvo, abort tooltip), deduplicated ordinally and capped
    /// at <see cref="MaxConstraints"/>. When <paramref name="attackMenu"/> is null the menu is bound
    /// from <see cref="EngageAttackOptions"/> over the same context and preview the order resolver uses.
    /// Advisory only — never a fire order.
    /// </summary>
    public static CommitConstraintStripState Bind(
        string optionId,
        in EngageContext engageContext,
        EngagePreview preview,
        AttackOptionsPreviewState? attackMenu = null,
        WraSalvoRemainingState? wra = null,
        WeaponAbortTooltipState? abortTooltip = null)
    {
        if (optionId is null)
        {
            throw new ArgumentNullException(nameof(optionId));
        }

        if (preview is null)
        {
            throw new ArgumentNullException(nameof(preview));
        }

        var roundsBefore = Math.Max(0, engageContext.RoundsRemaining);
        var cost = CostOf(optionId, engageContext.SalvoSize);
        var roundsAfter = Math.Max(0, roundsBefore - cost);
        var costLine = FormatCost(cost, roundsBefore, roundsAfter);

        if (string.Equals(optionId, HoldFireOptionId, StringComparison.Ordinal))
        {
            return Build(optionId, true, null, Array.Empty<CommitConstraintEntry>(), cost, roundsBefore, roundsAfter, costLine);
        }

        var menu = attackMenu ?? AttackOptionsPreviewBinder.Bind(EngageAttackOptions.Build(in engageContext, preview));
        var row = AttackOptionsPreviewBinder.FindRow(menu, optionId);

        var top = new List<CommitConstraintEntry>(MaxConstraints);
        string? primary;
        bool canCommit;
        if (row is null)
        {
            canCommit = false;
            primary = UnknownOptionCode;
            cost = 0;
            roundsAfter = roundsBefore;
            costLine = FormatCost(cost, roundsBefore, roundsAfter);
            Consider(top, UnknownOptionCode, CommitConstraintSources.AttackOption);
        }
        else
        {
            canCommit = row.Enabled;
            primary = row.Enabled ? null : row.AbortReason;
            Consider(top, primary, CommitConstraintSources.AttackOption);
        }

        Consider(top, preview.AbortPreviewCode, CommitConstraintSources.EngagePreview);
        Consider(top, wra?.AbortReasonCode, CommitConstraintSources.WraSalvo);
        if (abortTooltip is { IsDenied: true })
        {
            var reasons = abortTooltip.Reasons;
            for (var i = 0; i < reasons.Count; i++)
            {
                Consider(top, reasons[i], CommitConstraintSources.AbortTooltip);
            }
        }

        return Build(optionId, canCommit, primary, top, cost, roundsBefore, roundsAfter, costLine);
    }

    /// <summary>Maps bound strip state into element names, text, and cue classes for UI Toolkit hosts.</summary>
    public static IReadOnlyList<CommitConstraintStripRow> BindRows(CommitConstraintStripState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return new[]
        {
            new CommitConstraintStripRow(ConstraintElementName, state.ConstraintLine, state.CueClass),
            new CommitConstraintStripRow(CostElementName, state.CostLine, state.CueClass),
        };
    }

    private static CommitConstraintStripState Build(
        string optionId,
        bool canCommit,
        string? primary,
        IReadOnlyList<CommitConstraintEntry> constraints,
        int cost,
        int roundsBefore,
        int roundsAfter,
        string costLine)
    {
        var line = canCommit ? "COMMIT: READY" : "COMMIT: BLOCKED";
        for (var i = 0; i < constraints.Count; i++)
        {
            line += " | " + constraints[i].Code;
        }

        return new CommitConstraintStripState(
            optionId,
            canCommit,
            primary,
            constraints,
            cost,
            roundsBefore,
            roundsAfter,
            line,
            costLine,
            canCommit ? CommitConstraintStripCueClasses.Ready : CommitConstraintStripCueClasses.Blocked,
            canCommit ? CommitConstraintStripDeclutterTokens.Ready : CommitConstraintStripDeclutterTokens.Blocked,
            IsFireOrder: false);
    }

    private static void Consider(List<CommitConstraintEntry> top, string? code, string source)
    {
        if (string.IsNullOrWhiteSpace(code) || top.Count >= MaxConstraints)
        {
            return;
        }

        for (var i = 0; i < top.Count; i++)
        {
            if (string.Equals(top[i].Code, code, StringComparison.Ordinal))
            {
                return;
            }
        }

        top.Add(new CommitConstraintEntry(top.Count + 1, code, source));
    }

    private static int CostOf(string optionId, int salvoSize)
    {
        if (string.Equals(optionId, FireSingleOptionId, StringComparison.Ordinal))
        {
            return 1;
        }

        return string.Equals(optionId, FireSalvoOptionId, StringComparison.Ordinal)
            ? Math.Max(1, salvoSize)
            : 0;
    }

    private static string FormatCost(int cost, int roundsBefore, int roundsAfter)
    {
        var line = string.Format(
            CultureInfo.InvariantCulture,
            "COST: {0} rd | MAG {1} -> {2}",
            cost,
            roundsBefore,
            roundsAfter);
        var shortfall = cost - roundsBefore;
        return shortfall > 0
            ? line + string.Format(CultureInfo.InvariantCulture, " (short {0})", shortfall)
            : line;
    }
}
