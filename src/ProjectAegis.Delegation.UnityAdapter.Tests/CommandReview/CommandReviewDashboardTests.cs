using System.Diagnostics;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

public sealed class CommandReviewDashboardTests
{
    [Test]
    public void Large_status_bind_is_bounded_discloses_omissions_and_meets_rich_panel_budget()
    {
        var unknown = StatusFact.Unknown("unmodeled");
        var units = Enumerable.Range(0, 500).Select(i => new StatusUnitRow($"u{i}",
            new StatusFact(StatusKnowledge.Degraded, "damage recorded"), unknown, unknown, unknown,
            unknown, unknown, unknown)).ToArray();
        var status = new StatusFrame(20, [], units, [], [], null, []);
        for (var i = 0; i < 3; i++) CommandReviewDashboard.Build(status, null, CoordinationSnapshot.Empty);
        var samples = new List<double>();
        for (var i = 0; i < 20; i++)
        {
            var timer = Stopwatch.StartNew();
            var sections = CommandReviewDashboard.Build(status, null, CoordinationSnapshot.Empty);
            timer.Stop();
            samples.Add(timer.Elapsed.TotalMilliseconds);
            var damage = sections.Single(x => x.Id == "damage");
            Assert.That(damage.Lines, Has.Count.EqualTo(201));
            Assert.That(damage.Lines.Last(), Does.Contain("omitted"));
            Assert.That(damage.Lines[0], Does.Contain("SENSORS UNKNOWN"));
        }
        TestContext.Progress.WriteLine($"Slice C dashboard n=20 max={samples.Max():0.###}ms");
        Assert.That(samples.Max(), Is.LessThan(100), "Req 20 rich panel bind budget");
    }
}
