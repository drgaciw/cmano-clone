namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayDelegation;

using Controllers;
using Core;
using Orchestration;
using Targets;
using Replay;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.PlayDelegation;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using NUnit.Framework;

/// <summary>
/// S124-05 / S124-06 integration: S123 golden path (Load Baltic → Mixed + Friendly → Begin) → Assign Agent →
/// Rebrief success → one tick. Deterministic across runs; Baltic v2 replay hash unchanged.
/// </summary>
[TestFixture]
public sealed class PlayDelegationGoldenPathTests
{
    private const ulong BalticProductionWorldHash = 17144800277401907079UL;

    [Test]
    public void Golden_path_begin_assign_rebrief_one_tick()
    {
        var run = Run();

        Assert.That(run.Assign.Succeeded, Is.True, run.Assign.Message);
        Assert.That(run.Rebrief.Succeeded, Is.True, run.Rebrief.Message);
        var agent = run.Friendly.Slot.Active as AgentController;
        Assert.That(agent, Is.Not.Null);
        Assert.That(agent!.PersonalitySlug, Is.EqualTo("Cautious"));

        var log = run.Session.Bridge!.Orchestrator.DecisionLog;
        Assert.That(log.ControllerChanges.Single().NewKind, Is.EqualTo("Agent"));
        Assert.That(log.PolicyUpdates.Count(u => u.Field == DelegationOrchestrator.RebriefPolicyField), Is.EqualTo(1));
        Assert.That(
            log.Records.Any(r => r.TargetId == run.Friendly.Id),
            Is.True,
            "After Assign Agent the friendly unit's agent must reach the decision pipeline on the next tick.");
        Assert.That(run.Panel.Rows.Single().ControllerKind, Is.EqualTo("Agent"));
    }

    [Test]
    public void Golden_path_with_assign_and_rebrief_is_deterministic()
    {
        var a = Run();
        var b = Run();

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.Session.Bridge!.Orchestrator.DecisionLog.Records.Count,
            Is.EqualTo(a.Session.Bridge!.Orchestrator.DecisionLog.Records.Count));
    }

    [Test]
    public void Golden_path_leaves_baltic_replay_hash_invariant_untouched()
    {
        _ = Run();

        Assert.That(BalticReplayHarness.Run(42, "baltic-patrol", 4).WorldHash, Is.EqualTo(BalticProductionWorldHash));
    }

    private static GoldenRun Run()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var commands = new PlayDelegationCommands(session);
        var assign = commands.TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.FullAutonomous, simTime: 0);
        var rebrief = commands.TryRebriefAgent(friendly.Id, "Cautious", simTime: 0);

        var world = new SingleContactWorld();
        world.AdvanceTime(1.0);
        session.Bridge!.Tick(world, world);

        return new GoldenRun(
            session,
            friendly,
            assign,
            rebrief,
            AssignAgentPanelPresentation.Project(session),
            OrderLogReplayFingerprint.ComputeSha256Hex(session.Bridge.Orchestrator.DecisionLog));
    }

    private sealed record GoldenRun(
        PlayEntrySession Session,
        ICommandableTarget Friendly,
        PlayEntryResult Assign,
        PlayEntryResult Rebrief,
        AssignAgentPanelPresentation Panel,
        string Fingerprint);

    private sealed class SingleContactWorld : ISimWorldSnapshot, IOrderSink
    {
        public double SimTime { get; private set; }

        public int ContactCount => 1;

        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryHostileContactId => new TargetId(PlayModeSmokeOrbatSeeder.HostileUnitId);

        public bool HasFireControlTrackOnPrimaryContact => true;

        public bool ObserverRadarEmconActive => true;

        public void AdvanceTime(double delta) => SimTime += delta;

        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order)
        {
        }
    }
}
