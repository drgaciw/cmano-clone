using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// UAT D3: the same abort reason for the same shooter/victim must not append a new
/// ENGAGE_ABORT line every tick. The projection collapses repeats into one line with a count;
/// the DecisionLog itself is untouched.
/// </summary>
public sealed class MessageLogAbortCollapseTests
{
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
            "Engagement aborted: ROE_WEAPONS_TIGHT (x2)",
            "Engagement aborted: ROE_WEAPONS_TIGHT",
            "Engagement aborted: DLZ_OUT",
            "Engagement aborted: ROE_WEAPONS_TIGHT",
        }));
    }

    private static EngagementRecord Abort(string shooter, string victim, string reason, double simTime) =>
        new(0, simTime, (ulong)simTime, new TargetId(shooter), 0, false, reason,
            new TargetId(victim), "Gun");
}
