namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using CombatEvents;
using EngagementExplanation;
using Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using TargetabilityAccept;
using NUnit.Framework;

/// <summary>
/// DRG-166: one deterministic missile / gun / laser vertical slice from player command through outcome,
/// with map, event log and replay correlated by one id per engagement.
/// </summary>
[TestFixture]
public sealed class CombatVerticalSliceHarnessTests
{
    private static readonly CombatZoomBand[] Zooms =
        { CombatZoomBand.Tactical, CombatZoomBand.Operational, CombatZoomBand.Theater };

    [TestCase("Missile")]
    [TestCase("Gun")]
    [TestCase("Laser")]
    public void Permitted_engagement_runs_from_player_command_through_terminal_outcome(string family)
    {
        var slice = CombatVerticalSliceHarness.Run(7);
        var leg = slice.Legs.Single(l => l.WeaponFamilyId == family && l.ExpectedPermitted);
        var command = slice.Scenario.Commands.Single(c => c.ShooterId == leg.ShooterId);

        Assert.That(command.OrderId, Is.EqualTo(leg.CommandOrderId));
        Assert.That(command.CommandSimTime, Is.LessThan(command.ExecutedSimTime));
        Assert.That(leg.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
            CombatEventPhase.Firing,
            CombatEventPhase.TerminalOutcome,
        }));
        Assert.That(leg.Events.First().SimTime, Is.EqualTo(command.ExecutedSimTime));
        Assert.That(leg.Events[^1].Outcome, Is.Not.Empty);
        Assert.That(leg.Explanation.Status, Is.EqualTo(EngagementExplanationStatus.Selected));
        Assert.That(slice.Scenario.Log.Engagements.Single(e => e.SequenceId == leg.CorrelationId).WeaponFamilyId,
            Is.EqualTo(family), "family is sim-authored on the engagement row, not a presentation label");
    }

    [TestCase("Missile")]
    [TestCase("Gun")]
    [TestCase("Laser")]
    public void Refused_engagement_is_commanded_and_explicitly_refused_without_firing(string family)
    {
        var slice = CombatVerticalSliceHarness.Run(7);
        var leg = slice.Legs.Single(l => l.WeaponFamilyId == family && !l.ExpectedPermitted);

        Assert.That(slice.Scenario.Commands.Any(c => c.OrderId == leg.CommandOrderId), Is.True);
        Assert.That(leg.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.AuthorizationRefused,
        }));
        Assert.That(leg.Explanation.Status, Is.EqualTo(EngagementExplanationStatus.Refused));
        Assert.That(leg.Explanation.Headline, Does.Contain(leg.Events[^1].Outcome));
        foreach (var view in leg.Views)
        {
            Assert.That(view.Effect.Clearance, Is.EqualTo("Refused"));
            Assert.That(view.Effect.FamilyGlyph, Is.EqualTo("⊘"));
        }
    }

    [Test]
    public void Map_event_log_explanation_and_replay_share_one_correlation_id_per_engagement()
    {
        var slice = CombatVerticalSliceHarness.Run(7);
        var replay = CombatVerticalSliceHarness.Run(7);

        Assert.That(slice.Legs, Has.Count.EqualTo(6));
        foreach (var leg in slice.Legs)
        {
            var row = slice.Scenario.Log.Engagements.Single(e => e.ShooterTargetId.Value == leg.ShooterId);
            Assert.That(leg.CorrelationId, Is.EqualTo(row.SequenceId), leg.ShooterId);
            Assert.That(leg.Events.Select(e => e.CorrelationId).Distinct(), Is.EqualTo(new[] { leg.CorrelationId }));
            Assert.That(leg.EventLines, Is.Not.Empty);
            Assert.That(leg.EventLines.Select(l => l.CorrelationId).Distinct(), Is.EqualTo(new[] { leg.CorrelationId }));
            Assert.That(leg.EventLines.Select(l => l.Key).Distinct(), Is.EqualTo(new[] { leg.Key }));
            Assert.That(leg.Views.Select(v => v.Zoom), Is.EqualTo(Zooms));
            foreach (var view in leg.Views)
            {
                Assert.That(view.Effect.Key, Is.EqualTo(leg.Key));
                Assert.That(view.Effect.CorrelationId, Is.EqualTo(leg.CorrelationId));
            }

            Assert.That(leg.Explanation.CorrelationId, Is.EqualTo(leg.CorrelationId));
            Assert.That(slice.Frame.Explanations.Single(x => x.ShooterId == leg.ShooterId).CorrelationId,
                Is.EqualTo(leg.CorrelationId));
            var replayed = replay.Legs.Single(l => l.ShooterId == leg.ShooterId);
            Assert.That(replayed.CorrelationId, Is.EqualTo(leg.CorrelationId));
            Assert.That(replayed.Key, Is.EqualTo(leg.Key));
        }
    }

    [Test]
    public void Presentation_frame_and_harness_read_the_same_combat_event_facts()
    {
        var slice = CombatVerticalSliceHarness.Run(7);

        Assert.That(slice.Frame.Events.Events, Is.EqualTo(slice.Events.Events));
        Assert.That(slice.Frame.Events.Execution, Is.EqualTo(slice.Events.Execution));
        Assert.That(slice.Events.Targetability, Is.Not.Empty, "Slice A facts are consumed, not rebuilt");
    }

    [Test]
    public void Tactical_zoom_draws_each_fired_engagement_with_its_own_motion_cue()
    {
        var slice = CombatVerticalSliceHarness.Run(7);
        var tactical = slice.Maps.Single(m => m.Zoom == CombatZoomBand.Tactical).Map;

        var fired = slice.Legs.Where(l => l.ExpectedPermitted).ToArray();
        Assert.That(tactical.Effects.Select(e => e.Key), Is.EquivalentTo(fired.Select(l => l.Key)));
        Assert.That(tactical.Effects.All(e => e.Count == 1), Is.True);
        Assert.That(tactical.Effects.Select(e => e.LinePattern).Distinct().Count(), Is.EqualTo(3));
        Assert.That(tactical.EventLines, Has.Count.EqualTo(slice.Events.Events.Count));
    }

    [Test]
    public void Operational_zoom_keeps_per_engagement_identity_and_theater_zoom_flattens_motion()
    {
        var slice = CombatVerticalSliceHarness.Run(7);
        var operational = slice.Maps.Single(m => m.Zoom == CombatZoomBand.Operational).Map;
        var theater = slice.Maps.Single(m => m.Zoom == CombatZoomBand.Theater).Map;
        var tactical = slice.Maps.Single(m => m.Zoom == CombatZoomBand.Tactical).Map;

        Assert.That(operational.Effects.Select(e => e.LinePattern),
            Is.EquivalentTo(tactical.Effects.Select(e => e.LinePattern)));
        Assert.That(theater.Effects.All(e => e.LinePattern == "none" || e.Count == 1), Is.True);
        foreach (var leg in slice.Legs)
        {
            var operationalView = leg.Views.Single(v => v.Zoom == CombatZoomBand.Operational);
            Assert.That(operationalView.Effect.Count, Is.EqualTo(1), "a selected leg is never folded into a summary");
        }
    }

    [Test]
    public void Symbol_lod_is_identity_at_close_band_and_accounts_for_every_symbol_at_coarse_bands()
    {
        var slice = CombatVerticalSliceHarness.Run(7);

        var close = slice.SymbolLod.Single(l => l.Band == MapLodBand.Close);
        Assert.That(close.OutputCount, Is.EqualTo(slice.Symbols.Count));
        Assert.That(close.Clusters.All(c => !c.IsCluster), Is.True);
        foreach (var lod in slice.SymbolLod)
        {
            Assert.That(lod.Clusters.Sum(c => c.Count), Is.EqualTo(slice.Symbols.Count), lod.Band.ToString());
        }
    }

    [Test]
    public void Every_engagement_state_has_a_non_color_discriminator()
    {
        var slice = CombatVerticalSliceHarness.Run(7);

        var tactical = slice.Legs.Select(l => l.Views.Single(v => v.Zoom == CombatZoomBand.Tactical).Effect).ToArray();
        var signatures = tactical
            .Select(e => (e.WeaponFamilyId, e.Clearance, e.FamilyGlyph, e.LinePattern, e.DeclutterToken))
            .ToArray();
        var fired = tactical.Where(e => e.Clearance == "Cleared").ToArray();
        Assert.That(fired.Select(e => e.FamilyGlyph).Distinct().Count(), Is.EqualTo(3));
        Assert.That(fired.Select(e => e.LinePattern).Distinct().Count(), Is.EqualTo(3));
        Assert.That(fired.Select(e => e.DeclutterToken).Distinct().Count(), Is.EqualTo(3));
        Assert.That(fired.Select(e => e.MotionLabel).Distinct().Count(), Is.EqualTo(3));
        Assert.That(signatures.Distinct().Count(), Is.EqualTo(6));
        foreach (var effect in tactical)
        {
            Assert.That(effect.Label, Does.Contain(effect.FamilyGlyph));
            Assert.That(effect.Label, Does.Contain(effect.WeaponFamilyId));
            Assert.That(effect.Label, Does.Contain(effect.Clearance));
            Assert.That(effect.Label, Does.Contain(effect.Outcome));
            Assert.That(effect.Label, Does.Contain(effect.Affiliation));
        }
    }

    [Test]
    public void Explanation_cites_slice_a_targetability_and_execution_time_firing_solution()
    {
        var slice = CombatVerticalSliceHarness.Run(7);

        foreach (var leg in slice.Legs)
        {
            Assert.That(leg.Targetability, Is.Not.Null, leg.ShooterId);
            Assert.That(leg.Targetability!.ContactId, Is.EqualTo(CombatVerticalSliceHarness.ContactIdFor(leg.TargetId)));
            var expected = leg.HasFireControlTrack
                ? "Fire-control track present at execution"
                : "No fire-control track at execution";
            Assert.That(leg.Explanation.FiringSolution, Is.EqualTo(expected), leg.ShooterId);
            Assert.That(leg.Explanation.ContactConfidence, Is.Not.EqualTo(EngagementExplanationProjection.Unknown));
        }

        foreach (var leg in slice.Legs.Where(l => l.HasFireControlTrack))
        {
            Assert.That(leg.Targetability!.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted),
                $"{leg.ShooterId}: Slice A at command time, not after the kill");
            Assert.That(leg.Explanation.HardConstraints.Single(c => c.Name == EngagementConstraintNames.Track).State,
                Is.EqualTo(EngagementConstraintState.Satisfied));
        }

        foreach (var leg in slice.Legs.Where(l => !l.HasFireControlTrack))
        {
            Assert.That(leg.Targetability!.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
            Assert.That(leg.Targetability.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.NoFireControl));
        }

        var outOfEnvelope = slice.Legs.Single(l => l.WeaponFamilyId == "Gun" && !l.ExpectedPermitted);
        Assert.That(outOfEnvelope.Targetability!.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted),
            "targetable track, refused by the weapon envelope at execution");
        Assert.That(outOfEnvelope.Explanation.HardConstraints
            .Single(c => c.Name == EngagementConstraintNames.Engagement).State,
            Is.EqualTo(EngagementConstraintState.Violated));
    }

    [Test]
    public void Run_is_replay_stable_for_the_same_seed()
    {
        var first = CombatVerticalSliceHarness.Run(7);
        var second = CombatVerticalSliceHarness.Run(7);

        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
        Assert.That(second.Events.Events, Is.EqualTo(first.Events.Events));
        Assert.That(second.Scenario.Log.ComputeFingerprint(), Is.EqualTo(first.Scenario.Log.ComputeFingerprint()));
    }

    [Test]
    public void Harness_reads_combat_facts_only_through_the_combat_event_contract()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "ProjectAegis.Delegation.UnityAdapter", "Baltic", "CombatVerticalSliceHarness.cs"));

        Assert.That(source, Does.Not.Contain(".Engagements"));
        Assert.That(source, Does.Not.Contain(".EngagementOutcomes"));
        Assert.That(source, Does.Not.Contain("17144800277401907079"));
        Assert.That(source, Does.Not.Contain("tests/regression"));
    }

    [Test]
    public void Fixture_is_synthetic_and_isolated_from_baltic_v2_goldens()
    {
        var root = FindRepoRoot();
        var fixture = File.ReadAllText(Path.Combine(root, "data", "scenarios", "slice-b-combat-acceptance.json"));

        Assert.That(fixture, Does.Contain("\"synthetic\": true"));
        Assert.That(fixture, Does.Not.Contain("baltic-v2"));
        Assert.That(fixture, Does.Not.Contain("17144800277401907079"));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("ProjectAegis.sln not found");
    }
}
