using System.Globalization;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-167: readable battle graphic for one combat frame.
/// Binds <see cref="CombatPresentationFrame"/> and map symbols only — never writes the order log
/// or resolves combat (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record BattleGraphicState(
    string StateLine,
    string PrimaryLine,
    string TrackLine,
    string TrailLine,
    string DetailText,
    string Tooltip,
    string DeclutterToken,
    string CueClass,
    string TrackCueClass,
    string TrailCueClass,
    CombatZoomBand Zoom,
    int VisibleCount,
    IReadOnlyList<BattleGraphicLeg> Legs)
{
    /// <summary>Cleared graphic when the frame or symbol list is absent.</summary>
    public static BattleGraphicState Empty { get; } = new(
        "BATTLE: —",
        "—",
        "TRACK: —",
        "TRAIL: —",
        string.Empty,
        "BATTLE: —",
        string.Empty,
        BattleGraphicCueClasses.Unknown,
        BattleGraphicCueClasses.Unknown,
        BattleGraphicCueClasses.Unknown,
        CombatZoomBand.Tactical,
        0,
        Array.Empty<BattleGraphicLeg>());
}

/// <summary>One presentation leg: text, cue, and trail samples copied from the map effect.</summary>
public sealed record BattleGraphicLeg(
    string Key,
    string Text,
    string Tooltip,
    string CueClass,
    string DeclutterToken,
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    CombatEventPhase Phase,
    bool? HasAllocationTrack,
    int? SalvoSize,
    int Count,
    IReadOnlyList<CombatMapTrailSample> Trail);

/// <summary>Non-color USS cue tokens for battle graphics (border geometry + text).</summary>
public static class BattleGraphicCueClasses
{
    public const string Unknown = "battle-cue--unknown";
    public const string Missile = "battle-cue--missile";
    public const string Gun = "battle-cue--gun";
    public const string Energy = "battle-cue--energy";
    public const string Track = "battle-cue--track";
    public const string NoTrack = "battle-cue--no-track";
    public const string Trail = "battle-cue--trail";

    /// <summary>All cue classes hosts must clear before applying the active row cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Unknown, Missile, Gun, Energy, Track, NoTrack, Trail };

    /// <summary>Maps a weapon-family id onto a cue class and declutter token. Energy includes laser.</summary>
    public static BattleGraphicFamilyMark ForFamily(string? familyId)
    {
        if (Is(familyId, "missile"))
        {
            return new BattleGraphicFamilyMark(Missile, BattleGraphicDeclutterTokens.Missile);
        }

        if (Is(familyId, "gun"))
        {
            return new BattleGraphicFamilyMark(Gun, BattleGraphicDeclutterTokens.Gun);
        }

        if (IsEnergy(familyId))
        {
            return new BattleGraphicFamilyMark(Energy, BattleGraphicDeclutterTokens.Energy);
        }

        return new BattleGraphicFamilyMark(Unknown, BattleGraphicDeclutterTokens.Unknown);
    }

    private static bool IsEnergy(string? familyId) =>
        Is(familyId, "laser") || Is(familyId, "energy") || Is(familyId, "directed-energy");

    private static bool Is(string? familyId, string expected) =>
        string.Equals(familyId, expected, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Family cue paired with its text token. Color is never the only distinction.</summary>
public readonly record struct BattleGraphicFamilyMark(string CueClass, string DeclutterToken);

/// <summary>Headless text declutter tokens paired with cue classes.</summary>
public static class BattleGraphicDeclutterTokens
{
    public const string Missile = "[BATTLE:MISSILE]";
    public const string Gun = "[BATTLE:GUN]";
    public const string Energy = "[BATTLE:ENERGY]";
    public const string Unknown = "[BATTLE:UNKNOWN]";
}

/// <summary>One UI Toolkit row: element name, text, cue class, and tooltip.</summary>
public sealed record BattleGraphicRow(string ElementName, string Text, string CueClass, string Tooltip);

/// <summary>Headless row binder for engagement battle-graphic labels and cue classes.</summary>
public static class BattleGraphicPanelBinder
{
    /// <summary>Maps bound state into element names, text, and cue classes for UI Toolkit classList swaps.</summary>
    public static IReadOnlyList<BattleGraphicRow> BindRows(BattleGraphicState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return new[]
        {
            new BattleGraphicRow("battle-graphic-summary", state.StateLine, state.CueClass, state.Tooltip),
            new BattleGraphicRow("battle-graphic-line", state.PrimaryLine, state.CueClass, state.Tooltip),
            new BattleGraphicRow("battle-graphic-track", state.TrackLine, state.TrackCueClass, state.TrackLine),
            new BattleGraphicRow("battle-graphic-trail", state.TrailLine, state.TrailCueClass, state.TrailLine),
            new BattleGraphicRow("battle-graphic-detail", state.DetailText, state.CueClass, state.DetailText),
        };
    }
}

/// <summary>
/// Maps a combat presentation frame into battle-graphic chrome.
/// Replay-stable: same frame, symbols, zoom, and selection produce the same text.
/// </summary>
public static class BattleGraphicBinder
{
    /// <summary>Short-lived fire/launch flash. Longer than this, the graphic drops unless inspected.</summary>
    public const double FireEffectHoldSeconds = 2d;

    /// <summary>Short-lived terminal flash.</summary>
    public const double TerminalEffectHoldSeconds = 2d;

    /// <summary>In-flight trail remains visible through this sim-time window.</summary>
    public const double InFlightHoldSeconds = 6d;

    private const int DetailLegCap = 8;

    /// <summary>
    /// Builds the battle graphic from projection facts and symbol positions.
    /// Does not enqueue orders or mutate <paramref name="frame"/>.
    /// </summary>
    public static BattleGraphicState Bind(
        CombatPresentationFrame? frame,
        IReadOnlyList<MapSymbolEntry>? symbols,
        CombatZoomBand zoom = CombatZoomBand.Tactical,
        string? selectedKey = null)
    {
        if (frame is null || symbols is null)
        {
            return BattleGraphicState.Empty;
        }

        var map = CombatMapPresenter.Build(
            frame.Events,
            symbols,
            frame.SimTime,
            zoom,
            selectedKey,
            holdSeconds: InFlightHoldSeconds,
            tracks: frame.Explanations,
            fireHoldSeconds: FireEffectHoldSeconds,
            terminalHoldSeconds: TerminalEffectHoldSeconds);

        if (map.Effects.Count == 0)
        {
            var history = JoinHistory(map.EventLines);
            var summary = map.EventLines.Count == 0
                ? "BATTLE: —"
                : FormattableString.Invariant($"BATTLE: — | {map.EventLines.Count} history");
            return new BattleGraphicState(
                summary,
                "—",
                "TRACK: —",
                "TRAIL: —",
                history,
                string.IsNullOrEmpty(history) ? summary : summary + " | " + history,
                string.Empty,
                BattleGraphicCueClasses.Unknown,
                BattleGraphicCueClasses.Unknown,
                BattleGraphicCueClasses.Unknown,
                zoom,
                0,
                Array.Empty<BattleGraphicLeg>());
        }

        var legs = new BattleGraphicLeg[map.Effects.Count];
        for (var i = 0; i < map.Effects.Count; i++)
        {
            legs[i] = ToLeg(map.Effects[i]);
        }

        var primary = ChoosePrimary(map.Effects, selectedKey);
        var primaryLeg = ToLeg(primary);
        var trackLine = FormatTrack(primary);
        var trailLine = FormatEffect(primary);
        var detail = JoinLegText(legs);
        var tooltip = string.Join(
            " | ",
            new[] { primaryLeg.Text, trackLine, trailLine });
        return new BattleGraphicState(
            FormattableString.Invariant(
                $"BATTLE {zoom} | {map.Effects.Count} visible | {map.EventLines.Count} history | {primary.DeclutterToken}"),
            primaryLeg.Text,
            trackLine,
            trailLine,
            detail,
            tooltip,
            primary.DeclutterToken,
            primary.CueClass,
            TrackCue(primary),
            primary.Trail.Count > 0 ? BattleGraphicCueClasses.Trail : BattleGraphicCueClasses.Unknown,
            zoom,
            map.Effects.Count,
            Array.AsReadOnly(legs));
    }

    private static CombatMapEffect ChoosePrimary(IReadOnlyList<CombatMapEffect> effects, string? selectedKey)
    {
        CombatMapEffect? selected = null;
        CombatMapEffect best = effects[0];
        for (var i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (selectedKey != null && (string.Equals(effect.Key, selectedKey, StringComparison.Ordinal)
                || ContainsKey(effect.CorrelationKeys, selectedKey)))
            {
                selected = effect;
            }

            if (effect.SourceSimTime > best.SourceSimTime
                || (effect.SourceSimTime.Equals(best.SourceSimTime)
                    && string.Compare(effect.Key, best.Key, StringComparison.Ordinal) > 0))
            {
                best = effect;
            }
        }

        return selected ?? best;
    }

    private static bool ContainsKey(IReadOnlyList<string> keys, string selectedKey)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            if (string.Equals(keys[i], selectedKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static BattleGraphicLeg ToLeg(CombatMapEffect effect)
    {
        var text = effect.Label + " | " + effect.DeclutterToken;
        var track = FormatTrack(effect);
        var effectLine = FormatEffect(effect);
        return new BattleGraphicLeg(
            effect.Key,
            text,
            text + " | " + track + " | " + effectLine,
            effect.CueClass,
            effect.DeclutterToken,
            effect.ShooterId,
            effect.TargetId,
            effect.WeaponFamilyId,
            effect.Phase,
            effect.HasAllocationTrack,
            effect.SalvoSize,
            effect.Count,
            effect.Trail);
    }

    private static string FormatTrack(CombatMapEffect effect)
    {
        if (effect.Count > 1 && effect.HasAllocationTrack == true)
        {
            return FormattableString.Invariant(
                $"TRACK ×{effect.Count} | {effect.ShooterId} → {effect.TargetId}");
        }

        if (effect.HasAllocationTrack == true)
        {
            var salvo = effect.SalvoSize is int size
                ? FormattableString.Invariant($" | salvo {size}")
                : string.Empty;
            return $"TRACK {effect.ShooterId} → {effect.TargetId}{salvo}";
        }

        if (effect.HasAllocationTrack == false)
        {
            return $"NO-TRACK {effect.ShooterId} → {effect.TargetId}";
        }

        return "TRACK: —";
    }

    private static string FormatEffect(CombatMapEffect effect)
    {
        if (effect.Trail.Count > 0)
        {
            var parts = new string[effect.Trail.Count];
            for (var i = 0; i < effect.Trail.Count; i++)
            {
                var sample = effect.Trail[i];
                parts[i] = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.00}@{1:0.000},{2:0.000}",
                    sample.Progress,
                    sample.X,
                    sample.Y);
            }

            return "TRAIL " + string.Join(" ", parts);
        }

        if (effect.Count > 1)
        {
            return FormattableString.Invariant($"EFFECT AGGREGATE ×{effect.Count}");
        }

        return effect.Phase switch
        {
            CombatEventPhase.Firing => "EFFECT FIRE",
            CombatEventPhase.TerminalOutcome => "EFFECT TERMINAL",
            _ => "TRAIL: —",
        };
    }

    private static string TrackCue(CombatMapEffect effect) =>
        effect.HasAllocationTrack switch
        {
            true => BattleGraphicCueClasses.Track,
            false => BattleGraphicCueClasses.NoTrack,
            _ => BattleGraphicCueClasses.Unknown,
        };

    private static string JoinLegText(IReadOnlyList<BattleGraphicLeg> legs)
    {
        var take = Math.Min(DetailLegCap, legs.Count);
        var lines = new string[take];
        for (var i = 0; i < take; i++)
        {
            lines[i] = legs[i].Tooltip;
        }

        var text = string.Join("\n", lines);
        return legs.Count > DetailLegCap ? text + "\n…" : text;
    }

    private static string JoinHistory(IReadOnlyList<CombatMapEventLine> lines)
    {
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var take = Math.Min(DetailLegCap, lines.Count);
        var start = lines.Count - take;
        var rows = new string[take];
        for (var i = 0; i < take; i++)
        {
            rows[i] = lines[start + i].Text;
        }

        return string.Join("\n", rows);
    }
}
