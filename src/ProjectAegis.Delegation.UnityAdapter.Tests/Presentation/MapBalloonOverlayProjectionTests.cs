using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MapBalloonOverlayProjectionTests
{
    [Test]
    public void Anchors_balloon_to_matching_MapSymbolEntry_coordinates()
    {
        var alerts = new[]
        {
            new MapBalloonAlert("msglog:10", 10, 5.0, "DAMAGE", "u1", "Damage u1: 100% -> 80% (Hit)", true),
        };
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "●", "u1", 0.35f, 0.65f, false),
        };

        var state = MapBalloonOverlayProjection.Project(alerts, symbols, isInspectingHistory: false);

        Assert.That(state.IsInspectingHistory, Is.False);
        Assert.That(state.AnchoredBalloons, Has.Count.EqualTo(1));
        Assert.That(state.UnanchoredBalloons, Is.Empty);
        var row = state.AnchoredBalloons[0];
        Assert.That(row.BalloonId, Is.EqualTo("msglog:10"));
        Assert.That(row.SequenceId, Is.EqualTo(10UL));
        Assert.That(row.Category, Is.EqualTo("DAMAGE"));
        Assert.That(row.SubjectId, Is.EqualTo("u1"));
        Assert.That(row.Text, Does.Contain("Damage u1"));
        Assert.That(row.HasMapAnchor, Is.True);
        Assert.That(row.NormalizedX, Is.EqualTo(0.35f).Within(0.0001f));
        Assert.That(row.NormalizedY, Is.EqualTo(0.65f).Within(0.0001f));
        Assert.That(row.IsPauseTriggered, Is.True);
        Assert.That(row.CssClass, Is.EqualTo(MapBalloonOverlayProjection.PauseBalloonCssClass));
    }

    [Test]
    public void Places_theater_events_without_symbol_in_UnanchoredBalloons()
    {
        var alerts = new[]
        {
            new MapBalloonAlert("msglog:12", 12, 8.0, "COMMS", "node-missing", "Comms node-missing: Nominal -> Degraded", false),
        };
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "●", "u1", 0.25f, 0.50f, false),
        };

        var state = MapBalloonOverlayProjection.Project(alerts, symbols, isInspectingHistory: false);

        Assert.That(state.AnchoredBalloons, Is.Empty);
        Assert.That(state.UnanchoredBalloons, Has.Count.EqualTo(1));
        Assert.That(state.UnanchoredBalloons[0].HasMapAnchor, Is.False);
        Assert.That(state.UnanchoredBalloons[0].CssClass, Is.EqualTo(MapBalloonOverlayProjection.AlertBalloonCssClass));
    }

    [Test]
    public void Fails_closed_to_empty_when_isInspectingHistory_is_true()
    {
        var alerts = new[]
        {
            new MapBalloonAlert("msglog:10", 10, 5.0, "DAMAGE", "u1", "Damage u1", true),
            new MapBalloonAlert("msglog:11", 11, 6.0, "COMMS", null, "Comms degraded", false),
        };
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "●", "u1", 0.35f, 0.65f, false),
        };

        var state = MapBalloonOverlayProjection.Project(alerts, symbols, isInspectingHistory: true);

        Assert.That(state.IsInspectingHistory, Is.True);
        Assert.That(state.AnchoredBalloons, Is.Empty);
        Assert.That(state.UnanchoredBalloons, Is.Empty);
    }

    [Test]
    public void Anchors_to_destroyed_wreck_symbol_and_falls_back_to_unanchored_when_symbol_is_removed()
    {
        var alerts = new[]
        {
            new MapBalloonAlert("msglog:20", 20, 9.0, "KILL_CONFIRMED", "hostile-wreck", "Kill confirmed hostile-wreck", false),
            new MapBalloonAlert("msglog:21", 21, 9.5, "HIT", "removed-sym", "Hit removed-sym", false),
        };
        var symbols = new[]
        {
            new MapSymbolEntry("hostile-wreck", "Hostile", "✕", "hostile-wreck", 0.42f, 0.58f, IsDestroyed: true),
        };

        var state = MapBalloonOverlayProjection.Project(alerts, symbols, isInspectingHistory: false);

        Assert.That(state.AnchoredBalloons, Has.Count.EqualTo(1));
        Assert.That(state.AnchoredBalloons[0].BalloonId, Is.EqualTo("msglog:20"));
        Assert.That(state.AnchoredBalloons[0].NormalizedX, Is.EqualTo(0.42f).Within(0.0001f));
        Assert.That(state.AnchoredBalloons[0].NormalizedY, Is.EqualTo(0.58f).Within(0.0001f));

        Assert.That(state.UnanchoredBalloons, Has.Count.EqualTo(1));
        Assert.That(state.UnanchoredBalloons[0].BalloonId, Is.EqualTo("msglog:21"));
        Assert.That(state.UnanchoredBalloons[0].HasMapAnchor, Is.False);
    }
}
