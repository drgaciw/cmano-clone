using System.Diagnostics;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Swarm;

/// <summary>DRG-91 / SWARM-A6: replay integrity events, performance caps, stress evidence.</summary>
public sealed class SwarmReplayAndCapsTests
{
    [Test]
    public void Replay_reconstructs_integrity_affecting_events()
    {
        var live = SwarmReplayHarness.RunGoldenScenario();
        Assert.That(live.IntegrityTimeline.Count >= 3, Is.True, "golden must include integrity deltas");
        Assert.That(live.Orders.Count >= 3, Is.True, "golden must include Hold/Move/Attack orders");

        var reconstructed = SwarmReplayHarness.Replay(
            live.Orders,
            live.IntegrityTimeline,
            seed: live.Seed);

        Assert.That(reconstructed.OrderFingerprint, Is.EqualTo(live.OrderFingerprint));
        // Integrity end-state + timeline deltas must match after authorized re-apply.
        Assert.That(
            reconstructed.FinalDroneCounts[SwarmReplayHarness.GoldenUnitId],
            Is.EqualTo(live.FinalDroneCounts[SwarmReplayHarness.GoldenUnitId]));
        Assert.That(reconstructed.IntegrityTimeline.Count, Is.EqualTo(live.IntegrityTimeline.Count));
        for (var i = 0; i < live.IntegrityTimeline.Count; i++)
        {
            Assert.That(reconstructed.IntegrityTimeline[i].UnitId, Is.EqualTo(live.IntegrityTimeline[i].UnitId));
            Assert.That(reconstructed.IntegrityTimeline[i].DronesLost, Is.EqualTo(live.IntegrityTimeline[i].DronesLost));
            Assert.That(reconstructed.IntegrityTimeline[i].PreviousDroneCount, Is.EqualTo(live.IntegrityTimeline[i].PreviousDroneCount));
            Assert.That(reconstructed.IntegrityTimeline[i].NewDroneCount, Is.EqualTo(live.IntegrityTimeline[i].NewDroneCount));
            Assert.That(reconstructed.IntegrityTimeline[i].ReasonCode, Is.EqualTo(live.IntegrityTimeline[i].ReasonCode));
        }

        // Same scenario+seed twice → identical canonical fingerprint (golden stability).
        var again = SwarmReplayHarness.RunGoldenScenario(live.Seed);
        Assert.That(again.CanonicalFingerprint, Is.EqualTo(live.CanonicalFingerprint));
        Assert.That(again.IntegrityHash, Is.EqualTo(live.IntegrityHash));
        Assert.That(again.OrderFingerprint, Is.EqualTo(live.OrderFingerprint));
    }

    [Test]
    public void Golden_canonical_fingerprint_is_pinned()
    {
        var live = SwarmReplayHarness.RunGoldenScenario(SwarmReplayHarness.GoldenSeed);
        var goldenPath = ResolveGoldenPath();
        Assert.That(File.Exists(goldenPath), Is.True, $"Missing golden file: {goldenPath}");
        var expected = File.ReadAllText(goldenPath).TrimEnd();
        Assert.That(live.CanonicalFingerprint, Is.EqualTo(expected));
    }

    [Test]
    public void Performance_caps_document_logical_vs_render_split()
    {
        Assert.That(SwarmPerformanceCaps.LogicalMaxDronesPerSwarm, Is.EqualTo(40));
        Assert.That(SwarmPerformanceCaps.LogicalMaxDronesPerSwarm, Is.EqualTo(CatalogSwarmPlatformDefaults.GenericMaxDrones));
        Assert.That(SwarmPerformanceCaps.RenderMaxMembersPerSwarm, Is.EqualTo(12));
        Assert.That(SwarmPerformanceCaps.RenderMaxMembersPerSwarm < SwarmPerformanceCaps.LogicalMaxDronesPerSwarm, Is.True);
        Assert.That(SwarmPerformanceCaps.DesignMaxConcurrentSwarms, Is.EqualTo(16));
        Assert.That(SwarmPerformanceCaps.DesignMaxLogicalDrones, Is.EqualTo(640));

        Assert.That(SwarmPerformanceCaps.RenderMemberCount(40), Is.EqualTo(12));
        Assert.That(SwarmPerformanceCaps.RenderMemberCount(5), Is.EqualTo(5));
        Assert.That(SwarmPerformanceCaps.RenderMemberCount(0), Is.EqualTo(0));

        Assert.That(SwarmPerformanceCaps.EngagementWorkUnitsPerPulse(16), Is.EqualTo(16));
        Assert.That(
            SwarmPerformanceCaps.EngagementWorkUnitsPerPulse(SwarmPerformanceCaps.DesignMaxConcurrentSwarms),
            Is.Not.EqualTo(SwarmPerformanceCaps.DesignMaxLogicalDrones));
    }

    [Test]
    public void Register_clamps_logical_max_to_performance_cap()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(1));
        ctl.Register(
            new SwarmUnitIntegrity("over", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 99, 99),
            0,
            0);
        Assert.That(ctl.TryGetIntegrity("over", out var integrity), Is.True);
        Assert.That(integrity.MaxDrones, Is.EqualTo(SwarmPerformanceCaps.LogicalMaxDronesPerSwarm));
        Assert.That(integrity.DroneCount, Is.EqualTo(SwarmPerformanceCaps.LogicalMaxDronesPerSwarm));
    }

    [Test]
    public void Design_max_stress_is_O_swarms_not_O_drones_and_meets_pulse_budget()
    {
        var sw = Stopwatch.StartNew();
        var result = SwarmReplayHarness.RunDesignMaxStress();
        sw.Stop();

        Assert.That(result.ConcurrentSwarms, Is.EqualTo(SwarmPerformanceCaps.DesignMaxConcurrentSwarms));
        Assert.That(result.Ticks, Is.EqualTo(SwarmPerformanceCaps.StressScenarioTicks));
        Assert.That(result.LogicalDronesAtStart, Is.EqualTo(SwarmPerformanceCaps.DesignMaxLogicalDrones));

        // Hard gate: work units scale with concurrent swarms × ticks, NOT logical drones × ticks.
        Assert.That(result.EngagementWorkUnits, Is.EqualTo(result.ExpectedWorkUnitsCeiling));
        Assert.That(
            result.EngagementWorkUnits < result.LogicalDronesAtStart * result.Ticks,
            Is.True,
            "Aggregate SoT must not expand engagement work to logical drone count.");
        Assert.That(result.EngagementWorkUnits, Is.EqualTo(result.ConcurrentSwarms * result.Ticks));

        // Soft wall-clock budget (CI-friendly).
        Assert.That(
            sw.ElapsedMilliseconds < SwarmPerformanceCaps.StressPulseBudgetMs,
            Is.True,
            $"Stress elapsed {sw.ElapsedMilliseconds}ms exceeds budget {SwarmPerformanceCaps.StressPulseBudgetMs}ms");

        // Deterministic integrity hash for stress seed.
        var again = SwarmReplayHarness.RunDesignMaxStress();
        Assert.That(again.FinalIntegrityHash, Is.EqualTo(result.FinalIntegrityHash));
    }

    [Test]
    public void Integrity_replay_via_controller_api_matches_live_end_state()
    {
        var liveCtl = new SwarmController(SimSeed.FromScenario(11));
        liveCtl.Register(
            new SwarmUnitIntegrity("s1", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40),
            1,
            2);
        liveCtl.IssueHold("s1", 1, 1.0);
        Assert.That(liveCtl.TryApplyIntegrityDamage("s1", 3, 2, 2.0, "test-hit", out _), Is.True);
        Assert.That(liveCtl.TryApplyIntegrityDamage("s1", 5, 3, 3.0, "test-hit", out _), Is.True);

        var replayCtl = new SwarmController(SimSeed.FromScenario(11));
        replayCtl.Register(
            new SwarmUnitIntegrity("s1", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40),
            1,
            2);
        SwarmController.ReplayOrders(replayCtl, liveCtl.OrderLog);
        SwarmController.ReplayIntegrityTimeline(replayCtl, liveCtl.IntegrityTimeline);

        Assert.That(liveCtl.TryGetIntegrity("s1", out var live), Is.True);
        Assert.That(replayCtl.TryGetIntegrity("s1", out var replayed), Is.True);
        Assert.That(replayed.DroneCount, Is.EqualTo(live.DroneCount));
        Assert.That(replayed.DroneCount, Is.EqualTo(32)); // 40 - 3 - 5
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
