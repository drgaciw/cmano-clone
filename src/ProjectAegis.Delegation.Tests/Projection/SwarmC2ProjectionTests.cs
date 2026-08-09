using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class SwarmC2ProjectionTests
{
    [Test]
    public void Selection_selects_single_swarm_unit_id()
    {
        var readout = new SwarmIntegrityReadout("uas-swarm-1", 24, 40);
        var panel = SwarmUnitPanelProjection.Project(readout);
        var selected = SwarmUnitPanelProjection.SelectSwarmUnit(readout.UnitId);

        Assert.That(selected, Is.EqualTo("uas-swarm-1"));
        Assert.That(panel.SelectedUnitId, Is.EqualTo("uas-swarm-1"));
        Assert.That(panel.IsSwarm, Is.True);

        // Map + OOB single-node selection sync (same as any unit — one id).
        var oob = new[] { new OobTreeEntry("uas-swarm-1", true), new OobTreeEntry("u1", true) };
        var symbols = new[]
        {
            SwarmMapSymbolProjection.Project(readout, normalizedX: 0.2f, normalizedY: 0.3f),
            new MapSymbolEntry("u1", "Friendly", App6Sidc.FriendlySurfaceUnitGlyph, "u1", 0.4f, 0.4f, false),
        };
        var mapState = MapPanelBinder.Bind(symbols, "baltic-patrol", selected, null);
        var oobState = OobTreePanelBinder.Bind(oob, selected);

        Assert.That(mapState.Symbols.Count(s => s.IsSelected), Is.EqualTo(1));
        Assert.That(mapState.Symbols.Single(s => s.IsSelected).SymbolId, Is.EqualTo("uas-swarm-1"));
        Assert.That(oobState.UnitRows.Count(r => r.IsSelected), Is.EqualTo(1));
        Assert.That(oobState.UnitRows.Single(r => r.IsSelected).UnitId, Is.EqualTo("uas-swarm-1"));
    }

    [Test]
    public void Panel_shows_textual_integrity_not_color_only()
    {
        var readout = new SwarmIntegrityReadout("swarm-a", 24, 40);
        var panel = SwarmUnitPanelProjection.Project(readout);

        Assert.That(panel.IntegrityLine, Does.Contain("24/40"));
        Assert.That(panel.IntegrityLine, Does.Contain("INTEGRITY:"));
        Assert.That(readout.CountLabel, Is.EqualTo("24/40"));
        // Color is not the sole channel: CountLabel / IntegrityLine are required text.
        Assert.That(string.IsNullOrWhiteSpace(panel.IntegrityLine), Is.False);
    }

    [Test]
    public void Map_symbol_is_distinct_from_single_light_aircraft_surface_unit()
    {
        var readout = new SwarmIntegrityReadout("swarm-a", 40, 40);
        var swarm = SwarmMapSymbolProjection.Project(readout);
        var lightAircraft = new MapSymbolEntry(
            "ucav-1",
            "Friendly",
            App6Sidc.FriendlySurfaceUnitGlyph,
            "ucav-1",
            0.1f,
            0.1f,
            false,
            App6Sidc.FriendlySurfaceUnitSidc,
            App6Sidc.FriendlySurfaceUnitFrame);

        Assert.That(swarm.IsSwarm, Is.True);
        Assert.That(swarm.IntegrityLabel, Is.EqualTo("40/40"));
        Assert.That(swarm.ShapeGlyph, Is.EqualTo(SwarmMapSymbolProjection.FriendlySwarmGlyph));
        Assert.That(swarm.ShapeGlyph, Is.Not.EqualTo(lightAircraft.ShapeGlyph));
        Assert.That(swarm.App6UssFrameId, Is.EqualTo(SwarmMapSymbolProjection.FriendlySwarmFrame));
        Assert.That(swarm.App6UssFrameId, Is.Not.EqualTo(lightAircraft.App6UssFrameId));
        Assert.That(swarm.Label, Does.Contain("[40/40]"));
    }

    [Test]
    public void Destroyed_swarm_panel_marks_destroyed_in_text()
    {
        var readout = new SwarmIntegrityReadout("swarm-dead", 0, 40, IsDestroyed: true);
        var panel = SwarmUnitPanelProjection.Project(readout);
        var symbol = SwarmMapSymbolProjection.Project(readout);

        Assert.That(panel.IsDestroyed, Is.True);
        Assert.That(panel.IntegrityLine, Does.Contain("DESTROYED"));
        Assert.That(symbol.IsDestroyed, Is.True);
    }
}
