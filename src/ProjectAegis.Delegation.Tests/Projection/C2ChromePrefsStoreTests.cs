using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>CMD-23 pure prefs bag for chrome collapse (UI-local, in-memory).</summary>
public sealed class C2ChromePrefsStoreTests
{
    [Test]
    public void Defaults_are_expanded_when_bag_empty()
    {
        var store = new C2ChromePrefsStore();

        Assert.That(store.MessageLogCollapsed, Is.False);
        Assert.That(store.LeftDrawerCollapsed, Is.False);
        Assert.That(store.GetSnapshot(), Is.Empty);

        var restored = store.Restore();
        Assert.That(restored, Is.EqualTo(C2ChromeCollapseState.Expanded));
    }

    [Test]
    public void Get_Set_round_trips_individual_flags()
    {
        var store = new C2ChromePrefsStore
        {
            MessageLogCollapsed = true,
            LeftDrawerCollapsed = false,
        };

        Assert.That(store.MessageLogCollapsed, Is.True);
        Assert.That(store.LeftDrawerCollapsed, Is.False);

        store.LeftDrawerCollapsed = true;
        Assert.That(store.LeftDrawerCollapsed, Is.True);
    }

    [Test]
    public void Capture_and_Restore_round_trips_state()
    {
        var store = new C2ChromePrefsStore();
        var edited = C2ChromeCollapseState.Expanded
            .SetMessageLogCollapsed(true)
            .SetLeftDrawerCollapsed(true);

        store.Capture(edited);
        var snapshot = store.GetSnapshot();

        Assert.That(snapshot[C2ChromePrefsStore.MessageLogKey], Is.True);
        Assert.That(snapshot[C2ChromePrefsStore.LeftDrawerKey], Is.True);

        var restored = store.Restore(C2ChromeCollapseState.Expanded);
        Assert.That(restored.MessageLogCollapsed, Is.True);
        Assert.That(restored.LeftDrawerCollapsed, Is.True);
        Assert.That(restored, Is.EqualTo(edited));
    }

    [Test]
    public void SetSnapshot_null_clears_bag()
    {
        var store = new C2ChromePrefsStore();
        store.SetSnapshot(new Dictionary<string, bool>
        {
            [C2ChromePrefsStore.MessageLogKey] = true,
            [C2ChromePrefsStore.LeftDrawerKey] = true,
        });

        store.SetSnapshot(null);

        Assert.That(store.GetSnapshot(), Is.Empty);
        Assert.That(store.Restore(), Is.EqualTo(C2ChromeCollapseState.Expanded));
    }

    [Test]
    public void SetSnapshot_ignores_unknown_keys()
    {
        var store = new C2ChromePrefsStore();
        store.SetSnapshot(new Dictionary<string, bool>
        {
            ["UnknownKey"] = true,
            [C2ChromePrefsStore.MessageLogKey] = true,
        });

        var snapshot = store.GetSnapshot();
        Assert.That(snapshot.ContainsKey("UnknownKey"), Is.False);
        Assert.That(snapshot[C2ChromePrefsStore.MessageLogKey], Is.True);
        Assert.That(snapshot.ContainsKey(C2ChromePrefsStore.LeftDrawerKey), Is.False);
    }

    [Test]
    public void Capture_null_throws()
    {
        var store = new C2ChromePrefsStore();
        try
        {
            store.Capture(null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            Assert.Pass();
        }
    }

    [Test]
    public void Toggle_sequence_persists_via_store()
    {
        var store = new C2ChromePrefsStore();
        var state = store.Restore();

        state = state.ToggleMessageLog();
        store.Capture(state);
        Assert.That(store.Restore().MessageLogCollapsed, Is.True);
        Assert.That(store.Restore().LeftDrawerCollapsed, Is.False);

        state = store.Restore().ToggleLeftDrawer();
        store.Capture(state);
        Assert.That(store.Restore().MessageLogCollapsed, Is.True);
        Assert.That(store.Restore().LeftDrawerCollapsed, Is.True);

        state = store.Restore().ToggleMessageLog().ToggleLeftDrawer();
        store.Capture(state);
        Assert.That(store.Restore(), Is.EqualTo(C2ChromeCollapseState.Expanded));
    }

    [Test]
    public void Snapshot_dictionary_round_trips_via_new_store()
    {
        var source = new C2ChromePrefsStore();
        source.Capture(new C2ChromeCollapseState(
            MessageLogCollapsed: true,
            LeftDrawerCollapsed: false));

        var target = new C2ChromePrefsStore();
        target.SetSnapshot(source.GetSnapshot());

        var restored = target.Restore();
        Assert.That(restored.MessageLogCollapsed, Is.True);
        Assert.That(restored.LeftDrawerCollapsed, Is.False);

        var applied = C2ChromeCollapseApplyState.Apply(restored);
        Assert.That(applied.MessageLogAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.ExpandMessageLogLabel));
        Assert.That(applied.LeftDrawerAffordanceLabel,
            Is.EqualTo(C2ChromeCollapseProjection.CollapseLeftDrawerLabel));
    }
}
