using NUnit.Framework;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AgentRosterPresentationCacheTests
{
    [Test]
    public void Unchanged_roster_reuses_presentation_and_allocates_nothing_after_warmup()
    {
        var cache = new AgentRosterPresentationCache();
        var entries = new[] { Entry("u1"), Entry("u2") };
        var initial = cache.Apply(entries);
        for (var i = 0; i < 100; i++)
        {
            cache.Apply(entries);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        AgentRosterPresentation? repeated = null;
        for (var i = 0; i < 1000; i++)
        {
            repeated = cache.Apply(entries);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(repeated, Is.SameAs(initial));
        Assert.That(allocated, Is.Zero);
        Assert.That(cache.Apply(new[] { Entry("u1"), Entry("u2") }), Is.SameAs(initial));
    }

    [TestCase("Human", ControllerBadgeKind.Human)]
    [TestCase("AgentSuspended", ControllerBadgeKind.AgentSuspended)]
    [TestCase("None", ControllerBadgeKind.None)]
    public void Controller_changes_in_reused_input_list_rebuild_badges(string mode, ControllerBadgeKind kind)
    {
        var cache = new AgentRosterPresentationCache();
        var entries = new[] { Entry("u1") };
        var initial = cache.Apply(entries);
        entries[0] = entries[0] with { ModeLabel = mode };
        var changed = cache.Apply(entries);

        Assert.That(changed, Is.Not.SameAs(initial));
        Assert.That(changed.Rows[0].Badge!.Kind, Is.EqualTo(kind));
        entries[0] = Entry("u1");
        Assert.That(cache.Apply(entries).Rows[0].Badge!.Kind, Is.EqualTo(ControllerBadgeKind.Agent));
        Assert.That(initial.Rows[0].ModeLabel, Is.EqualTo("Agent"));
    }

    [Test]
    public void Attention_identity_autonomy_and_status_changes_invalidate_cached_rows()
    {
        var cache = new AgentRosterPresentationCache();
        var entries = new[] { Entry("u1") };
        var previous = cache.Apply(entries);
        var updates = new[]
        {
            entries[0] with { AttentionLabel = "ATTENTION: overloaded" },
            entries[0] with { AgentId = "other" },
            entries[0] with { AutonomyLabel = "Manual" },
            entries[0] with { StatusLabel = "Suspended" },
            entries[0] with { UnitId = "u2" },
        };
        foreach (var update in updates)
        {
            entries[0] = update;
            var changed = cache.Apply(entries);
            Assert.That(changed, Is.Not.SameAs(previous));
            var expected = AgentRosterApplyState.Apply(entries);
            Assert.That(changed.Rows, Is.EqualTo(expected.Rows));
            Assert.That(changed.Lines, Is.EqualTo(expected.Lines));
            previous = changed;
        }
    }

    [Test]
    public void Reorder_remove_clear_and_repopulate_refresh_the_presentation()
    {
        var cache = new AgentRosterPresentationCache();
        cache.Apply(new[] { Entry("u1"), Entry("u2") });
        var reversed = cache.Apply(new[] { Entry("u2"), Entry("u1") });
        Assert.That(reversed.Rows.Select(row => row.UnitId), Is.EqualTo(new[] { "u2", "u1" }));
        Assert.That(cache.Apply(new[] { Entry("u1") }).Count, Is.EqualTo(1));
        Assert.That(cache.Apply(null), Is.SameAs(AgentRosterPresentation.Empty));
        Assert.That(cache.Apply(Array.Empty<AgentRosterEntry>()), Is.SameAs(AgentRosterPresentation.Empty));
        Assert.That(cache.Apply(new[] { Entry("u1") }).Count, Is.EqualTo(1));
    }

    private static AgentRosterEntry Entry(string unitId) =>
        new("agent-a", unitId, "FullAutonomous", "Active", "ATTENTION: —", "Agent");
}
