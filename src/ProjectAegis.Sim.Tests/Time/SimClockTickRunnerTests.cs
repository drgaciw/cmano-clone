using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Time;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Time;

/// <summary>TC-CLK-1..5 — pause and acceleration on the tick loop (S112-01 / DRG-14).</summary>
public sealed class SimClockTickRunnerTests
{
    // TC-CLK-1
    [Test]
    public void TC_CLK_1_Pause_then_TickOnce_RealTime_leaves_tick_and_hash_unchanged()
    {
        var runner = new SimTickRunner(SimSeed.FromScenario(42));
        runner.TickOnce(TimeCompressionMode.RealTime);
        var tick = runner.Clock.SimTick;
        var hash = runner.LastWorldHash;

        runner.Clock.Pause();
        runner.TickOnce(TimeCompressionMode.RealTime);

        Assert.That(runner.Clock.SimTick, Is.EqualTo(tick));
        Assert.That(runner.LastWorldHash, Is.EqualTo(hash));
    }

    // TC-CLK-2
    [Test]
    public void TC_CLK_2_Resume_then_TickOnce_advances()
    {
        var runner = new SimTickRunner(SimSeed.FromScenario(42));
        runner.Clock.Pause();
        runner.TickOnce(TimeCompressionMode.RealTime);
        Assert.That(runner.Clock.SimTick, Is.EqualTo(0UL));

        runner.Clock.Resume();
        runner.TickOnce(TimeCompressionMode.RealTime);

        Assert.That(runner.Clock.SimTick, Is.EqualTo(1UL));
        Assert.That(runner.LastWorldHash, Is.Not.EqualTo(0UL));
    }

    // TC-CLK-3
    [Test]
    public void TC_CLK_3_Accelerated_factor_4_once_matches_RealTime_four_times()
    {
        const ulong seed = 9034412;
        var accelerated = new SimTickRunner(SimSeed.FromScenario(seed));
        accelerated.Clock.SetAccelerationFactor(4);
        accelerated.TickOnce(TimeCompressionMode.Accelerated);

        var realtime = new SimTickRunner(SimSeed.FromScenario(seed));
        for (var i = 0; i < 4; i++)
        {
            realtime.TickOnce(TimeCompressionMode.RealTime);
        }

        Assert.That(accelerated.Clock.SimTick, Is.EqualTo(realtime.Clock.SimTick));
        Assert.That(accelerated.LastWorldHash, Is.EqualTo(realtime.LastWorldHash));
        Assert.That(accelerated.Clock.SimTick, Is.EqualTo(4UL));
    }

    // TC-CLK-4
    [Test]
    public void TC_CLK_4_Paused_blocks_Accelerated_too()
    {
        var runner = new SimTickRunner(SimSeed.FromScenario(7));
        runner.Clock.SetAccelerationFactor(8);
        runner.Clock.Pause();
        runner.TickOnce(TimeCompressionMode.Accelerated);

        Assert.That(runner.Clock.SimTick, Is.EqualTo(0UL));
        Assert.That(runner.LastWorldHash, Is.EqualTo(0UL));
    }

    // TC-CLK-5 — HeadlessBatch overrides pause for CI/batch
    [Test]
    public void TC_CLK_5_HeadlessBatch_overrides_pause_and_advances()
    {
        var runner = new SimTickRunner(SimSeed.FromScenario(99));
        runner.Clock.Pause();
        runner.TickOnce(TimeCompressionMode.HeadlessBatch);

        Assert.That(runner.Clock.SimTick, Is.EqualTo(1UL));
        Assert.That(runner.LastWorldHash, Is.Not.EqualTo(0UL));
    }

    [Test]
    public void Pause_mid_run_freezes_resume_continues_deterministically()
    {
        const ulong seed = 111;
        var a = new SimTickRunner(SimSeed.FromScenario(seed));
        a.TickOnce(TimeCompressionMode.RealTime);
        a.TickOnce(TimeCompressionMode.RealTime);
        a.Clock.Pause();
        a.TickOnce(TimeCompressionMode.RealTime);
        a.TickOnce(TimeCompressionMode.Accelerated);
        a.Clock.Resume();
        a.TickOnce(TimeCompressionMode.RealTime);
        a.TickOnce(TimeCompressionMode.RealTime);

        var b = new SimTickRunner(SimSeed.FromScenario(seed));
        for (var i = 0; i < 4; i++)
        {
            b.TickOnce(TimeCompressionMode.RealTime);
        }

        Assert.That(a.Clock.SimTick, Is.EqualTo(b.Clock.SimTick));
        Assert.That(a.LastWorldHash, Is.EqualTo(b.LastWorldHash));
    }

    [Test]
    public void Pipeline_Accelerated_runs_engagement_per_step()
    {
        var resolver = new RecordingEngagementResolver();
        var pipeline = new SimTickPipeline(SimSeed.FromScenario(1), resolver);
        pipeline.Clock.SetAccelerationFactor(3);

        pipeline.EnqueueEngagement(new EngageRequest(1, 2, 0, 0));
        pipeline.EnqueueEngagement(new EngageRequest(3, 4, 0, 0));
        // Pending drains on first sub-step; later accelerated steps see empty pending.
        pipeline.TickOnce(TimeCompressionMode.Accelerated);

        Assert.That(pipeline.Clock.SimTick, Is.EqualTo(3UL));
        Assert.That(resolver.Requests.Count, Is.EqualTo(2));
    }

    [Test]
    public void Pipeline_paused_RealTime_is_noop()
    {
        var resolver = new RecordingEngagementResolver();
        var pipeline = new SimTickPipeline(SimSeed.FromScenario(1), resolver);
        pipeline.EnqueueEngagement(new EngageRequest(1, 2, 0, 0));
        pipeline.Clock.Pause();
        pipeline.TickOnce(TimeCompressionMode.RealTime);

        Assert.That(pipeline.Clock.SimTick, Is.EqualTo(0UL));
        Assert.That(resolver.Requests, Is.Empty);
        AssertEx.Single(pipeline.PendingEngagements);
    }

    [Test]
    public void Pipeline_HeadlessBatch_overrides_pause()
    {
        var resolver = new RecordingEngagementResolver();
        var pipeline = new SimTickPipeline(SimSeed.FromScenario(1), resolver);
        pipeline.EnqueueEngagement(new EngageRequest(1, 2, 0, 0));
        pipeline.Clock.Pause();
        pipeline.TickOnce(TimeCompressionMode.HeadlessBatch);

        Assert.That(pipeline.Clock.SimTick, Is.EqualTo(1UL));
        AssertEx.Single(resolver.Requests);
    }

    [Test]
    public void Pipeline_Accelerated_matches_RealTime_hash_without_engagements()
    {
        const ulong seed = 55;
        var accel = new SimTickPipeline(SimSeed.FromScenario(seed), new RecordingEngagementResolver());
        accel.Clock.SetAccelerationFactor(5);
        accel.TickOnce(TimeCompressionMode.Accelerated);

        var rt = new SimTickPipeline(SimSeed.FromScenario(seed), new RecordingEngagementResolver());
        for (var i = 0; i < 5; i++)
        {
            rt.TickOnce(TimeCompressionMode.RealTime);
        }

        Assert.That(accel.Clock.SimTick, Is.EqualTo(rt.Clock.SimTick));
        Assert.That(accel.LastWorldHash, Is.EqualTo(rt.LastWorldHash));
    }
}
