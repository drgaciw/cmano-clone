namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

using System.Globalization;
using System.Text;
using Bridge;
using CombatEvents;
using Decision;
using EngagementExplanation;
using Presentation;
using Projection;
using SensorToShooter;
using Skills;
using TargetabilityAccept;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>One zoom band's view of a single selected engagement leg.</summary>
public sealed record CombatVerticalSliceView(CombatZoomBand Zoom, CombatMapEffect Effect);

/// <summary>Unselected (declutter) combat map for one zoom band.</summary>
public sealed record CombatVerticalSliceZoomMap(CombatZoomBand Zoom, CombatMapPresentation Map);

/// <summary>
/// DRG-166: one commanded engagement correlated across the event log, map, explanation and replay by
/// <see cref="CorrelationId"/>.
/// </summary>
public sealed record CombatVerticalSliceLeg(
    string ShooterId,
    string TargetId,
    string WeaponFamilyId,
    bool ExpectedPermitted,
    bool HasFireControlTrack,
    long CommandOrderId,
    ulong CorrelationId,
    string Key,
    IReadOnlyList<CombatEvent> Events,
    IReadOnlyList<CombatMapEventLine> EventLines,
    IReadOnlyList<CombatVerticalSliceView> Views,
    EngagementExplanationSurface Explanation,
    CombatTargetabilityFact? Targetability);

/// <summary>DRG-166/170: executed vertical-slice evidence plus a replay-stable fingerprint.</summary>
public sealed record CombatVerticalSliceEvidence(
    SliceBCombatScenario.Result Scenario,
    TargetabilityAcceptSnapshot TargetabilityAccept,
    CombatEventSnapshot Events,
    CombatPresentationFrame Frame,
    IReadOnlyList<MapSymbolEntry> Symbols,
    IReadOnlyList<CombatVerticalSliceLeg> Legs,
    IReadOnlyList<CombatVerticalSliceZoomMap> Maps,
    IReadOnlyList<MapLodApplyResult> SymbolLod,
    string Fingerprint);

/// <summary>
/// DRG-166: headless combat vertical slice. Drives every fixture leg from a player command through the real
/// resolver, then reads the outcome only through real projections: Slice A targetability, the DRG-165
/// combat-event contract, the presentation frame, the combat map at each zoom band, the DRG-168 explanation,
/// and symbol LOD. Presentation never invents combat truth (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
/// <remarks>Synthetic test evidence; isolated from the Baltic v2 replay goldens.</remarks>
public static class CombatVerticalSliceHarness
{
    private static readonly CombatZoomBand[] ZoomBands =
        { CombatZoomBand.Tactical, CombatZoomBand.Operational, CombatZoomBand.Theater };

    private static readonly MapLodBand[] LodBands =
        { MapLodBand.Close, MapLodBand.Tactical, MapLodBand.Theater, MapLodBand.Overview };

    /// <summary>Synthetic sensor contact id the scenario reports for a fixture target.</summary>
    public static string ContactIdFor(string targetId) => SliceBCombatScenario.ContactIdFor(targetId);

    /// <summary>Runs the checked-in Slice B fixture as a commanded vertical slice.</summary>
    public static CombatVerticalSliceEvidence Run(int seed = 7) =>
        Run(seed, SliceBCombatScenario.LoadDefaultDefinition());

    /// <summary>Runs <paramref name="definition"/> as a commanded vertical slice.</summary>
    public static CombatVerticalSliceEvidence Run(
        int seed,
        SliceBCombatScenario.Definition definition,
        int maxEffects = 64) =>
        Project(SliceBCombatScenario.RunCommanded(seed, definition), definition, maxEffects);

    private static CombatVerticalSliceEvidence Project(
        SliceBCombatScenario.Result scenario,
        SliceBCombatScenario.Definition definition,
        int maxEffects)
    {
        var legs = definition.Legs;
        if (scenario.Commands.Count != legs.Count)
        {
            throw new InvalidOperationException("Every vertical-slice leg must be driven by one approved command.");
        }

        var endTime = scenario.Events.Events.Count == 0 ? 0 : scenario.Events.Events.Max(e => e.SimTime);
        var targetability = ProjectTargetabilityAtCommand(scenario, legs);
        var events = CombatEventLogProjection.Build(scenario.Log, endTime, targetability);
        var frame = CombatPresentationFrameBridge.Build(scenario.Log, SliceAContactFrame.Empty, endTime);
        var symbols = DistinctSymbols(scenario.Symbols);
        var holdSeconds = Math.Max(endTime, BattleGraphicBinder.InFlightHoldSeconds);

        var maps = ZoomBands
            .Select(zoom => new CombatVerticalSliceZoomMap(zoom, BuildMap(
                events, symbols, endTime, zoom, null, maxEffects, holdSeconds, frame)))
            .ToArray();
        var projectedLegs = ProjectLegs(
            legs, scenario.Commands, events, symbols, endTime, maxEffects, holdSeconds, frame);
        var lod = LodBands.Select(band => MapLodApplyState.Apply(symbols, band)).ToArray();

        return new CombatVerticalSliceEvidence(
            scenario,
            targetability,
            events,
            frame,
            symbols,
            projectedLegs,
            Array.AsReadOnly(maps),
            Array.AsReadOnly(lod),
            ComposeFingerprint(events, targetability, projectedLegs, maps, lod, scenario.Commands));
    }

    /// <summary>
    /// Slice A acceptance for each target as it stood when its first engage command was approved: the real
    /// projection over the order log bounded to that sim time. Later kills do not rewrite the reason a shot
    /// was permitted.
    /// </summary>
    private static TargetabilityAcceptSnapshot ProjectTargetabilityAtCommand(
        SliceBCombatScenario.Result scenario,
        IReadOnlyList<SliceBCombatScenario.Leg> legs)
    {
        var authority = new C2AuthorityProjectionContext(
            RoeLevel.WeaponsFree, SkillLane.Read, RequiredApproval.None, TrackSource.Organic, true, null, true);
        var fireControl = new LegFireControl(legs);
        var shooters = new LegShooters(legs);
        var targets = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<TargetabilityAcceptContactRow>();
        for (var i = 0; i < legs.Count; i++)
        {
            if (!targets.Add(legs[i].TargetId))
            {
                continue;
            }

            var commandTime = scenario.Commands[i].CommandSimTime;
            var bounded = new DecisionLog();
            foreach (var entry in scenario.Log.ChronologicalEntries())
            {
                if (entry.SimTime <= commandTime)
                {
                    bounded.Append(entry);
                }
            }

            var snapshot = TargetabilityAcceptProjection.Project(
                bounded, (ulong)commandTime, authority, fireControl: fireControl, shooters: shooters);
            rows.AddRange(snapshot.Contacts.Where(r => r.TargetId == legs[i].TargetId));
        }

        return new TargetabilityAcceptSnapshot(
            Array.AsReadOnly(rows.OrderBy(r => r.ContactId, StringComparer.Ordinal).ToArray()));
    }

    private static IReadOnlyList<CombatVerticalSliceLeg> ProjectLegs(
        IReadOnlyList<SliceBCombatScenario.Leg> legs,
        IReadOnlyList<SliceBCombatScenario.Command> commands,
        CombatEventSnapshot events,
        IReadOnlyList<MapSymbolEntry> symbols,
        double endTime,
        int maxEffects,
        double holdSeconds,
        CombatPresentationFrame frame)
    {
        var occurrence = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new CombatVerticalSliceLeg[legs.Count];
        for (var i = 0; i < legs.Count; i++)
        {
            var leg = legs[i];
            var pair = leg.ShooterId + "\u001f" + leg.TargetId;
            occurrence.TryGetValue(pair, out var nth);
            occurrence[pair] = nth + 1;

            var correlations = events.Events
                .Where(e => e.ShooterId == leg.ShooterId && e.TargetId == leg.TargetId)
                .Select(e => e.CorrelationId)
                .Distinct()
                .OrderBy(c => c)
                .ToArray();
            if (nth >= correlations.Length)
            {
                throw new InvalidOperationException(
                    $"No combat-event correlation for '{leg.ShooterId}' → '{leg.TargetId}' (#{nth + 1}).");
            }

            var correlationId = correlations[nth];
            var command = commands[i];
            var key = CombatMapPresenter.KeyFor(leg.ShooterId, leg.TargetId, correlationId);
            var views = new CombatVerticalSliceView[ZoomBands.Length];
            IReadOnlyList<CombatMapEventLine> lines = Array.Empty<CombatMapEventLine>();
            for (var z = 0; z < ZoomBands.Length; z++)
            {
                var map = BuildMap(events, symbols, endTime, ZoomBands[z], key, maxEffects, holdSeconds, frame);
                var effect = map.Effects.FirstOrDefault(e => e.Key == key)
                    ?? throw new InvalidOperationException($"Selected leg '{key}' is missing at {ZoomBands[z]}.");
                views[z] = new CombatVerticalSliceView(ZoomBands[z], effect);
                if (z == 0)
                {
                    lines = map.EventLines.Where(l => l.Key == key).ToArray();
                }
            }

            result[i] = new CombatVerticalSliceLeg(
                leg.ShooterId,
                leg.TargetId,
                leg.WeaponFamilyId,
                leg.Permitted,
                leg.HasFireControlTrack,
                command.OrderId,
                correlationId,
                key,
                Array.AsReadOnly(events.Events.Where(e => e.CorrelationId == correlationId
                    && e.ShooterId == leg.ShooterId).ToArray()),
                lines,
                Array.AsReadOnly(views),
                EngagementExplanationProjection.Build(events, leg.ShooterId, leg.TargetId, correlationId),
                events.Targetability.FirstOrDefault(f => f.TargetId == leg.TargetId));
        }

        return Array.AsReadOnly(result);
    }

    private static CombatMapPresentation BuildMap(
        CombatEventSnapshot events,
        IReadOnlyList<MapSymbolEntry> symbols,
        double endTime,
        CombatZoomBand zoom,
        string? selectedKey,
        int maxEffects,
        double holdSeconds,
        CombatPresentationFrame frame) =>
        CombatMapPresenter.Build(
            events, symbols, endTime, zoom, selectedKey, maxEffects, holdSeconds, frame.Explanations);

    private static IReadOnlyList<MapSymbolEntry> DistinctSymbols(IReadOnlyList<MapSymbolEntry> symbols)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return Array.AsReadOnly(symbols.Where(s => seen.Add(s.SymbolId)).ToArray());
    }

    private static string ComposeFingerprint(
        CombatEventSnapshot events,
        TargetabilityAcceptSnapshot targetability,
        IReadOnlyList<CombatVerticalSliceLeg> legs,
        IReadOnlyList<CombatVerticalSliceZoomMap> maps,
        IReadOnlyList<MapLodApplyResult> lod,
        IReadOnlyList<SliceBCombatScenario.Command> commands)
    {
        var builder = new StringBuilder();
        builder.Append("cvs:ev=").Append(CombatEventFingerprint.Compute(events));
        builder.Append("\nta=").Append(TargetabilityAcceptFingerprint.Compute(targetability));
        foreach (var command in commands)
        {
            builder.Append("\ncmd=").Append(command.ShooterId).Append(',')
                .Append(command.OrderId.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(command.CommandSimTime.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(command.ExecutedSimTime.ToString("R", CultureInfo.InvariantCulture));
        }

        foreach (var leg in legs)
        {
            builder.Append("\nleg=").Append(leg.Key);
            builder.Append("|eex=").Append(EngagementExplanationFingerprint.Compute(leg.Explanation));
            foreach (var view in leg.Views)
            {
                AppendEffect(builder.Append("|v").Append((int)view.Zoom).Append('='), view.Effect);
            }
        }

        foreach (var zoom in maps)
        {
            builder.Append("\nmap").Append((int)zoom.Zoom).Append('=');
            foreach (var effect in zoom.Map.Effects)
            {
                AppendEffect(builder, effect);
                builder.Append(';');
            }

            foreach (var line in zoom.Map.EventLines)
            {
                builder.Append("\n  ").Append(line.Text);
            }
        }

        foreach (var band in lod)
        {
            builder.Append("\nlod").Append((int)band.Band).Append('=');
            foreach (var cluster in band.Clusters)
            {
                builder.Append(cluster.ClusterId).Append('×')
                    .Append(cluster.Count.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(cluster.AffiliationMajority).Append(',').Append(cluster.App6Glyph).Append(';');
            }
        }

        return builder.ToString();
    }

    private static void AppendEffect(StringBuilder builder, CombatMapEffect effect) =>
        builder.Append(effect.Key).Append('|').Append(effect.Label).Append('|').Append(effect.LinePattern)
            .Append('|').Append(effect.CueClass).Append('|').Append(effect.DeclutterToken)
            .Append('|').Append(effect.Count.ToString(CultureInfo.InvariantCulture))
            .Append('|').Append(effect.HasAllocationTrack?.ToString() ?? "?")
            .Append('|').Append(effect.SalvoSize?.ToString(CultureInfo.InvariantCulture) ?? "?");

    private sealed class LegFireControl : IKillChainFireControlSource
    {
        private readonly HashSet<string> _tracked;

        public LegFireControl(IReadOnlyList<SliceBCombatScenario.Leg> legs) =>
            _tracked = new HashSet<string>(
                legs.Where(l => l.HasFireControlTrack).Select(l => l.TargetId), StringComparer.Ordinal);

        public bool HasFireControlTrack(string contactId, string targetId) => _tracked.Contains(targetId);
    }

    private sealed class LegShooters : ISensorToShooterShooterSource
    {
        private readonly IReadOnlyList<SliceBCombatScenario.Leg> _legs;

        public LegShooters(IReadOnlyList<SliceBCombatScenario.Leg> legs) => _legs = legs;

        public IReadOnlyList<SensorToShooterShooterCandidate> GetCandidatesForTarget(string targetId) =>
            _legs.Where(l => l.TargetId == targetId)
                .GroupBy(l => l.ShooterId, StringComparer.Ordinal)
                .Select(g => new SensorToShooterShooterCandidate(
                    g.Key, ScenarioEngageDefaults.MvpFallback, g.First().RoundsRemaining))
                .ToArray();
    }
}
