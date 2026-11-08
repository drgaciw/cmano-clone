using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class PendingApprovalProjectionTests
{
    [Test]
    public void Project_empty_returns_empty_list()
    {
        var rows = PendingApprovalProjection.Project(null);
        Assert.That(rows, Is.Empty);

        var rows2 = PendingApprovalProjection.Project(Array.Empty<PendingApprovalEntry>());
        Assert.That(rows2, Is.Empty);
    }

    [Test]
    public void Project_single_engage_high_risk_formats_row()
    {
        var order = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        var entries = new[] { new PendingApprovalEntry(order) };

        var rows = PendingApprovalProjection.Project(entries);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].OrderId, Is.EqualTo(new OrderId(1)));
        Assert.That(rows[0].TargetId, Is.EqualTo(new TargetId("u1")));
        Assert.That(rows[0].IsHighRisk, Is.True);
        Assert.That(rows[0].RiskLabel, Is.EqualTo("RISK: HIGH"));
        Assert.That(rows[0].SummaryLine, Does.Contain("ENGAGE"));
        Assert.That(rows[0].SummaryLine, Does.Contain("u1"));
    }

    [Test]
    public void Project_hold_low_risk_formats_correctly()
    {
        var order = new Order(new OrderId(2), new TargetId("u2"), 0, OrderKind.Hold, RiskLevel.Low);
        var entries = new[] { new PendingApprovalEntry(order) };

        var rows = PendingApprovalProjection.Project(entries);

        Assert.That(rows[0].IsHighRisk, Is.False);
        Assert.That(rows[0].RiskLabel, Is.EqualTo("RISK: LOW"));
        Assert.That(rows[0].SummaryLine, Does.Contain("HOLD"));
    }

    [Test]
    public void FormatBadge_empty_returns_pending_dash()
    {
        Assert.That(PendingApprovalProjection.FormatBadge(null), Is.EqualTo("PENDING: —"));
        Assert.That(
            PendingApprovalProjection.FormatBadge(Array.Empty<PendingApprovalEntry>()),
            Is.EqualTo("PENDING: —"));
    }

    [Test]
    public void FormatBadge_shows_count_and_high_risk_count()
    {
        var low = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Hold, RiskLevel.Low);
        var high1 = new Order(new OrderId(2), new TargetId("u2"), 0, OrderKind.Engage, RiskLevel.High);
        var high2 = new Order(new OrderId(3), new TargetId("u3"), 0, OrderKind.Engage, RiskLevel.High);
        var entries = new[]
        {
            new PendingApprovalEntry(low),
            new PendingApprovalEntry(high1),
            new PendingApprovalEntry(high2),
        };

        var badge = PendingApprovalProjection.FormatBadge(entries);

        Assert.That(badge, Is.EqualTo("PENDING: 3 (2 HIGH)"));
    }

    [Test]
    public void FormatBadge_all_low_risk_omits_high_count()
    {
        var low = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Hold, RiskLevel.Low);
        var entries = new[] { new PendingApprovalEntry(low) };

        var badge = PendingApprovalProjection.FormatBadge(entries);

        Assert.That(badge, Is.EqualTo("PENDING: 1"));
    }

    [Test]
    public void Project_preserves_order_count_for_multiple_entries()
    {
        var entries = new[]
        {
            new PendingApprovalEntry(new Order(new OrderId(10), new TargetId("ua"), 0, OrderKind.Move, RiskLevel.Low)),
            new PendingApprovalEntry(new Order(new OrderId(11), new TargetId("ub"), 0, OrderKind.Engage, RiskLevel.High)),
        };

        var rows = PendingApprovalProjection.Project(entries);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].OrderId, Is.EqualTo(new OrderId(10)));
        Assert.That(rows[1].OrderId, Is.EqualTo(new OrderId(11)));
    }
}
