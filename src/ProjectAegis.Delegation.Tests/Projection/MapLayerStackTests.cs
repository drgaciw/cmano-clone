using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapLayerStackTests
{
    [Test]
    public void DefaultStack_includes_all_stable_ids_in_order()
    {
        var stack = MapLayerStackProjection.DefaultStack();

        Assert.That(stack.Count, Is.EqualTo(8));
        Assert.That(stack.Layers.Select(l => l.Id), Is.EqualTo(new[]
        {
            MapLayerId.Satellite,
            MapLayerId.Relief,
            MapLayerId.Borders,
            MapLayerId.Terrain,
            MapLayerId.Roads,
            MapLayerId.LandCover,
            MapLayerId.Placenames,
            MapLayerId.DayNight,
        }));
    }

    [Test]
    public void DefaultStack_day_night_off_all_others_on()
    {
        var stack = MapLayerStackState.WithDefaults();

        Assert.That(stack.VisibleCount, Is.EqualTo(7));
        Assert.That(stack.TryGet(MapLayerId.DayNight, out var dayNight), Is.True);
        Assert.That(dayNight.IsVisible, Is.False);
        Assert.That(stack.TryGet(MapLayerId.Satellite, out var sat), Is.True);
        Assert.That(sat.IsVisible, Is.True);
        Assert.That(stack.TryGet(MapLayerId.Borders, out var borders), Is.True);
        Assert.That(borders.IsVisible, Is.True);
    }

    [Test]
    public void Toggle_flips_visibility_immutably()
    {
        var before = MapLayerStackProjection.DefaultStack();
        var after = before.Toggle(MapLayerId.DayNight);

        Assert.That(before.TryGet(MapLayerId.DayNight, out var dayBefore), Is.True);
        Assert.That(dayBefore.IsVisible, Is.False);
        Assert.That(after.TryGet(MapLayerId.DayNight, out var dayAfter), Is.True);
        Assert.That(dayAfter.IsVisible, Is.True);
        Assert.That(after.VisibleCount, Is.EqualTo(8));
        Assert.That(before.VisibleCount, Is.EqualTo(7));
    }

    [Test]
    public void Toggle_unknown_id_returns_same_instance()
    {
        var stack = MapLayerStackProjection.DefaultStack();
        // Fabricate via cast of out-of-range enum value — should not match any layer.
        var unknown = (MapLayerId)999;
        var result = stack.Toggle(unknown);
        Assert.That(result, Is.SameAs(stack));
    }

    [Test]
    public void SetVisible_sets_explicit_flag()
    {
        var stack = MapLayerStackProjection.DefaultStack()
            .SetVisible(MapLayerId.Roads, false)
            .SetVisible(MapLayerId.DayNight, true);

        Assert.That(stack.TryGet(MapLayerId.Roads, out var roads), Is.True);
        Assert.That(roads.IsVisible, Is.False);
        Assert.That(stack.TryGet(MapLayerId.DayNight, out var day), Is.True);
        Assert.That(day.IsVisible, Is.True);
    }

    [Test]
    public void SetVisible_same_value_returns_same_instance()
    {
        var stack = MapLayerStackProjection.DefaultStack();
        var same = stack.SetVisible(MapLayerId.Satellite, true);
        Assert.That(same, Is.SameAs(stack));
    }

    [Test]
    public void ApplyState_formats_checklist_lines_and_summary()
    {
        var stack = MapLayerStackProjection.DefaultStack().Toggle(MapLayerId.DayNight);
        var applied = MapLayerStackApplyState.Apply(stack);

        Assert.That(applied.TotalCount, Is.EqualTo(8));
        Assert.That(applied.VisibleCount, Is.EqualTo(8));
        Assert.That(applied.SummaryLabel, Is.EqualTo("LAYERS: 8/8"));
        Assert.That(applied.DisplayLines.Count, Is.EqualTo(8));
        Assert.That(applied.Lines[0].DisplayLine, Does.StartWith("[x] Satellite"));
        Assert.That(applied.Lines[^1].Id, Is.EqualTo(nameof(MapLayerId.DayNight)));
        Assert.That(applied.Lines[^1].IsVisible, Is.True);
    }

    [Test]
    public void Apply_null_returns_empty()
    {
        var applied = MapLayerStackApplyState.Apply(null);
        Assert.That(applied.TotalCount, Is.EqualTo(0));
        Assert.That(applied.SummaryLabel, Is.EqualTo("LAYERS: 0/0"));
        Assert.That(applied.Lines, Is.Empty);
    }

    [Test]
    public void ProjectAndApplyDefaults_is_deterministic()
    {
        var a = MapLayerStackApplyState.ProjectAndApplyDefaults();
        var b = MapLayerStackApplyState.ProjectAndApplyDefaults();

        Assert.That(a.SummaryLabel, Is.EqualTo(b.SummaryLabel));
        Assert.That(a.DisplayLines, Is.EqualTo(b.DisplayLines));
        Assert.That(a.VisibleCount, Is.EqualTo(7));
        Assert.That(a.TotalCount, Is.EqualTo(8));
    }

    [Test]
    public void Store_round_trips_visibility_snapshot()
    {
        var store = new MapLayerStackStore();
        var edited = MapLayerStackProjection.DefaultStack()
            .SetVisible(MapLayerId.Relief, false)
            .SetVisible(MapLayerId.DayNight, true);

        store.Capture(edited);
        var snapshot = store.GetSnapshot();

        Assert.That(snapshot[nameof(MapLayerId.Relief)], Is.False);
        Assert.That(snapshot[nameof(MapLayerId.DayNight)], Is.True);
        Assert.That(snapshot[nameof(MapLayerId.Satellite)], Is.True);

        var restored = store.Restore(MapLayerStackProjection.DefaultStack());
        Assert.That(restored.TryGet(MapLayerId.Relief, out var relief), Is.True);
        Assert.That(relief.IsVisible, Is.False);
        Assert.That(restored.TryGet(MapLayerId.DayNight, out var day), Is.True);
        Assert.That(day.IsVisible, Is.True);
        Assert.That(restored.VisibleCount, Is.EqualTo(7));
    }

    [Test]
    public void Store_SetSnapshot_null_clears_bag()
    {
        var store = new MapLayerStackStore();
        store.SetSnapshot(new Dictionary<string, bool> { [nameof(MapLayerId.Roads)] = false });
        store.SetSnapshot(null);

        var restored = store.Restore(MapLayerStackProjection.DefaultStack());
        Assert.That(restored.TryGet(MapLayerId.Roads, out var roads), Is.True);
        Assert.That(roads.IsVisible, Is.True);
        Assert.That(store.GetSnapshot(), Is.Empty);
    }

    [Test]
    public void DefaultStack_is_deterministic_across_calls()
    {
        var a = MapLayerStackProjection.DefaultStack();
        var b = MapLayerStackProjection.DefaultStack();

        Assert.That(a.Layers.Select(l => (l.Id, l.Label, l.IsVisible, l.ShortcutHint)),
            Is.EqualTo(b.Layers.Select(l => (l.Id, l.Label, l.IsVisible, l.ShortcutHint))));
    }
}
