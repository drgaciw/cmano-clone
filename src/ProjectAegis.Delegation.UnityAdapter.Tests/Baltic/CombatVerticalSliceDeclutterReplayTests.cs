namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using System.Globalization;
using System.Text.Json;
using CombatEvents;
using EngagementExplanation;
using Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

/// <summary>
/// DRG-170: tactical / operational / theater declutter, non-color recognition, and replay stability for the
/// DRG-166 combat vertical slice.
/// </summary>
[TestFixture]
public sealed class CombatVerticalSliceDeclutterReplayTests
{
    private const int Replicas = 12;
    private const int Repeats = 2;
    private const int MaxEffects = 64;

    private static readonly Lazy<CombatVerticalSliceEvidence> Clutter = new(() => RunClutter(7));

    [Test]
    public void Tactical_band_draws_each_fired_engagement_individually_within_the_effect_budget()
    {
        var slice = Clutter.Value;
        var tactical = MapAt(slice, CombatZoomBand.Tactical);
        var fired = FiredLegCount(slice);

        Assert.That(fired, Is.GreaterThan(MaxEffects), "theater-scale clutter must exceed the effect budget");
        Assert.That(tactical.Effects, Has.Count.EqualTo(MaxEffects));
        Assert.That(tactical.Effects.All(e => e.Count == 1), Is.True);
        Assert.That(tactical.Effects.Select(e => e.Key).Distinct().Count(), Is.EqualTo(MaxEffects));
        Assert.That(tactical.EventLines, Has.Count.EqualTo(slice.Events.Events.Count), "history is never decluttered");
    }

    [Test]
    public void Operational_band_folds_repeat_engagements_of_one_pair_and_keeps_motion_cues()
    {
        var slice = Clutter.Value;
        var operational = MapAt(slice, CombatZoomBand.Operational);
        var fired = FiredLegCount(slice);

        Assert.That(operational.Effects.Count, Is.LessThan(fired));
        Assert.That(operational.Effects.Sum(e => e.Count), Is.EqualTo(fired));
        Assert.That(operational.Effects.Any(e => e.Count == Repeats), Is.True);
        foreach (var effect in operational.Effects)
        {
            var members = effect.CorrelationKeys.Select(k => LegFor(slice, k)).ToArray();
            Assert.That(members.Select(m => (m.ShooterId, m.TargetId)).Distinct().Count(), Is.EqualTo(1),
                "operational aggregates never merge different shooter/target pairs");
            Assert.That(effect.LinePattern, Is.Not.EqualTo("none"));
            Assert.That(effect.MotionLabel, Is.Not.EqualTo("Static"));
            if (effect.Count > 1)
            {
                Assert.That(effect.Label, Does.StartWith("Operational summary:"));
                Assert.That(effect.Label, Does.Contain(FormattableString.Invariant($"×{effect.Count}")));
            }
        }
    }

    [Test]
    public void Theater_band_collapses_to_family_clearance_and_outcome_summaries()
    {
        var slice = Clutter.Value;
        var theater = MapAt(slice, CombatZoomBand.Theater);
        var fired = FiredLegCount(slice);

        Assert.That(theater.Effects.Count, Is.LessThanOrEqualTo(3 * 2), "≤ families × terminal outcomes");
        Assert.That(theater.Effects.Sum(e => e.Count), Is.EqualTo(fired));
        Assert.That(theater.Effects.Any(e => e.Count > 1), Is.True);
        foreach (var effect in theater.Effects.Where(e => e.Count > 1))
        {
            Assert.That(effect.LinePattern, Is.EqualTo("none"));
            Assert.That(effect.MotionLabel, Is.EqualTo("Static"));
            Assert.That(effect.Trail, Is.Empty, "no false cross-location theater trajectory");
            Assert.That(effect.Label, Does.StartWith("Theater summary:"));
            var members = effect.CorrelationKeys.Select(k => LegFor(slice, k)).ToArray();
            Assert.That(members.Select(m => m.WeaponFamilyId).Distinct(), Is.EqualTo(new[] { effect.WeaponFamilyId }));
            Assert.That(members.Select(m => m.Events[^1].Outcome).Distinct(), Is.EqualTo(new[] { effect.Outcome }));
        }
    }

    [Test]
    public void Selected_engagement_survives_declutter_at_every_band()
    {
        var slice = Clutter.Value;

        foreach (var leg in slice.Legs)
        {
            foreach (var view in leg.Views)
            {
                Assert.That(view.Effect.Key, Is.EqualTo(leg.Key), $"{leg.Key} @ {view.Zoom}");
                Assert.That(view.Effect.Count, Is.EqualTo(1), $"{leg.Key} @ {view.Zoom}");
            }
        }
    }

    [Test]
    public void Symbol_lod_reduces_theater_scale_symbol_clutter_and_accounts_for_every_symbol()
    {
        var slice = Clutter.Value;
        var symbols = slice.Symbols.Count;

        Assert.That(symbols, Is.EqualTo(Replicas * 12));
        var close = Lod(slice, MapLodBand.Close);
        var theater = Lod(slice, MapLodBand.Theater);
        var overview = Lod(slice, MapLodBand.Overview);
        Assert.That(close.OutputCount, Is.EqualTo(symbols));
        Assert.That(theater.OutputCount, Is.LessThan(close.OutputCount));
        Assert.That(overview.OutputCount, Is.LessThanOrEqualTo(theater.OutputCount));
        foreach (var lod in slice.SymbolLod)
        {
            Assert.That(lod.Clusters.Sum(c => c.Count), Is.EqualTo(symbols), lod.Band.ToString());
            Assert.That(lod.Clusters.All(c => !string.IsNullOrEmpty(c.App6Glyph)), Is.True);
        }
    }

    [Test]
    public void Weapon_family_is_recognisable_without_color_at_every_band()
    {
        var slice = Clutter.Value;

        foreach (var zoom in new[] { CombatZoomBand.Tactical, CombatZoomBand.Operational, CombatZoomBand.Theater })
        {
            var effects = MapAt(slice, zoom).Effects;
            var byFamily = effects.GroupBy(e => e.WeaponFamilyId).ToArray();
            Assert.That(byFamily, Has.Length.EqualTo(3), zoom.ToString());
            Assert.That(byFamily.Select(g => g.Select(e => (e.FamilyGlyph, e.DeclutterToken)).Distinct().Single())
                .Distinct().Count(), Is.EqualTo(3), zoom.ToString());
            foreach (var effect in effects)
            {
                Assert.That(effect.Label, Does.Contain(effect.FamilyGlyph));
                Assert.That(effect.Label, Does.Contain(effect.WeaponFamilyId));
            }
        }
    }

    [Test]
    public void Affiliation_is_recognisable_without_color()
    {
        var slice = Clutter.Value;

        var friendly = slice.Symbols.Where(s => s.Affiliation == "Friendly").ToArray();
        var hostile = slice.Symbols.Where(s => s.Affiliation == "Hostile" && !s.IsDestroyed).ToArray();
        Assert.That(friendly, Is.Not.Empty);
        Assert.That(hostile, Is.Not.Empty);
        Assert.That(friendly.Select(s => s.ShapeGlyph).Distinct().Single(),
            Is.Not.EqualTo(hostile.Select(s => s.ShapeGlyph).Distinct().Single()));
        var close = Lod(slice, MapLodBand.Close).Clusters;
        var friendlyGlyphs = close.Where(c => c.AffiliationMajority == "Friendly").Select(c => c.App6Glyph).Distinct();
        var hostileGlyphs = close.Where(c => c.AffiliationMajority == "Hostile").Select(c => c.App6Glyph).Distinct();
        Assert.That(friendlyGlyphs.Intersect(hostileGlyphs), Is.Empty, "APP-6 frame glyph differs by affiliation");
        foreach (var effect in MapAt(slice, CombatZoomBand.Theater).Effects)
        {
            Assert.That(effect.Label, Does.Contain($"| {effect.Affiliation} |"));
        }
    }

    [Test]
    public void Clearance_is_recognisable_without_color()
    {
        var slice = Clutter.Value;
        var tactical = slice.Legs.Select(l => l.Views.Single(v => v.Zoom == CombatZoomBand.Tactical).Effect).ToArray();
        var refused = tactical.Where(e => e.Clearance == "Refused").ToArray();
        var cleared = tactical.Where(e => e.Clearance == "Cleared").ToArray();

        Assert.That(refused, Is.Not.Empty);
        Assert.That(cleared, Is.Not.Empty);
        Assert.That(refused.All(e => e.FamilyGlyph == "⊘" && e.LinePattern == "none"), Is.True);
        Assert.That(cleared.All(e => e.FamilyGlyph != "⊘" && e.LinePattern != "none"), Is.True);
        Assert.That(refused.All(e => e.Label.Contains("| Refused |")), Is.True);
        Assert.That(cleared.All(e => e.Label.Contains("| Cleared |")), Is.True);
    }

    [Test]
    public void Outcome_is_recognisable_without_color()
    {
        var slice = Clutter.Value;

        foreach (var leg in slice.Legs)
        {
            var outcome = leg.Events[^1].Outcome;
            var effect = leg.Views.Single(v => v.Zoom == CombatZoomBand.Tactical).Effect;
            Assert.That(effect.Outcome, Is.EqualTo(outcome));
            Assert.That(effect.Label, Does.Contain($"| {outcome} |"));
            Assert.That(leg.EventLines[^1].Text, Does.EndWith($"| {outcome}"));
        }
    }

    [Test]
    public void Distinct_engagement_states_never_share_a_text_and_glyph_signature()
    {
        var slice = Clutter.Value;
        var tactical = slice.Legs.Select(l => l.Views.Single(v => v.Zoom == CombatZoomBand.Tactical).Effect).ToArray();

        var signaturesByState = tactical
            .GroupBy(e => (e.WeaponFamilyId, e.Affiliation, e.Clearance, e.Outcome))
            .ToDictionary(
                g => g.Key,
                g => g.Select(Signature).Distinct().ToArray());
        Assert.That(signaturesByState.Values.All(s => s.Length == 1), Is.True, "one state, one signature");
        Assert.That(signaturesByState.Values.Select(s => s[0]).Distinct().Count(), Is.EqualTo(signaturesByState.Count));
    }

    [Test]
    public void Same_seed_replays_identical_event_order_and_presentation_state()
    {
        var first = RunClutter(7);
        var second = RunClutter(7);

        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
        Assert.That(second.Events.Events, Is.EqualTo(first.Events.Events));
        for (var i = 1; i < first.Events.Events.Count; i++)
        {
            Assert.That(first.Events.Events[i].SimTime, Is.GreaterThanOrEqualTo(first.Events.Events[i - 1].SimTime));
        }

        foreach (var zoom in first.Maps)
        {
            var replayed = second.Maps.Single(m => m.Zoom == zoom.Zoom).Map;
            Assert.That(replayed.Effects.Select(e => e.Label), Is.EqualTo(zoom.Map.Effects.Select(e => e.Label)));
            Assert.That(replayed.EventLines.Select(l => l.Text), Is.EqualTo(zoom.Map.EventLines.Select(l => l.Text)));
        }

        Assert.That(second.Legs.Select(l => EngagementExplanationFingerprint.Compute(l.Explanation)),
            Is.EqualTo(first.Legs.Select(l => EngagementExplanationFingerprint.Compute(l.Explanation))));
    }

    [Test]
    public void Default_vertical_slice_fingerprint_is_stable_across_runs()
    {
        var fingerprints = Enumerable.Range(0, 3).Select(_ => CombatVerticalSliceHarness.Run().Fingerprint).ToArray();

        Assert.That(fingerprints.Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Replay_seek_matches_the_run_that_stopped_at_the_same_sim_time()
    {
        var definition = LoadDefinition();
        var full = CombatVerticalSliceHarness.Run(7, definition);
        var prefix = CombatVerticalSliceHarness.Run(7, definition with { Legs = definition.Legs.Take(3).ToArray() });
        var cutoff = prefix.Events.Events.Max(e => e.SimTime);

        var seek = CombatEventLogProjection.Build(full.Scenario.Log, cutoff);
        Assert.That(seek.Events, Is.EqualTo(prefix.Events.Events));
        var seekMap = CombatMapPresenter.Build(seek, full.Symbols, cutoff, CombatZoomBand.Tactical, holdSeconds: cutoff);
        var prefixMap = CombatMapPresenter.Build(
            prefix.Events, prefix.Symbols, cutoff, CombatZoomBand.Tactical, holdSeconds: cutoff);
        Assert.That(seekMap.EventLines.Select(l => l.Text), Is.EqualTo(prefixMap.EventLines.Select(l => l.Text)));
        Assert.That(seekMap.Effects.Select(e => e.Label), Is.EqualTo(prefixMap.Effects.Select(e => e.Label)));
    }

    /// <summary>Rendered glyph, line pattern and label text with the per-leg unit ids removed.</summary>
    private static string Signature(CombatMapEffect effect) =>
        string.Join("|", effect.FamilyGlyph, effect.LinePattern, effect.DeclutterToken,
            effect.Label.Substring(effect.Label.IndexOf(" | ", StringComparison.Ordinal)));

    private static CombatMapPresentation MapAt(CombatVerticalSliceEvidence slice, CombatZoomBand zoom) =>
        slice.Maps.Single(m => m.Zoom == zoom).Map;

    private static MapLodApplyResult Lod(CombatVerticalSliceEvidence slice, MapLodBand band) =>
        slice.SymbolLod.Single(l => l.Band == band);

    private static CombatVerticalSliceLeg LegFor(CombatVerticalSliceEvidence slice, string key) =>
        slice.Legs.Single(l => l.Key == key);

    private static int FiredLegCount(CombatVerticalSliceEvidence slice) =>
        slice.Legs.Count(l => l.Events.Any(e => e.Phase == CombatEventPhase.Firing));

    private static CombatVerticalSliceEvidence RunClutter(int seed)
    {
        var baseline = LoadDefinition();
        var legs = new List<SliceBCombatScenario.Leg>(baseline.Legs.Count * Replicas * Repeats);
        for (var r = 0; r < Replicas; r++)
        {
            var suffix = "-r" + r.ToString("00", CultureInfo.InvariantCulture);
            var offset = r * 0.004f;
            foreach (var leg in baseline.Legs)
            {
                var replica = leg with
                {
                    ShooterId = leg.ShooterId + suffix,
                    TargetId = leg.TargetId + suffix,
                    ShooterX = leg.ShooterX + offset,
                    ShooterY = leg.ShooterY + (offset / 2),
                    TargetX = leg.TargetX + offset,
                    TargetY = leg.TargetY + (offset / 2),
                };
                for (var s = 0; s < Repeats; s++)
                {
                    legs.Add(replica);
                }
            }
        }

        return CombatVerticalSliceHarness.Run(seed, baseline with { Legs = legs });
    }

    private static SliceBCombatScenario.Definition LoadDefinition()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        var path = Path.Combine(
            dir?.FullName ?? throw new InvalidOperationException("ProjectAegis.sln not found"),
            "data", "scenarios", "slice-b-combat-acceptance.json");
        return JsonSerializer.Deserialize<SliceBCombatScenario.Definition>(
                File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException(path);
    }
}
