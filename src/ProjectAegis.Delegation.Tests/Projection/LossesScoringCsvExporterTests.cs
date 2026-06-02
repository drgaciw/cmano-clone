using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class LossesScoringCsvExporterTests
{
    [Test]
    public void FormatRow_includes_header_columns_and_fingerprint()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 0, 1, new AgentId("a1"), new TargetId("u1"), 0, FireAbortReason.RoeHoldFire, OrderKind.Engage));

        var row = LossesScoringCsvExporter.FormatRow("baltic-patrol", 42, "BLUE", log);

        Assert.That(row, Does.StartWith("baltic-patrol,42,BLUE,"));
        Assert.That(row, Does.Contain("PolicyDenial"));
        Assert.That(row, Does.Not.Contain('\n'));
    }

    [Test]
    public void FormatBatch_prefixes_header_line()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("t1"), 1, EngagementOutcomeCodes.Kill, 0.1));

        var csv = LossesScoringCsvExporter.FormatBatch(
            [LossesScoringCsvExporter.FormatRow("s1", 1, "BLUE", log)]);

        Assert.That(csv, Does.StartWith(LossesScoringCsvExporter.Header));
        var lines = csv.TrimEnd().Split('\n');
        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(lines[1], Does.StartWith("s1,1,BLUE,"));
    }
}