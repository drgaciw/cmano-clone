namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayEntry;

using Core;
using Orchestration;
using Replay;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using NUnit.Framework;

/// <summary>
/// S123-07 W3-CORE-01 headless golden path: Load Baltic → briefing → mode + side → Begin Execution → one tick.
/// Feeds the DRG-208 interim owner walk (DRG-244 evidence index); does not close DRG-208.
/// </summary>
[TestFixture]
public sealed class PlayEntryGoldenPathSmokeTests
{
    private const ulong BalticProductionWorldHash = 17144800277401907079UL;

    [Test]
    public void Golden_path_load_baltic_briefing_mode_side_begin_one_tick()
    {
        var run = RunGoldenPath();

        Assert.That(run.LoadResult.Succeeded, Is.True, run.LoadResult.Message);
        Assert.That(run.PhaseAfterLoad, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(run.Briefing.State, Is.Not.EqualTo(BriefingContentState.NoPackage));
        Assert.That(run.Briefing.MissionLines, Is.Not.Empty);
        Assert.That(run.ButtonBeforeSelection.IsEnabled, Is.False);
        Assert.That(run.ButtonAfterSelection.IsEnabled, Is.True);
        Assert.That(run.BeginResult.Succeeded, Is.True, run.BeginResult.Message);
        Assert.That(run.Session.State.Phase, Is.EqualTo(SimulationPhase.Executing));
        Assert.That(run.Session.State.Mode, Is.EqualTo(SimulationModeKind.Mixed));
        Assert.That(run.Session.State.Side, Is.EqualTo(PlaySide.Friendly));

        var log = run.Session.Bridge!.Orchestrator.DecisionLog;
        Assert.That(log.ModeChanges, Has.Count.EqualTo(1));
        Assert.That(log.ModeChanges[0].NewMode, Is.EqualTo(nameof(SimulationPhase.Executing)));
        Assert.That(run.DecisionRecordsAfterTick, Is.GreaterThan(0),
            "The executing tick must reach the delegation pipeline (agent-controlled opposing unit decides).");
    }

    [Test]
    public void Golden_path_is_deterministic_for_the_same_package()
    {
        var a = RunGoldenPath();
        var b = RunGoldenPath();

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.DecisionRecordsAfterTick, Is.EqualTo(a.DecisionRecordsAfterTick));
    }

    [Test]
    public void Golden_path_leaves_baltic_replay_hash_invariant_untouched()
    {
        _ = RunGoldenPath();

        var replay = BalticReplayHarness.Run(42, "baltic-patrol", 4);

        Assert.That(replay.WorldHash, Is.EqualTo(BalticProductionWorldHash));
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var golden = File.ReadAllText(Path.Combine(
            repoRoot!, "tests", "regression", "replay-golden-baltic-engage-2026-06-02.txt"));
        Assert.That(golden, Does.Contain("WORLD_HASH=" + BalticProductionWorldHash));
    }

    private static GoldenPathRun RunGoldenPath()
    {
        var session = new PlayEntrySession();
        var entry = PlayEntrySessionTests.BalticEntry();

        var load = session.TryLoad(entry);
        var phaseAfterLoad = session.State.Phase;
        var briefing = BriefingContentPresentation.Bind(session);
        var before = BeginExecutionButtonPresentation.Project(session.EvaluateBeginGate());

        session.TrySelectMode(SimulationModeKind.Mixed);
        session.TrySelectSide(PlaySide.Friendly);
        var after = BeginExecutionButtonPresentation.Project(session.EvaluateBeginGate());

        var (friendly, opposing) = PlayEntrySessionTests.RegisterBalticForces(session.Bridge!);
        var begin = session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        var world = new SingleContactWorld();
        world.AdvanceTime(1.0);
        session.Bridge!.Tick(world, world);

        var log = session.Bridge.Orchestrator.DecisionLog;
        return new GoldenPathRun(
            session,
            load,
            phaseAfterLoad,
            briefing,
            before,
            after,
            begin,
            log.Records.Count,
            OrderLogReplayFingerprint.ComputeSha256Hex(log));
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName;
    }

    private sealed record GoldenPathRun(
        PlayEntrySession Session,
        PlayEntryResult LoadResult,
        SimulationPhase? PhaseAfterLoad,
        BriefingContentPresentation Briefing,
        BeginExecutionButtonPresentation ButtonBeforeSelection,
        BeginExecutionButtonPresentation ButtonAfterSelection,
        PlayEntryResult BeginResult,
        int DecisionRecordsAfterTick,
        string Fingerprint);

    private sealed class SingleContactWorld : ISimWorldSnapshot, IOrderSink
    {
        private double _simTime;

        public double SimTime => _simTime;

        public int ContactCount => 1;

        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryHostileContactId => new TargetId(PlayModeSmokeOrbatSeeder.HostileUnitId);

        public bool HasFireControlTrackOnPrimaryContact => true;

        public bool ObserverRadarEmconActive => true;

        public void AdvanceTime(double delta) => _simTime += delta;

        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order)
        {
        }
    }
}
