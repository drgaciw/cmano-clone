using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class OobTreeProjectionTests
{
    [Test]
    public void Project_orders_members_by_target_id_and_marks_dead()
    {
        var members = new[]
        {
            new TargetId("u2"),
            new TargetId("u1"),
            new TargetId("u3"),
        };

        var rows = OobTreeProjection.Project(members, id => id.Value != "u2");

        Assert.That(rows.Select(r => r.UnitId), Is.EqualTo(new[] { "u1", "u2", "u3" }));
        Assert.That(rows[0].IsAlive, Is.True);
        Assert.That(rows[1].IsAlive, Is.False);
        Assert.That(rows[2].IsAlive, Is.True);
    }
}