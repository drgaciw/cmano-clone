namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

/// <summary>
/// Headless equivalent of <c>C2TopBarPanelHost</c> Begin Execution wiring.
/// </summary>
[TestFixture]
public sealed class C2TopBarBeginExecutionTests
{
    [Test]
    public void BeginExecution_transitions_planning_to_executing_via_bridge()
    {
        var bridge = new DelegationBridge(42);
        Assert.That(bridge.Phase, Is.EqualTo(SimulationPhase.Planning));

        bridge.BeginExecution();

        Assert.That(bridge.Phase, Is.EqualTo(SimulationPhase.Executing));
        Assert.That(bridge.Orchestrator.DecisionLog.ModeChanges, Has.Count.EqualTo(1));
        Assert.That(bridge.Orchestrator.DecisionLog.ModeChanges[0].NewMode, Is.EqualTo("Executing"));
    }

    [Test]
    public void Planning_ticks_no_op_until_begin_execution_like_top_bar_button()
    {
        var bridge = new DelegationBridge(42);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        bridge.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        bridge.Orchestrator.Register(unit);

        var harness = new PlayModeHarness(contactCount: 2, hasFireControlTrack: true);
        harness.AdvanceTime(1.0 / 60.0);
        bridge.Tick(harness, harness);

        Assert.That(bridge.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(bridge.Orchestrator.DecisionLog.Records, Is.Empty);

        bridge.BeginExecution();
        harness.AdvanceTime(1.0 / 60.0);
        bridge.Tick(harness, harness);

        Assert.That(bridge.Phase, Is.EqualTo(SimulationPhase.Executing));
        Assert.That(bridge.Orchestrator.DecisionLog.Records, Is.Not.Empty);
    }

    [Test]
    public void BeginExecution_double_call_appends_single_mode_change_row()
    {
        var bridge = new DelegationBridge(42);

        bridge.BeginExecution();
        bridge.BeginExecution();

        Assert.That(bridge.Phase, Is.EqualTo(SimulationPhase.Executing));
        Assert.That(bridge.Orchestrator.DecisionLog.ModeChanges, Has.Count.EqualTo(1));
    }

    [Test]
    public void Planning_top_bar_projection_freezes_score_until_execution()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var log = bridge.Orchestrator.DecisionLog;
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));

        var planning = C2TopBarProjection.Project(5, bridge.Phase, "1x", "Mixed", log);
        bridge.BeginExecution();
        var executing = C2TopBarProjection.Project(5, bridge.Phase, "1x", "Mixed", log);

        Assert.That(planning.ScoreLabel, Is.EqualTo("SCORE: 0  KILLS: 0  MSLS: 0"));
        Assert.That(executing.ScoreLabel, Does.Contain("KILLS: 1"));
    }

    [Test]
    public void C2_top_bar_panel_wires_begin_execution_button_to_bridge_host()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "TopBar",
            "C2TopBarPanel.uxml");
        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "C2TopBarPanelHost.cs");

        Assert.That(File.Exists(uxmlPath), Is.True);
        Assert.That(File.Exists(hostPath), Is.True);

        var uxml = File.ReadAllText(uxmlPath);
        var host = File.ReadAllText(hostPath);

        Assert.That(uxml, Does.Contain("begin-execution-button"));
        Assert.That(host, Does.Contain("bridgeHost.BeginExecution()"));
        Assert.That(host, Does.Contain("SimulationPhase.Planning"));
        Assert.That(host, Does.Not.Contain("Bridge.BeginExecution()"));
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
        {
            _ = perceived;
            _ = traits;
            return [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
        }
    }

    private sealed class PlayModeHarness : ISimWorldSnapshot, IOrderSink
    {
        private readonly int _contactCount;
        private readonly bool _hasFireControlTrack;
        private double _simTime;
        private readonly List<(EntityKey Entity, Order Order)> _applied = new();

        public PlayModeHarness(int contactCount, bool hasFireControlTrack)
        {
            _contactCount = contactCount;
            _hasFireControlTrack = hasFireControlTrack;
        }

        public double SimTime => _simTime;

        public int ContactCount => _contactCount;

        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryHostileContactId =>
            _contactCount > 0 ? new TargetId("hostile-1") : null;

        public bool HasFireControlTrackOnPrimaryContact =>
            _contactCount > 0 && _hasFireControlTrack;

        public bool ObserverRadarEmconActive => true;

        public IReadOnlyList<(EntityKey Entity, Order Order)> AppliedOrders => _applied;

        public void AdvanceTime(double delta) => _simTime += delta;

        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order) =>
            _applied.Add((entity, order));
    }
}