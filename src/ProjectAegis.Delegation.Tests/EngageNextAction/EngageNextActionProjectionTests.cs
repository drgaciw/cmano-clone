using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.EngageNextAction;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.EngageNextAction;

[TestFixture]
public sealed class EngageNextActionProjectionTests
{
    [Test]
    public void Winchester_withhold_projects_reload_rearm_next_action()
    {
        var snapshot = EngageNextActionProjection.Project(new EngageNextActionInput(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            WithholdReason: AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE));

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));

        var row = snapshot.Rows[0];
        Assert.That(row.ShooterId, Is.EqualTo("u1"));
        Assert.That(row.WeaponFamilyId, Is.EqualTo(CatalogWeaponIds.MvpDefault));
        Assert.That(row.WithholdReason, Is.EqualTo(AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE));
        Assert.That(row.NextActionCode, Is.EqualTo(EngageNextActionCodes.ReloadRearm));
        Assert.That(row.IsFireOrder, Is.False);
    }

    [Test]
    public void No_ammo_withhold_also_projects_reload_rearm_next_action()
    {
        var snapshot = EngageNextActionProjection.Project(new EngageNextActionInput(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            WithholdReason: AbortReasonCatalog.Engage.NO_AMMO));

        Assert.That(snapshot.Rows[0].NextActionCode, Is.EqualTo(EngageNextActionCodes.ReloadRearm));
        Assert.That(snapshot.IsFireOrder, Is.False);
    }

    [Test]
    public void Roe_withhold_projects_approval_next_action()
    {
        var snapshot = EngageNextActionProjection.Project(new EngageNextActionInput(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            WithholdReason: $"policy:{FireAbortReason.RoeHoldFire}"));

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));

        var row = snapshot.Rows[0];
        Assert.That(row.WithholdReason, Is.EqualTo($"policy:{FireAbortReason.RoeHoldFire}"));
        Assert.That(row.NextActionCode, Is.EqualTo(EngageNextActionCodes.Approval));
        Assert.That(row.IsFireOrder, Is.False);
    }

    [Test]
    public void Roe_hold_fire_catalog_code_projects_approval_next_action()
    {
        var snapshot = EngageNextActionProjection.Project(new EngageNextActionInput(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            WithholdReason: AbortReasonCatalog.Engage.ROE_HOLD_FIRE));

        Assert.That(snapshot.Rows[0].NextActionCode, Is.EqualTo(EngageNextActionCodes.Approval));
        Assert.That(snapshot.IsFireOrder, Is.False);
    }

    [Test]
    public void Feasible_shot_with_no_withhold_emits_no_next_action()
    {
        var snapshot = EngageNextActionProjection.Project(new EngageNextActionInput(
            ShooterId: "u1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            WithholdReason: null));

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        Assert.That(snapshot.Rows[0].WithholdReason, Is.Null);
        Assert.That(snapshot.Rows[0].NextActionCode, Is.Null);
        Assert.That(snapshot.Rows[0].IsFireOrder, Is.False);
    }

    [Test]
    public void Every_row_and_snapshot_carry_is_fire_order_false()
    {
        var snapshot = EngageNextActionProjection.Project(new[]
        {
            new EngageNextActionInput("u1", CatalogWeaponIds.MvpDefault, AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE),
            new EngageNextActionInput("u2", CatalogWeaponIds.MvpDefault, $"policy:{FireAbortReason.RoeHoldFire}"),
            new EngageNextActionInput("u3", CatalogWeaponIds.MvpDefault, null),
        });

        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.Rows.All(r => r.IsFireOrder == false), Is.True);
    }

    [Test]
    public void Multiple_rows_sort_by_shooter_then_weapon_family()
    {
        var snapshot = EngageNextActionProjection.Project(new[]
        {
            new EngageNextActionInput("u2", "sam", AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE),
            new EngageNextActionInput("u1", "asm", AbortReasonCatalog.Engage.NO_AMMO),
            new EngageNextActionInput("u1", "sam", AbortReasonCatalog.Engage.ROE_HOLD_FIRE),
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
        var inputs = new[]
        {
            new EngageNextActionInput("u1", CatalogWeaponIds.MvpDefault, AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE),
            new EngageNextActionInput("u2", CatalogWeaponIds.MvpDefault, AbortReasonCatalog.Engage.ROE_HOLD_FIRE),
        };

        var a = EngageNextActionProjection.Project(inputs);
        var b = EngageNextActionProjection.Project(inputs);

        Assert.That(EngageNextActionFingerprint.Compute(a), Is.EqualTo(EngageNextActionFingerprint.Compute(b)));
    }

    [Test]
    public void Null_or_empty_input_returns_empty_snapshot()
    {
        Assert.That(EngageNextActionProjection.Project((IReadOnlyList<EngageNextActionInput>?)null).Rows, Is.Empty);
        Assert.That(EngageNextActionProjection.Project(Array.Empty<EngageNextActionInput>()).Rows, Is.Empty);
        Assert.That(EngageNextActionFingerprint.Compute(EngageNextActionSnapshot.Empty), Is.EqualTo("ena:empty"));
    }

    [Test]
    public void Dtos_omit_ui_derived_truth_fields()
    {
        var types = new[]
        {
            typeof(EngageNextActionInput),
            typeof(EngageNextActionRow),
            typeof(EngageNextActionSnapshot),
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
