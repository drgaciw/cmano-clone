using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class UnitDetailApplyStateTests
{
    [Test]
    public void BindAndApply_maps_entry_lines_into_presentation()
    {
        var entry = new UnitDetailEntry(
            "blue-1",
            IsAlive: true,
            StatusLabel: "READY",
            MagazineLabel: "MAG: 4/8",
            EmconLabel: "EMCON: A",
            DoctrineLabel: "DOC: HOLD",
            FuelLabel: "FUEL: 80%",
            EngagePreviewLabel: "ENG: —",
            AttackOptionsLabel: "ATK: 0",
            AttackMenu: Array.Empty<EngageAttackOptions.AttackOption>());

        var applied = UnitDetailApplyState.BindAndApply(entry, "CONTACT: red-9");

        Assert.That(applied.UnitIdLine, Is.EqualTo("UNIT: blue-1"));
        Assert.That(applied.StatusLine, Is.EqualTo("STATUS: READY"));
        Assert.That(applied.MagazineLine, Is.EqualTo("MAG: 4/8"));
        Assert.That(applied.EmconLine, Is.EqualTo("EMCON: A"));
        Assert.That(applied.ContactLine, Is.EqualTo("CONTACT: red-9"));
        Assert.That(applied.AttackOptionCount, Is.EqualTo(0));
    }

    [Test]
    public void Apply_null_returns_empty_presentation()
    {
        var applied = UnitDetailApplyState.Apply(null);
        Assert.That(applied.UnitIdLine, Is.EqualTo("UNIT: —"));
        Assert.That(applied.AttackOptionCount, Is.EqualTo(0));
    }

    [Test]
    public void Apply_uses_shipped_binder_output()
    {
        var bound = UnitDetailPanelBinder.Bind(null);
        var applied = UnitDetailApplyState.Apply(bound);
        Assert.That(applied.UnitIdLine, Is.EqualTo(bound.UnitIdLine));
        Assert.That(applied.StatusLine, Is.EqualTo(bound.StatusLine));
    }
}
