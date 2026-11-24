using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Gameplay;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Gameplay;

[TestFixture]
public sealed class FixedStepFrameRunnerSafeStopTests
{
    [Test]
    public void Dynamic_acceleration_drop_interrupts_batch_at_exact_safe_stop_step_and_resets_excess_backlog()
    {
        var runner = new FixedStepFrameRunner(fixedDeltaSeconds: 0.1, maxStepsPerFrame: 64);
        var factor = 8;
        var steps = 0;

        // 0.1s elapsed at 8x = 0.8s -> would execute 8 steps without safe-stop.
        // At step 3, DropToRealTime lowers factor to 1x -> must break immediately at steps == 3.
        var executed = runner.Advance(
            0.1,
            () => factor,
            () => true,
            () =>
            {
                steps++;
                if (steps == 3)
                {
                    factor = 1;
                }
            });

        Assert.That(executed, Is.EqualTo(3));
        Assert.That(steps, Is.EqualTo(3));

        // Verify backlog was discarded: a zero-elapsed call runs 0 steps, and a 0.1s @ 1x call runs 1 step.
        Assert.That(runner.Advance(0.0, () => factor, () => true, () => steps++), Is.Zero);
        Assert.That(runner.Advance(0.1, () => factor, () => true, () => steps++), Is.EqualTo(1));
        Assert.That(steps, Is.EqualTo(4));
    }

    [Test]
    public void Dynamic_pause_interrupts_batch_at_exact_step_and_discards_backlog()
    {
        var runner = new FixedStepFrameRunner(fixedDeltaSeconds: 0.1, maxStepsPerFrame: 64);
        var factor = 4;
        var paused = false;
        var steps = 0;

        var executed = runner.Advance(
            0.2,
            () => factor,
            () => !paused,
            () =>
            {
                steps++;
                if (steps == 2)
                {
                    paused = true;
                }
            });

        Assert.That(executed, Is.EqualTo(2));
        Assert.That(steps, Is.EqualTo(2));
        Assert.That(runner.Advance(0.0, () => factor, () => true, () => steps++), Is.Zero);
    }
}
