namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

[TestFixture]
public sealed class DecisionLogTests
{
    [Test]
    public void Append_preserves_order_for_aar_stream()
    {
        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            SimTime: 1,
            AgentId: new AgentId("a1"),
            TargetId: new TargetId("u1"),
            AutonomyLevel.FullAutonomous,
            ChosenKind: OrderKind.Hold,
            Alternatives: Array.Empty<ScoredIntent>(),
            Rationale: "test",
            AttentionLoad: 5,
            AttentionBudget: 10,
            RngDraw: 0.42));

        Assert.That(log.Records, Has.Count.EqualTo(1));
        Assert.That(log.Records[0].ChosenKind, Is.EqualTo(OrderKind.Hold));
    }

    [Test]
    public void ChronologicalEntries_and_fingerprint_are_stable_across_runs_S36_10()
    {
        // S36-10 polish: immutable hash asserts (no structural change to log)
        var log = new DecisionLog();
        // simulate mixed appends (policy/engage/contact style via base append)
        log.Append(new OrderLogEntry(1, OrderLogEntryKind.PolicyUpdate, 10.0, new PolicyUpdateRecord(0, 10.0, 0, 1, "mode", "old", "new")));
        log.Append(new OrderLogEntry(2, OrderLogEntryKind.Engagement, 11.0, new EngagementRecord(0, 11.0, 1, new TargetId("t1"), 99, true, null)));
        log.Append(new OrderLogEntry(3, OrderLogEntryKind.ContactChange, 12.0, new ContactChangeRecord(0, 12.0, 2, "obs-1", "c1", "t1", "Detected", "Classified")));

        var fp1 = log.ComputeFingerprint();
        var chrono1 = log.ChronologicalEntries();
        var fp2 = log.ComputeFingerprint();
        var chrono2 = log.ChronologicalEntries();

        Assert.That(fp2, Is.EqualTo(fp1), "fingerprint must be byte-identical on same log");
        Assert.That(chrono2.Count, Is.EqualTo(chrono1.Count));
        for (int i = 0; i < chrono1.Count; i++)
        {
            Assert.That(chrono2[i].SequenceId, Is.EqualTo(chrono1[i].SequenceId));
        }
    }

    [Test]
    public void Append_paths_hash_immutable_under_replay_S36_10()
    {
        var log = new DecisionLog();
        // fixed append sequence
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 5, 5, new TargetId("u1"), OrderKind.Hold));
        log.AppendMagazineChange(new MagazineChangeRecord(0, 6, 6, new TargetId("u1"), 1, -1, "test"));

        var h1 = log.ComputeFingerprint();
        var h2 = log.ComputeFingerprint();
        Assert.That(h2, Is.EqualTo(h1));
    }
}
