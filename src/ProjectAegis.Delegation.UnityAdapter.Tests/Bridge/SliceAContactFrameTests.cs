using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Delegation.Roe;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class SliceAContactFrameTests
{
    [Test]
    public void Null_inputs_throw()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => SliceAContactFrameBridge.Build(null!, new DelegationBridge(1))));
        Assert.Throws<ArgumentNullException>((Action)(() => SliceAContactFrameBridge.Build(new SimWorldSnapshotStub(), null!)));
    }

    [Test]
    public void Missing_shooter_evidence_never_becomes_clearance()
    {
        var bridge = CreateBridge();
        var frame = SliceAContactFrameBridge.Build(World(), bridge);
        Assert.That(frame.KillChain.Contacts[0].Targetable, Is.True);
        Assert.That(frame.Chains.Chains[0].IsComplete, Is.False);
        Assert.That(frame.Authorities, Is.Empty);
        Assert.That(frame.EligibilityAvailable, Is.False);
    }

    [Test]
    public void Multiple_observers_of_same_target_do_not_collide()
    {
        var bridge = CreateBridge();
        bridge.Orchestrator.DecisionLog.AppendContactChange(new ContactChangeRecord(0, 1, 1, "u2", "c2", "hostile-1", "Unknown", "Identified"));
        var frame = SliceAContactFrameBridge.Build(World(), bridge);
        Assert.That(frame.Contacts.Select(c => c.ContactId), Is.EqualTo(new[] { "c1", "c2" }));
    }

    [TestCase(1, BdaContactDamageStates.DegradedL1)]
    [TestCase(2, BdaContactDamageStates.DegradedL2)]
    [TestCase(3, BdaContactDamageStates.Lost)]
    public void Bda_projects_to_each_observer_and_retains_lost_identity(int level, string expected)
    {
        var bridge = CreateBridge();
        var log = bridge.Orchestrator.DecisionLog;
        log.AppendContactChange(new ContactChangeRecord(0, 1, 1, "u2", "c2", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(0, 2, 2, new TargetId("hostile-1"), 100, 100 - level * 25, "Hit", level));
        var frame = SliceAContactFrameBridge.Build(World(), bridge);
        Assert.That(frame.Contacts, Has.Count.EqualTo(2));
        Assert.That(frame.Contacts.All(c => c.LifecycleState == expected), Is.True);
        Assert.That(frame.Chains.Chains.All(c => !c.IsComplete), Is.True);
    }

    [Test]
    public void Rebuilding_is_deterministic_and_does_not_mutate_log()
    {
        var bridge = CreateBridge();
        var before = bridge.Orchestrator.DecisionLog.ContactChanges.Count;
        var first = SliceAContactFrameBridge.Build(World(), bridge);
        var second = SliceAContactFrameBridge.Build(World(), bridge);
        Assert.That(second.Contacts, Is.EqualTo(first.Contacts));
        Assert.That(second.Provenance.Contacts, Is.EqualTo(first.Provenance.Contacts));
        Assert.That(bridge.Orchestrator.DecisionLog.ContactChanges.Count, Is.EqualTo(before));
    }

    [Test]
    public void Stale_contact_remains_visible_but_not_targetable()
    {
        var frame = SliceAContactFrameBridge.Build(World(40), CreateBridge());
        Assert.That(frame.Provenance.Contacts[0].Freshness, Is.EqualTo(ContactProvenanceFreshness.Stale));
        Assert.That(frame.Chains.Chains[0].IsComplete, Is.False);
    }

    [Test]
    public void Fire_control_for_other_target_does_not_transfer()
    {
        var world = new SimWorldSnapshotStub(simTime: 2, contactCount: 1, primaryHostileContactId: new TargetId("other"), hasFireControlTrackOnPrimaryContact: true);
        var frame = SliceAContactFrameBridge.Build(world, CreateBridge());
        Assert.That(frame.KillChain.Contacts[0].Targetable, Is.False);
    }

    private static DelegationBridge CreateBridge()
    {
        var bridge = new DelegationBridge(1);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.Orchestrator.DecisionLog.AppendContactChange(new ContactChangeRecord(0, 1, 1, "u1", "c1", "hostile-1", "Unknown", "Identified"));
        return bridge;
    }

    private static SimWorldSnapshotStub World(double time = 2) => new(simTime: time, contactCount: 1, primaryHostileContactId: new TargetId("hostile-1"), hasFireControlTrackOnPrimaryContact: true);

    [Test]
    public void Explicit_eligible_shooter_requires_release_approval_not_clearance()
    {
        var frame = SliceAContactFrameBridge.Build(new EvidenceSnapshot(), CreateBridge());
        Assert.That(frame.EligibilityAvailable, Is.True);
        Assert.That(frame.SimTick, Is.EqualTo(2UL));
        Assert.That(frame.Chains.Chains[0].IsComplete, Is.True);
        Assert.That(frame.Authorities["c1"].Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.ApprovalRequired));
    }

    [Test]
    public void Empty_ammo_cannot_complete_chain()
    {
        var frame = SliceAContactFrameBridge.Build(new EvidenceSnapshot { Rounds = 0 }, CreateBridge());
        Assert.That(frame.Chains.Chains[0].IsComplete, Is.False);
        Assert.That(frame.Authorities, Is.Empty);
    }

    [Test]
    public void Tracked_empty_magazine_overrides_source_and_is_never_refilled()
    {
        var bridge = CreateBridge().EnableMvpEngagement();
        var shooterId = OrderActionMapper.TargetIdToUlong(new TargetId("u1"));
        bridge.Session!.Magazines!.SetRounds(shooterId, 0, 0);
        var frame = SliceAContactFrameBridge.Build(new EvidenceSnapshot(), bridge);
        Assert.That(frame.Chains.Chains[0].IsComplete, Is.False);
        Assert.That(bridge.Session.Magazines.GetRounds(shooterId, 0), Is.Zero);
    }

    [Test]
    public void Registered_shooter_without_authority_evidence_remains_unknown()
    {
        var frame = SliceAContactFrameBridge.Build(new EvidenceSnapshot { HasAuthority = false }, CreateBridge());
        Assert.That(frame.Chains.Chains[0].IsComplete, Is.True);
        Assert.That(frame.Authorities, Is.Empty);
    }

    [Test]
    public void Technical_eligibility_does_not_override_hold_fire()
    {
        var frame = SliceAContactFrameBridge.Build(new EvidenceSnapshot { Roe = RoeLevel.HoldFire }, CreateBridge());
        Assert.That(frame.Chains.Chains[0].IsComplete, Is.True);
        Assert.That(frame.Authorities["c1"].Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
    }

    [Test]
    public void Denied_comms_withholds_authority_despite_eligible_shooter()
    {
        var bridge = CreateBridge();
        bridge.Orchestrator.DecisionLog.AppendCommsStateChange(new CommsStateChangeRecord(0, 2, 2, "node", CommsState.Nominal, CommsState.Denied, "test"));
        var frame = SliceAContactFrameBridge.Build(new EvidenceSnapshot(), bridge);
        Assert.That(frame.Provenance.Contacts[0].OutOfCommsUnknown, Is.True);
        Assert.That(frame.Authorities["c1"].Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
    }

    private sealed class EvidenceSnapshot : ISimWorldSnapshot, ISensorToShooterShooterSource, ISliceAContactAuthoritySource
    {
        public int Rounds { get; init; } = 2;
        public bool HasAuthority { get; init; } = true;
        public RoeLevel Roe { get; init; } = RoeLevel.WeaponsFree;
        public double SimTime => 2;
        public int ContactCount => 1;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => new TargetId("hostile-1");
        public bool HasFireControlTrackOnPrimaryContact => true;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => true;
        public IReadOnlyList<SensorToShooterShooterCandidate> GetCandidatesForTarget(string targetId) =>
            new[] { new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, Rounds) };
        public bool TryGetAuthorityContext(string contactId, string shooterUnitId, out C2AuthorityProjectionContext context)
        {
            context = new C2AuthorityProjectionContext(Roe, SkillLane.Read, RequiredApproval.None, TrackSource.Organic, true);
            return HasAuthority && contactId == "c1" && shooterUnitId == "u1";
        }
    }
}
