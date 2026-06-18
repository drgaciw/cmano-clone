using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class DoctrineInheritancePanelBinderTests
{
    [Test]
    public void Bind_maps_entry_roe_salvo_and_source_lines()
    {
        var entry = new DoctrineInheritanceEntry(
            "u1",
            "ROE: WeaponsTight",
            "SALVO: 2",
            "SOURCE: Mission",
            IsInheritedFromMission: true,
            HasLocalOverride: false,
            "OVERRIDE: NONE");

        var panel = DoctrineInheritancePanelBinder.Bind(entry);

        Assert.That(panel.UnitIdLine, Is.EqualTo("UNIT: u1"));
        Assert.That(panel.RoeLine, Is.EqualTo("ROE: WeaponsTight"));
        Assert.That(panel.SalvoLine, Is.EqualTo("SALVO: 2"));
        Assert.That(panel.SourceLine, Is.EqualTo("SOURCE: Mission"));
        Assert.That(panel.OverrideLine, Is.EqualTo("OVERRIDE: NONE"));
    }

    [Test]
    public void Bind_mission_inherited_without_override_disables_override_controls()
    {
        var entry = new DoctrineInheritanceEntry(
            "u1",
            "ROE: WeaponsTight",
            "SALVO: 2",
            "SOURCE: Mission",
            IsInheritedFromMission: true,
            HasLocalOverride: false,
            "OVERRIDE: NONE");

        var panel = DoctrineInheritancePanelBinder.Bind(entry);

        Assert.That(panel.CanOverride, Is.False);
        Assert.That(panel.RoeOptions.Any(o => o.IsCurrent && o.Label == "WeaponsTight"), Is.True);
    }

    [Test]
    public void Bind_null_entry_returns_unavailable_state()
    {
        var panel = DoctrineInheritancePanelBinder.Bind(null);

        Assert.That(panel.UnitIdLine, Is.EqualTo("UNIT: —"));
        Assert.That(panel.RoeLine, Is.EqualTo("ROE: —"));
        Assert.That(panel.SalvoLine, Is.EqualTo("SALVO: —"));
        Assert.That(panel.SourceLine, Is.EqualTo("SOURCE: —"));
        Assert.That(panel.OverrideLine, Is.EqualTo("OVERRIDE: UNAVAILABLE"));
        Assert.That(panel.CanOverride, Is.False);
        Assert.That(panel.RoeOptions, Is.Empty);
    }

    [Test]
    public void Bind_scenario_default_unit_enables_override_controls()
    {
        var entry = new DoctrineInheritanceEntry(
            "u2",
            "ROE: WeaponsFree",
            "SALVO: 1",
            "SOURCE: Scenario Default",
            IsInheritedFromMission: false,
            HasLocalOverride: false,
            "OVERRIDE: NONE");

        var panel = DoctrineInheritancePanelBinder.Bind(entry);

        Assert.That(panel.CanOverride, Is.True);
        Assert.That(panel.RoeOptions, Has.Count.EqualTo(3));
    }
}