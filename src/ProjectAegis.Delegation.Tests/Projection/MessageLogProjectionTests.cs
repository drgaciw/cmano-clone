using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MessageLogProjectionTests
{
    /// <summary>Wave 2 adversarial: PolicyDenial sequenceId identity for AAR (doc 17 AC-3).</summary>
    [Test]
    public void PolicyDenial_sequenceId_matches_order_log_entry()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            SequenceId: 0,
            SimTime: 1.0,
            SimTick: 1,
            AgentId: new AgentId("a1"),
            TargetId: new TargetId("u1"),
            PolicySnapshotId: 1,
            Reason: FireAbortReason.WeaponsTight,
            AttemptedKind: OrderKind.Engage));

        var entries = log.ChronologicalEntries();
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Kind, Is.EqualTo(OrderLogEntryKind.PolicyDenial));
        Assert.That(entries[0].SequenceId, Is.GreaterThan(0u));

        var messages = MessageLogProjection.Project(log);
        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].Category, Is.EqualTo("POLICY_DENIAL"));
        Assert.That(messages[0].SequenceId, Is.EqualTo(entries[0].SequenceId));
        Assert.That(messages[0].Text, Does.Contain("WeaponsTight"));
    }

    [Test]
    public void Kill_and_intercept_map_to_distinct_message_categories()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 2, 2, new TargetId("u1"), new TargetId("hostile-2"), 2,
            EngagementOutcomeCodes.Intercept, 0.2));

        var messages = MessageLogProjection.Project(log);

        Assert.That(messages[0].Category, Is.EqualTo("KILL_CONFIRMED"));
        Assert.That(messages[0].Text, Does.Contain("destroyed"));
        Assert.That(messages[1].Category, Is.EqualTo("INTERCEPT_SUCCESS"));
        Assert.That(messages[1].Text, Does.Contain("remains operational"));
    }
}