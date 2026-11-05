namespace ProjectAegis.Delegation.UnityAdapter.Tests.Gameplay;

using ProjectAegis.Delegation.UnityAdapter.Gameplay;
using NUnit.Framework;

[TestFixture]
public sealed class FixedStepFrameRunnerTests
{
    [TestCase(30)]
    [TestCase(60)]
    [TestCase(144)]
    public void Equal_elapsed_time_advances_equal_steps_at_different_render_cadences(int framesPerSecond)
    {
        var runner = new FixedStepFrameRunner();
        var ticks = 0;
        for (var frame = 0; frame < framesPerSecond * 10; frame++)
        {
            runner.Advance(1.0 / framesPerSecond, 1, () => true, () => ticks++);
        }

        Assert.That(ticks, Is.EqualTo(600));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(32)]
    [TestCase(256)]
    public void Acceleration_applies_exactly_once(int acceleration)
    {
        var runner = new FixedStepFrameRunner();
        var ticks = 0;
        for (var frame = 0; frame < 60; frame++)
        {
            runner.Advance(1.0 / 60, acceleration, () => true, () => ticks++);
        }

        Assert.That(ticks, Is.EqualTo(60 * acceleration));
    }

    [Test]
    public void Running_backlog_is_retained_when_frame_work_is_bounded()
    {
        var runner = new FixedStepFrameRunner(0.1, maxStepsPerFrame: 2);
        var ticks = 0;
        Assert.That(runner.Advance(0.5, 1, () => true, () => ticks++), Is.EqualTo(2));
        Assert.That(runner.Advance(0, 1, () => true, () => ticks++), Is.EqualTo(2));
        Assert.That(runner.Advance(0, 1, () => true, () => ticks++), Is.EqualTo(1));
        Assert.That(ticks, Is.EqualTo(5));
    }

    [Test]
    public void Pause_interrupts_current_batch_and_discards_backlog()
    {
        var runner = new FixedStepFrameRunner(0.1);
        var ticks = 0;
        var running = true;
        var advanced = runner.Advance(1, 8, () => running, () =>
        {
            ticks++;
            running = false;
        });
        Assert.That(advanced, Is.EqualTo(1));
        Assert.That(runner.Advance(60, 8, () => running, () => ticks++), Is.Zero);
        running = true;
        Assert.That(runner.Advance(0, 1, () => running, () => ticks++), Is.Zero);
        Assert.That(runner.Advance(0.1, 1, () => running, () => ticks++), Is.EqualTo(1));
        Assert.That(ticks, Is.EqualTo(2));
    }

    [Test]
    public void Pause_clears_fractional_accumulation_and_reset_clears_running_backlog()
    {
        var runner = new FixedStepFrameRunner(0.1, maxStepsPerFrame: 1);
        runner.Advance(0.05, 1, () => true, () => { });
        runner.Advance(0, 1, () => false, () => Assert.Fail("Paused step"));
        Assert.That(runner.Advance(0.05, 1, () => true, () => { }), Is.Zero);
        runner.Advance(1, 1, () => true, () => { });
        runner.Reset();
        Assert.That(runner.Advance(0, 1, () => true, () => { }), Is.Zero);
    }

    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void Invalid_elapsed_time_is_rejected(double elapsed)
    {
        var runner = new FixedStepFrameRunner();
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => runner.Advance(elapsed, 1, () => true, () => { })));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void Invalid_fixed_delta_is_rejected(double fixedDelta) =>
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new FixedStepFrameRunner(fixedDelta)));

    [Test]
    public void Invalid_callbacks_and_frame_budget_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new FixedStepFrameRunner(maxStepsPerFrame: 0)));
        var runner = new FixedStepFrameRunner();
        Assert.Throws<ArgumentNullException>((Action)(() => runner.Advance(0, 1, null!, () => { })));
        Assert.Throws<ArgumentNullException>((Action)(() => runner.Advance(0, 1, () => true, null!)));
    }

    [TestCase(0, 1)]
    [TestCase(-5, 1)]
    [TestCase(1000, 256)]
    public void Acceleration_is_clamped_to_supported_range(int supplied, int expected)
    {
        var runner = new FixedStepFrameRunner();
        var ticks = 0;
        runner.Advance(1.0 / 60, supplied, () => true, () => ticks++);
        Assert.That(ticks, Is.EqualTo(expected));
    }
}
