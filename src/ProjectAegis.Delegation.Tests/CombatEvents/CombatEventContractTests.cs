using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.CombatEvents;

/// <summary>
/// DRG-165: combat event projection contract — Slice A targetability consumption, execution facts,
/// replay stability, and the no-overlay-rebuild fence.
/// </summary>
[TestFixture]
public sealed class CombatEventContractTests
{
    [Test]
    public void Assess_other_shooters_permitted_row_does_not_authorize_requested_shooter()
    {
        var row = ForShooter(PermittedTargetability().Contacts.Single(), "u2");
        var snapshot = CombatEventProjection.Project(Input("hostile-1", true), null,
            new TargetabilityAcceptSnapshot(new[] { row }));

        Assert.That(snapshot.Events[^1].Phase, Is.EqualTo(CombatEventPhase.AuthorizationRefused));
        Assert.That(snapshot.Targetability, Is.Empty);
    }

    [Test]
    public void Assess_matching_withheld_row_is_not_overridden_by_another_shooters_permission()
    {
        var permitted = ForShooter(PermittedTargetability().Contacts.Single(), "u2");
        var withheld = PermittedTargetability().Contacts.Single() with
        {
            Disposition = TargetabilityAcceptDisposition.Withheld,
            WithheldCauseCode = TargetabilityAcceptCauseCodes.RoeHoldFire,
            Authority = C2AuthorityProjector.Project(new C2AuthorityProjectionContext(
                RoeLevel.HoldFire, SkillLane.Read, RequiredApproval.None, TrackSource.Organic, true)),
        };
        var snapshot = CombatEventProjection.Project(Input("hostile-1", true), null,
            new TargetabilityAcceptSnapshot(new[] { permitted, withheld }));

        Assert.That(snapshot.Events[^1].Phase, Is.EqualTo(CombatEventPhase.AuthorizationRefused));
        Assert.That(snapshot.Events[^1].Outcome, Is.EqualTo(withheld.WithheldCauseCode));
        Assert.That(snapshot.Targetability.Single().Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(snapshot.Targetability.Single().ShooterId, Is.EqualTo("u1"));
    }

    [Test]
    public void Assess_unlinked_shooter_identity_fails_closed()
    {
        var row = PermittedTargetability().Contacts.Single();
        row = row with
        {
            SensorToShooter = row.SensorToShooter! with
            {
                Links = row.SensorToShooter.Links.Select(l => l.Kind == SensorToShooterLinkKind.EligibleShooter
                    ? l with { IsLinked = false } : l).ToArray(),
            },
        };
        var snapshot = CombatEventProjection.Project(Input("hostile-1", true), null,
            new TargetabilityAcceptSnapshot(new[] { row }));

        Assert.That(snapshot.Events[^1].Phase, Is.EqualTo(CombatEventPhase.AuthorizationRefused));
        Assert.That(snapshot.Targetability, Is.Empty);
    }

    [Test]
    public void Log_build_does_not_attach_other_shooters_facts_to_the_same_target()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(0, 2, 2, new TargetId("u1"), 41, true,
            VictimTargetId: new TargetId("hostile-1"), WeaponFamilyId: "Missile"));
        var other = ForShooter(PermittedTargetability().Contacts.Single(), "u2");
        var snapshot = CombatEventLogProjection.Build(log, 2, new TargetabilityAcceptSnapshot(new[] { other }));

        Assert.That(snapshot.Targetability, Is.Empty);
        Assert.That(snapshot.Events.Any(e => e.Phase == CombatEventPhase.Firing), Is.True);
    }

    [Test]
    public void Fingerprint_distinguishes_targetability_for_different_shooters()
    {
        var row = PermittedTargetability().Contacts.Single();
        var first = new CombatEventSnapshot(Array.Empty<CombatEvent>())
        {
            Targetability = new[] { CombatTargetabilityFact.FromRow(row) },
        };
        var second = first with
        {
            Targetability = new[] { CombatTargetabilityFact.FromRow(ForShooter(row, "u2")) },
        };

        Assert.That(CombatEventFingerprint.Compute(second), Is.Not.EqualTo(CombatEventFingerprint.Compute(first)));
    }

    private static TargetabilityAcceptContactRow ForShooter(TargetabilityAcceptContactRow row, string shooterId) =>
        row with
        {
            SensorToShooter = row.SensorToShooter! with
            {
                Links = row.SensorToShooter.Links.Select(l => l.Kind == SensorToShooterLinkKind.EligibleShooter
                    ? l with { UnitId = shooterId } : l).ToArray(),
            },
        };

    [Test]
    public void Assess_permitted_slice_a_row_authorizes_and_attaches_targetability_fact()
    {
        var targetability = PermittedTargetability();
        var row = targetability.Contacts.Single();

        var snapshot = CombatEventProjection.Project(Input("hostile-1", canFire: true), log: null, targetability);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
        }));
        var fact = snapshot.Targetability.Single();
        Assert.That(fact.ContactId, Is.EqualTo("c1"));
        Assert.That(fact.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(fact.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(fact.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.None));
        Assert.That(fact.Confidence, Is.EqualTo(row.Provenance!.Confidence));
        Assert.That(fact.SensorToShooterComplete, Is.True);
        Assert.That(fact.RoeAllowsEngage, Is.EqualTo(row.Authority.Roe.EngageAllowedByRoe));
        Assert.That(fact.TargetingDisposition, Is.EqualTo(C2AuthorityDisposition.Permitted));
    }

    [Test]
    public void Assess_withheld_slice_a_row_emits_named_targetability_refusal_even_when_preview_can_fire()
    {
        var targetability = Targetability(fireControl: new StubFireControl());
        var row = targetability.Contacts.Single();
        Assert.That(row.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));

        var snapshot = CombatEventProjection.Project(Input("hostile-1", canFire: true), log: null, targetability);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.AuthorizationRefused,
        }));
        var refused = snapshot.Events[^1];
        Assert.That(refused.Outcome, Is.EqualTo(row.WithheldCauseCode));
        Assert.That(refused.ExplanationRef, Is.EqualTo($"targetability:{row.WithheldCauseCode}"));
        Assert.That(snapshot.Targetability.Single().Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
    }

    [Test]
    public void Assess_supplied_slice_a_without_target_row_fails_closed_as_missing_provenance()
    {
        var snapshot = CombatEventProjection.Project(
            Input("hostile-9", canFire: true), log: null, PermittedTargetability());

        var refused = snapshot.Events[^1];
        Assert.That(refused.Phase, Is.EqualTo(CombatEventPhase.AuthorizationRefused));
        Assert.That(refused.Outcome, Is.EqualTo(TargetabilityAcceptCauseCodes.MissingProvenance));
        Assert.That(refused.ExplanationRef,
            Is.EqualTo($"targetability:{TargetabilityAcceptCauseCodes.MissingProvenance}"));
        Assert.That(snapshot.Targetability, Is.Empty);
    }

    [Test]
    public void Assess_policy_denial_outranks_slice_a_withheld_cause()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 1.2, 2, new AgentId("a1"), new TargetId("u1"), 0,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));

        var snapshot = CombatEventProjection.Project(
            Input("hostile-1", canFire: true), log, Targetability(fireControl: new StubFireControl()));

        Assert.That(snapshot.Events[^1].ExplanationRef, Is.EqualTo($"policy:{FireAbortReason.RoeHoldFire}"));
    }

    [Test]
    public void Assess_without_slice_a_snapshot_keeps_legacy_lifecycle()
    {
        var snapshot = CombatEventProjection.Project(Input("hostile-1", canFire: true), log: null);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
        }));
        Assert.That(snapshot.Targetability, Is.Empty);
    }

    [Test]
    public void Assess_launched_leg_carries_execution_fact_for_correlation()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            1, 1.5, 2, new TargetId("u1"), 42, Launched: true,
            VictimTargetId: new TargetId("hostile-1"), WeaponFamilyId: "Missile",
            SalvoSize: 2, HasFireControlTrack: true));

        var snapshot = CombatEventProjection.Project(Input("hostile-1", canFire: true), log);

        var fact = snapshot.Execution.Single();
        Assert.That(fact.CorrelationId, Is.EqualTo(42UL));
        Assert.That(fact.ShooterId, Is.EqualTo("u1"));
        Assert.That(fact.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(fact.HasFireControlTrack, Is.True);
        Assert.That(fact.SalvoSize, Is.EqualTo(2));
    }

    [Test]
    public void Log_build_projects_execution_facts_keyed_by_log_correlation_in_sequence_order()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 2, 2, new TargetId("u1"), 41, true, "Launched",
            new TargetId("hostile-1"), "Missile", 2, HasFireControlTrack: true));
        log.AppendEngagement(new EngagementRecord(
            0, 3, 3, new TargetId("u2"), 0, false, "ROE_WEAPONS_TIGHT",
            new TargetId("hostile-2"), "Gun"));
        log.AppendEngagement(new EngagementRecord(
            0, 9, 9, new TargetId("u3"), 50, true, "Launched"));

        var snapshot = CombatEventLogProjection.Build(log, 5);

        Assert.That(snapshot.Execution.Select(f => f.CorrelationId), Is.EqualTo(new[]
        {
            log.Engagements[0].SequenceId,
            log.Engagements[1].SequenceId,
        }));
        Assert.That(snapshot.Execution[0].HasFireControlTrack, Is.True);
        Assert.That(snapshot.Execution[0].SalvoSize, Is.EqualTo(2));
        Assert.That(snapshot.Execution[1].HasFireControlTrack, Is.Null);
        Assert.That(snapshot.Execution[1].TargetId, Is.EqualTo("hostile-2"));
    }

    [Test]
    public void Log_build_with_slice_a_attaches_facts_for_event_targets_without_altering_phases()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 2, 2, new TargetId("u1"), 41, true, "Launched",
            new TargetId("hostile-1"), "Missile"));

        var plain = CombatEventLogProjection.Build(log, 2);
        var augmented = CombatEventLogProjection.Build(log, 2, PermittedTargetability());

        Assert.That(augmented.Events, Is.EqualTo(plain.Events));
        Assert.That(augmented.Targetability.Select(f => f.TargetId), Is.EqualTo(new[] { "hostile-1" }));
        Assert.That(plain.Targetability, Is.Empty);
    }

    [Test]
    public void Replay_same_inputs_produce_identical_ordered_events_and_fingerprint()
    {
        var first = RunReplayLeg();
        var second = RunReplayLeg();

        Assert.That(second.Events, Is.EqualTo(first.Events));
        Assert.That(second.Targetability, Is.EqualTo(first.Targetability));
        Assert.That(second.Execution, Is.EqualTo(first.Execution));
        Assert.That(CombatEventFingerprint.Compute(second), Is.EqualTo(CombatEventFingerprint.Compute(first)));
    }

    [Test]
    public void Fingerprint_covers_targetability_and_execution_facts()
    {
        var events = RunReplayLeg();
        var eventsOnly = new CombatEventSnapshot(events.Events);
        var flipped = events with
        {
            Targetability = events.Targetability
                .Select(f => f with { Disposition = TargetabilityAcceptDisposition.Withheld })
                .ToArray(),
        };

        Assert.That(CombatEventFingerprint.Compute(eventsOnly), Does.Not.Contain("|ta="));
        Assert.That(CombatEventFingerprint.Compute(eventsOnly), Does.Not.Contain("|ex="));
        Assert.That(CombatEventFingerprint.Compute(events), Does.Contain("|ta="));
        Assert.That(CombatEventFingerprint.Compute(events), Does.Contain("|ex="));
        Assert.That(CombatEventFingerprint.Compute(flipped), Is.Not.EqualTo(CombatEventFingerprint.Compute(events)));
    }

    [Test]
    public void Fact_dtos_omit_ui_derived_truth_fields()
    {
        var uiDerivedNames = new[] { "Selection", "Hover", "Camera", "Panel", "Visible", "Chrome", "IsSelected" };
        foreach (var type in new[] { typeof(CombatTargetabilityFact), typeof(CombatExecutionFact) })
        {
            foreach (var prop in type.GetProperties())
            {
                Assert.That(
                    uiDerivedNames.Any(n => prop.Name.Contains(n, StringComparison.OrdinalIgnoreCase)),
                    Is.False,
                    $"{type.Name}.{prop.Name} must not encode UI-derived truth");
            }
        }
    }

    [Test]
    public void Combat_event_sources_do_not_rebuild_sensor_envelope_or_datalink_overlays()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null, "repo root not found");
        var forbidden = new[]
        {
            "SensorToShooterProjection.",
            "ContactProvenanceProjection.",
            "TargetabilityAcceptProjection.",
            "KillChainContactStateProjection.",
            "C2AuthorityProjector.",
            "CatalogEngageEnvelope",
            "CatalogEnvelopeRangeResolver",
            "MapEnvelopePlatformResolver",
            "WeaponEnvelope",
            "DatalinkPictureProjection",
            "DatalinkUnitPairFeed",
            "DatalinkSidePictureMerger",
            "TacticalOverlayProjection",
            "SensorClassify",
        };
        var files = Directory.GetFiles(
            Path.Combine(root!, "src", "ProjectAegis.Delegation", "CombatEvents"), "*.cs");
        Assert.That(files, Is.Not.Empty);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                Assert.That(text, Does.Not.Contain(token), $"{Path.GetFileName(file)} must not reference {token}");
            }
        }
    }

    private static CombatEventSnapshot RunReplayLeg()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            1, 1.5, 2, new TargetId("u1"), 42, Launched: true,
            VictimTargetId: new TargetId("hostile-1"), WeaponFamilyId: "Missile",
            SalvoSize: 2, HasFireControlTrack: true));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 3.0, 5, new TargetId("u1"), new TargetId("hostile-1"), 42,
            EngagementOutcomeCodes.Kill, 0.1));
        return CombatEventProjection.Project(Input("hostile-1", canFire: true), log, PermittedTargetability());
    }

    private static CombatEngageAssessInput Input(string targetId, bool canFire) =>
        new(
            ShooterId: "u1",
            TargetId: targetId,
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 42,
            Preview: new EngagePreview(canFire ? "DLZ: In" : "DLZ: Out", canFire, canFire ? null : "DLZ_OUT"));

    private static TargetabilityAcceptSnapshot PermittedTargetability() =>
        Targetability(fireControl: new StubFireControl("c1"));

    private static TargetabilityAcceptSnapshot Targetability(IKillChainFireControlSource fireControl)
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(0, 1, 1, "u1", "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(0, 5, 5, "u1", "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(new ContactChangeRecord(0, 9, 9, "u1", "c1", "hostile-1", "Classified", "Identified"));
        return TargetabilityAcceptProjection.Project(
            log,
            currentSimTick: 9,
            new C2AuthorityProjectionContext(
                RoeLevel.WeaponsFree, SkillLane.Read, RequiredApproval.None, TrackSource.Organic, true),
            fireControl: fireControl,
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private sealed class FixedShooterSource : ISensorToShooterShooterSource
    {
        private readonly SensorToShooterShooterCandidate[] _candidates;

        public FixedShooterSource(params SensorToShooterShooterCandidate[] candidates) =>
            _candidates = candidates;

        public IReadOnlyList<SensorToShooterShooterCandidate> GetCandidatesForTarget(string targetId) =>
            _candidates;
    }

    private sealed class StubFireControl : IKillChainFireControlSource
    {
        private readonly HashSet<string> _contactIds;

        public StubFireControl(params string[] contactIds) =>
            _contactIds = new HashSet<string>(contactIds, StringComparer.Ordinal);

        public bool HasFireControlTrack(string contactId, string targetId) =>
            _contactIds.Contains(contactId);
    }
}
