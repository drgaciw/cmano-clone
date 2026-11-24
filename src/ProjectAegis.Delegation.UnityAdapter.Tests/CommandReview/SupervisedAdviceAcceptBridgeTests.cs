using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

[TestFixture]
public sealed class SupervisedAdviceAcceptBridgeTests
{
    private sealed class NoOpOrderSink : IOrderSink
    {
        public void ApplyOrder(EntityKey entity, in Order order) { }
    }

    private sealed class TestWorldSnapshot : ISimWorldSnapshot
    {
        public double SimTime { get; set; } = 5.0;
        public int ContactCount { get; set; } = 1;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId { get; set; } = new("hostile-1");
        public bool HasFireControlTrackOnPrimaryContact { get; set; } = true;
        public bool ObserverRadarEmconActive { get; set; } = true;
        public bool IsMemberAlive(TargetId memberId) => true;
    }

    [Test]
    public void Card_projection_requires_at_least_one_evidence_pointer_and_formats_unit_contact_policy_seq()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);

        var card = SupervisedAdviceCardBinder.Project(
            bridge, snapshot, advice, selectedUnitId: "u1", proposedCommandId: "engage", attackOptionId: "fire-single");

        Assert.That(card.CardVisible, Is.True);
        Assert.That(card.CanAccept, Is.True);
        Assert.That(card.UnitId, Is.EqualTo("u1"));
        Assert.That(card.ContactId, Is.EqualTo("c1"));
        Assert.That(card.ProposedCommandId, Is.EqualTo("engage"));
        Assert.That(card.AttackOptionId, Is.EqualTo("fire-single"));
        Assert.That(card.Rationale, Is.Not.Empty);
        Assert.That(card.AuthoritySummary, Is.Not.Empty);
        Assert.That(card.ResolvedAutonomy, Is.EqualTo(AutonomyLevel.Assisted));
        Assert.That(card.EvidenceLines, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(card.EvidenceLines[0].FormattedPointer, Does.Contain("u1 | c1 | "));
        Assert.That(card.EvidenceLines[0].FormattedPointer.Split('|'), Has.Length.EqualTo(4));
        Assert.That(card.EvidenceLines[0].Kind, Is.EqualTo(EvidenceKind.Contact));
        Assert.That(card.EvidenceLines[0].Id, Is.Not.Empty);
        Assert.That(card.EvidenceLines[0].PolicySnapshotId, Is.Not.Empty);
        Assert.That(card.EvidenceLines[0].SequenceId, Is.Not.Empty);

        var noEvidenceAdvice = advice with { Evidence = Array.Empty<EvidencePointer>() };
        var blockedNoEvidence = SupervisedAdviceCardBinder.Project(
            bridge, snapshot, noEvidenceAdvice, selectedUnitId: "u1", proposedCommandId: "engage", attackOptionId: "fire-single");
        Assert.That(blockedNoEvidence.CanAccept, Is.False);
        Assert.That(blockedNoEvidence.BlockReason, Is.EqualTo(SupervisedAdviceAcceptBridge.ReasonMissingEvidencePointer));

        var staleAdvice = advice with { Availability = AdviceAvailability.Stale };
        var blockedStale = SupervisedAdviceCardBinder.Project(
            bridge, snapshot, staleAdvice, selectedUnitId: "u1", proposedCommandId: "engage", attackOptionId: "fire-single");
        Assert.That(blockedStale.CanAccept, Is.False);
        Assert.That(blockedStale.BlockReason, Is.EqualTo(SupervisedAdviceAcceptBridge.ReasonAdviceUnavailable));
    }

    [Test]
    public void Accept_enqueues_player_order_through_policy_and_engage_gates()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        var beforeOrders = bridge.Orchestrator.DecisionLog.PlayerOrders.Count;

        var result = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest(
                UnitId: "u1",
                Advice: advice,
                CommandId: "engage",
                AttackOptionId: "fire-single",
                ExplicitFullAutonomousLethalOptIn: false));

        Assert.That(result.Accepted, Is.True, result.FailureReason);
        Assert.That(result.UnitId, Is.EqualTo("u1"));
        Assert.That(result.ContactId, Is.EqualTo("c1"));
        Assert.That(result.EnqueuedKind, Is.EqualTo(OrderKind.Engage));
        Assert.That(result.PlayerOrderSequenceId, Is.Not.Null);
        Assert.That(result.ResolvedExecuteSimTick, Is.Not.Null);
        Assert.That(result.EvidenceLines, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(beforeOrders + 1));

        var latest = bridge.Orchestrator.DecisionLog.PlayerOrders[^1];
        Assert.That(latest.UnitId.Value, Is.EqualTo("u1"));
        Assert.That(latest.Kind, Is.EqualTo(OrderKind.Engage));
        Assert.That(latest.Source, Is.EqualTo("player"));
    }

    [Test]
    public void Accept_respects_roe_hold_fire_and_engage_gates_without_mutating_order_log()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.HoldFire);
        var beforeCount = bridge.Orchestrator.DecisionLog.PlayerOrders.Count;
        var beforeFp = bridge.Orchestrator.DecisionLog.ComputeFingerprint();

        var holdFireResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest("u1", advice, "engage", "fire-single"));

        Assert.That(holdFireResult.Accepted, Is.False);
        Assert.That(holdFireResult.FailureReason, Is.EqualTo(nameof(FireAbortReason.RoeHoldFire)));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(beforeCount));
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));

        // Now allow ROE, but remove FireControlTrack -> engage gate must reject BEFORE taking direct control.
        Assert.That(DoctrineOverrideCommand.TryApply(bridge.Orchestrator, new TargetId("u1"), "WeaponsFree", 5.0), Is.True);
        var fpAfterRoe = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        snapshot.HasFireControlTrackOnPrimaryContact = false;

        var noTrackResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest("u1", advice, "engage", "fire-single"));

        Assert.That(noTrackResult.Accepted, Is.False);
        Assert.That(noTrackResult.FailureReason, Is.Not.Null.And.Not.Empty);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(beforeCount));
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(fpAfterRoe));
        Assert.That(bridge.Registry.TryGetBinding(new TargetId("u1"), out var u1Binding), Is.True);
        Assert.That(u1Binding.Target.Slot.Active, Is.InstanceOf<AgentController>(),
            "Failed engage pre-check must not strip AgentController into HumanController.");
    }

    [Test]
    public void Accept_blocks_full_autonomous_lethal_engage_without_explicit_player_opt_in()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.FullAutonomous, RoeLevel.WeaponsFree);
        var cardBlocked = SupervisedAdviceCardBinder.Project(
            bridge, snapshot, advice, "u1", "engage", "fire-single", explicitFullAutonomousLethalOptIn: false);

        Assert.That(cardBlocked.RequiresFullAutonomousOptIn, Is.True);
        Assert.That(cardBlocked.IsLethalOrder, Is.True);
        Assert.That(cardBlocked.CanAccept, Is.False);
        Assert.That(cardBlocked.BlockReason, Is.EqualTo(SupervisedAdviceAcceptBridge.ReasonFullAutonomousOptInRequired));

        var beforeFp = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var blocked = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest("u1", advice, "engage", "fire-single", ExplicitFullAutonomousLethalOptIn: false));

        Assert.That(blocked.Accepted, Is.False);
        Assert.That(blocked.FailureReason, Is.EqualTo(SupervisedAdviceAcceptBridge.ReasonFullAutonomousOptInRequired));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));

        var optedIn = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest("u1", advice, "engage", "fire-single", ExplicitFullAutonomousLethalOptIn: true));

        Assert.That(optedIn.Accepted, Is.True, optedIn.FailureReason);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1));
    }

    [Test]
    public void Dismiss_leaves_zero_order_log_and_fingerprint_mutation()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        var beforeCount = bridge.Orchestrator.DecisionLog.PlayerOrders.Count;
        var beforeFp = bridge.Orchestrator.DecisionLog.ComputeFingerprint();

        var binder = new SupervisedAdviceCardBinder();
        var visibleCard = binder.ProjectForUnit(bridge, snapshot, advice, "u1", "engage", "fire-single");
        Assert.That(visibleCard.CardVisible, Is.True);

        binder.Dismiss(advice.ContactId);
        var afterDismiss = binder.ProjectForUnit(bridge, snapshot, advice, "u1", "engage", "fire-single");

        Assert.That(afterDismiss.CardVisible, Is.False);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(beforeCount));
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));
    }

    [Test]
    public void Accept_blocks_replay_viewer_and_opposing_ai_unit()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        bridge.AttachReplayViewer = true;

        var replayResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridge, snapshot, new SupervisedAdviceAcceptRequest("u1", advice, "engage", "fire-single"));
        Assert.That(replayResult.Accepted, Is.False);
        Assert.That(replayResult.FailureReason, Is.EqualTo(C2PlayerCommandBridge.ReasonReplayAttached));

        bridge.AttachReplayViewer = false;
        var hostileResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridge, snapshot, new SupervisedAdviceAcceptRequest("hostile-1", advice, "engage", "fire-single"));
        Assert.That(hostileResult.Accepted, Is.False);
        Assert.That(hostileResult.FailureReason, Is.EqualTo(C2PlayerCommandBridge.ReasonNotPlayerControlled));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Accept_blocks_withheld_authority_even_under_weapons_free_without_taking_control()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        var withheldAdvice = advice with
        {
            Authority = new C2AuthorityProjection(
                new(RoeLevel.WeaponsFree, "WEAPONS_FREE", C2AuthorityDisposition.Withheld, "NO_WEAPONS_RELEASE", false),
                new(C2AuthorityDisposition.Withheld, "NO_WEAPONS_RELEASE", null),
                Array.Empty<C2AuthorityActionState>()),
        };

        var result = SupervisedAdviceAcceptBridge.TryAccept(
            bridge, snapshot, new SupervisedAdviceAcceptRequest("u1", withheldAdvice, "engage", "fire-single"));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("NO_WEAPONS_RELEASE"));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(bridge.Registry.TryGetBinding(new TargetId("u1"), out var binding), Is.True);
        Assert.That(binding.Target.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [Test]
    public void Accept_blocks_off_grid_unit_without_taking_control()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        bridge.Orchestrator.CommsGrid = new CommsGridRegistry(
            new[] { new ScenarioCommsGridTransition(1, "u1", "OffGrid", "below comms depth") });

        var result = SupervisedAdviceAcceptBridge.TryAccept(
            bridge, snapshot, new SupervisedAdviceAcceptRequest("u1", advice, "engage", "fire-single"));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(C2PlayerCommandBridge.ReasonOffGrid));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(bridge.Registry.TryGetBinding(new TargetId("u1"), out var binding), Is.True);
        Assert.That(binding.Target.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [Test]
    public void Accept_does_not_strip_agent_controller_when_attack_option_or_command_resolution_fails()
    {
        var (bridgeBogus, snapBogus, adviceBogus) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        var bogusResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridgeBogus,
            snapBogus,
            new SupervisedAdviceAcceptRequest("u1", adviceBogus, "engage", "bogus-option"));

        Assert.That(bogusResult.Accepted, Is.False);
        Assert.That(bogusResult.FailureReason, Is.EqualTo("UNKNOWN_OPTION"));
        Assert.That(bridgeBogus.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(bridgeBogus.Registry.TryGetBinding(new TargetId("u1"), out var bogusBinding), Is.True);
        Assert.That(bogusBinding.Target.Slot.Active, Is.InstanceOf<AgentController>());

        var (bridgeEmptyCmd, snapEmptyCmd, adviceEmptyCmd) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        var emptyCmdResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridgeEmptyCmd,
            snapEmptyCmd,
            new SupervisedAdviceAcceptRequest("u1", adviceEmptyCmd, "", "hold-fire"));

        Assert.That(emptyCmdResult.Accepted, Is.False);
        Assert.That(emptyCmdResult.FailureReason, Is.EqualTo(C2CommandIssuance.ReasonUnknownCommand));
        Assert.That(bridgeEmptyCmd.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(bridgeEmptyCmd.Registry.TryGetBinding(new TargetId("u1"), out var emptyCmdBinding), Is.True);
        Assert.That(emptyCmdBinding.Target.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [Test]
    public void Binder_and_bridge_agree_on_command_precedence_for_hold_with_fire_single_option()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.FullAutonomous, RoeLevel.WeaponsFree);

        var card = SupervisedAdviceCardBinder.Project(
            bridge,
            snapshot,
            advice,
            selectedUnitId: "u1",
            proposedCommandId: "hold",
            attackOptionId: "fire-single",
            explicitFullAutonomousLethalOptIn: false);

        Assert.That(card.IsLethalOrder, Is.False);
        Assert.That(card.RequiresFullAutonomousOptIn, Is.False);
        Assert.That(card.CanAccept, Is.True);

        var result = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest(
                "u1",
                advice,
                CommandId: "hold",
                AttackOptionId: "fire-single",
                ExplicitFullAutonomousLethalOptIn: false));

        Assert.That(result.Accepted, Is.True, result.FailureReason);
        Assert.That(result.EnqueuedKind, Is.EqualTo(OrderKind.Hold));
    }

    [Test]
    public void Engage_with_hold_fire_option_never_enqueues_engage_even_under_full_auto_hold_fire_withheld_or_no_track()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.FullAutonomous, RoeLevel.HoldFire);
        snapshot.HasFireControlTrackOnPrimaryContact = false;
        var withheldAdvice = advice with
        {
            Authority = new C2AuthorityProjection(
                new(RoeLevel.HoldFire, "HOLD_FIRE", C2AuthorityDisposition.Withheld, "NO_WEAPONS_RELEASE", false),
                new(C2AuthorityDisposition.Withheld, "NO_WEAPONS_RELEASE", null),
                Array.Empty<C2AuthorityActionState>()),
        };

        var card = SupervisedAdviceCardBinder.Project(
            bridge,
            snapshot,
            withheldAdvice,
            selectedUnitId: "u1",
            proposedCommandId: "engage",
            attackOptionId: "hold-fire",
            explicitFullAutonomousLethalOptIn: false);

        Assert.That(card.IsLethalOrder, Is.False);
        Assert.That(card.RequiresFullAutonomousOptIn, Is.False);
        Assert.That(card.CanAccept, Is.True);

        var result = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest(
                UnitId: "u1",
                Advice: withheldAdvice,
                CommandId: "engage",
                AttackOptionId: "hold-fire",
                ExplicitFullAutonomousLethalOptIn: false));

        Assert.That(result.Accepted, Is.True, result.FailureReason);
        Assert.That(result.EnqueuedKind, Is.EqualTo(OrderKind.Hold));
        Assert.That(result.EnqueuedKind, Is.Not.EqualTo(OrderKind.Engage));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders[^1].Kind, Is.EqualTo(OrderKind.Hold));
    }

    [Test]
    public void Attack_option_ordinal_casing_is_consistent_between_engage_and_empty_command()
    {
        var (bridgeEngage, snapEngage, adviceEngage) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        var engageCard = SupervisedAdviceCardBinder.Project(
            bridgeEngage, snapEngage, adviceEngage, selectedUnitId: "u1", proposedCommandId: "engage", attackOptionId: "FIRE-SINGLE");
        Assert.That(engageCard.IsLethalOrder, Is.False);

        var engageResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridgeEngage,
            snapEngage,
            new SupervisedAdviceAcceptRequest("u1", adviceEngage, "engage", "FIRE-SINGLE"));
        Assert.That(engageResult.Accepted, Is.False);
        Assert.That(engageResult.FailureReason, Is.EqualTo("UNKNOWN_OPTION"));
        Assert.That(bridgeEngage.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(bridgeEngage.Registry.TryGetBinding(new TargetId("u1"), out var engageBinding), Is.True);
        Assert.That(engageBinding.Target.Slot.Active, Is.InstanceOf<AgentController>());

        var (bridgeEmpty, snapEmpty, adviceEmpty) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        var emptyCard = SupervisedAdviceCardBinder.Project(
            bridgeEmpty, snapEmpty, adviceEmpty, selectedUnitId: "u1", proposedCommandId: string.Empty, attackOptionId: "FIRE-SINGLE");
        Assert.That(emptyCard.IsLethalOrder, Is.False);

        var emptyResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridgeEmpty,
            snapEmpty,
            new SupervisedAdviceAcceptRequest("u1", adviceEmpty, string.Empty, "FIRE-SINGLE"));
        Assert.That(emptyResult.Accepted, Is.False);
        Assert.That(emptyResult.FailureReason, Is.EqualTo("UNKNOWN_OPTION"));
        Assert.That(bridgeEmpty.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(bridgeEmpty.Registry.TryGetBinding(new TargetId("u1"), out var emptyBinding), Is.True);
        Assert.That(emptyBinding.Target.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(-1.0)]
    public void Accept_rejects_non_finite_and_negative_sim_time_without_mutating_order_log(double invalidSimTime)
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        var beforeFp = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        snapshot.SimTime = invalidSimTime;

        var result = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest("u1", advice, "hold"));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonInvalidSimTime));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));

        var card = SupervisedAdviceCardBinder.Project(bridge, snapshot, advice, "u1", "hold");
        Assert.That(card.CanAccept, Is.False);
        Assert.That(card.BlockReason, Is.EqualTo(CoordinationCommandBridge.ReasonInvalidSimTime));
    }

    [Test]
    public void Accept_rejected_by_engage_gate_at_rejoin_tick_does_not_mutate_comms_grid_reconciler_or_decision_log()
    {
        var (bridge, snapshot, advice) = CreateRejoinPendingScenario();
        var beforeFp = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var beforeGridHash = bridge.Orchestrator.CommsGrid!.ComputeHash();
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("u1"), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);

        snapshot.SimTime = 20.5;
        snapshot.HasFireControlTrackOnPrimaryContact = false;

        var noTrackResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest("u1", advice, "engage", "fire-single"));

        Assert.That(noTrackResult.Accepted, Is.False);
        Assert.That(noTrackResult.FailureReason, Is.EqualTo("NO_FIRE_CONTROL_TRACK"));
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));
        Assert.That(bridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(beforeGridHash));
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("u1"), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);

        snapshot.HasFireControlTrackOnPrimaryContact = true;
        var unknownOptionResult = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest("u1", advice, "engage", "nonexistent-option"));

        Assert.That(unknownOptionResult.Accepted, Is.False);
        Assert.That(unknownOptionResult.FailureReason, Is.EqualTo("UNKNOWN_OPTION"));
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));
        Assert.That(bridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(beforeGridHash));
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("u1"), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);
    }

    [Test]
    public void Accept_rejected_by_not_human_control_at_rejoin_tick_does_not_mutate_comms_grid_reconciler_or_decision_log()
    {
        var (bridge, snapshot, advice) = CreateRejoinPendingScenario();
        var beforeFp = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var beforeGridHash = bridge.Orchestrator.CommsGrid!.ComputeHash();
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("u1"), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);

        snapshot.SimTime = 20.5;
        var result = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest(
                UnitId: "u1",
                Advice: advice,
                CommandId: "hold",
                AllowTakeControlFromAssistedAgent: false));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(C2PlayerCommandBridge.ReasonNotHumanControl));
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));
        Assert.That(bridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(beforeGridHash));
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("u1"), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);
    }

    [Test]
    public void Accept_rejected_by_take_control_failed_on_group_target_at_rejoin_tick_does_not_mutate_comms_grid_reconciler_or_decision_log()
    {
        var bridge = new DelegationBridge(globalSeed: 7);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.Registry.LinkGroupMember(group.TargetId, u1.TargetId);
        bridge.Registry.MarkFriendlyMeshMember(group.TargetId);
        bridge.Registry.MarkFriendlyMeshMember(u1.TargetId);
        var agent = bridge.Orchestrator.CreateAgentFromPreset(new AgentId("a-grp"), PersonalityCatalog.All[0], AutonomyLevel.Assisted);
        Assert.That(bridge.Orchestrator.TryAssignAgentController(group.Target, agent, isFriendly: true).Allowed, Is.True);
        bridge.Orchestrator.CommsGrid = new CommsGridRegistry(
            new[]
            {
                new ScenarioCommsGridTransition(10, "g1", "OffGrid", "submerged"),
                new ScenarioCommsGridTransition(20, "g1", "OnGrid", "surfaced"),
            });
        bridge.BeginExecution();

        var snapshot = new TestWorldSnapshot { SimTime = 15.0 };
        bridge.Tick(snapshot, new NoOpOrderSink());
        bridge.Orchestrator.CommsGridRejoinReconciler.RecordUnitObservation("g1", "hostile-2", ContactLifecycleState.Identified);
        bridge.Orchestrator.DecisionLog.AppendContactChange(new ContactChangeRecord(
            SequenceId: 0,
            SimTime: 1.0,
            SimTick: 1,
            ObserverId: "g1",
            ContactId: "c1",
            TargetId: "hostile-1",
            PreviousState: "Unknown",
            NewState: "Classified"));
        var seqId = checked((int)bridge.Orchestrator.DecisionLog.ContactChanges[^1].SequenceId);
        var advice = CreateAdviceFrame(seqId, RoeLevel.WeaponsFree);

        var beforeFp = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var beforeGridHash = bridge.Orchestrator.CommsGrid.ComputeHash();
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("g1"), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);

        snapshot.SimTime = 20.5;
        var result = SupervisedAdviceAcceptBridge.TryAccept(
            bridge,
            snapshot,
            new SupervisedAdviceAcceptRequest("g1", advice, "hold"));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SupervisedAdviceAcceptBridge.ReasonTakeControlFailed));
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));
        Assert.That(bridge.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(beforeGridHash));
        Assert.That(bridge.Orchestrator.CommsGrid.IsOffGrid("g1"), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);
    }

    [Test]
    public void Accept_at_rejoin_tick_allows_valid_engage_and_non_engage_orders_and_blocks_while_off_grid()
    {
        var (bridgeOffGrid, snapOffGrid, adviceOffGrid) = CreateRejoinPendingScenario();
        var beforeFp = bridgeOffGrid.Orchestrator.DecisionLog.ComputeFingerprint();
        var beforeGridHash = bridgeOffGrid.Orchestrator.CommsGrid!.ComputeHash();
        snapOffGrid.SimTime = 15.0;

        var blockedOffGrid = SupervisedAdviceAcceptBridge.TryAccept(
            bridgeOffGrid,
            snapOffGrid,
            new SupervisedAdviceAcceptRequest("u1", adviceOffGrid, "engage", "fire-single"));

        Assert.That(blockedOffGrid.Accepted, Is.False);
        Assert.That(blockedOffGrid.FailureReason, Is.EqualTo(C2PlayerCommandBridge.ReasonOffGrid));
        Assert.That(bridgeOffGrid.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(beforeFp));
        Assert.That(bridgeOffGrid.Orchestrator.CommsGrid.ComputeHash(), Is.EqualTo(beforeGridHash));
        Assert.That(bridgeOffGrid.Orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);

        var (bridgeEngage, snapEngage, adviceEngage) = CreateRejoinPendingScenario();
        snapEngage.SimTime = 20.5;
        var acceptedEngage = SupervisedAdviceAcceptBridge.TryAccept(
            bridgeEngage,
            snapEngage,
            new SupervisedAdviceAcceptRequest("u1", adviceEngage, "engage", "fire-single"));

        Assert.That(acceptedEngage.Accepted, Is.True, acceptedEngage.FailureReason);
        Assert.That(acceptedEngage.EnqueuedKind, Is.EqualTo(OrderKind.Engage));
        Assert.That(bridgeEngage.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1));

        var (bridgeHold, snapHold, adviceHold) = CreateRejoinPendingScenario();
        snapHold.SimTime = 20.5;
        var acceptedHold = SupervisedAdviceAcceptBridge.TryAccept(
            bridgeHold,
            snapHold,
            new SupervisedAdviceAcceptRequest("u1", adviceHold, "hold"));

        Assert.That(acceptedHold.Accepted, Is.True, acceptedHold.FailureReason);
        Assert.That(acceptedHold.EnqueuedKind, Is.EqualTo(OrderKind.Hold));
        Assert.That(bridgeHold.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1));
    }

    private static (DelegationBridge Bridge, TestWorldSnapshot Snapshot, AdviceFrame Advice) CreateRejoinPendingScenario()
    {
        var (bridge, snapshot, advice) = CreateAvailableScenario(AutonomyLevel.Assisted, RoeLevel.WeaponsFree);
        bridge.Orchestrator.CommsGrid = new CommsGridRegistry(
            new[]
            {
                new ScenarioCommsGridTransition(10, "u1", "OffGrid", "submerged"),
                new ScenarioCommsGridTransition(20, "u1", "OnGrid", "surfaced"),
            });
        snapshot.SimTime = 15.0;
        bridge.Tick(snapshot, new NoOpOrderSink());
        bridge.Orchestrator.CommsGridRejoinReconciler.RecordUnitObservation("u1", "hostile-2", ContactLifecycleState.Identified);
        return (bridge, snapshot, advice);
    }

    private static (DelegationBridge Bridge, TestWorldSnapshot Snapshot, AdviceFrame Advice) CreateAvailableScenario(
        AutonomyLevel autonomy,
        RoeLevel roe)
    {
        var bridge = new DelegationBridge(globalSeed: 7);
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var hostile = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.Registry.MarkFriendlyMeshMember(u1.TargetId);
        var agent = bridge.Orchestrator.CreateAgentFromPreset(new AgentId("a-sup"), PersonalityCatalog.All[0], autonomy);
        Assert.That(bridge.Orchestrator.TryAssignAgentController(u1.Target, agent, isFriendly: true).Allowed, Is.True);

        var opposing = bridge.Orchestrator.CreateAgentFromPreset(
            new AgentId("a-red"), PersonalityCatalog.All[0], AutonomyLevel.FullAutonomous);
        Assert.That(bridge.Orchestrator.TryAssignAgentController(hostile.Target, opposing, isFriendly: false).Allowed, Is.True);

        if (roe != RoeLevel.WeaponsFree)
        {
            Assert.That(DoctrineOverrideCommand.TryApply(bridge.Orchestrator, u1.TargetId, roe.ToString(), 0.0), Is.True);
        }
        bridge.BeginExecution();

        var snapshot = new TestWorldSnapshot();
        bridge.Orchestrator.DecisionLog.AppendContactChange(new ContactChangeRecord(
            SequenceId: 0,
            SimTime: 1.0,
            SimTick: 1,
            ObserverId: "u1",
            ContactId: "c1",
            TargetId: "hostile-1",
            PreviousState: "Unknown",
            NewState: "Classified"));
        var seqId = checked((int)bridge.Orchestrator.DecisionLog.ContactChanges[^1].SequenceId);
        var advice = CreateAdviceFrame(seqId, roe);
        return (bridge, snapshot, advice);
    }

    private static AdviceFrame CreateAdviceFrame(int seqId, RoeLevel roe) =>
        new(
            ContactId: "c1",
            SimTick: 5,
            SimTime: 5.0,
            Availability: AdviceAvailability.Available,
            Confidence: 0.85,
            ContactConfidenceLabel: "HIGH",
            Evidence: new[]
            {
                new EvidencePointer(EvidenceKind.Contact, "c1", seqId, "SliceAContactFrame", "Status"),
            },
            Assumptions: Array.Empty<string>(),
            Rationale: "Hostile surface contact within weapon envelope.",
            HardConstraints: new[] { "advisory-only:no-authority-or-order" },
            PolicyConstraints: Array.Empty<string>(),
            Alternatives: new[] { "preferred:s1/w1:fire-single" },
            ResourceCommitments: Array.Empty<string>(),
            MissionPackageFacts: Array.Empty<string>(),
            Authority: new C2AuthorityProjection(
                new(roe, roe.ToString(), roe == RoeLevel.HoldFire ? C2AuthorityDisposition.Withheld : C2AuthorityDisposition.Permitted, "ROE", roe == RoeLevel.WeaponsFree),
                new(roe == RoeLevel.HoldFire ? C2AuthorityDisposition.Withheld : C2AuthorityDisposition.Permitted, "OK", null),
                Array.Empty<C2AuthorityActionState>()),
            Fallback: "Manual command review.",
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false);
}
