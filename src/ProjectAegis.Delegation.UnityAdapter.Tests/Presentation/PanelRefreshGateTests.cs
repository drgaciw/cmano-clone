using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>req 20 AC-10 (NFR virtualization): the ListView refresh gate must skip
/// itemsSource/Rebuild work when the bound snapshot is unchanged from the last-applied one —
/// this is the headless-provable mechanism behind "5k rows scroll without frame drop" (FPS itself
/// cannot be measured outside the Editor).</summary>
public sealed class PanelRefreshGateTests
{
    [Test]
    public void IsDirty_is_true_before_anything_has_been_applied()
    {
        var gate = new PanelRefreshGate<MessageLogDisplayRow>();

        Assert.That(gate.IsDirty(Rows("a")), Is.True);
    }

    [Test]
    public void IsDirty_is_false_when_candidate_is_structurally_identical_to_last_applied()
    {
        var gate = new PanelRefreshGate<MessageLogDisplayRow>();
        var first = Rows("CONTACT", "COMMS");
        gate.MarkApplied(first);

        // A brand new List<T> instance with equal record content — this is exactly what
        // MessageLogPanelBinder.Bind(...) produces every frame even when the sim tick has not
        // advanced (new list reference, same content).
        var second = Rows("CONTACT", "COMMS");

        Assert.That(gate.IsDirty(second), Is.False);
    }

    [Test]
    public void IsDirty_is_true_when_row_count_differs()
    {
        var gate = new PanelRefreshGate<MessageLogDisplayRow>();
        gate.MarkApplied(Rows("CONTACT"));

        Assert.That(gate.IsDirty(Rows("CONTACT", "COMMS")), Is.True);
    }

    [Test]
    public void IsDirty_is_true_when_any_row_content_differs()
    {
        var gate = new PanelRefreshGate<MessageLogDisplayRow>();
        gate.MarkApplied(Rows("CONTACT", "COMMS"));

        Assert.That(gate.IsDirty(Rows("CONTACT", "MAGAZINE")), Is.True);
    }

    [Test]
    public void MarkApplied_is_only_called_by_the_host_when_dirty_so_repeated_unchanged_frames_do_no_render_work()
    {
        // Simulates N unchanged "LateUpdate" frames the way a host should drive the gate: only
        // call MarkApplied (the stand-in for itemsSource=... + Rebuild()) when IsDirty is true.
        var gate = new PanelRefreshGate<OobTreeDisplayRow>();
        var snapshot = new List<OobTreeDisplayRow>
        {
            new("u1", "u1 [ALIVE]", true, false, "oob-row"),
        };

        for (var frame = 0; frame < 10; frame++)
        {
            var candidate = new List<OobTreeDisplayRow>
            {
                new("u1", "u1 [ALIVE]", true, false, "oob-row"),
            };

            if (gate.IsDirty(candidate))
            {
                gate.MarkApplied(candidate);
            }
        }

        // First frame is the only real change (nothing was applied yet); the following 9
        // structurally-identical frames must not trigger a re-apply.
        Assert.That(gate.AppliedCount, Is.EqualTo(1));
    }

    [Test]
    public void MarkApplied_then_IsDirty_with_reference_equal_candidate_short_circuits_to_false()
    {
        var gate = new PanelRefreshGate<MessageLogDisplayRow>();
        var rows = Rows("CONTACT");
        gate.MarkApplied(rows);

        Assert.That(gate.IsDirty(rows), Is.False);
    }

    private static List<MessageLogDisplayRow> Rows(params string[] categories)
    {
        var rows = new List<MessageLogDisplayRow>(categories.Length);
        foreach (var category in categories)
        {
            rows.Add(new MessageLogDisplayRow(category, $"[{category}] line"));
        }

        return rows;
    }
}
