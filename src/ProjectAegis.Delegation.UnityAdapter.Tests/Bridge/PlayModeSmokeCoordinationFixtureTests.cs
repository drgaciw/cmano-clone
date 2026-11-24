namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using C2Nodes;
using Controllers;
using Core;
using MissionIntent;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using Targets;
using NUnit.Framework;

/// <summary>
/// Headless TDD acceptance tests for DRG-239 ([HYG-05] smoke-scene task-group + coverage fixture).
/// Exercises the live <see cref="PlayModeSmokeCoordinationFactsProvider"/> wired with
/// <see cref="PlayModeSmokeOrbatSeeder"/> through <see cref="CoordinationBridge"/> and
/// <see cref="CoordinationCommandBridge"/> without touching <c>DelegationBridge.cs</c>.
///
/// Note: This fixture enables owner Play Mode signoff for DRG-175, DRG-185, DRG-192, DRG-235,
/// and DRG-244, and does NOT replace DRG-208 owner acceptance.
/// </summary>
[TestFixture]
public sealed class PlayModeSmokeCoordinationFixtureTests
{
    [Test]
    public void Default_smoke_seed_registers_named_task_group_with_authored_package_intent_and_valid_coverage_polygon()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeed(bridge), Is.True);

        // Preserve existing smoke-ORBAT member count invariant (u1 + hostile-1).
        Assert.That(bridge.Registry.CollectMemberIds(), Has.Count.EqualTo(2));
        Assert.That(
            bridge.Registry.TryGetBinding(new TargetId(PlayModeSmokeOrbatSeeder.TaskGroupId), out var groupBinding),
            Is.True);
        Assert.That(groupBinding.Target, Is.InstanceOf<GroupTarget>());
        Assert.That(groupBinding.Target.Slot.Active, Is.InstanceOf<HumanController>());

        var baseSnapshot = new MutableSmokeSnapshot(simTime: 2.0, aliveIds: [PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.HostileUnitId]);
        var liveSnapshot = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(baseSnapshot);

        // Uses the exact single-snapshot overload invoked by DelegationBridgeHost.RefreshPresentation.
        var coordination = CoordinationBridge.Build(bridge, liveSnapshot);

        Assert.That(coordination.Groups, Has.Count.EqualTo(1));
        var group = coordination.Groups[0];
        Assert.That(group.Coordination.GroupId, Is.EqualTo(PlayModeSmokeCoordinationFactsProvider.TaskGroupId));
        Assert.That(group.Coordination.AssignedPackageId, Is.EqualTo(PlayModeSmokeCoordinationFactsProvider.PackageId));
        Assert.That(group.Coordination.Members, Is.EqualTo(new[] { PlayModeSmokeOrbatSeeder.FriendlyUnitId }));
        Assert.That(group.Coordination.GapCode, Is.EqualTo("NONE"));
        Assert.That(group.Intent.IntentCode, Is.EqualTo(MissionIntentCode.Hold));
        Assert.That(group.Intent.AdvisoryRetask, Is.EqualTo(MissionIntentRetaskAdvice.Withdraw));

        var leadEffect = group.Effects.Single(e => e.UnitId == PlayModeSmokeOrbatSeeder.FriendlyUnitId);
        Assert.That(leadEffect.Role, Is.EqualTo(C2NodeRole.C2));
        Assert.That(leadEffect.State, Is.EqualTo(CoordinationEffectState.Available));
        Assert.That(leadEffect.Coverage.Status, Is.EqualTo(CoverageStatus.Covered));
        AssertValidSourceLabelledPolygon(leadEffect.Coverage.Geometry, PlayModeSmokeCoordinationFactsProvider.C2CoverageSourceRef);
    }

    [Test]
    public void Escort_task_group_fixture_exposes_two_friendly_members_with_distinct_roles_and_source_labelled_polygons()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        // Idempotent second call must not throw or duplicate members.
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);

        var snapshot = new MutableSmokeSnapshot(
            simTime: 2.0,
            aliveIds: [PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId, PlayModeSmokeOrbatSeeder.HostileUnitId]);
        var facts = PlayModeSmokeCoordinationFactsProvider.CreateDefault();

        var coordination = CoordinationBridge.Build(bridge, snapshot, facts);
        var group = coordination.Groups.Single();

        Assert.That(group.Coordination.GroupId, Is.EqualTo(PlayModeSmokeOrbatSeeder.TaskGroupId));
        Assert.That(group.Coordination.Members, Is.EqualTo(new[]
        {
            PlayModeSmokeOrbatSeeder.FriendlyUnitId,
            PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId,
        }));
        Assert.That(group.Coordination.AssignedPackageId, Is.EqualTo(PlayModeSmokeCoordinationFactsProvider.PackageId));
        Assert.That(group.Gaps, Is.Empty);

        var lead = group.Effects.Single(e => e.UnitId == PlayModeSmokeOrbatSeeder.FriendlyUnitId);
        var escort = group.Effects.Single(e => e.UnitId == PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId);

        Assert.That(lead.Role, Is.EqualTo(C2NodeRole.C2));
        Assert.That(lead.Coverage.Status, Is.EqualTo(CoverageStatus.Covered));
        AssertValidSourceLabelledPolygon(lead.Coverage.Geometry, PlayModeSmokeCoordinationFactsProvider.C2CoverageSourceRef);

        Assert.That(escort.Role, Is.EqualTo(C2NodeRole.Shooter));
        Assert.That(escort.Coverage.Status, Is.EqualTo(CoverageStatus.Covered));
        AssertValidSourceLabelledPolygon(escort.Coverage.Geometry, PlayModeSmokeCoordinationFactsProvider.EscortCoverageSourceRef);
    }

    [Test]
    public void Group_hold_and_withdraw_submit_through_command_facade_and_expand_to_all_members()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        bridge.BeginExecution();

        var baseSnapshot = new MutableSmokeSnapshot(
            simTime: 3.0,
            aliveIds: [PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId, PlayModeSmokeOrbatSeeder.HostileUnitId]);
        var liveSnapshot = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(baseSnapshot);
        var downstream = new RecordingSink();

        // 1. Group Hold via CoordinationCommandBridge
        var hold = CoordinationCommandBridge.Submit(
            bridge,
            liveSnapshot,
            PlayModeSmokeOrbatSeeder.TaskGroupId,
            CoordinationDecision.Hold);
        Assert.That(hold.Accepted, Is.True, hold.FailureReason);
        Assert.That(hold.AffectedUnitIds, Is.EqualTo(new[]
        {
            PlayModeSmokeOrbatSeeder.FriendlyUnitId,
            PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId,
        }));
        Assert.That(hold.ApprovedScope, Is.Not.Null);

        var holdSink = new CoordinationOrderSink(bridge.Registry, liveSnapshot, downstream, [hold.ApprovedScope!]);
        bridge.Tick(liveSnapshot, holdSink);

        Assert.That(downstream.Applied.Where(a => a.Order.Kind == OrderKind.Hold).Select(a => a.Order.Target.Value),
            Is.EqualTo(new[] { PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId }));

        // 2. Group Withdraw via CoordinationCommandBridge
        baseSnapshot.SimTime = 4.0;
        var withdraw = CoordinationCommandBridge.Submit(
            bridge,
            liveSnapshot,
            PlayModeSmokeOrbatSeeder.TaskGroupId,
            CoordinationDecision.Withdraw);
        Assert.That(withdraw.Accepted, Is.True, withdraw.FailureReason);
        Assert.That(withdraw.AffectedUnitIds, Is.EqualTo(new[]
        {
            PlayModeSmokeOrbatSeeder.FriendlyUnitId,
            PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId,
        }));
        Assert.That(withdraw.ApprovedScope, Is.Not.Null);

        var withdrawSink = new CoordinationOrderSink(bridge.Registry, liveSnapshot, downstream, [withdraw.ApprovedScope!]);
        bridge.Tick(liveSnapshot, withdrawSink);

        Assert.That(downstream.Applied.Where(a => a.Order.Kind == OrderKind.ReturnToBase).Select(a => a.Order.Target.Value),
            Is.EqualTo(new[] { PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId }));

        // 3. Advisory-only / constrained Reattack must stay rejected.
        baseSnapshot.SimTime = 5.0;
        var reattack = CoordinationCommandBridge.Submit(
            bridge,
            liveSnapshot,
            PlayModeSmokeOrbatSeeder.TaskGroupId,
            CoordinationDecision.Reattack);
        Assert.That(reattack.Accepted, Is.False);
    }

    [Test]
    public void Detached_escort_member_produces_coverage_gap_with_explicit_reason_and_excludes_detached_from_group_scope()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);

        Assert.That(
            bridge.TryTakeDirectControl(PlayModeSmokeCoordinationFactsProvider.FriendlyEscortEntityKey, simTime: 2.0),
            Is.True);

        var baseSnapshot = new MutableSmokeSnapshot(
            simTime: 2.0,
            aliveIds: [PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId, PlayModeSmokeOrbatSeeder.HostileUnitId]);
        var liveSnapshot = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(baseSnapshot);

        var group = CoordinationBridge.Build(bridge, liveSnapshot).Groups.Single();

        Assert.That(group.Coordination.GapCode, Is.EqualTo("SPLIT"));
        Assert.That(group.Gaps.Select(g => g.Code), Does.Contain("DETACHED_SHOOTER"));

        var escort = group.Effects.Single(e => e.UnitId == PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId);
        Assert.That(escort.State, Is.EqualTo(CoordinationEffectState.Detached));
        Assert.That(escort.Coverage.Status, Is.EqualTo(CoverageStatus.Gap));
        Assert.That(escort.Coverage.ReasonCode, Is.EqualTo("CONTRIBUTOR_UNAVAILABLE"));
        AssertValidSourceLabelledPolygon(escort.Coverage.Geometry, PlayModeSmokeCoordinationFactsProvider.EscortCoverageSourceRef);

        var lead = group.Effects.Single(e => e.UnitId == PlayModeSmokeOrbatSeeder.FriendlyUnitId);
        Assert.That(lead.Coverage.Status, Is.EqualTo(CoverageStatus.Covered));

        var holdAfterDetach = CoordinationCommandBridge.Submit(
            bridge,
            liveSnapshot,
            PlayModeSmokeOrbatSeeder.TaskGroupId,
            CoordinationDecision.Hold);
        Assert.That(holdAfterDetach.Accepted, Is.True);
        Assert.That(holdAfterDetach.AffectedUnitIds, Is.EqualTo(new[] { PlayModeSmokeOrbatSeeder.FriendlyUnitId }));
    }

    [Test]
    public void Lost_escort_member_produces_coverage_gap_with_explicit_reason_and_blocks_group_command()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);

        // u2 is lost (not alive in snapshot)
        var baseSnapshot = new MutableSmokeSnapshot(
            simTime: 3.0,
            aliveIds: [PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.HostileUnitId]);
        var liveSnapshot = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(baseSnapshot);

        var group = CoordinationBridge.Build(bridge, liveSnapshot).Groups.Single();

        Assert.That(group.Gaps.Select(g => g.Code), Does.Contain("LOST_SHOOTER"));
        var escort = group.Effects.Single(e => e.UnitId == PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId);
        Assert.That(escort.State, Is.EqualTo(CoordinationEffectState.Lost));
        Assert.That(escort.Coverage.Status, Is.EqualTo(CoverageStatus.Gap));
        Assert.That(escort.Coverage.ReasonCode, Is.EqualTo("CONTRIBUTOR_UNAVAILABLE"));
        AssertValidSourceLabelledPolygon(escort.Coverage.Geometry, PlayModeSmokeCoordinationFactsProvider.EscortCoverageSourceRef);

        var decision = CoordinationCommandBridge.Submit(
            bridge,
            liveSnapshot,
            PlayModeSmokeOrbatSeeder.TaskGroupId,
            CoordinationDecision.Withdraw);
        Assert.That(decision.Accepted, Is.False);
        Assert.That(decision.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonMissingMember));
    }

    [Test]
    public void Missing_assignment_negative_case_leaves_package_roles_and_coverage_explicitly_unknown()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);

        var snapshot = new MutableSmokeSnapshot(
            simTime: 2.0,
            aliveIds: [PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId, PlayModeSmokeOrbatSeeder.HostileUnitId]);
        var unassignedFacts = PlayModeSmokeCoordinationFactsProvider.CreateWithoutAssignment();

        var group = CoordinationBridge.Build(bridge, snapshot, unassignedFacts).Groups.Single();

        Assert.That(group.Coordination.AssignedPackageId, Is.Empty);
        Assert.That(group.Coordination.GapCode, Is.EqualTo("UNKNOWN"));
        Assert.That(group.Effects, Has.Count.EqualTo(2));
        Assert.That(group.Effects.All(e => e.Role is null), Is.True);
        Assert.That(group.Effects.All(e => e.State == CoordinationEffectState.UnknownRole), Is.True);
        Assert.That(group.Effects.All(e => e.Coverage.Status == CoverageStatus.Unknown), Is.True);
        Assert.That(group.Effects.All(e => e.Coverage.ReasonCode == "COVERAGE_FACT_MISSING"), Is.True);
        Assert.That(group.Effects.All(e => e.Coverage.Geometry is null), Is.True);
        Assert.That(group.Gaps.Count(g => g.Code == "UNKNOWN_ROLE"), Is.EqualTo(2));
    }

    [Test]
    public void Invalid_coverage_geometry_is_suppressed_rather_than_fabricated()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);

        var defaults = PlayModeSmokeCoordinationFactsProvider.CreateDefault();
        var invalidFacts = new PlayModeSmokeCoordinationFactsProvider(
            defaults.Packages,
            [
                new CoverageFact(
                    PlayModeSmokeCoordinationFactsProvider.C2ElementId,
                    "cov-invalid-nan",
                    "Invalid NaN Sector",
                    new CoverageAreaGeometry(
                        [new(double.NaN, 0.2), new(1.2, 0.2), new(0.5, 0.8)],
                        string.Empty)),
                new CoverageFact(
                    PlayModeSmokeCoordinationFactsProvider.EscortElementId,
                    "cov-invalid-two-points",
                    "Invalid Degenerate Sector",
                    new CoverageAreaGeometry(
                        [new(0.1, 0.1), new(0.9, 0.9)],
                        "fixture:invalid-two-points")),
            ],
            defaults.Intents,
            defaults.Assignments);

        var snapshot = new MutableSmokeSnapshot(
            simTime: 2.0,
            aliveIds: [PlayModeSmokeOrbatSeeder.FriendlyUnitId, PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId]);

        var group = CoordinationBridge.Build(bridge, snapshot, invalidFacts).Groups.Single();

        Assert.That(group.Effects.All(e => e.Coverage.Geometry is null), Is.True);
    }

    private static void AssertValidSourceLabelledPolygon(CoverageAreaGeometry? geometry, string expectedSourceRef)
    {
        Assert.That(geometry, Is.Not.Null);
        Assert.That(geometry!.SourceRef, Is.EqualTo(expectedSourceRef));
        Assert.That(geometry.Boundary, Has.Count.GreaterThanOrEqualTo(3));
        foreach (var point in geometry.Boundary)
        {
            Assert.That(double.IsFinite(point.NormalizedX), Is.True);
            Assert.That(double.IsFinite(point.NormalizedY), Is.True);
            Assert.That(point.NormalizedX, Is.InRange(0.0, 1.0));
            Assert.That(point.NormalizedY, Is.InRange(0.0, 1.0));
        }
    }

    private sealed class MutableSmokeSnapshot : ISimWorldSnapshot
    {
        private readonly HashSet<string> _aliveIds;

        public MutableSmokeSnapshot(double simTime, IEnumerable<string> aliveIds)
        {
            SimTime = simTime;
            _aliveIds = new HashSet<string>(aliveIds, StringComparer.Ordinal);
        }

        public double SimTime { get; set; }
        public int ContactCount => 1;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => new(PlayModeSmokeOrbatSeeder.HostileUnitId);
        public bool HasFireControlTrackOnPrimaryContact => true;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => _aliveIds.Contains(memberId.Value);
    }

    private sealed class RecordingSink : IOrderSink
    {
        public List<(EntityKey Entity, Order Order)> Applied { get; } = [];
        public void ApplyOrder(EntityKey entity, in Order order) => Applied.Add((entity, order));
    }
}
