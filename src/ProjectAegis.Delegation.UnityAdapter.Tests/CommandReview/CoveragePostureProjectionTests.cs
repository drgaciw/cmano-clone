namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

using C2Nodes;
using Core;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

[TestFixture]
public sealed class CoveragePostureProjectionTests
{
    [Test]
    public void Project_accounts_for_layered_defense_roles_covered_and_gap_areas()
    {
        var (bridge, snapshot) = CreateSmokeGroup();
        var coord = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, snapshot);
        var posture = CoveragePostureProjection.Project(coord);

        Assert.That(posture.Groups, Has.Count.EqualTo(1));
        Assert.That(posture.TotalCovered, Is.EqualTo(2));
        Assert.That(posture.TotalGaps, Is.Zero);
        Assert.That(posture.TotalUsablePolygons, Is.EqualTo(2));
        Assert.That(posture.HasGaps, Is.False);

        var group = posture.Groups[0];
        Assert.That(group.Severity, Is.EqualTo(CoveragePostureSeverity.Nominal));
        Assert.That(group.GapCode, Is.EqualTo("NONE"));
        Assert.That(group.AuthorityBoundary.IsOrder, Is.False);
        Assert.That(group.AuthorityBoundary.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(group.AuthorityBoundary.IsFireOrder, Is.False);
        Assert.That(group.AuthorityBoundary.IsAutomaticEngagement, Is.False);

        var c2Layer = group.Layers.Single(l => l.Role == C2NodeRole.C2);
        Assert.That(c2Layer.Status, Is.EqualTo(CoverageLayerStatus.Intact));
        Assert.That(c2Layer.AvailableCount, Is.EqualTo(1));
        Assert.That(c2Layer.TotalElements, Is.EqualTo(1));

        var shooterLayer = group.Layers.Single(l => l.Role == C2NodeRole.Shooter);
        Assert.That(shooterLayer.Status, Is.EqualTo(CoverageLayerStatus.Intact));
        Assert.That(shooterLayer.AvailableCount, Is.EqualTo(1));
        Assert.That(shooterLayer.TotalElements, Is.EqualTo(1));
    }

    [Test]
    public void Compare_explains_posture_degradation_when_escort_detaches()
    {
        var (bridge, snapshot) = CreateSmokeGroup();
        var baseline = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, snapshot);

        Assert.That(
            bridge.TryTakeDirectControl(PlayModeSmokeCoordinationFactsProvider.FriendlyEscortEntityKey, simTime: 2.0),
            Is.True);
        var current = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, snapshot);

        var diff = CoveragePostureProjection.Compare(baseline, current);
        Assert.That(diff.IsHypothetical, Is.False);
        Assert.That(diff.OverallDelta, Is.EqualTo(CoveragePostureDeltaKind.Degraded));

        var groupDiff = diff.GroupDiffs.Single();
        Assert.That(groupDiff.PreviousGapCode, Is.EqualTo("NONE"));
        Assert.That(groupDiff.CurrentGapCode, Is.EqualTo("SPLIT"));
        Assert.That(groupDiff.LostOrDetachedUnitIds, Does.Contain(PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId));
        Assert.That(groupDiff.DegradedOrLostRoles, Does.Contain(C2NodeRole.Shooter));
        Assert.That(groupDiff.CoverageTransitions.Any(t =>
            t.CoverageId == PlayModeSmokeCoordinationFactsProvider.EscortCoverageId
            && t.PreviousStatus == CoverageStatus.Covered
            && t.CurrentStatus == CoverageStatus.Gap), Is.True);

        var joined = string.Join(" ", diff.ExplanationLines);
        Assert.That(joined, Does.Contain("IsOrder=false"));
        Assert.That(joined, Does.Contain(PlayModeSmokeCoordinationFactsProvider.EscortCoverageId));
    }

    [Test]
    public void Compare_explains_collapse_and_command_block_when_c2_or_member_lost()
    {
        var (bridge, _) = CreateSmokeGroup();
        var aliveBoth = new StubSnapshot(new Dictionary<string, bool>
        {
            [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
            [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
        });
        var baseline = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, aliveBoth);

        var c2Lost = new StubSnapshot(new Dictionary<string, bool>
        {
            [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = false,
            [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
        });
        var current = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, c2Lost);
        var posture = CoveragePostureProjection.Project(current);
        Assert.That(posture.Groups.Single().AuthorityBoundary.GroupCommandsBlockedByMissingMember, Is.True);
        Assert.That(posture.Groups.Single().Severity, Is.EqualTo(CoveragePostureSeverity.Critical));

        var diff = CoveragePostureProjection.Compare(baseline, current);
        Assert.That(diff.OverallDelta, Is.EqualTo(CoveragePostureDeltaKind.Collapsed));
        Assert.That(diff.GroupDiffs.Single().PreviousGapCode, Is.EqualTo("NONE"));
        Assert.That(diff.GroupDiffs.Single().CurrentGapCode, Is.EqualTo("NO_C2"));
        Assert.That(diff.GroupDiffs.Single().DegradedOrLostRoles, Does.Contain(C2NodeRole.C2));
    }

    [Test]
    public void SimulateElementChange_projects_hypothetical_reassignment_and_loss_without_mutating_baseline()
    {
        var (bridge, snapshot) = CreateSmokeGroup();
        var baseline = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, snapshot);
        var baselineGapBefore = baseline.Groups[0].Coordination.GapCode;
        var baselineStatusBefore = baseline.Groups[0].Effects.Select(e => e.Coverage.Status).ToArray();

        var reassignDiff = CoveragePostureProjection.SimulateElementChange(
            baseline,
            PlayModeSmokeOrbatSeeder.TaskGroupId,
            PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId,
            HypotheticalElementChangeKind.DetachOrReassignment,
            targetGroupId: "tg-reserve");

        Assert.That(reassignDiff.IsHypothetical, Is.True);
        Assert.That(reassignDiff.HypotheticalSubjectUnitId, Is.EqualTo(PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId));
        Assert.That(reassignDiff.HypotheticalChangeKind, Is.EqualTo(HypotheticalElementChangeKind.DetachOrReassignment));
        Assert.That(reassignDiff.ReassignmentTargetGroupId, Is.EqualTo("tg-reserve"));
        Assert.That(reassignDiff.GroupDiffs.Single().CurrentGapCode, Is.EqualTo("SPLIT"));
        Assert.That(string.Join(" ", reassignDiff.ExplanationLines), Does.Contain("tg-reserve"));

        var c2LossDiff = CoveragePostureProjection.SimulateElementChange(
            baseline,
            PlayModeSmokeOrbatSeeder.TaskGroupId,
            PlayModeSmokeOrbatSeeder.FriendlyUnitId,
            HypotheticalElementChangeKind.Loss);
        Assert.That(c2LossDiff.IsHypothetical, Is.True);
        Assert.That(c2LossDiff.OverallDelta, Is.EqualTo(CoveragePostureDeltaKind.Collapsed));
        Assert.That(c2LossDiff.GroupDiffs.Single().CurrentGapCode, Is.EqualTo("NO_C2"));

        // Baseline must remain untouched
        Assert.That(baseline.Groups[0].Coordination.GapCode, Is.EqualTo(baselineGapBefore));
        Assert.That(baseline.Groups[0].Effects.Select(e => e.Coverage.Status).ToArray(), Is.EqualTo(baselineStatusBefore));
    }

    [Test]
    public void Project_unassigned_package_reports_unknown_severity_and_no_fabricated_coverage()
    {
        var (bridge, snapshot) = CreateSmokeGroup();
        var unassigned = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(
            bridge,
            snapshot,
            PlayModeSmokeCoordinationFactsProvider.CreateWithoutAssignment());
        var posture = CoveragePostureProjection.Project(unassigned);

        var group = posture.Groups.Single();
        Assert.That(group.Severity, Is.EqualTo(CoveragePostureSeverity.Unknown));
        Assert.That(posture.TotalCovered, Is.Zero);
        Assert.That(posture.TotalUsablePolygons, Is.Zero);
    }

    [Test]
    public void Compare_reports_collapsed_with_explicit_transition_when_baseline_group_disappears_from_current()
    {
        var (bridge, snapshot) = CreateSmokeGroup();
        var baseline = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, snapshot);
        var current = new CoordinationSnapshot(Array.Empty<CoordinationGroupSnapshot>());

        var diff = CoveragePostureProjection.Compare(baseline, current);

        Assert.That(diff.OverallDelta, Is.EqualTo(CoveragePostureDeltaKind.Collapsed));
        Assert.That(diff.GroupDiffs, Has.Count.EqualTo(1));
        var groupDiff = diff.GroupDiffs[0];
        Assert.That(groupDiff.DeltaKind, Is.EqualTo(CoveragePostureDeltaKind.Collapsed));
        Assert.That(groupDiff.CurrentGapCode, Is.Not.EqualTo("NONE"));
        Assert.That(groupDiff.CoverageTransitions, Is.Not.Empty);
        Assert.That(groupDiff.LostOrDetachedUnitIds, Does.Contain(PlayModeSmokeOrbatSeeder.FriendlyUnitId));
        Assert.That(string.Join(" ", diff.ExplanationLines), Does.Not.Contain("POSTURE UNCHANGED"));
        Assert.That(diff.Headline, Does.Contain("COLLAPSED"));
    }

    [Test]
    public void Compare_handles_duplicate_group_ids_and_duplicate_effect_keys_without_throwing()
    {
        var (bridge, snapshot) = CreateSmokeGroup();
        var baseline = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, snapshot);
        var group = baseline.Groups[0];
        var dupEffects = new[] { group.Effects[0], group.Effects[0] };
        var dupBaseline = new CoordinationSnapshot(new[]
        {
            group with { Effects = dupEffects },
            group,
        });
        var dupCurrent = new CoordinationSnapshot(new[]
        {
            group with { Effects = dupEffects },
            group,
        });

        var diff = CoveragePostureProjection.Compare(dupBaseline, dupCurrent);
        Assert.That(diff.OverallDelta, Is.EqualTo(CoveragePostureDeltaKind.Unchanged));
        Assert.That(diff.GroupDiffs, Has.Count.EqualTo(1));
    }

    [Test]
    public void Compare_handles_duplicate_effect_keys_within_single_group_without_throwing()
    {
        var (bridge, snapshot) = CreateSmokeGroup();
        var baseline = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, snapshot);
        var group = baseline.Groups[0];
        var dupEffects = new[] { group.Effects[0], group.Effects[0] };
        var singleGroupDupEffects = new CoordinationSnapshot(new[] { group with { Effects = dupEffects } });

        var diff = CoveragePostureProjection.Compare(singleGroupDupEffects, singleGroupDupEffects);
        Assert.That(diff.OverallDelta, Is.EqualTo(CoveragePostureDeltaKind.Unchanged));
        Assert.That(diff.GroupDiffs, Has.Count.EqualTo(1));
    }

    [Test]
    public void SimulateElementChange_with_nonexistent_unit_for_detachment_reports_no_posture_change_and_no_split()
    {
        var (bridge, snapshot) = CreateSmokeGroup();
        var baseline = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, snapshot);

        var diff = CoveragePostureProjection.SimulateElementChange(
            baseline,
            PlayModeSmokeOrbatSeeder.TaskGroupId,
            "nonexistent-unit-999",
            HypotheticalElementChangeKind.DetachOrReassignment,
            targetGroupId: "tg-reserve");

        Assert.That(diff.IsHypothetical, Is.True);
        Assert.That(diff.OverallDelta, Is.EqualTo(CoveragePostureDeltaKind.Unchanged));
        Assert.That(diff.GroupDiffs.Single().CurrentGapCode, Is.EqualTo("NONE"));
        Assert.That(string.Join(" ", diff.ExplanationLines), Does.Contain("nonexistent-unit-999"));
        Assert.That(string.Join(" ", diff.ExplanationLines), Does.Contain("No posture change"));
    }

    private static (DelegationBridge Bridge, StubSnapshot Snapshot) CreateSmokeGroup()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
        var snapshot = new StubSnapshot(new Dictionary<string, bool>
        {
            [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
            [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
        });
        return (bridge, snapshot);
    }

    private sealed class StubSnapshot : ISimWorldSnapshot
    {
        private readonly IReadOnlyDictionary<string, bool> _alive;

        public StubSnapshot(IReadOnlyDictionary<string, bool> alive) => _alive = alive;

        public double SimTime => 2.0;
        public int ContactCount => 1;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => new(PlayModeSmokeOrbatSeeder.HostileUnitId);
        public bool HasFireControlTrackOnPrimaryContact => true;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => _alive.TryGetValue(memberId.Value, out var ok) && ok;
    }
}
