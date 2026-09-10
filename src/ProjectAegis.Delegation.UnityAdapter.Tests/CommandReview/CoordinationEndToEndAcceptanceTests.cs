namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

using BdaAssess;
using Controllers;
using Core;
using Decision;
using MissionIntent;
using Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

[TestFixture]
public sealed class CoordinationEndToEndAcceptanceTests
{
    [Test]
    public void Review_advice_hold_group_effects_bda_review_and_withdraw_use_real_delayed_ticks()
    {
        var run = RunFlow();

        Assert.That(run.InitialAdvice.Availability, Is.EqualTo(AdviceAvailability.Available));
        Assert.That(run.InitialAdvice.IsFireOrder, Is.False);
        Assert.That(run.Applied.Select(x => x.Kind), Is.EqualTo(new[]
        {
            OrderKind.Hold,
            OrderKind.Hold,
            OrderKind.ReturnToBase,
            OrderKind.ReturnToBase,
        }));
        Assert.That(run.BdaContacts, Is.GreaterThan(0));
        Assert.That(run.ReviewStatus, Does.StartWith("REVIEW"));
        Assert.That(run.FinalAdvice.Availability, Is.EqualTo(AdviceAvailability.Available));
        Assert.That(run.FinalIntent.AdvisoryRetask, Is.EqualTo(MissionIntentRetaskAdvice.Withdraw));
        Assert.That(run.PendingScopes, Is.Zero);
    }

    [Test]
    public void Complete_review_decision_and_bda_retask_flow_is_deterministic()
    {
        var first = RunFlow();
        var second = RunFlow();

        Assert.That(first.LogFingerprint, Is.EqualTo(second.LogFingerprint));
        Assert.That(first.Applied, Is.EqualTo(second.Applied));
        Assert.That(first.ReviewStatus, Is.EqualTo(second.ReviewStatus));
        Assert.That(
            MissionIntentProjection.ComputeFingerprint(first.FinalIntent),
            Is.EqualTo(MissionIntentProjection.ComputeFingerprint(second.FinalIntent)));
    }

    private static FlowResult RunFlow()
    {
        var bridge = new DelegationBridge(175192, mvpEngagement: false, scenarioPolicyId: "baltic-patrol-comms");
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g|acceptance,1");
        group.Target.Slot.SetActive(new HumanController());
        var c2 = bridge.Registry.RegisterUnit(new EntityKey(1), "c2,one");
        var shooter = bridge.Registry.RegisterUnit(new EntityKey(2), "shooter|two");
        bridge.Registry.LinkGroupMember(group.TargetId, c2.TargetId);
        bridge.Registry.LinkGroupMember(group.TargetId, shooter.TargetId);
        bridge.BeginExecution();
        var applied = new RecordingSink();
        bridge.Tick(new Snapshot(2), applied);

        var contactAtThree = ContactFrame(3, "Tracked");
        var initialAdvice = AdviceBridge.Build(bridge, new Snapshot(3), contactAtThree);
        var timeline = new CommandReviewTimeline();
        timeline.Capture(CombatPresentationFrame.Empty with { SimTime = 3, Contacts = contactAtThree }, []);

        var holdIntent = MissionIntentProjection.Project(new MissionIntentInput(
            group.TargetId.Value, "", MissionIntentCode.Hold, []));
        var hold = CoordinationCommandBridge.Submit(
            bridge, new Snapshot(3), group.TargetId.Value, CoordinationDecision.Hold, holdIntent);
        var pending = new List<CoordinationApprovedScope> { hold.ApprovedScope! };
        RunThroughExecuteTick(bridge, applied, pending, hold.ApprovedScope!);

        bridge.Orchestrator.DecisionLog.AppendEngagement(new EngagementRecord(
            0,
            6,
            6,
            shooter.TargetId,
            77,
            true,
            "Launched",
            new TargetId("hostile-1"),
            "acceptance-weapon",
            1,
            true));
        bridge.Orchestrator.DecisionLog.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0,
            6,
            6,
            shooter.TargetId,
            new TargetId("hostile-1"),
            77,
            EngagementOutcomeCodes.Kill,
            0.25));
        bridge.Orchestrator.DecisionLog.AppendContactChange(new ContactChangeRecord(
            0,
            6,
            6,
            "c2,one",
            "contact-1",
            "hostile-1",
            "Tracked",
            BdaContactDamageStates.Lost));
        var contactAtSix = ContactFrame(6, BdaContactDamageStates.Lost);
        var combatFrame = CombatPresentationFrameBridge.Build(
            bridge.Orchestrator.DecisionLog,
            contactAtSix,
            6);
        timeline.Capture(combatFrame, []);
        var killed = timeline.Filter(new(Outcome: EngagementOutcomeCodes.Kill)).Single();
        Assert.That(timeline.Inspect(killed), Is.True);
        var finalAdvice = AdviceBridge.Build(bridge, new Snapshot(6), contactAtSix);

        var terminalAssessment = combatFrame.Assessments.Contacts.Single(c =>
            string.Equals(c.ContactId, "contact-1", StringComparison.Ordinal));
        Assert.That(terminalAssessment.State, Is.EqualTo(BdaAssessStateKind.Destroyed));
        var bdaRetask = terminalAssessment.State == BdaAssessStateKind.Destroyed
            ? MissionIntentRetaskAdvice.Withdraw
            : MissionIntentRetaskAdvice.None;
        var withdrawIntent = MissionIntentProjection.Project(new MissionIntentInput(
            group.TargetId.Value,
            "",
            MissionIntentCode.Hold,
            [MissionIntentConstraintCode.NoStrike],
            bdaRetask));
        var withdraw = CoordinationCommandBridge.Submit(
            bridge, new Snapshot(6), group.TargetId.Value, CoordinationDecision.Withdraw, withdrawIntent, pending);
        pending.Add(withdraw.ApprovedScope!);
        RunThroughExecuteTick(bridge, applied, pending, withdraw.ApprovedScope!);

        return new FlowResult(
            initialAdvice,
            finalAdvice,
            withdrawIntent,
            combatFrame.Assessments.Contacts.Count,
            timeline.StatusLine,
            bridge.Orchestrator.DecisionLog.ComputeFingerprint(),
            applied.Applied.ToArray(),
            pending.Count);
    }

    private static void RunThroughExecuteTick(
        DelegationBridge bridge,
        RecordingSink downstream,
        List<CoordinationApprovedScope> pending,
        CoordinationApprovedScope scope)
    {
        var record = bridge.Orchestrator.DecisionLog.PlayerOrders.Single(p =>
            p.SequenceId == scope.PlayerOrderSequenceId);
        var firstTick = (ulong)scope.SimTime;
        for (var tick = firstTick; tick <= record.ResolvedExecuteSimTick; tick++)
        {
            var snapshot = new Snapshot(tick);
            var sink = new CoordinationOrderSink(bridge.Registry, snapshot, downstream, pending);
            bridge.Tick(snapshot, sink);
            pending.RemoveAll(s => sink.ConsumedScopeIds.Contains(s.ScopeId));
        }
    }

    private static SliceAContactFrame ContactFrame(double time, string state) =>
        SliceAContactFrame.Empty with
        {
            SimTick = (ulong)time,
            SimTime = time,
            Contacts = [new ContactPictureEntry("contact-1", "hostile-1", "c2,one", state, (ulong)time, time)],
        };

    private sealed class Snapshot(double time) : ISimWorldSnapshot
    {
        public double SimTime { get; } = time;
        public int ContactCount => 1;
        public int ActiveEngagementCount => 0;
        public bool IsMemberAlive(TargetId memberId) => true;
        public TargetId? PrimaryHostileContactId => new("hostile-1");
        public bool HasFireControlTrackOnPrimaryContact => true;
        public bool ObserverRadarEmconActive => true;
    }

    private sealed class RecordingSink : IOrderSink
    {
        public List<CoordinationAppliedOrder> Applied { get; } = [];
        public void ApplyOrder(EntityKey entity, in Order order) =>
            Applied.Add(new CoordinationAppliedOrder(entity, order.Target.Value, order.Kind));
    }

    private sealed record FlowResult(
        AdviceFrame InitialAdvice,
        AdviceFrame FinalAdvice,
        MissionIntentSnapshot FinalIntent,
        int BdaContacts,
        string ReviewStatus,
        string LogFingerprint,
        IReadOnlyList<CoordinationAppliedOrder> Applied,
        int PendingScopes);
}
