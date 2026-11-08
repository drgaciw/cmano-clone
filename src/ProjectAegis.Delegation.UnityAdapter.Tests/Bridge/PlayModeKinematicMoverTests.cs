namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

public sealed class PlayModeKinematicMoverTests
{
    [Test]
    public void Advance_moves_seeded_unit_along_course()
    {
        var mover = new PlayModeKinematicMover();
        mover.EnsureSeeded("u1", layoutSeed: 42, hostile: false);
        Assert.That(mover.TryGetPose("u1", out var before), Is.True);

        mover.Advance(60);

        Assert.That(mover.TryGetPose("u1", out var after), Is.True);
        var dx = after.NormalizedX!.Value - before.NormalizedX!.Value;
        var dy = after.NormalizedY!.Value - before.NormalizedY!.Value;
        Assert.That(Math.Abs(dx) + Math.Abs(dy), Is.GreaterThan(1e-4f));
        Assert.That(after.CourseDeg, Is.EqualTo(before.CourseDeg));
    }

    [Test]
    public void Advance_is_deterministic_for_same_dt()
    {
        var a = new PlayModeKinematicMover();
        var b = new PlayModeKinematicMover();
        a.EnsureSeeded("u1", 7, hostile: false);
        b.EnsureSeeded("u1", 7, hostile: false);
        a.Advance(12.5);
        b.Advance(12.5);

        Assert.That(a.TryGetPose("u1", out var pa), Is.True);
        Assert.That(b.TryGetPose("u1", out var pb), Is.True);
        Assert.That(pa.NormalizedX, Is.EqualTo(pb.NormalizedX));
        Assert.That(pa.NormalizedY, Is.EqualTo(pb.NormalizedY));
    }

    [Test]
    public void Advance_skips_destroyed_ids()
    {
        var mover = new PlayModeKinematicMover();
        mover.EnsureSeeded("u1", 42, hostile: false);
        mover.TryGetPose("u1", out var before);
        mover.Advance(30, destroyedIds: ["u1"]);
        mover.TryGetPose("u1", out var after);

        Assert.That(after.NormalizedX, Is.EqualTo(before.NormalizedX));
        Assert.That(after.NormalizedY, Is.EqualTo(before.NormalizedY));
    }

    [Test]
    public void PlotCourseAhead_publishes_two_or_more_waypoints()
    {
        var mover = new PlayModeKinematicMover();
        mover.EnsureSeeded("u1", 42, hostile: false);
        mover.PlotCourseAhead("u1");

        var course = mover.GetCourse("u1");
        Assert.That(course, Is.Not.Null);
        Assert.That(course!, Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Halt_clears_course_and_stops_motion()
    {
        var mover = new PlayModeKinematicMover();
        mover.EnsureSeeded("u1", 42, hostile: false);
        mover.PlotCourseAhead("u1");
        mover.Halt("u1");
        mover.TryGetPose("u1", out var before);
        mover.Advance(45);
        mover.TryGetPose("u1", out var after);

        Assert.That(mover.GetCourse("u1"), Is.Null);
        Assert.That(after.NormalizedX, Is.EqualTo(before.NormalizedX));
        Assert.That(after.SpeedNmPerHour, Is.EqualTo(0f));
    }
}
