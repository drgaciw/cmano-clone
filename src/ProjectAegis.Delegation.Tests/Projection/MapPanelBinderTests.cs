using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapPanelBinderTests
{
    [Test]
    public void Bind_selected_unit_adds_selected_style_class()
    {
        var state = MapPanelBinder.Bind(
            [new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.3f, false)],
            "baltic-patrol",
            selectedUnitId: "u1",
            selectedContactId: null);

        Assert.That(state.Symbols[0].IsSelected, Is.True);
        Assert.That(state.Symbols[0].StyleClass, Does.Contain("map-symbol--selected"));
    }

    [Test]
    public void Bind_degraded_comms_marks_hostile_stale()
    {
        var state = MapPanelBinder.Bind(
            [
                new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.3f, false),
                new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.5f, 0.5f, false),
            ],
            "test",
            selectedUnitId: null,
            selectedContactId: null,
            commsState: CommsState.Degraded);

        Assert.That(state.Symbols[1].StyleClass, Does.Contain("map-symbol--stale"));
    }

    [Test]
    public void Bind_degraded_comms_adds_ghost_row_for_hostile()
    {
        var display = new ScenarioCommsDisplaySettings(3, 0.08f, 0.05f);
        var state = MapPanelBinder.Bind(
            [new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.5f, 0.5f, false)],
            "test",
            null,
            null,
            CommsState.Degraded,
            display);

        Assert.That(state.Symbols, Has.Count.EqualTo(2));
        Assert.That(state.Symbols[0].IsGhost, Is.True);
        Assert.That(state.Symbols[0].SymbolId, Is.EqualTo("ghost:c1"));
        Assert.That(state.Symbols[0].StyleClass, Does.Contain("map-symbol--ghost"));
        Assert.That(state.Symbols[0].NormalizedX, Is.EqualTo(0.42f).Within(0.001f));
        Assert.That(state.Symbols[1].IsGhost, Is.False);
    }

    [Test]
    public void Bind_projected_app6_glyphs_flow_to_display_rows()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0)],
            layoutSeed: 42);

        var state = MapPanelBinder.Bind(symbols, "baltic-patrol-app6", App6AtlasCatalog.Unavailable);

        var friendly = state.Symbols.Single(s => s.SymbolId == "u1");
        var hostile = state.Symbols.Single(s => s.SymbolId == "c1");

        Assert.That(friendly.Glyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(hostile.Glyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
        Assert.That(friendly.Glyph, Is.Not.EqualTo(hostile.Glyph));
    }

    [Test]
    public void Bind_with_default_atlas_uses_distinct_frame_classes_for_app6_symbols()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0)],
            layoutSeed: 42);

        var state = MapPanelBinder.Bind(symbols, "baltic-patrol-app6-atlas", App6AtlasCatalog.Default);

        var friendly = state.Symbols.Single(s => s.SymbolId == "u1");
        var hostile = state.Symbols.Single(s => s.SymbolId == "c1");

        Assert.That(friendly.UsesAtlasFrame, Is.True);
        Assert.That(hostile.UsesAtlasFrame, Is.True);
        Assert.That(friendly.AtlasFrameClass, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(hostile.AtlasFrameClass, Is.EqualTo(App6Sidc.HostileContactFrame));
    }

    [Test]
    public void Bind_with_unavailable_atlas_degrades_to_unicode_glyphs()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0)],
            layoutSeed: 42);

        var state = MapPanelBinder.Bind(symbols, "baltic-patrol-app6-fallback", App6AtlasCatalog.Unavailable);

        var friendly = state.Symbols.Single(s => s.SymbolId == "u1");
        var hostile = state.Symbols.Single(s => s.SymbolId == "c1");

        Assert.That(friendly.UsesAtlasFrame, Is.False);
        Assert.That(hostile.UsesAtlasFrame, Is.False);
        Assert.That(friendly.Glyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(hostile.Glyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
    }

    [Test]
    public void Bind_symbol_with_domain_populates_domain_modifier_on_display_row()
    {
        var state = MapPanelBinder.Bind(
            [new MapSymbolEntry("f1", "Hostile", "◆", "f1", 0.5f, 0.5f, false, Domain: CombatDomain.Air)],
            "test");

        var row = state.Symbols.Single(s => s.SymbolId == "f1");

        Assert.That(row.DomainModifierClass, Is.EqualTo(App6DomainModifier.AirModifierClass));
        Assert.That(row.DomainModifierGlyph, Is.EqualTo(App6DomainModifier.Resolve(CombatDomain.Air).UnicodeIcon));
    }

    [Test]
    public void Bind_symbol_without_domain_leaves_domain_modifier_null()
    {
        var state = MapPanelBinder.Bind(
            [new MapSymbolEntry("f1", "Friendly", "■", "f1", 0.5f, 0.5f, false)],
            "test");

        var row = state.Symbols.Single(s => s.SymbolId == "f1");

        Assert.That(row.DomainModifierClass, Is.Null);
        Assert.That(row.DomainModifierGlyph, Is.Null);
    }
}