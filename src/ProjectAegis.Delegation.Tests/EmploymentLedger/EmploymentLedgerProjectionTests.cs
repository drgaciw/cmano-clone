using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.EmploymentLedger;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.EmploymentLedger;

[TestFixture]
public sealed class EmploymentLedgerProjectionTests
{
    [Test]
    public void Winchester_empty_magazine_withholds_with_winchester_ordnance()
    {
        var snapshot = EmploymentLedgerProjection.Project(new EmploymentLedgerMagazineFacts(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            RoundsRemaining: 0,
            SalvoSize: 2,
            LastEmploymentTick: 42));

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));

        var row = snapshot.Rows[0];
        Assert.That(row.ShooterId, Is.EqualTo("u1"));
        Assert.That(row.WeaponFamilyId, Is.EqualTo(CatalogWeaponIds.MvpDefault));
        Assert.That(row.RoundsRemaining, Is.EqualTo(0));
        Assert.That(row.SalvoSize, Is.EqualTo(2));
        Assert.That(row.LastEmploymentTick, Is.EqualTo(42UL));
        Assert.That(row.WithholdReason, Is.EqualTo(AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE));
        Assert.That(row.IsFireOrder, Is.False);
    }

    [Test]
    public void Winchester_at_threshold_blocks_even_when_salvo_is_one()
    {
        var snapshot = EmploymentLedgerProjection.Project(new EmploymentLedgerMagazineFacts(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            RoundsRemaining: 0,
            SalvoSize: 1,
            LastEmploymentTick: 7));

        Assert.That(snapshot.Rows[0].WithholdReason, Is.EqualTo(AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE));
        Assert.That(snapshot.Rows[0].SalvoSize, Is.EqualTo(1));
        Assert.That(snapshot.IsFireOrder, Is.False);
    }

    [Test]
    public void Below_salvo_magazine_withholds_with_no_ammo()
    {
        var snapshot = EmploymentLedgerProjection.Project(new EmploymentLedgerMagazineFacts(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            RoundsRemaining: 1,
            SalvoSize: 2,
            LastEmploymentTick: 15));

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));

        var row = snapshot.Rows[0];
        Assert.That(row.RoundsRemaining, Is.EqualTo(1));
        Assert.That(row.SalvoSize, Is.EqualTo(2));
        Assert.That(row.LastEmploymentTick, Is.EqualTo(15UL));
        Assert.That(row.WithholdReason, Is.EqualTo(AbortReasonCatalog.Engage.NO_AMMO));
        Assert.That(row.IsFireOrder, Is.False);
    }

    [Test]
    public void Sufficient_rounds_emit_no_withhold_reason()
    {
        var snapshot = EmploymentLedgerProjection.Project(new EmploymentLedgerMagazineFacts(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            RoundsRemaining: 4,
            SalvoSize: 2,
            LastEmploymentTick: 99));

        Assert.That(snapshot.Rows[0].WithholdReason, Is.Null);
        Assert.That(snapshot.Rows[0].IsFireOrder, Is.False);
        Assert.That(snapshot.IsFireOrder, Is.False);
    }

    [Test]
    public void Multiple_magazines_sort_by_shooter_then_weapon_family()
    {
        var snapshot = EmploymentLedgerProjection.Project(new[]
        {
            new EmploymentLedgerMagazineFacts("u2", "sam", 8, 1, 3),
            new EmploymentLedgerMagazineFacts("u1", "asm", 2, 2, 1),
            new EmploymentLedgerMagazineFacts("u1", "sam", 0, 1, 2),
        });

        Assert.That(snapshot.Rows.Select(r => (r.ShooterId, r.WeaponFamilyId)), Is.EqualTo(new[]
        {
            ("u1", "asm"),
            ("u1", "sam"),
            ("u2", "sam"),
        }));
    }

    [Test]
    public void Fingerprint_is_identical_for_identical_inputs()
    {
        var magazines = new[]
        {
            new EmploymentLedgerMagazineFacts("u1", CatalogWeaponIds.MvpDefault, 1, 2, 5),
            new EmploymentLedgerMagazineFacts("u2", CatalogWeaponIds.MvpDefault, 0, 1, 9),
        };

        var a = EmploymentLedgerProjection.Project(magazines);
        var b = EmploymentLedgerProjection.Project(magazines);

        Assert.That(EmploymentLedgerFingerprint.Compute(a), Is.EqualTo(EmploymentLedgerFingerprint.Compute(b)));
    }

    [Test]
    public void Null_or_empty_input_returns_empty_snapshot()
    {
        Assert.That(EmploymentLedgerProjection.Project((IReadOnlyList<EmploymentLedgerMagazineFacts>?)null).Rows, Is.Empty);
        Assert.That(EmploymentLedgerProjection.Project(Array.Empty<EmploymentLedgerMagazineFacts>()).Rows, Is.Empty);
        Assert.That(EmploymentLedgerFingerprint.Compute(EmploymentLedgerSnapshot.Empty), Is.EqualTo("el:empty"));
    }

    [Test]
    public void Dtos_omit_ui_derived_truth_fields()
    {
        var types = new[]
        {
            typeof(EmploymentLedgerMagazineFacts),
            typeof(EmploymentLedgerRow),
            typeof(EmploymentLedgerSnapshot),
        };
        string[] forbidden = ["selection", "hover", "camera", "visible", "visibility", "selected"];

        foreach (var type in types)
        {
            foreach (var property in type.GetProperties())
            {
                var name = property.Name.ToLowerInvariant();
                Assert.That(
                    forbidden.Any(token => name.Contains(token, StringComparison.Ordinal)),
                    Is.False,
                    $"{type.Name}.{property.Name} is UI-derived truth");
            }
        }
    }
}
