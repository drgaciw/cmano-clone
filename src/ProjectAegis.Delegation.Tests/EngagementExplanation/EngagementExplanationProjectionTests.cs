using System.Reflection;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.EngagementExplanation;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.EngagementExplanation;

/// <summary>DRG-168: engagement explanation surface read from combat-event and explanation projections only.</summary>
[TestFixture]
public sealed class EngagementExplanationProjectionTests
{
    [Test]
    public void Available_leg_explains_why_permitted_with_satisfied_constraints()
    {
        var events = CombatEventProjection.Project(Input(canFire: true), log: null, Targetability(fireControl: true));

        var surface = EngagementExplanationProjection.Build(events, "u1", "hostile-1", 42);

        Assert.That(surface.Status, Is.EqualTo(EngagementExplanationStatus.Available));
        Assert.That(surface.Headline, Does.StartWith("Available"));
        Assert.That(surface.HardConstraints.Single(c => c.Name == EngagementConstraintNames.Track).State,
            Is.EqualTo(EngagementConstraintState.Satisfied));
        Assert.That(surface.HardConstraints.Single(c => c.Name == EngagementConstraintNames.FiringSolution).State,
            Is.EqualTo(EngagementConstraintState.Satisfied));
        Assert.That(surface.DoctrineConstraints.Single(c => c.Name == EngagementConstraintNames.Roe).State,
            Is.EqualTo(EngagementConstraintState.Satisfied));
        Assert.That(surface.DoctrineConstraints.Single(c => c.Name == EngagementConstraintNames.TargetingAuthority).State,
            Is.EqualTo(EngagementConstraintState.Satisfied));
        Assert.That(surface.ContactConfidence, Is.EqualTo(events.Targetability.Single().Confidence.ToString()));
        Assert.That(surface.Weapon, Does.Contain(CatalogWeaponIds.MvpDefault));
        Assert.That(surface.FiringSolution, Does.Contain("complete"));
        Assert.That(surface.ActionableReason, Is.Null);
        Assert.That(surface.ExplanationRef, Is.EqualTo(EngageExplainProjection.CanFireLabel));
        Assert.That(
            surface.HardConstraints.Concat(surface.DoctrineConstraints).Select(c => c.Text),
            Has.All.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Selected_leg_explains_weapon_salvo_and_firing_solution_at_execution()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 2, 2, new TargetId("u1"), 41, true, "Launched",
            new TargetId("hostile-1"), "Missile", 2, HasFireControlTrack: true));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 3, 3, new TargetId("u1"), new TargetId("hostile-1"), 41,
            EngagementOutcomeCodes.Kill, 0.1));
        var events = CombatEventLogProjection.Build(log, 3);

        var surface = EngagementExplanationProjection.Build(
            events, "u1", "hostile-1", log.Engagements[0].SequenceId);

        Assert.That(surface.Status, Is.EqualTo(EngagementExplanationStatus.Selected));
        Assert.That(surface.Headline, Does.StartWith("Selected"));
        Assert.That(surface.Headline, Does.Contain(EngagementOutcomeCodes.Kill));
        Assert.That(surface.Weapon, Does.Contain("Missile"));
        Assert.That(surface.Weapon, Does.Contain("Salvo: 2"));
        Assert.That(surface.FiringSolution, Is.EqualTo("Fire-control track present at execution"));
        Assert.That(surface.ContactConfidence, Is.EqualTo(EngagementExplanationProjection.Unknown));
        Assert.That(surface.ActionableReason, Is.Null);
    }

    [Test]
    public void Refused_targetability_hard_constraint_states_actionable_reason()
    {
        var events = CombatEventProjection.Project(Input(canFire: true), log: null, Targetability(fireControl: false));
        var cause = events.Targetability.Single().WithheldCauseCode;

        var surface = EngagementExplanationProjection.Build(events, "u1", "hostile-1", 42);

        Assert.That(surface.Status, Is.EqualTo(EngagementExplanationStatus.Refused));
        Assert.That(surface.Headline, Does.StartWith("Refused"));
        var violated = surface.HardConstraints.Where(c => c.State == EngagementConstraintState.Violated).ToArray();
        Assert.That(violated.Select(c => c.Code), Does.Contain(cause));
        Assert.That(surface.DoctrineConstraints.Any(c => c.State == EngagementConstraintState.Violated), Is.False);
        Assert.That(surface.FiringSolution, Does.Contain("broken"));
        Assert.That(surface.ActionableReason, Is.Not.Null.And.Not.Empty);
        Assert.That(surface.ActionableReason, Does.Contain("fire-control"));
        Assert.That(surface.ExplanationRef, Is.EqualTo($"targetability:{cause}"));
    }

    [Test]
    public void Refused_policy_denial_is_a_doctrine_constraint_with_approval_action()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 1.2, 2, new AgentId("a1"), new TargetId("u1"), 0,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));
        var events = CombatEventProjection.Project(Input(canFire: true), log, Targetability(fireControl: true));

        var surface = EngagementExplanationProjection.Build(events, "u1", "hostile-1", 42);

        Assert.That(surface.Status, Is.EqualTo(EngagementExplanationStatus.Refused));
        var doctrine = surface.DoctrineConstraints.Single(c => c.State == EngagementConstraintState.Violated);
        Assert.That(doctrine.Code, Is.EqualTo(nameof(FireAbortReason.RoeHoldFire)));
        Assert.That(surface.HardConstraints.Any(c => c.State == EngagementConstraintState.Violated), Is.False);
        Assert.That(surface.ActionableReason, Does.Contain("approval"));
    }

    [Test]
    public void Refused_wra_range_policy_denial_recommends_closing_range()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 1.2, 2, new AgentId("a1"), new TargetId("u1"), 0,
            FireAbortReason.WraRange, OrderKind.Engage));
        var events = CombatEventProjection.Project(Input(canFire: true), log, Targetability(fireControl: true));

        var surface = EngagementExplanationProjection.Build(events, "u1", "hostile-1", 42);

        Assert.That(surface.Status, Is.EqualTo(EngagementExplanationStatus.Refused));
        var doctrine = surface.DoctrineConstraints.Single(c => c.State == EngagementConstraintState.Violated);
        Assert.That(doctrine.Code, Is.EqualTo(nameof(FireAbortReason.WraRange)));
        Assert.That(surface.ActionableReason, Is.EqualTo("Close to within the weapon launch zone before engaging."));
        Assert.That(surface.ActionableReason, Does.Not.Contain("approval"));
    }

    [Test]
    public void Refused_domain_abort_is_hard_constraint_with_weapon_selection_action()
    {
        var input = Input(canFire: false) with
        {
            Preview = new EngagePreview("Domain", false, AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK),
        };
        var events = CombatEventProjection.Project(input, log: null);

        var surface = EngagementExplanationProjection.Build(events, "u1", "hostile-1", 42);

        Assert.That(surface.Status, Is.EqualTo(EngagementExplanationStatus.Refused));
        Assert.That(surface.HardConstraints.Single(c => c.State == EngagementConstraintState.Violated).Code,
            Is.EqualTo(AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK));
        Assert.That(surface.ActionableReason, Does.Contain("weapon"));
        Assert.That(surface.HardConstraints.Single(c => c.Name == EngagementConstraintNames.Track).State,
            Is.EqualTo(EngagementConstraintState.Unknown));
    }

    [Test]
    public void Refused_without_remedy_reports_no_actionable_reason()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 4, 4, new TargetId("u1"), 0, false, AbortReasonCatalog.Engage.TARGET_DESTROYED,
            new TargetId("hostile-1"), "Gun"));
        var events = CombatEventLogProjection.Build(log, 4);

        var surface = EngagementExplanationProjection.Build(
            events, "u1", "hostile-1", log.Engagements[0].SequenceId);

        Assert.That(surface.Status, Is.EqualTo(EngagementExplanationStatus.Refused));
        Assert.That(surface.ActionableReason, Is.Null);
        Assert.That(surface.Headline, Does.Contain(AbortReasonCatalog.Engage.TARGET_DESTROYED));
    }

    [Test]
    public void Unmatched_leg_returns_empty_surface()
    {
        var events = CombatEventProjection.Project(Input(canFire: true), log: null);

        Assert.That(EngagementExplanationProjection.Build(events, "u1", "hostile-1", 999),
            Is.SameAs(EngagementExplanationSurface.Empty));
        Assert.That(EngagementExplanationProjection.Build(null, "u1", "hostile-1", 42),
            Is.SameAs(EngagementExplanationSurface.Empty));
    }

    [Test]
    public void Intent_only_leg_is_unknown_not_silently_available()
    {
        var events = CombatEventProjection.Project(Input(canFire: true) with { Preview = null }, log: null);

        var surface = EngagementExplanationProjection.Build(events, "u1", "hostile-1", 42);

        Assert.That(surface.Status, Is.EqualTo(EngagementExplanationStatus.Unknown));
        Assert.That(surface.FiringSolution, Is.EqualTo(EngagementExplanationProjection.Unknown));
    }

    [Test]
    public void Replay_same_inputs_produce_identical_surface_and_fingerprint()
    {
        string Run(bool fireControl) => EngagementExplanationFingerprint.Compute(
            EngagementExplanationProjection.Build(
                CombatEventProjection.Project(Input(canFire: true), log: null, Targetability(fireControl)),
                "u1", "hostile-1", 42));

        Assert.That(Run(fireControl: true), Is.EqualTo(Run(fireControl: true)));
        Assert.That(Run(fireControl: false), Is.EqualTo(Run(fireControl: false)));
        Assert.That(Run(fireControl: true), Is.Not.EqualTo(Run(fireControl: false)));
        Assert.That(EngagementExplanationFingerprint.Compute(EngagementExplanationSurface.Empty), Is.EqualTo("eex:empty"));
    }

    [Test]
    public void Fence_public_api_consumes_only_combat_event_snapshot_and_leg_keys()
    {
        var allowed = new[] { typeof(CombatEventSnapshot), typeof(string), typeof(ulong) };
        var methods = typeof(EngagementExplanationProjection)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.That(methods, Is.Not.Empty);
        foreach (var method in methods)
        {
            foreach (var parameter in method.GetParameters())
            {
                Assert.That(allowed, Does.Contain(parameter.ParameterType),
                    $"{method.Name}({parameter.Name}) must read only combat-event / explanation projections");
            }
        }
    }

    [Test]
    public void Fence_sources_issue_no_sim_queries_or_rebuilt_projections()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null, "repo root not found");
        var forbidden = new[]
        {
            "DecisionLog",
            "ISimWorldSnapshot",
            "ObservedState",
            "SimulationSession",
            "DelegationOrchestrator",
            "DelegationBridge",
            "IOrderSink",
            "SliceAContactFrame",
            "TargetabilityAcceptProjection.",
            "SensorToShooterProjection.",
            "ContactProvenanceProjection.",
            "KillChainContactStateProjection.",
            "C2AuthorityProjector.",
            "EngagePreviewProjection",
            "ContactCombatCard",
            "CombatEventProjection.",
            "CombatEventLogProjection.",
            "ICatalogReader",
        };
        var files = Directory.GetFiles(
            Path.Combine(root!, "src", "ProjectAegis.Delegation", "EngagementExplanation"), "*.cs");
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

    [Test]
    public void Dto_surface_omits_ui_derived_truth_fields()
    {
        var uiDerivedNames = new[] { "Selection", "Hover", "Camera", "Panel", "Visible", "Chrome", "IsSelected" };
        foreach (var type in new[] { typeof(EngagementExplanationSurface), typeof(EngagementConstraint) })
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

    private static CombatEngageAssessInput Input(bool canFire) =>
        new(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 42,
            Preview: new EngagePreview(canFire ? "DLZ: In" : "DLZ: Out", canFire, canFire ? null : "DLZ_OUT"));

    private static TargetabilityAcceptSnapshot Targetability(bool fireControl)
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
            fireControl: new StubFireControl(fireControl),
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
        private readonly bool _hasTrack;

        public StubFireControl(bool hasTrack) => _hasTrack = hasTrack;

        public bool HasFireControlTrack(string contactId, string targetId) => _hasTrack;
    }
}
