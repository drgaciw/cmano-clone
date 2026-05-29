namespace ProjectAegis.Delegation.Tests.Targets;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Targets;
using NUnit.Framework;

[TestFixture]
public sealed class TargetTests
{
    [Test]
    public void UnitTarget_starts_with_no_active_controller()
    {
        var unit = new UnitTarget(new TargetId("u1"));
        Assert.That(unit.Slot.Active, Is.Null);
        Assert.That(unit.Slot.SuspendedAgent, Is.Null);
    }

    [Test]
    public void GroupTarget_tracks_members()
    {
        var group = new GroupTarget(new TargetId("g1"));
        var member = new TargetId("u1");
        group.AddMember(member);
        Assert.That(group.Members, Does.Contain(member));
    }
}
