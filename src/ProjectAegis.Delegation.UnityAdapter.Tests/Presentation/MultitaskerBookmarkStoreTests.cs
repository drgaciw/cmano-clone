using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// T5 (req 20 §Simulation Controls / Multitasker mode): pure bookmark save/restore of camera pose +
/// selection set ids + optional agent-pause flag, with push/pop of
/// <see cref="PauseReasonIds.MultitaskerBookmark"/> around capture/restore.
/// </summary>
[TestFixture]
public sealed class MultitaskerBookmarkStoreTests
{
    private static readonly CameraPose BalticPose = new(
        LongitudeDegrees: 19.0,
        LatitudeDegrees: 56.0,
        HeightMeters: 250_000,
        HeadingDegrees: 0f,
        PitchDegrees: -45f);

    [Test]
    public void Capture_stores_camera_selection_and_agent_pause_flag()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);

        var saved = store.Capture(
            id: "slot-1",
            camera: BalticPose,
            selectionUnitIds: new[] { "u1", "u2" },
            agentPaused: true);

        Assert.That(saved.Id, Is.EqualTo("slot-1"));
        Assert.That(saved.Camera, Is.EqualTo(BalticPose));
        Assert.That(saved.SelectionUnitIds, Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(saved.AgentPaused, Is.True);
        Assert.That(store.Count, Is.EqualTo(1));
        Assert.That(store.Contains("slot-1"), Is.True);
    }

    [Test]
    public void Capture_pushes_and_pops_MultitaskerBookmark_reason()
    {
        var stack = new PauseReasonStack();
        stack.Push(PauseReasonIds.User); // pre-existing reason must survive
        var store = new MultitaskerBookmarkStore(stack);

        store.Capture("slot-1", BalticPose, new[] { "u1" });

        Assert.That(stack.Contains(PauseReasonIds.MultitaskerBookmark), Is.False,
            "multitasker reason must be popped after capture completes");
        Assert.That(stack.Contains(PauseReasonIds.User), Is.True,
            "unrelated reasons are left alone");
        Assert.That(stack.IsPaused, Is.True);
    }

    [Test]
    public void Capture_holds_MultitaskerBookmark_only_during_write()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);

        Assert.That(stack.IsPaused, Is.False);
        store.Capture("a", BalticPose, Array.Empty<string>());
        Assert.That(stack.IsPaused, Is.False, "store does not leave the sim paused after capture");
        Assert.That(stack.Contains(PauseReasonIds.MultitaskerBookmark), Is.False);
    }

    [Test]
    public void Capture_with_user_pause_held_still_pops_MultitaskerBookmark()
    {
        var stack = new PauseReasonStack();
        stack.Push(PauseReasonIds.User);
        var store = new MultitaskerBookmarkStore(stack);

        store.Capture("ok", BalticPose, new[] { "u1" });

        Assert.That(stack.Contains(PauseReasonIds.MultitaskerBookmark), Is.False);
        Assert.That(stack.Contains(PauseReasonIds.User), Is.True);
        Assert.That(stack.IsPaused, Is.True);
    }

    [Test]
    public void TryRestore_returns_snapshot_and_pops_MultitaskerBookmark()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);
        store.Capture("slot-1", BalticPose, new[] { "u1", "ucav-blue" }, agentPaused: false);

        Assert.That(store.TryRestore("slot-1", out var restored), Is.True);
        Assert.That(restored, Is.Not.Null);
        Assert.That(restored!.Camera, Is.EqualTo(BalticPose));
        Assert.That(restored.SelectionUnitIds, Is.EqualTo(new[] { "u1", "ucav-blue" }));
        Assert.That(restored.AgentPaused, Is.False);
        Assert.That(stack.Contains(PauseReasonIds.MultitaskerBookmark), Is.False);
        Assert.That(stack.IsPaused, Is.False);
    }

    [Test]
    public void TryRestore_missing_slot_is_false_and_does_not_push()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);

        Assert.That(store.TryRestore("missing", out var restored), Is.False);
        Assert.That(restored, Is.Null);
        Assert.That(stack.IsPaused, Is.False);
        Assert.That(stack.Reasons, Is.Empty);
    }

    [Test]
    public void Capture_overwrites_existing_slot()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);
        store.Capture("slot-1", BalticPose, new[] { "u1" }, agentPaused: true);

        var nextPose = BalticPose with { HeightMeters = 100_000 };
        store.Capture("slot-1", nextPose, new[] { "u2" }, agentPaused: null);

        Assert.That(store.Count, Is.EqualTo(1));
        Assert.That(store.TryGet("slot-1", out var bookmark), Is.True);
        Assert.That(bookmark!.Camera.HeightMeters, Is.EqualTo(100_000));
        Assert.That(bookmark.SelectionUnitIds, Is.EqualTo(new[] { "u2" }));
        Assert.That(bookmark.AgentPaused, Is.Null);
    }

    [Test]
    public void Capture_dedupes_selection_ids_and_drops_null_empty()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);

        var saved = store.Capture(
            "slot-1",
            BalticPose,
            new[] { "u1", "", "u1", "u2", null! });

        Assert.That(saved.SelectionUnitIds, Is.EqualTo(new[] { "u1", "u2" }));
    }

    [Test]
    public void Capture_rejects_empty_id()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);

        Assert.Throws<ArgumentException>((Action)(() => store.Capture("", BalticPose, null)));
        Assert.Throws<ArgumentException>((Action)(() => store.Capture(null!, BalticPose, null)));
        Assert.That(stack.IsPaused, Is.False, "failed capture must not leave MultitaskerBookmark pushed");
    }

    [Test]
    public void Remove_and_Clear_manage_slots()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);
        store.Capture("a", BalticPose, new[] { "u1" });
        store.Capture("b", BalticPose, new[] { "u2" });

        Assert.That(store.Remove("a"), Is.True);
        Assert.That(store.Contains("a"), Is.False);
        Assert.That(store.Count, Is.EqualTo(1));

        store.Clear();
        Assert.That(store.Count, Is.EqualTo(0));
        Assert.That(store.TryRestore("b", out _), Is.False);
    }

    [Test]
    public void TryRestore_returns_independent_selection_copy()
    {
        var stack = new PauseReasonStack();
        var store = new MultitaskerBookmarkStore(stack);
        store.Capture("slot-1", BalticPose, new[] { "u1", "u2" });

        Assert.That(store.TryRestore("slot-1", out var first), Is.True);
        Assert.That(store.TryGet("slot-1", out var stored), Is.True);

        // Restored list is a snapshot — must not share the stored list instance.
        Assert.That(first!.SelectionUnitIds, Is.Not.SameAs(stored!.SelectionUnitIds));
        Assert.That(first.SelectionUnitIds, Is.EqualTo(stored.SelectionUnitIds));
    }
}
