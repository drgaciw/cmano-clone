using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class DoctrineInheritancePanelBinderTests
{
    [Test]
    public void Bind_maps_entry_roe_salvo_emcon_and_source_lines()
    {
        var entry = new DoctrineInheritanceEntry(
            "u1", "ROE: WeaponsTight", "SALVO: 2", "EMCON: ACTIVE", "SOURCE: Mission", true, false, "OVERRIDE: NONE");
        var panel = DoctrineInheritancePanelBinder.Bind(entry);
        Assert.That(panel.UnitIdLine, Is.EqualTo("UNIT: u1"));
        Assert.That(panel.RoeLine, Is.EqualTo("ROE: WeaponsTight"));
        Assert.That(panel.SalvoLine, Is.EqualTo("SALVO: 2"));
        Assert.That(panel.EmconLine, Is.EqualTo("EMCON: ACTIVE"));
        Assert.That(panel.WraLine, Is.EqualTo("WRA: max salvo 2"));
        Assert.That(panel.PositiveControlLine, Is.EqualTo("POSITIVE_CONTROL: REQUIRED"));
        Assert.That(panel.SourceLine, Is.EqualTo("SOURCE: Mission"));
        Assert.That(panel.OverrideLine, Is.EqualTo("OVERRIDE: NONE"));
    }

    [Test]
    public void Bind_mission_inherited_without_override_disables_override_controls()
    {
        var entry = new DoctrineInheritanceEntry(
            "u1", "ROE: WeaponsTight", "SALVO: 2", "EMCON: OFF", "SOURCE: Mission", true, false, "OVERRIDE: NONE");
        var panel = DoctrineInheritancePanelBinder.Bind(entry);
        Assert.That(panel.CanOverride, Is.False);
        Assert.That(panel.RoeOptions.Any(o => o.IsCurrent && o.Label == "WeaponsTight"), Is.True);
        Assert.That(panel.EmconLine, Is.EqualTo("EMCON: OFF"));
        Assert.That(panel.WraLine, Is.EqualTo("WRA: max salvo 2"));
    }

    [Test]
    public void Bind_null_entry_returns_unavailable_state()
    {
        var panel = DoctrineInheritancePanelBinder.Bind(null);
        Assert.That(panel.UnitIdLine, Is.EqualTo("UNIT: —"));
        Assert.That(panel.RoeLine, Is.EqualTo("ROE: —"));
        Assert.That(panel.SalvoLine, Is.EqualTo("SALVO: —"));
        Assert.That(panel.EmconLine, Is.EqualTo("EMCON: —"));
        Assert.That(panel.WraLine, Is.EqualTo("WRA: —"));
        Assert.That(panel.PositiveControlLine, Is.EqualTo("POSITIVE_CONTROL: —"));
        Assert.That(panel.SourceLine, Is.EqualTo("SOURCE: —"));
        Assert.That(panel.OverrideLine, Is.EqualTo("OVERRIDE: UNAVAILABLE"));
        Assert.That(panel.CanOverride, Is.False);
        Assert.That(panel.RoeOptions, Is.Empty);
    }

    [Test]
    public void Bind_scenario_default_unit_enables_override_controls()
    {
        var entry = new DoctrineInheritanceEntry(
            "u2", "ROE: WeaponsFree", "SALVO: 1", "EMCON: PASSIVE", "SOURCE: Scenario Default", false, false, "OVERRIDE: NONE");
        var panel = DoctrineInheritancePanelBinder.Bind(entry);
        Assert.That(panel.CanOverride, Is.True);
        Assert.That(panel.RoeOptions, Has.Count.EqualTo(3));
        Assert.That(panel.WraLine, Is.EqualTo("WRA: max salvo 1"));
        Assert.That(panel.PositiveControlLine, Is.EqualTo("POSITIVE_CONTROL: NOT_REQUIRED"));
    }

    [Test]
    public void FormatWraSummary_from_salvo_label()
    {
        Assert.That(DoctrineInheritancePanelBinder.FormatWraSummary("SALVO: 4"), Is.EqualTo("WRA: max salvo 4"));
        Assert.That(DoctrineInheritancePanelBinder.FormatWraSummary("SALVO: —"), Is.EqualTo("WRA: —"));
    }
}
