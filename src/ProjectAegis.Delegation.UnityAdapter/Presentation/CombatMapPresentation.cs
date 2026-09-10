namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

using System.Collections.ObjectModel;
using CombatEvents;
using Projection;

/// <summary>Presentation density used when projecting combat legs onto the map.</summary>
public enum CombatZoomBand
{
    Tactical = 1,
    Operational = 2,
    Theater = 3,
}

/// <summary>One immutable, presentation-only combat effect row.</summary>
public sealed record CombatMapEffect(
    string Key,
    ulong CorrelationId,
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    string FamilyGlyph,
    string LinePattern,
    string MotionLabel,
    string Affiliation,
    string Clearance,
    string Outcome,
    CombatEventPhase Phase,
    string Label,
    float FromX,
    float FromY,
    float ToX,
    float ToY,
    int Count,
    IReadOnlyList<string> CorrelationKeys);

/// <summary>One readable combat history row.</summary>
public sealed record CombatMapEventLine(
    string Key,
    ulong CorrelationId,
    double SimTime,
    CombatEventPhase Phase,
    string Text);

/// <summary>Immutable combat-map effects and their chronological event history.</summary>
public sealed record CombatMapPresentation(
    IReadOnlyList<CombatMapEffect> Effects,
    IReadOnlyList<CombatMapEventLine> EventLines)
{
    public static CombatMapPresentation Empty { get; } =
        new(Array.Empty<CombatMapEffect>(), Array.Empty<CombatMapEventLine>());
}

/// <summary>
/// Builds read-only combat map state from replay-stable combat facts and map symbols.
/// This presenter never writes simulation truth (ADR-010, ADR-007, ADR-001).
/// </summary>
public static class CombatMapPresenter
{
    private const string Cleared = "Cleared";
    private const string Pending = "Pending";
    private const string Refused = "Refused";

    /// <summary>Returns the collision-safe presentation identity for a combat leg.</summary>
    public static string KeyFor(CombatEvent evt)
    {
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }
        return KeyFor(evt.ShooterId, evt.TargetId, evt.CorrelationId);
    }

    /// <summary>Returns the collision-safe presentation identity for a combat leg.</summary>
    public static string KeyFor(string shooterId, string targetId, ulong correlationId)
    {
        if (shooterId is null)
        {
            throw new ArgumentNullException(nameof(shooterId));
        }

        if (targetId is null)
        {
            throw new ArgumentNullException(nameof(targetId));
        }

        return FormattableString.Invariant(
            $"combat|{shooterId.Length}:{shooterId}|{correlationId}|{targetId.Length}:{targetId}");
    }

    /// <summary>Builds a deterministic and bounded combat-map presentation as of simulation time.</summary>
    public static CombatMapPresentation Build(
        CombatEventSnapshot snapshot,
        IReadOnlyList<MapSymbolEntry> symbols,
        double nowSimTime,
        CombatZoomBand zoom,
        string? selectedKey = null,
        int maxEffects = 64,
        double holdSeconds = 6)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }
        if (double.IsNaN(nowSimTime) || double.IsInfinity(nowSimTime))
        {
            throw new ArgumentOutOfRangeException(nameof(nowSimTime));
        }

        if (maxEffects < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEffects));
        }

        if (holdSeconds < 0 || double.IsNaN(holdSeconds) || double.IsInfinity(holdSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(holdSeconds));
        }

        var positions = BuildPositionIndex(symbols);
        var history = new List<(CombatEvent Event, int InputIndex)>();
        var lastByLeg = new Dictionary<string, (CombatEvent Event, int InputIndex)>(StringComparer.Ordinal);

        for (var i = 0; i < snapshot.Events.Count; i++)
        {
            var evt = snapshot.Events[i];
            if (evt is null || evt.SimTime > nowSimTime)
            {
                continue;
            }

            history.Add((evt, i));
            var key = KeyFor(evt);
            if (!lastByLeg.TryGetValue(key, out var current)
                || evt.SimTime > current.Event.SimTime
                || (evt.SimTime.Equals(current.Event.SimTime) && i > current.InputIndex))
            {
                lastByLeg[key] = (evt, i);
            }
        }

        if (history.Count == 0)
        {
            return CombatMapPresentation.Empty;
        }

        history.Sort(static (a, b) =>
        {
            var time = a.Event.SimTime.CompareTo(b.Event.SimTime);
            return time != 0 ? time : a.InputIndex.CompareTo(b.InputIndex);
        });

        var eventLines = new CombatMapEventLine[history.Count];
        for (var i = 0; i < history.Count; i++)
        {
            eventLines[i] = ToEventLine(history[i].Event);
        }

        var candidates = new List<CombatMapEffect>(lastByLeg.Count);
        foreach (var pair in lastByLeg.OrderBy(static x => x.Key, StringComparer.Ordinal))
        {
            var evt = pair.Value.Event;
            if ((!IsFirePhase(evt.Phase) && !string.Equals(pair.Key, selectedKey, StringComparison.Ordinal))
                || (!string.Equals(pair.Key, selectedKey, StringComparison.Ordinal)
                    && nowSimTime - evt.SimTime > holdSeconds)
                || !positions.TryGetValue(evt.ShooterId, out var from)
                || !positions.TryGetValue(evt.TargetId, out var to))
            {
                continue;
            }

            candidates.Add(ToEffect(pair.Key, evt, from, to, positions[evt.ShooterId].Affiliation));
        }

        IReadOnlyList<CombatMapEffect> projected = zoom == CombatZoomBand.Tactical
            ? candidates
            : Aggregate(candidates, zoom, selectedKey);
        var bounded = Bound(projected, selectedKey, maxEffects);
        return new CombatMapPresentation(bounded, Array.AsReadOnly(eventLines));
    }

    private static IReadOnlyDictionary<string, (float X, float Y, string Affiliation)> BuildPositionIndex(
        IReadOnlyList<MapSymbolEntry> symbols)
    {
        var result = new Dictionary<string, (float X, float Y, string Affiliation)>(StringComparer.Ordinal);
        for (var i = 0; i < symbols.Count; i++)
        {
            var symbol = symbols[i];
            if (symbol is not null
                && !string.IsNullOrWhiteSpace(symbol.SymbolId)
                && IsFinite(symbol.NormalizedX)
                && IsFinite(symbol.NormalizedY))
            {
                result.TryAdd(symbol.SymbolId, (symbol.NormalizedX, symbol.NormalizedY, symbol.Affiliation));
            }
        }

        return result;
    }

    private static CombatMapEffect ToEffect(
        string key,
        CombatEvent evt,
        (float X, float Y, string Affiliation) from,
        (float X, float Y, string Affiliation) to,
        string affiliation)
    {
        var clearance = ClearanceFor(evt.Phase);
        var family = IsFirePhase(evt.Phase)
            ? ResolveFamily(evt.WeaponFamilyId)
            : evt.Phase == CombatEventPhase.AuthorizationRefused
                ? (Glyph: "⊘", Pattern: "none", Motion: "Static")
                : (Glyph: "○", Pattern: "none", Motion: "Static");
        var label = FormattableString.Invariant(
            $"{family.Glyph} {evt.ShooterId} → {evt.TargetId} | {affiliation} | {evt.WeaponFamilyId} | {family.Motion} | {clearance} | {evt.Outcome} | {PhaseText(evt.Phase)}");
        return new CombatMapEffect(
            key, evt.CorrelationId, evt.ShooterId, evt.TargetId, evt.WeaponFamilyId,
            family.Glyph, family.Pattern, family.Motion, affiliation, clearance, evt.Outcome,
            evt.Phase, label, from.X, from.Y, to.X, to.Y, 1,
            Array.AsReadOnly(new[] { key }));
    }

    private static IReadOnlyList<CombatMapEffect> Aggregate(
        IReadOnlyList<CombatMapEffect> source,
        CombatZoomBand zoom,
        string? selectedKey)
    {
        var result = new List<CombatMapEffect>();
        var groups = new SortedDictionary<string, List<CombatMapEffect>>(StringComparer.Ordinal);
        for (var i = 0; i < source.Count; i++)
        {
            var effect = source[i];
            if (string.Equals(effect.Key, selectedKey, StringComparison.Ordinal))
            {
                result.Add(effect);
                continue;
            }

            var groupKey = zoom == CombatZoomBand.Operational
                ? JoinKey(
                    effect.ShooterId,
                    effect.TargetId,
                    effect.WeaponFamilyId,
                    effect.Affiliation,
                    effect.Clearance,
                    effect.Outcome,
                    ((int)effect.Phase).ToString(System.Globalization.CultureInfo.InvariantCulture))
                : JoinKey(effect.WeaponFamilyId, effect.Affiliation, effect.Clearance, effect.Outcome);
            if (!groups.TryGetValue(groupKey, out var group))
            {
                group = new List<CombatMapEffect>();
                groups.Add(groupKey, group);
            }

            group.Add(effect);
        }

        foreach (var pair in groups)
        {
            var group = pair.Value;
            var first = group[0];
            if (group.Count == 1)
            {
                result.Add(first);
                continue;
            }

            var keys = group.Select(static x => x.Key).OrderBy(static x => x, StringComparer.Ordinal).ToArray();
            var prefix = zoom == CombatZoomBand.Theater ? "Theater" : "Operational";
            result.Add(first with
            {
                Key = $"aggregate|{pair.Key}",
                LinePattern = zoom == CombatZoomBand.Theater ? "none" : first.LinePattern,
                MotionLabel = zoom == CombatZoomBand.Theater ? "Static" : first.MotionLabel,
                Label = FormattableString.Invariant($"{prefix} summary: {first.FamilyGlyph} {first.WeaponFamilyId} ×{group.Count} | {first.Affiliation} | {first.Clearance} | {first.Outcome} | {(zoom == CombatZoomBand.Theater ? "Static" : first.MotionLabel)}"),
                Count = group.Count,
                CorrelationKeys = Array.AsReadOnly(keys),
            });
        }

        result.Sort(static (a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
        return result;
    }

    private static IReadOnlyList<CombatMapEffect> Bound(
        IReadOnlyList<CombatMapEffect> source,
        string? selectedKey,
        int maxEffects)
    {
        if (maxEffects == 0 || source.Count == 0)
        {
            return Array.Empty<CombatMapEffect>();
        }

        var ordered = source.OrderBy(static x => x.Key, StringComparer.Ordinal).ToList();
        if (ordered.Count <= maxEffects)
        {
            return new ReadOnlyCollection<CombatMapEffect>(ordered);
        }

        var selected = ordered.FindIndex(x => string.Equals(x.Key, selectedKey, StringComparison.Ordinal));
        var result = ordered.Take(maxEffects).ToList();
        if (selected >= maxEffects)
        {
            result[result.Count - 1] = ordered[selected];
            result.Sort(static (a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
        }

        return new ReadOnlyCollection<CombatMapEffect>(result);
    }

    private static CombatMapEventLine ToEventLine(CombatEvent evt)
    {
        var key = KeyFor(evt);
        var text = FormattableString.Invariant(
            $"T+{evt.SimTime:F3} {evt.ShooterId} → {evt.TargetId} | {evt.WeaponFamilyId} | {PhaseText(evt.Phase)} | {evt.Outcome}");
        return new CombatMapEventLine(key, evt.CorrelationId, evt.SimTime, evt.Phase, text);
    }

    private static bool IsFirePhase(CombatEventPhase phase) =>
        phase is CombatEventPhase.Firing or CombatEventPhase.InFlight or CombatEventPhase.TerminalOutcome;

    private static string ClearanceFor(CombatEventPhase phase) =>
        phase switch
        {
            CombatEventPhase.AuthorizationRefused => Refused,
            CombatEventPhase.Authorized or CombatEventPhase.Firing or CombatEventPhase.InFlight or CombatEventPhase.TerminalOutcome => Cleared,
            _ => Pending,
        };

    private static string PhaseText(CombatEventPhase phase) =>
        phase switch
        {
            CombatEventPhase.IntentAccepted => "Intent accepted",
            CombatEventPhase.Authorized => "Authorized",
            CombatEventPhase.AuthorizationRefused => "Authorization refused",
            CombatEventPhase.Firing => "Firing",
            CombatEventPhase.InFlight => "In flight",
            CombatEventPhase.TerminalOutcome => "Terminal outcome",
            _ => "Unknown phase",
        };

    private static (string Glyph, string Pattern, string Motion) ResolveFamily(string familyId)
    {
        if (string.Equals(familyId, "missile", StringComparison.OrdinalIgnoreCase))
        {
            return ("➤", "dash", "Track");
        }

        if (string.Equals(familyId, "gun", StringComparison.OrdinalIgnoreCase))
        {
            return ("✦", "dot", "Pulse");
        }

        if (string.Equals(familyId, "laser", StringComparison.OrdinalIgnoreCase))
        {
            return ("═", "double-solid", "Beam");
        }

        return ("?", "solid", "Static");
    }

    private static string JoinKey(params string[] values) =>
        string.Join("|", values.Select(static value => $"{value.Length}:{value}"));

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
