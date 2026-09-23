namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using Controllers;
using Core;
using Decision;
using MissionIntent;
using Policy;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using Traits;
using NUnit.Framework;

[TestFixture]
public sealed class CoordinationAssistedWithdrawRedeployConfirmBinderTests
{
    [SetUp]
    public void SetUp() => CoordinationAssistedWithdrawRedeployConfirmBinder.ResetForTests();

    [TearDown]
    public void TearDown() => CoordinationAssistedWithdrawRedeployConfirmBinder.ResetForTests();

    [Test]
    public void Assisted_withdraw_requires_explicit_confirm_before_submit()
    {
        Assert.That(
            CoordinationAssistedWithdrawRedeployConfirmBinder.RequiresExplicitConfirm(
                AutonomyLevel.Assisted,
                CoordinationDecision.Withdraw),
            Is.True);
        Assert.That(
            CoordinationAssistedWithdrawRedeployConfirmBinder.RequiresExplicitConfirm(
                AutonomyLevel.Assisted,
                CoordinationDecision.Hold),
            Is.False);
    }

    [Test]
    public void Begin_assisted_withdraw_does_not_mutate_player_order_log()
    {
        var (bridge, groupId) = CreateAssistedHumanGroup();
        var intent = MissionIntentProjection.Project(new MissionIntentInput(
            groupId, "", MissionIntentCode.Hold, [], MissionIntentRetaskAdvice.Withdraw));

        Assert.That(
            CoordinationAssistedWithdrawRedeployConfirmBinder.TryBegin(
                new CoordinationAssistedWithdrawRedeployConfirmInput(
                    AutonomyLevel.Assisted,
                    groupId,
                    CoordinationDecision.Withdraw,
                    intent),
                out var chrome),
            Is.True);
        var panelLabels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(chrome);
        Assert.That(panelLabels.Title, Does.Contain("redeploy").IgnoreCase);
        Assert.That(panelLabels.Body, Is.Not.Empty);
        Assert.That(panelLabels.ConfirmLabel, Is.Not.Empty);
        Assert.That(panelLabels.CancelLabel, Is.Not.Empty);
        var pendingLabels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(
            CoordinationAssistedWithdrawRedeployConfirmBinder.Pending!);
        Assert.That(pendingLabels.GroupId, Is.EqualTo(groupId));
        Assert.That(pendingLabels.Decision, Is.EqualTo(CoordinationDecision.Withdraw));
        Assert.That(pendingLabels.RedeploySuggestion, Is.True);
        var inputLabels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(
            new CoordinationAssistedWithdrawRedeployConfirmInput(
                AutonomyLevel.Assisted,
                groupId,
                CoordinationDecision.Withdraw,
                intent));
        Assert.That(inputLabels.DelegationAutonomy, Is.EqualTo(AutonomyLevel.Assisted));
        Assert.That(inputLabels.GroupId, Is.EqualTo(groupId));
        Assert.That(inputLabels.Decision, Is.EqualTo(CoordinationDecision.Withdraw));
        Assert.That(inputLabels.ReviewedIntent, Is.SameAs(intent));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
        Assert.That(CoordinationAssistedWithdrawRedeployConfirmBinder.Pending, Is.Not.Null);
    }

    [Test]
    public void Cancel_clears_pending_without_order_log_mutation()
    {
        var (bridge, groupId) = CreateAssistedHumanGroup();
        CoordinationAssistedWithdrawRedeployConfirmBinder.TryBegin(
            new CoordinationAssistedWithdrawRedeployConfirmInput(
                AutonomyLevel.Assisted,
                groupId,
                CoordinationDecision.Withdraw,
                ReviewedIntent: null),
            out _);

        Assert.That(CoordinationAssistedWithdrawRedeployConfirmBinder.TryCancel(), Is.True);
        Assert.That(CoordinationAssistedWithdrawRedeployConfirmBinder.Pending, Is.Null);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Confirm_routes_through_command_facade_delegate_only()
    {
        var (bridge, groupId) = CreateAssistedHumanGroup();
        CoordinationAssistedWithdrawRedeployConfirmBinder.TryBegin(
            new CoordinationAssistedWithdrawRedeployConfirmInput(
                AutonomyLevel.Assisted,
                groupId,
                CoordinationDecision.Withdraw,
                ReviewedIntent: null),
            out _);

        var submitCalls = 0;
        var result = CoordinationAssistedWithdrawRedeployConfirmBinder.Confirm(
            (id, decision) =>
            {
                submitCalls++;
                var command = CoordinationCommandBridge.Submit(bridge, new Snapshot(3), id, decision);
                return command.Accepted ? "ok" : command.FailureReason ?? "failed";
            });

        Assert.That(submitCalls, Is.EqualTo(1));
        var resultLabels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(result);
        Assert.That(resultLabels.Accepted, Is.True);
        Assert.That(resultLabels.StatusLine, Is.EqualTo("ok"));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1));
        Assert.That(CoordinationAssistedWithdrawRedeployConfirmBinder.Pending, Is.Null);
    }

    [Test]
    public void Manual_withdraw_does_not_require_confirm_gate()
    {
        Assert.That(
            CoordinationAssistedWithdrawRedeployConfirmBinder.RequiresExplicitConfirm(
                AutonomyLevel.Manual,
                CoordinationDecision.Withdraw),
            Is.False);
    }

    [Test]
    public void Panel_binder_reads_every_record_property_for_inspect()
    {
        var chrome = new CoordinationAssistedWithdrawRedeployChrome(
            "Confirm withdraw / redeploy",
            "body",
            "CONFIRM",
            "CANCEL");
        var chromeLabels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(chrome);
        Assert.That(chromeLabels.Title, Is.EqualTo(chrome.Title));
        Assert.That(chromeLabels.Body, Is.EqualTo(chrome.Body));
        Assert.That(chromeLabels.ConfirmLabel, Is.EqualTo(chrome.ConfirmLabel));
        Assert.That(chromeLabels.CancelLabel, Is.EqualTo(chrome.CancelLabel));

        var result = new CoordinationAssistedWithdrawRedeployConfirmResult(true, "Queued");
        var resultLabels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(result);
        Assert.That(resultLabels.Accepted, Is.True);
        Assert.That(resultLabels.StatusLine, Is.EqualTo("Queued"));

        var pending = new CoordinationAssistedWithdrawRedeployPendingRequest(
            "g1",
            CoordinationDecision.Withdraw,
            null,
            true);
        var pendingLabels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(pending);
        Assert.That(pendingLabels.GroupId, Is.EqualTo("g1"));
        Assert.That(pendingLabels.Decision, Is.EqualTo(CoordinationDecision.Withdraw));
        Assert.That(pendingLabels.ReviewedIntent, Is.Null);
        Assert.That(pendingLabels.RedeploySuggestion, Is.True);

        var input = new CoordinationAssistedWithdrawRedeployConfirmInput(
            AutonomyLevel.Assisted,
            "g1",
            CoordinationDecision.Withdraw,
            null);
        var inputLabels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(input);
        Assert.That(inputLabels.DelegationAutonomy, Is.EqualTo(AutonomyLevel.Assisted));
        Assert.That(inputLabels.GroupId, Is.EqualTo("g1"));
        Assert.That(inputLabels.Decision, Is.EqualTo(CoordinationDecision.Withdraw));
        Assert.That(inputLabels.ReviewedIntent, Is.Null);
    }

    [Test]
    public void Resolve_delegation_autonomy_reads_suspended_agent_on_group_members()
    {
        var bridge = new DelegationBridge(42);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g-assisted");
        group.Target.Slot.SetActive(new HumanController());
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SuspendAgent(CreateAssistedAgent("a1"));
        unit.Target.Slot.SetActive(new HumanController());
        bridge.Registry.LinkGroupMember(group.TargetId, unit.TargetId);

        Assert.That(
            CoordinationAssistedWithdrawRedeployConfirmBinder.ResolveDelegationAutonomy(bridge, "g-assisted"),
            Is.EqualTo(AutonomyLevel.Assisted));
    }

    private static (DelegationBridge Bridge, string GroupId) CreateAssistedHumanGroup()
    {
        var bridge = new DelegationBridge(42);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        group.Target.Slot.SetActive(new HumanController());
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SuspendAgent(CreateAssistedAgent("agent-1"));
        unit.Target.Slot.SetActive(new HumanController());
        bridge.Registry.LinkGroupMember(group.TargetId, unit.TargetId);
        return (bridge, group.TargetId.Value);
    }

    private static AgentController CreateAssistedAgent(string id) => new(
        new AgentId(id),
        PersonalityCatalog.All[0].Traits,
        AutonomyLevel.Assisted,
        new SeededRng(1, 1),
        new StubPatrolPolicy(),
        attentionBudget: 20);

    private sealed class Snapshot(double simTime) : ISimWorldSnapshot
    {
        public double SimTime { get; } = simTime;
        public int ContactCount => 0;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => null;
        public bool HasFireControlTrackOnPrimaryContact => false;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => memberId.Value == "u1";
    }
}
