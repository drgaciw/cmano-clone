namespace ProjectAegis.Delegation.Projection;

using CombatEvents;
using ProjectAegis.Sim.Engage;

/// <summary>
/// Presentation-only CMO-style combat VFX (Track C, DRG-403).
/// Projects terminal rows from a <see cref="CombatEventSnapshot"/> into transient fire lines
/// and impact markers. The snapshot has no sim RNG, so this path cannot read a Pk draw (ADR-010).
/// </summary>
public static class CombatVfxProjection
{
    /// <summary>Sim-time hold for shooter→target fire lines.</summary>
    public const double FireLineHoldSeconds = 4.0;

    /// <summary>Sim-time hold for impact markers. Longer than the fire line, so the marker outlives it.</summary>
    public const double ImpactHoldSeconds = 6.0;

    public const string StyleFireLine = "map-combat-vfx-fireline";
    public const string StyleImpactHit = "map-combat-vfx-impact--hit";
    public const string StyleImpactKill = "map-combat-vfx-impact--kill";
    public const string StyleImpactMiss = "map-combat-vfx-impact--miss";
    public const string StyleImpactIntercept = "map-combat-vfx-impact--intercept";
    public const string StyleImpactUnknown = "map-combat-vfx-impact--unknown";

    /// <summary>Projects live VFX from map-symbol positions (destroyed units keep last pose).</summary>
    public static CombatVfxFrame Project(
        CombatEventSnapshot? events,
        IReadOnlyList<MapSymbolEntry>? symbols,
        double nowSimTime) =>
        Project(events, BuildPositionIndex(symbols), nowSimTime);

    /// <summary>
    /// Projects live VFX from an explicit unit-id → normalized xy index.
    /// Only <see cref="CombatEventPhase.TerminalOutcome"/> rows draw. The line's
    /// <see cref="CombatVfxFireLine.EngagementId"/> is the combat-event correlation id.
    /// </summary>
    public static CombatVfxFrame Project(
        CombatEventSnapshot? events,
        IReadOnlyDictionary<string, (float X, float Y)>? positions,
        double nowSimTime)
    {
        var rows = events?.Events;
        if (rows is null || rows.Count == 0 || positions is null || positions.Count == 0)
        {
            return CombatVfxFrame.Empty;
        }

        List<CombatVfxFireLine>? lines = null;
        List<CombatVfxImpactMarker>? markers = null;

        for (var i = 0; i < rows.Count; i++)
        {
            var entry = rows[i];
            if (entry.Phase != CombatEventPhase.TerminalOutcome)
            {
                continue;
            }

            var shooter = entry.ShooterId;
            var victim = entry.TargetId;
            if (string.IsNullOrWhiteSpace(shooter)
                || string.IsNullOrWhiteSpace(victim)
                || !positions.TryGetValue(shooter, out var from)
                || !positions.TryGetValue(victim, out var to))
            {
                continue;
            }

            var age = nowSimTime - entry.SimTime;
            if (age < 0)
            {
                continue;
            }

            if (age <= FireLineHoldSeconds)
            {
                lines ??= new List<CombatVfxFireLine>();
                lines.Add(new CombatVfxFireLine(
                    Key: $"vfx-line:{entry.CorrelationId}",
                    EngagementId: entry.CorrelationId,
                    ShooterUnitId: shooter,
                    TargetUnitId: victim,
                    FromX: from.X,
                    FromY: from.Y,
                    ToX: to.X,
                    ToY: to.Y,
                    SimTime: entry.SimTime,
                    StyleClass: StyleFireLine));
            }

            if (age <= ImpactHoldSeconds)
            {
                markers ??= new List<CombatVfxImpactMarker>();
                markers.Add(new CombatVfxImpactMarker(
                    Key: $"vfx-impact:{entry.CorrelationId}",
                    EngagementId: entry.CorrelationId,
                    TargetUnitId: victim,
                    OutcomeCode: entry.Outcome,
                    X: to.X,
                    Y: to.Y,
                    SimTime: entry.SimTime,
                    StyleClass: ResolveImpactStyle(entry.Outcome)));
            }
        }

        if (lines is null && markers is null)
        {
            return CombatVfxFrame.Empty;
        }

        return new CombatVfxFrame(
            lines ?? (IReadOnlyList<CombatVfxFireLine>)Array.Empty<CombatVfxFireLine>(),
            markers ?? (IReadOnlyList<CombatVfxImpactMarker>)Array.Empty<CombatVfxImpactMarker>());
    }

    /// <summary>Builds a unit-id position index. Destroyed symbols stay so kill markers can land.</summary>
    public static IReadOnlyDictionary<string, (float X, float Y)> BuildPositionIndex(
        IReadOnlyList<MapSymbolEntry>? symbols)
    {
        if (symbols is null || symbols.Count == 0)
        {
            return EmptyPositions;
        }

        var positions = new Dictionary<string, (float X, float Y)>(StringComparer.Ordinal);
        for (var i = 0; i < symbols.Count; i++)
        {
            var symbol = symbols[i];
            if (symbol is null || string.IsNullOrWhiteSpace(symbol.SymbolId))
            {
                continue;
            }

            positions.TryAdd(symbol.SymbolId, (symbol.NormalizedX, symbol.NormalizedY));
        }

        return positions;
    }

    private static string ResolveImpactStyle(string? outcomeCode) =>
        outcomeCode switch
        {
            EngagementOutcomeCodes.Hit => StyleImpactHit,
            EngagementOutcomeCodes.Kill => StyleImpactKill,
            EngagementOutcomeCodes.Miss => StyleImpactMiss,
            EngagementOutcomeCodes.Intercept => StyleImpactIntercept,
            _ => StyleImpactUnknown,
        };

    private static readonly IReadOnlyDictionary<string, (float X, float Y)> EmptyPositions =
        new Dictionary<string, (float X, float Y)>(StringComparer.Ordinal);
}

/// <summary>One transient CMO-style fire line (shooter → target).</summary>
public sealed record CombatVfxFireLine(
    string Key,
    ulong EngagementId,
    string ShooterUnitId,
    string TargetUnitId,
    float FromX,
    float FromY,
    float ToX,
    float ToY,
    double SimTime,
    string StyleClass);

/// <summary>One transient impact marker at the victim pose.</summary>
public sealed record CombatVfxImpactMarker(
    string Key,
    ulong EngagementId,
    string TargetUnitId,
    string OutcomeCode,
    float X,
    float Y,
    double SimTime,
    string StyleClass);

/// <summary>One presentation frame of transient combat VFX. Empty when nothing is live.</summary>
public sealed record CombatVfxFrame(
    IReadOnlyList<CombatVfxFireLine> FireLines,
    IReadOnlyList<CombatVfxImpactMarker> ImpactMarkers)
{
    public static CombatVfxFrame Empty { get; } = new(
        Array.Empty<CombatVfxFireLine>(),
        Array.Empty<CombatVfxImpactMarker>());

    public bool Equals(CombatVfxFrame? other) =>
        other is not null
        && FireLines.SequenceEqual(other.FireLines)
        && ImpactMarkers.SequenceEqual(other.ImpactMarkers);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (var i = 0; i < FireLines.Count; i++)
        {
            hash.Add(FireLines[i]);
        }

        for (var i = 0; i < ImpactMarkers.Count; i++)
        {
            hash.Add(ImpactMarkers[i]);
        }

        return hash.ToHashCode();
    }
}
