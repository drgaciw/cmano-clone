using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-258 / REQ-13 ROE-07: hover/focus tooltip for a denied weapon row.
/// Up to <see cref="WeaponAbortTooltipBinder.MaxReasons"/> already-computed abort strings.
/// Reads explain and combat-detail DTOs only (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record WeaponAbortTooltipState(
    string TooltipText,
    IReadOnlyList<string> Reasons,
    bool IsDenied)
{
    /// <summary>No deny reasons to show.</summary>
    public static WeaponAbortTooltipState Empty { get; } = new(
        string.Empty,
        Array.Empty<string>(),
        false);
}

/// <summary>One weapon-row tooltip binding. Hosts copy <see cref="TooltipText"/> onto the row.</summary>
public sealed record WeaponAbortTooltipRow(
    string ElementName,
    string OptionId,
    string TooltipText,
    bool IsDenied);

/// <summary>
/// Binds top-3 <see cref="FireAbortReason"/> names (or equivalent abort-catalog strings)
/// from an existing explain projection and combat-detail card. Does not query the sim.
/// </summary>
public static class WeaponAbortTooltipBinder
{
    /// <summary>REQ-13 ROE-07 cap: hover lists at most this many blocking rules.</summary>
    public const int MaxReasons = 3;

    /// <summary>UXML name for the single-round weapon row.</summary>
    public const string FireSingleRowName = "weapon-row-fire-single";

    /// <summary>UXML name for the salvo weapon row.</summary>
    public const string FireSalvoRowName = "weapon-row-fire-salvo";

    /// <summary>Attack-menu option id for the single-round weapon button.</summary>
    public const string FireSingleOptionId = "fire-single";

    /// <summary>Attack-menu option id for the salvo weapon button.</summary>
    public const string FireSalvoOptionId = "fire-salvo";

    /// <summary>USS class for a denied weapon row that carries an abort tooltip.</summary>
    public const string DeniedRowClass = "unit-detail-weapon-row--denied";

    private const int EquivalentPriorityBase = 1000;

    private static readonly AbortToken[] Tokens = BuildTokens();

    /// <summary>
    /// Collects distinct abort strings already present on <paramref name="explain"/> and
    /// <paramref name="detail"/>. A clear (permitted) explain suppresses historical detail.
    /// </summary>
    public static WeaponAbortTooltipState Bind(EngageExplain? explain, CombatDetailPresentation? detail = null)
    {
        if (IsClear(explain))
        {
            return WeaponAbortTooltipState.Empty;
        }

        var useExplain = explain is { IsBlocked: true };
        var useDetail = detail is not null && detail != CombatDetailPresentation.Empty;
        if (!useExplain && !useDetail)
        {
            return WeaponAbortTooltipState.Empty;
        }

        var top = new Slot[MaxReasons];
        var count = 0;
        for (var i = 0; i < Tokens.Length; i++)
        {
            var token = Tokens[i];
            if (!Appears(token.Token, useExplain, explain, useDetail, detail))
            {
                continue;
            }

            Consider(top, ref count, token.Priority, token.Display);
        }

        if (count == 0)
        {
            return WeaponAbortTooltipState.Empty;
        }

        var reasons = new string[count];
        for (var i = 0; i < count; i++)
        {
            reasons[i] = top[i].Display;
        }

        return new WeaponAbortTooltipState(
            string.Join("\n", reasons),
            reasons,
            true);
    }

    /// <summary>Maps bound tooltip text onto the two weapon rows the unit panel already wires.</summary>
    public static IReadOnlyList<WeaponAbortTooltipRow> BindRows(WeaponAbortTooltipState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return new[]
        {
            new WeaponAbortTooltipRow(FireSingleRowName, FireSingleOptionId, state.TooltipText, state.IsDenied),
            new WeaponAbortTooltipRow(FireSalvoRowName, FireSalvoOptionId, state.TooltipText, state.IsDenied),
        };
    }

    /// <summary>True for fire weapon options. Hold-fire is not a weapon-abort row.</summary>
    public static bool AppliesTo(string? optionId) =>
        string.Equals(optionId, FireSingleOptionId, StringComparison.Ordinal)
        || string.Equals(optionId, FireSalvoOptionId, StringComparison.Ordinal);

    private static bool IsClear(EngageExplain? explain) =>
        explain is { IsBlocked: false }
        && string.Equals(explain.StatusLine, EngageExplainProjection.CanFireLabel, StringComparison.Ordinal);

    private static bool Appears(
        string token,
        bool useExplain,
        EngageExplain? explain,
        bool useDetail,
        CombatDetailPresentation? detail)
    {
        if (useExplain && explain is not null
            && (Hit(explain.ReasonCode, token)
                || Hit(explain.StatusLine, token)
                || Hit(explain.ReasonPlain, token)))
        {
            return true;
        }

        if (!useDetail || detail is null)
        {
            return false;
        }

        return Hit(detail.StatusLine, token)
            || Hit(detail.WeaponLine, token)
            || Hit(detail.HardConstraintsLine, token)
            || Hit(detail.PolicyLine, token)
            || Hit(detail.ConfidenceLine, token)
            || Hit(detail.FiringSolutionLine, token)
            || Hit(detail.NextActionLine, token)
            || Hit(detail.BdaLine, token)
            || Hit(detail.PostureLine, token)
            || Hit(detail.CorrelationLine, token);
    }

    private static void Consider(Slot[] top, ref int count, int priority, string display)
    {
        for (var i = 0; i < count; i++)
        {
            if (string.Equals(top[i].Display, display, StringComparison.Ordinal))
            {
                return;
            }
        }

        if (count < MaxReasons)
        {
            top[count] = new Slot(priority, display);
            count++;
            SortSlots(top, count);
            return;
        }

        if (priority >= top[MaxReasons - 1].Priority)
        {
            return;
        }

        top[MaxReasons - 1] = new Slot(priority, display);
        SortSlots(top, MaxReasons);
    }

    private static void SortSlots(Slot[] slots, int count)
    {
        for (var i = 1; i < count; i++)
        {
            var current = slots[i];
            var j = i - 1;
            while (j >= 0 && slots[j].Priority > current.Priority)
            {
                slots[j + 1] = slots[j];
                j--;
            }

            slots[j + 1] = current;
        }
    }

    private static bool Hit(string? text, string token) => IndexOfToken(text, token) >= 0;

    private static int IndexOfToken(string? text, string code)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(code) || text.Length < code.Length)
        {
            return -1;
        }

        var start = 0;
        while (start <= text.Length - code.Length)
        {
            var index = text.IndexOf(code, start, StringComparison.Ordinal);
            if (index < 0)
            {
                return -1;
            }

            var end = index + code.Length;
            var leftOk = index == 0 || !IsCodeChar(text[index - 1]);
            var rightOk = end == text.Length || !IsCodeChar(text[end]);
            if (leftOk && rightOk)
            {
                return index;
            }

            start = index + 1;
        }

        return -1;
    }

    // netstandard2.1: char.IsAsciiLetterOrDigit is .NET 5+ only.
    private static bool IsCodeChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_';

    private static AbortToken[] BuildTokens()
    {
        var list = new List<AbortToken>(64);
        foreach (FireAbortReason reason in Enum.GetValues(typeof(FireAbortReason)))
        {
            if (reason == FireAbortReason.None)
            {
                continue;
            }

            var display = reason.ToString();
            var priority = (int)reason;
            Add(list, display, display, priority);
            Add(list, ToLogCode(display), display, priority);
        }

        Add(
            list,
            AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT,
            nameof(FireAbortReason.WeaponsTight),
            (int)FireAbortReason.WeaponsTight);

        var equivalent = new[]
        {
            AbortReasonCatalog.Engage.MOUNT_OFFLINE,
            AbortReasonCatalog.Engage.DLZ_OUT,
            AbortReasonCatalog.Engage.OUT_OF_ENVELOPE,
            AbortReasonCatalog.Engage.NO_AMMO,
            AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE,
            AbortReasonCatalog.Engage.SHOTGUN_ORDNANCE,
            AbortReasonCatalog.Engage.AIR_NOT_READY,
            AbortReasonCatalog.Engage.CYBER_SPOOF_TRACK,
            AbortReasonCatalog.Engage.TARGET_DESTROYED,
            AbortReasonCatalog.Engage.SHOOTER_DESTROYED,
            AbortReasonCatalog.Engage.DOMAIN_NO_SOLUTION,
            AbortReasonCatalog.Engage.BLACK_PROJECT_REQUIRED,
            AbortReasonCatalog.Engage.TECHNOLOGY_LEVEL_EXCEEDED,
            AbortReasonCatalog.Engage.DAMAGE_WITHDRAW_RECOMMENDED,
            AbortReasonCatalog.Engage.BINGO_FUEL,
            AbortReasonCatalog.Engage.CEC_REMOTE_TRACK_UNAVAILABLE,
            AbortReasonCatalog.Logistics.STRIKE_UNREACHABLE,
            AbortReasonCatalog.Logistics.STRIKE_UNREACHABLE_FUEL,
            AbortReasonCatalog.Logistics.FERRY_UNREACHABLE,
            AbortReasonCatalog.Logistics.FERRY_UNREACHABLE_FUEL,
            AbortReasonCatalog.Sensor.SENSOR_OFFLINE,
            AbortReasonCatalog.Sensor.SENSOR_EMCON_BLOCKED,
            AbortReasonCatalog.Sensor.DATALINK_STALE,
            AbortReasonCatalog.Sensor.TRACK_STALE,
            AbortReasonCatalog.Cyber.CYBER_LINK_DEGRADED,
            AbortReasonCatalog.Cyber.CYBER_LINK_DOWN,
            AbortReasonCatalog.Cyber.CYBER_ORDER_DELAY,
        };

        for (var i = 0; i < equivalent.Length; i++)
        {
            Add(list, equivalent[i], equivalent[i], EquivalentPriorityBase + i);
        }

        return list.ToArray();
    }

    private static void Add(List<AbortToken> list, string token, string display, int priority)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i].Token, token, StringComparison.Ordinal))
            {
                return;
            }
        }

        list.Add(new AbortToken(token, display, priority));
    }

    private static string ToLogCode(string enumName)
    {
        var chars = new char[enumName.Length * 2];
        var n = 0;
        for (var i = 0; i < enumName.Length; i++)
        {
            var c = enumName[i];
            if (i > 0 && c >= 'A' && c <= 'Z')
            {
                chars[n++] = '_';
            }

            chars[n++] = c >= 'a' && c <= 'z' ? (char)(c - 32) : c;
        }

        return new string(chars, 0, n);
    }

    private readonly struct AbortToken
    {
        public AbortToken(string token, string display, int priority)
        {
            Token = token;
            Display = display;
            Priority = priority;
        }

        public string Token { get; }

        public string Display { get; }

        public int Priority { get; }
    }

    private readonly struct Slot
    {
        public Slot(int priority, string display)
        {
            Priority = priority;
            Display = display;
        }

        public int Priority { get; }

        public string Display { get; }
    }
}
