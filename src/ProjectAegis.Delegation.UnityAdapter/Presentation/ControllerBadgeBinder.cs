using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Presentation-only controller badges for map symbols and OOB rows (DEL-01 / DRG-374).
/// Reads <see cref="ControllerBadgeProjection"/>; never mutates sim state or controller slots
/// (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class ControllerBadgeBinder
{
    /// <summary>UI Toolkit element-name prefix. Full name is <see cref="ElementName"/>.</summary>
    public const string ElementNamePrefix = "controller-badge-";

    /// <summary>Class on the map-symbol badge label.</summary>
    public const string MapBadgeClass = "map-controller-badge";

    /// <summary>Class on an OOB row that shows a visible controller badge.</summary>
    public const string OobBadgeClass = "oob-controller-badge";

    private const string FriendlyToken = "map-symbol--friendly";
    private const string FriendlyDeadToken = "map-symbol--friendly-dead";

    /// <summary>UI Toolkit name for the badge child of <paramref name="unitId"/>.</summary>
    public static string ElementName(string unitId) => ElementNamePrefix + unitId;

    /// <summary>
    /// Project roster rows into a unit-id lookup. Null or empty input returns an empty dictionary.
    /// Later rows win when a unit id repeats. <see cref="ControllerBadgeKind.None"/> stays in the
    /// map with <see cref="ControllerBadgeBinding.Visible"/> false.
    /// </summary>
    public static IReadOnlyDictionary<string, ControllerBadgeBinding> BindRoster(IReadOnlyList<AgentRosterEntry>? roster)
    {
        var badges = ControllerBadgeProjection.Project(roster);
        if (badges.Count == 0)
        {
            return new Dictionary<string, ControllerBadgeBinding>(StringComparer.Ordinal);
        }

        var map = new Dictionary<string, ControllerBadgeBinding>(badges.Count, StringComparer.Ordinal);
        for (var i = 0; i < badges.Count; i++)
        {
            var badge = badges[i];
            if (string.IsNullOrEmpty(badge.UnitId))
            {
                continue;
            }

            map[badge.UnitId] = FromBadge(badge);
        }

        return map;
    }

    /// <summary>Lookup a binding. Missing ids and null inputs return false.</summary>
    public static bool TryGet(
        IReadOnlyDictionary<string, ControllerBadgeBinding>? bindings,
        string? unitId,
        out ControllerBadgeBinding? binding)
    {
        if (bindings is null || string.IsNullOrEmpty(unitId))
        {
            binding = null;
            return false;
        }

        if (bindings.TryGetValue(unitId, out var found))
        {
            binding = found;
            return true;
        }

        binding = null;
        return false;
    }

    /// <summary>
    /// Append <c> [HUM]</c> / <c> [AGT]</c> / <c> [AGT-SUSP]</c> when the binding is visible.
    /// Hidden and missing bindings leave the line unchanged.
    /// </summary>
    public static string DecorateOobLine(string? displayLine, ControllerBadgeBinding? binding)
    {
        var line = displayLine ?? string.Empty;
        if (binding is not { Visible: true })
        {
            return line;
        }

        return line + binding.OobSuffix;
    }

    /// <summary>
    /// True when <paramref name="styleClass"/> contains the friendly or friendly-destroyed token.
    /// Hostile and other affiliations do not match.
    /// </summary>
    public static bool IsFriendlyMapSymbol(string? styleClass)
    {
        if (string.IsNullOrEmpty(styleClass))
        {
            return false;
        }

        var start = 0;
        for (var i = 0; i <= styleClass.Length; i++)
        {
            if (i != styleClass.Length && styleClass[i] != ' ')
            {
                continue;
            }

            var length = i - start;
            if (IsFriendlyToken(styleClass, start, length))
            {
                return true;
            }

            start = i + 1;
        }

        return false;
    }

    /// <summary>
    /// Bind a map symbol only when it is friendly (or friendly-destroyed) and the roster badge is visible.
    /// Hostile symbols and unknown units return false.
    /// </summary>
    public static bool TryBindMapSymbol(
        string? symbolId,
        string? styleClass,
        IReadOnlyDictionary<string, ControllerBadgeBinding>? bindings,
        out ControllerBadgeBinding? binding)
    {
        if (!IsFriendlyMapSymbol(styleClass)
            || !TryGet(bindings, symbolId, out binding)
            || binding is not { Visible: true })
        {
            binding = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Roster to paint. Live command uses <paramref name="liveRoster"/>. A historical review uses
    /// <paramref name="asOfRoster"/> captured at that event's tick. A null as-of roster is unknown
    /// and must not fall back to the live roster (no future facts).
    /// </summary>
    public static IReadOnlyList<AgentRosterEntry>? RosterForBadges(
        bool inspecting,
        IReadOnlyList<AgentRosterEntry>? liveRoster,
        IReadOnlyList<AgentRosterEntry>? asOfRoster) =>
        inspecting ? asOfRoster : liveRoster;

    /// <summary>
    /// 64-bit content stamp (count, unit id, mode, agent id). Equal stamps are not proof of equal
    /// rosters — confirm with <see cref="SameRoster"/> via <see cref="NeedsRebind"/>.
    /// </summary>
    public static long RosterStamp(IReadOnlyList<AgentRosterEntry>? roster)
    {
        if (roster is null || roster.Count == 0)
        {
            return 0;
        }

        unchecked
        {
            long hash = roster.Count;
            for (var i = 0; i < roster.Count; i++)
            {
                var entry = roster[i];
                hash = (hash * 31) ^ Mix(entry.UnitId);
                hash = (hash * 31) ^ Mix(entry.ModeLabel);
                hash = (hash * 31) ^ Mix(entry.AgentId);
            }

            return hash;
        }
    }

    /// <summary>Structural equality of the fields that change a controller badge, in roster order.</summary>
    public static bool SameRoster(IReadOnlyList<AgentRosterEntry>? left, IReadOnlyList<AgentRosterEntry>? right)
    {
        var leftCount = left?.Count ?? 0;
        var rightCount = right?.Count ?? 0;
        if (leftCount != rightCount)
        {
            return false;
        }

        for (var i = 0; i < leftCount; i++)
        {
            var a = left![i];
            var b = right![i];
            if (!string.Equals(a.UnitId, b.UnitId, StringComparison.Ordinal)
                || !string.Equals(a.ModeLabel, b.ModeLabel, StringComparison.Ordinal)
                || !string.Equals(a.AgentId, b.AgentId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// True when bindings must be rebuilt. A matching stamp still rebinds when roster content differs,
    /// so a 64-bit polynomial collision cannot keep a stale tooltip.
    /// </summary>
    public static bool NeedsRebind(
        bool hasApplied,
        long appliedStamp,
        IReadOnlyList<AgentRosterEntry>? applied,
        IReadOnlyList<AgentRosterEntry>? next)
    {
        if (!hasApplied)
        {
            return true;
        }

        if (RosterStamp(next) != appliedStamp)
        {
            return true;
        }

        return !SameRoster(applied, next);
    }

    private static ControllerBadgeBinding FromBadge(ControllerBadge badge)
    {
        var visible = badge.Kind != ControllerBadgeKind.None;
        return new ControllerBadgeBinding(
            badge.UnitId,
            badge.ShortText,
            badge.AccessibleLabel,
            badge.ColorToken,
            "badge-shape-" + badge.Shape,
            visible,
            visible ? " [" + badge.ShortText + "]" : string.Empty);
    }

    private static bool IsFriendlyToken(string styleClass, int start, int length)
    {
        if (length == FriendlyToken.Length
            && string.Compare(styleClass, start, FriendlyToken, 0, length, StringComparison.Ordinal) == 0)
        {
            return true;
        }

        return length == FriendlyDeadToken.Length
            && string.Compare(styleClass, start, FriendlyDeadToken, 0, length, StringComparison.Ordinal) == 0;
    }

    private static int Mix(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        unchecked
        {
            var hash = value.Length;
            for (var i = 0; i < value.Length; i++)
            {
                hash = (hash * 31) + value[i];
            }

            return hash;
        }
    }
}

/// <summary>Applied controller badge for one unit. Colour, shape, and text are independent channels.</summary>
public sealed record ControllerBadgeBinding(
    string UnitId,
    string ShortText,
    string Tooltip,
    string ColorClass,
    string ShapeClass,
    bool Visible,
    string OobSuffix);
