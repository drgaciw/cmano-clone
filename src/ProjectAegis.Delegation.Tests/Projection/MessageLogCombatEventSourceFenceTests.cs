using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// DRG-402: combat message categories are sourced from <see cref="CombatEventLogProjection"/>,
/// not from a second reading of the engagement collections.
/// </summary>
public sealed class MessageLogCombatEventSourceFenceTests
{
    [Test]
    public void Source_builds_combat_lines_through_the_combat_event_log()
    {
        var source = File.ReadAllText(ProjectionPath());

        Assert.That(source, Does.Contain("CombatEventLogProjection.Build"));
        Assert.That(source, Does.Not.Contain(".Engagements"));
        Assert.That(source, Does.Not.Contain(".EngagementOutcomes"));
    }

    [Test]
    public void Stable_combat_categories_follow_the_combat_event_snapshot()
    {
        var log = new DecisionLog();
        log.AppendEngagement(Launch("u1", "hostile-1", engagementId: 7, simTime: 1));
        log.AppendEngagementOutcome(Outcome("u1", "hostile-1", 7, EngagementOutcomeCodes.Hit, simTime: 2));
        log.AppendEngagement(Launch("u1", "hostile-2", engagementId: 8, simTime: 3));
        log.AppendEngagementOutcome(Outcome("u1", "hostile-2", 8, EngagementOutcomeCodes.Miss, simTime: 4));
        log.AppendEngagement(new EngagementRecord(
            0, 5, 5, new TargetId("u1"), 0, false, "DLZ_OUT", new TargetId("hostile-3"), "Gun"));

        var messages = MessageLogProjection.Project(log);

        Assert.That(messages.Select(m => m.Category), Is.EqualTo(new[]
        {
            "WEAPON_LAUNCH",
            "HIT",
            "WEAPON_LAUNCH",
            "MISS",
            "ENGAGE_ABORT",
        }));
        Assert.That(messages[0].Text, Is.EqualTo("Unit u1 launched engagement 7"));
        Assert.That(messages[1].Text, Is.EqualTo("Weapon hit hostile-1 (engagement 7)"));
        Assert.That(messages[3].Text, Is.EqualTo("Weapon missed hostile-2 (engagement 8)"));
        Assert.That(messages[4].Text, Is.EqualTo("Engagement aborted: DLZ_OUT"));
        Assert.That(messages[4].UnitId, Is.EqualTo("u1"));
    }

    [Test]
    public void Outcome_without_a_combat_event_does_not_invent_a_kill_line()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));

        var messages = MessageLogProjection.Project(log);

        Assert.That(messages.Any(m => m.Category == "KILL_CONFIRMED"), Is.False);
        Assert.That(messages, Is.Empty);
    }

    [Test]
    public void Policy_denial_stays_a_policy_line_and_is_not_also_an_engage_abort()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 1, 1, new AgentId("a1"), new TargetId("u1"), 1,
            FireAbortReason.WeaponsTight, OrderKind.Engage));

        var messages = MessageLogProjection.Project(log);

        Assert.That(messages.Select(m => m.Category), Is.EqualTo(new[] { "POLICY_DENIAL" }));
    }

    [Test]
    public void Single_abort_entry_still_projects_for_the_commit_bark()
    {
        var entry = OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(
                4, 2, 2, new TargetId("u1"), 0, false, "DLZ_OUT", new TargetId("hostile-1"), "Gun"),
            sequenceId: 4);

        var messages = MessageLogProjection.Project(new[] { entry });

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].Category, Is.EqualTo("ENGAGE_ABORT"));
        Assert.That(messages[0].Text, Does.Contain("DLZ_OUT"));
    }

    private static EngagementRecord Launch(
        string shooter,
        string victim,
        ulong engagementId,
        double simTime) =>
        new(0, simTime, (ulong)simTime, new TargetId(shooter), engagementId, true, "Launched",
            new TargetId(victim), "Missile");

    private static EngagementOutcomeRecord Outcome(
        string shooter,
        string victim,
        ulong engagementId,
        string code,
        double simTime) =>
        new(0, simTime, (ulong)simTime, new TargetId(shooter), new TargetId(victim), engagementId, code, 0.1);

    private static string ProjectionPath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "repo root not found");
        return Path.Combine(
            dir!.FullName,
            "src",
            "ProjectAegis.Delegation",
            "Projection",
            "MessageLogProjection.cs");
    }
}
