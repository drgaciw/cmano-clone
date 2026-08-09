using System.Diagnostics;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Swarm;
using Xunit;

namespace ProjectAegis.Sim.Tests.Swarm;

/// <summary>DRG-91 / SWARM-A6: replay integrity events, performance caps, stress evidence.</summary>
public sealed class SwarmReplayAndCapsTests
{
    [Fact]
    public void Replay_reconstructs_integrity_affecting_events()
    {
        var live = SwarmReplayHarness.RunGoldenScenario();
        Assert.True(live.IntegrityTimeline.Count >= 3, "golden must include integrity deltas");
        Assert.True(live.Orders.Count >= 3, "golden must include Hold/Move/Attack orders");

        var reconstructed = SwarmReplayHarness.Replay(
            live.Orders,
            live.IntegrityTimeline,
            seed: live.Seed);

        Assert.Equal(live.OrderFingerprint, reconstructed.OrderFingerprint);
        // Integrity end-state + timeline deltas must match after authorized re-apply.
        Assert.Equal(live.FinalDroneCounts[SwarmReplayHarness.GoldenUnitId],
            reconstructed.FinalDroneCounts[SwarmReplayHarness.GoldenUnitId]);
        Assert.Equal(live.IntegrityTimeline.Count, reconstructed.IntegrityTimeline.Count);
        for (var i = 0; i < live.IntegrityTimeline.Count; i++)
        {
            Assert.Equal(live.IntegrityTimeline[i].UnitId, reconstructed.IntegrityTimeline[i].UnitId);
            Assert.Equal(live.IntegrityTimeline[i].DronesLost, reconstructed.IntegrityTimeline[i].DronesLost);
            Assert.Equal(live.IntegrityTimeline[i].PreviousDroneCount, reconstructed.IntegrityTimeline[i].PreviousDroneCount);
            Assert.Equal(live.IntegrityTimeline[i].NewDroneCount, reconstructed.IntegrityTimeline[i].NewDroneCount);
            Assert.Equal(live.IntegrityTimeline[i].ReasonCode, reconstructed.IntegrityTimeline[i].ReasonCode);
        }

        // Same scenario+seed twice → identical canonical fingerprint (golden stability).
        var again = SwarmReplayHarness.RunGoldenScenario(live.Seed);
        Assert.Equal(live.CanonicalFingerprint, again.CanonicalFingerprint);
        Assert.Equal(live.IntegrityHash, again.IntegrityHash);
        Assert.Equal(live.OrderFingerprint, again.OrderFingerprint);
    }

    [Fact]
    public void Golden_canonical_fingerprint_is_pinned()
    {
        var live = SwarmReplayHarness.RunGoldenScenario(SwarmReplayHarness.GoldenSeed);
        var goldenPath = ResolveGoldenPath();
        Assert.True(File.Exists(goldenPath), $"Missing golden file: {goldenPath}");
        var expected = File.ReadAllText(goldenPath).TrimEnd();
        Assert.Equal(expected, live.CanonicalFingerprint);
    }

    [Fact]
    public void Performance_caps_document_logical_vs_render_split()
    {
        Assert.Equal(40, SwarmPerformanceCaps.LogicalMaxDronesPerSwarm);
        Assert.Equal(CatalogSwarmPlatformDefaults.GenericMaxDrones, SwarmPerformanceCaps.LogicalMaxDronesPerSwarm);
        Assert.Equal(12, SwarmPerformanceCaps.RenderMaxMembersPerSwarm);
        Assert.True(SwarmPerformanceCaps.RenderMaxMembersPerSwarm < SwarmPerformanceCaps.LogicalMaxDronesPerSwarm);
        Assert.Equal(16, SwarmPerformanceCaps.DesignMaxConcurrentSwarms);
        Assert.Equal(640, SwarmPerformanceCaps.DesignMaxLogicalDrones);

        Assert.Equal(12, SwarmPerformanceCaps.RenderMemberCount(40));
        Assert.Equal(5, SwarmPerformanceCaps.RenderMemberCount(5));
        Assert.Equal(0, SwarmPerformanceCaps.RenderMemberCount(0));

        Assert.Equal(16, SwarmPerformanceCaps.EngagementWorkUnitsPerPulse(16));
        Assert.NotEqual(
            SwarmPerformanceCaps.DesignMaxLogicalDrones,
            SwarmPerformanceCaps.EngagementWorkUnitsPerPulse(SwarmPerformanceCaps.DesignMaxConcurrentSwarms));
    }

    [Fact]
    public void Register_clamps_logical_max_to_performance_cap()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(1));
        ctl.Register(
            new SwarmUnitIntegrity("over", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 99, 99),
            0,
            0);
        Assert.True(ctl.TryGetIntegrity("over", out var integrity));
        Assert.Equal(SwarmPerformanceCaps.LogicalMaxDronesPerSwarm, integrity.MaxDrones);
        Assert.Equal(SwarmPerformanceCaps.LogicalMaxDronesPerSwarm, integrity.DroneCount);
    }

    [Fact]
    public void Design_max_stress_is_O_swarms_not_O_drones_and_meets_pulse_budget()
    {
        var sw = Stopwatch.StartNew();
        var result = SwarmReplayHarness.RunDesignMaxStress();
        sw.Stop();

        Assert.Equal(SwarmPerformanceCaps.DesignMaxConcurrentSwarms, result.ConcurrentSwarms);
        Assert.Equal(SwarmPerformanceCaps.StressScenarioTicks, result.Ticks);
        Assert.Equal(SwarmPerformanceCaps.DesignMaxLogicalDrones, result.LogicalDronesAtStart);

        // Hard gate: work units scale with concurrent swarms × ticks, NOT logical drones × ticks.
        Assert.Equal(result.ExpectedWorkUnitsCeiling, result.EngagementWorkUnits);
        Assert.True(
            result.EngagementWorkUnits < result.LogicalDronesAtStart * result.Ticks,
            "Aggregate SoT must not expand engagement work to logical drone count.");
        Assert.Equal(
            result.ConcurrentSwarms * result.Ticks,
            result.EngagementWorkUnits);

        // Soft wall-clock budget (CI-friendly).
        Assert.True(
            sw.ElapsedMilliseconds < SwarmPerformanceCaps.StressPulseBudgetMs,
            $"Stress elapsed {sw.ElapsedMilliseconds}ms exceeds budget {SwarmPerformanceCaps.StressPulseBudgetMs}ms");

        // Deterministic integrity hash for stress seed.
        var again = SwarmReplayHarness.RunDesignMaxStress();
        Assert.Equal(result.FinalIntegrityHash, again.FinalIntegrityHash);
    }

    [Fact]
    public void Integrity_replay_via_controller_api_matches_live_end_state()
    {
        var liveCtl = new SwarmController(SimSeed.FromScenario(11));
        liveCtl.Register(
            new SwarmUnitIntegrity("s1", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40),
            1,
            2);
        liveCtl.IssueHold("s1", 1, 1.0);
        Assert.True(liveCtl.TryApplyIntegrityDamage("s1", 3, 2, 2.0, "test-hit", out _));
        Assert.True(liveCtl.TryApplyIntegrityDamage("s1", 5, 3, 3.0, "test-hit", out _));

        var replayCtl = new SwarmController(SimSeed.FromScenario(11));
        replayCtl.Register(
            new SwarmUnitIntegrity("s1", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40),
            1,
            2);
        SwarmController.ReplayOrders(replayCtl, liveCtl.OrderLog);
        SwarmController.ReplayIntegrityTimeline(replayCtl, liveCtl.IntegrityTimeline);

        Assert.True(liveCtl.TryGetIntegrity("s1", out var live));
        Assert.True(replayCtl.TryGetIntegrity("s1", out var replayed));
        Assert.Equal(live.DroneCount, replayed.DroneCount);
        Assert.Equal(32, replayed.DroneCount); // 40 - 3 - 5
    }

    private static string ResolveGoldenPath()
    {
        // Walk up from test output to repo root production/qa
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "production", "qa", "swarm-a6-replay-golden-fingerprint.txt");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            // Also check sibling when running from src/ProjectAegis.Sim.Tests/bin/...
            var alt = Path.Combine(dir.FullName, "..", "..", "..", "..", "..", "production", "qa", "swarm-a6-replay-golden-fingerprint.txt");
            var full = Path.GetFullPath(alt);
            if (File.Exists(full))
            {
                return full;
            }

            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "production", "qa", "swarm-a6-replay-golden-fingerprint.txt"));
    }
}
