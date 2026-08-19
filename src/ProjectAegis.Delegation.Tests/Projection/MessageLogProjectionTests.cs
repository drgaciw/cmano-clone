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

    [Test]
    public void PolicyUpdate_projects_field_transition()
    {
        var log = new DecisionLog();
        log.AppendPolicyUpdate(new PolicyUpdateRecord(
            0, 3.0, 3, 42, "roe", "WeaponsTight", "WeaponsFree"));

        var messages = MessageLogProjection.Project(log);
        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].Category, Is.EqualTo("POLICY_UPDATE"));
        Assert.That(messages[0].Text, Does.Contain("roe"));
        Assert.That(messages[0].Text, Does.Contain("WeaponsTight"));
        Assert.That(messages[0].Text, Does.Contain("WeaponsFree"));
        Assert.That(messages[0].SequenceId, Is.EqualTo(log.ChronologicalEntries()[0].SequenceId));
    }

    [Test]
    public void AgentDecision_mission_event_and_damage_project_player_facing_lines()
    {
        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            1.0,
            new AgentId("a1"),
            new TargetId("u1"),
            AutonomyLevel.Assisted,
            OrderKind.Hold,
            Array.Empty<ScoredIntent>(),
            "patrol station-keeping",
            1,
            20,
            0.1,
            SimTick: 1));
        log.AppendMissionTransition(new MissionTransitionRecord(0, 1.5, 2, "patrol-1", "START"));
        log.AppendEventFired(new EventFiredRecord(0, 2.0, 3, "recon-detect", "DETECTED"));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0, 2.5, 4, new TargetId("hostile-1"), 100, 85, "Hit", 1));
        log.AppendControllerChange(new ControllerChangeRecord(
            0, 3.0, new TargetId("u1"), "Human", "Agent", new AgentId("a1")));

        var messages = MessageLogProjection.Project(log);
        Assert.That(messages.Select(m => m.Category).ToArray(), Is.EqualTo(new[]
        {
            "AGENT_DECISION",
            "MISSION",
            "EVENT",
            "DAMAGE",
            "CONTROLLER",
        }));
        Assert.That(messages[0].Text, Does.Contain("Hold").And.Contain("patrol station-keeping"));
        Assert.That(messages[0].UnitId, Is.EqualTo("u1"));
        Assert.That(messages[1].Text, Does.Contain("patrol-1").And.Contain("START"));
        Assert.That(messages[2].Text, Does.Contain("recon-detect"));
        Assert.That(messages[3].Text, Does.Contain("85%").And.Contain("Hit"));
        Assert.That(messages[4].Text, Does.Contain("Human").And.Contain("Agent"));
    }

    [Test]
    public void Group_member_rows_remain_unprojected()
    {
        var log = new DecisionLog();
        log.AppendGroupMemberDetach(new GroupMemberDetachRecord(
            0, 1.0, new TargetId("g1"), new TargetId("u1")));

        Assert.That(MessageLogProjection.Project(log), Is.Empty);
    }
}