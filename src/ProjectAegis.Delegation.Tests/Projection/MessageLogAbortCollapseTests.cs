using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// UAT D3: the same abort reason for the same shooter/victim must not append a new
/// ENGAGE_ABORT line every tick. The projection collapses repeats into one line with a count;
/// the DecisionLog itself is untouched. Codex review: the collapsed row keeps recency
/// (last-12 HUD window) and a successful launch resets the run.
/// </summary>
public sealed class MessageLogAbortCollapseTests
{
    private const int RecentWindow = 12;

    [Test]
    public void Repeated_identical_abort_collapses_to_one_line_with_repeat_count()
    {
        var log = new DecisionLog();
        for (var t = 1; t <= 5; t++)
        {
            log.AppendEngagement(Abort("u1", "hostile-1", "ROE_WEAPONS_TIGHT", t));
        }

        var messages = MessageLogProjection.Project(log);

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].Category, Is.EqualTo("ENGAGE_ABORT"));
        Assert.That(messages[0].Text, Is.EqualTo("Engagement aborted: ROE_WEAPONS_TIGHT (x5)"));
        Assert.That(log.Engagements, Has.Count.EqualTo(5), "DecisionLog must be unchanged");
    }

    [Test]
    public void Collapse_is_per_shooter_victim_and_changes_when_reason_changes()
    {
        var log = new DecisionLog();
        log.AppendEngagement(Abort("u1", "h1", "ROE_WEAPONS_TIGHT", 1));
        log.AppendEngagement(Abort("u2", "h1", "ROE_WEAPONS_TIGHT", 2));
        log.AppendEngagement(Abort("u1", "h1", "ROE_WEAPONS_TIGHT", 3));
        log.AppendEngagement(Abort("u1", "h1", "DLZ_OUT", 4));
        log.AppendEngagement(Abort("u1", "h1", "ROE_WEAPONS_TIGHT", 5));

        var texts = MessageLogProjection.Project(log).Select(m => m.Text).ToArray();

        Assert.That(texts, Is.EqualTo(new[]
        {
            "Engagement aborted: ROE_WEAPONS_TIGHT",
            "Engagement aborted: ROE_WEAPONS_TIGHT (x2)",
            "Engagement aborted: DLZ_OUT",
            "Engagement aborted: ROE_WEAPONS_TIGHT",
        }));
    }

    [Test]
    public void Collapsed_abort_stays_in_the_recent_window_after_intervening_messages()
    {
        var log = new DecisionLog();
        log.AppendEngagement(Abort("u1", "hostile-1", "ROE_WEAPONS_TIGHT", 1));
        for (var i = 0; i < RecentWindow; i++)
        {
            log.AppendPolicyDenial(Denial($"filler-{i}", simTime: 2 + i));
        }

        var afterFillers = MessageLogProjection.Project(log);
        Assert.That(
            afterFillers.TakeLast(RecentWindow).Any(m => m.Category == "ENGAGE_ABORT"),
            Is.False,
            "precondition: the original abort has scrolled out of the last-12 HUD window");

        log.AppendEngagement(Abort("u1", "hostile-1", "ROE_WEAPONS_TIGHT", 20));
        var messages = MessageLogProjection.Project(log);
        var recent = messages.TakeLast(RecentWindow).ToArray();
        var abort = recent.Single(m => m.Category == "ENGAGE_ABORT");

        Assert.That(messages.Count(m => m.Category == "ENGAGE_ABORT"), Is.EqualTo(1));
        Assert.That(abort.Text, Is.EqualTo("Engagement aborted: ROE_WEAPONS_TIGHT (x2)"));
        Assert.That(abort.SimTime, Is.EqualTo(20));
        Assert.That(recent[^1].Category, Is.EqualTo("ENGAGE_ABORT"));
    }

    [Test]
    public void Successful_launch_resets_abort_run_for_same_shooter_victim()
    {
        var log = new DecisionLog();
        log.AppendEngagement(Abort("u1", "hostile-1", "ROE_WEAPONS_TIGHT", 1));
        log.AppendEngagement(Launch("u1", "hostile-1", engagementId: 7, simTime: 2));
        log.AppendEngagement(Abort("u1", "hostile-1", "ROE_WEAPONS_TIGHT", 3));

        var messages = MessageLogProjection.Project(log);
        var aborts = messages.Where(m => m.Category == "ENGAGE_ABORT").ToArray();

        Assert.That(messages.Single(m => m.Category == "WEAPON_LAUNCH").Text, Does.Contain("launched"));
        Assert.That(aborts, Has.Length.EqualTo(2));
        Assert.That(aborts[0].Text, Is.EqualTo("Engagement aborted: ROE_WEAPONS_TIGHT"));
        Assert.That(aborts[1].Text, Is.EqualTo("Engagement aborted: ROE_WEAPONS_TIGHT"));
        Assert.That(aborts[1].SimTime, Is.EqualTo(3));
    }

    private static EngagementRecord Abort(string shooter, string victim, string reason, double simTime) =>
        new(0, simTime, (ulong)simTime, new TargetId(shooter), 0, false, reason,
            new TargetId(victim), "Gun");

    private static EngagementRecord Launch(string shooter, string victim, ulong engagementId, double simTime) =>
        new(0, simTime, (ulong)simTime, new TargetId(shooter), engagementId, true, "Launched",
            new TargetId(victim), "Gun");

    private static PolicyDenialRecord Denial(string target, double simTime) =>
        new(
            SequenceId: 0,
            SimTime: simTime,
            SimTick: (ulong)simTime,
            AgentId: new AgentId("a1"),
            TargetId: new TargetId(target),
            PolicySnapshotId: 1,
            Reason: FireAbortReason.WeaponsTight,
            AttemptedKind: OrderKind.Engage);
}
