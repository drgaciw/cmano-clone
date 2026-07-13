using System.Linq;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Track T3 (req 20 §Alerting and Interruption, TR-c2-007): pure ToastStack queue/eviction
/// model — max 3 visible, 4th+ collapses to overflow, click resolves a focus target, replay suppresses.</summary>
[TestFixture]
public sealed class ToastStackModelTests
{
    private static ToastEntry Entry(ulong id, string? focusUnitId = null) =>
        new(id, AlertSeverity.Critical, "KILL_CONFIRMED", $"toast {id}", focusUnitId);

    [Test]
    public void Up_to_three_toasts_are_all_visible_with_no_overflow()
    {
        var model = new ToastStackModel();
        model.Add(Entry(1));
        model.Add(Entry(2));
        model.Add(Entry(3));

        Assert.That(model.VisibleToasts, Has.Count.EqualTo(3));
        Assert.That(model.OverflowCount, Is.EqualTo(0));
        Assert.That(model.TotalCount, Is.EqualTo(3));
    }

    [Test]
    public void Fourth_toast_collapses_into_overflow_count()
    {
        var model = new ToastStackModel();
        model.Add(Entry(1));
        model.Add(Entry(2));
        model.Add(Entry(3));
        model.Add(Entry(4));

        Assert.That(model.VisibleToasts, Has.Count.EqualTo(ToastStackModel.MaxVisible));
        Assert.That(model.VisibleToasts.Select(e => e.SequenceId), Is.EqualTo(new ulong[] { 1, 2, 3 }));
        Assert.That(model.OverflowCount, Is.EqualTo(1));
        Assert.That(model.TotalCount, Is.EqualTo(4));
    }

    [Test]
    public void Fifth_and_sixth_toasts_keep_growing_overflow_count()
    {
        var model = new ToastStackModel();
        for (ulong i = 1; i <= 6; i++)
        {
            model.Add(Entry(i));
        }

        Assert.That(model.OverflowCount, Is.EqualTo(3));
        Assert.That(model.TotalCount, Is.EqualTo(6));
    }

    [Test]
    public void Evict_removes_a_toast_and_frees_overflow_slot()
    {
        var model = new ToastStackModel();
        model.Add(Entry(1));
        model.Add(Entry(2));
        model.Add(Entry(3));
        model.Add(Entry(4));

        var evicted = model.Evict(1);

        Assert.That(evicted, Is.True);
        Assert.That(model.OverflowCount, Is.EqualTo(0));
        Assert.That(model.VisibleToasts.Select(e => e.SequenceId), Is.EqualTo(new ulong[] { 2, 3, 4 }));
    }

    [Test]
    public void Evict_of_unknown_sequence_id_returns_false()
    {
        var model = new ToastStackModel();
        model.Add(Entry(1));

        Assert.That(model.Evict(999), Is.False);
    }

    [Test]
    public void Evicting_an_overflow_toast_directly_reduces_overflow_without_changing_visible()
    {
        var model = new ToastStackModel();
        model.Add(Entry(1));
        model.Add(Entry(2));
        model.Add(Entry(3));
        model.Add(Entry(4));

        var evicted = model.Evict(4);

        Assert.That(evicted, Is.True);
        Assert.That(model.OverflowCount, Is.EqualTo(0));
        Assert.That(model.VisibleToasts.Select(e => e.SequenceId), Is.EqualTo(new ulong[] { 1, 2, 3 }));
        Assert.That(model.TotalCount, Is.EqualTo(3));
    }

    [Test]
    public void Adding_a_duplicate_sequence_id_is_rejected_and_does_not_grow_the_queue()
    {
        // Sequence ids are unique message-log line identifiers (MessageLogLine.SequenceId); the model
        // should never present the same alert twice in the stack, whether the duplicate comes from a
        // caller bug (re-adding the same batch) or an upstream data issue.
        var model = new ToastStackModel();
        model.Add(Entry(1));

        model.Add(Entry(1));

        Assert.That(model.TotalCount, Is.EqualTo(1), "duplicate sequence id must not be queued twice");
    }

    [Test]
    public void Click_resolves_to_focus_unit_id_when_present()
    {
        var model = new ToastStackModel();
        model.Add(Entry(1, focusUnitId: "unit-alpha"));

        Assert.That(model.ResolveFocusTarget(1), Is.EqualTo("unit-alpha"));
    }

    [Test]
    public void Click_falls_back_to_sequence_id_when_no_unit_id()
    {
        var model = new ToastStackModel();
        model.Add(Entry(1, focusUnitId: null));

        Assert.That(model.ResolveFocusTarget(1), Is.EqualTo("1"));
    }

    [Test]
    public void Click_on_unknown_toast_resolves_to_null()
    {
        var model = new ToastStackModel();

        Assert.That(model.ResolveFocusTarget(42), Is.Null);
    }

    [Test]
    public void Replay_suppression_means_add_is_a_no_op()
    {
        var model = new ToastStackModel(isReplaySuppressed: true);
        model.Add(Entry(1));
        model.Add(Entry(2));

        Assert.That(model.TotalCount, Is.EqualTo(0));
        Assert.That(model.VisibleToasts, Is.Empty);
        Assert.That(model.OverflowCount, Is.EqualTo(0));
    }

    [Test]
    public void Clear_empties_the_queue()
    {
        var model = new ToastStackModel();
        model.Add(Entry(1));
        model.Add(Entry(2));

        model.Clear();

        Assert.That(model.TotalCount, Is.EqualTo(0));
    }
}
