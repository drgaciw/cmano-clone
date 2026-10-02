using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class WeaponAbortTooltipBinderTests
{
    [Test]
    public void Null_explain_and_detail_yield_empty_tooltip()
    {
        var bound = WeaponAbortTooltipBinder.Bind(null);

        Assert.That(bound, Is.EqualTo(WeaponAbortTooltipState.Empty));
        Assert.That(bound.IsDenied, Is.False);
        Assert.That(bound.Reasons, Is.Empty);
        Assert.That(bound.TooltipText, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Clear_engage_explain_suppresses_historical_detail_reasons()
    {
        var explain = new EngageExplain(
            EngageExplainProjection.CanFireLabel,
            null,
            "Launch is permitted under current DLZ, track, mount, and EMCON constraints.",
            IsBlocked: false);
        var detail = DetailWithPolicy($"policy:{nameof(FireAbortReason.RoeHoldFire)}");

        var bound = WeaponAbortTooltipBinder.Bind(explain, detail);

        Assert.That(bound.IsDenied, Is.False);
        Assert.That(bound.Reasons, Is.Empty);
    }

    [Test]
    public void Blocked_explain_reason_code_is_the_tooltip()
    {
        var explain = Blocked(nameof(FireAbortReason.RoeHoldFire));

        var bound = WeaponAbortTooltipBinder.Bind(explain);

        Assert.That(bound.IsDenied, Is.True);
        Assert.That(bound.Reasons, Is.EqualTo(new[] { nameof(FireAbortReason.RoeHoldFire) }));
        Assert.That(bound.TooltipText, Is.EqualTo(nameof(FireAbortReason.RoeHoldFire)));
    }

    [Test]
    public void Catalog_log_code_maps_onto_FireAbortReason_name()
    {
        var explain = Blocked(AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT);

        var bound = WeaponAbortTooltipBinder.Bind(explain);

        Assert.That(bound.Reasons, Is.EqualTo(new[] { nameof(FireAbortReason.WeaponsTight) }));
    }

    [Test]
    public void Top_three_follow_FireAbortReason_order_not_field_order()
    {
        var detail = new CombatDetailPresentation(
            $"AuthorizationRefused / {nameof(FireAbortReason.CommsDenied)}",
            $"Weapon family: mvp | {nameof(FireAbortReason.EmconOff)}",
            $"Hard constraints: {nameof(FireAbortReason.WraSalvo)}",
            $"Policy: policy:{nameof(FireAbortReason.RoeHoldFire)}",
            "Contact confidence: UNKNOWN",
            "Firing solution: UNKNOWN",
            $"Next: {nameof(FireAbortReason.WeaponsTight)}",
            "BDA: UNKNOWN",
            "Posture: UNKNOWN",
            "Correlation: —");

        var bound = WeaponAbortTooltipBinder.Bind(null, detail);

        Assert.That(bound.Reasons, Is.EqualTo(new[]
        {
            nameof(FireAbortReason.RoeHoldFire),
            nameof(FireAbortReason.WeaponsTight),
            nameof(FireAbortReason.WraSalvo),
        }));
        Assert.That(bound.TooltipText, Is.EqualTo(string.Join("\n", bound.Reasons)));
        Assert.That(bound.Reasons, Has.No.Member(nameof(FireAbortReason.EmconOff)));
        Assert.That(bound.Reasons, Has.No.Member(nameof(FireAbortReason.CommsDenied)));
    }

    [Test]
    public void Duplicate_enum_name_and_log_code_count_once()
    {
        var explain = new EngageExplain(
            $"ENGAGE: BLOCKED — {AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE}",
            nameof(FireAbortReason.RoeHoldFire),
            $"Engagement blocked ({nameof(FireAbortReason.RoeHoldFire)}).",
            IsBlocked: true);

        var bound = WeaponAbortTooltipBinder.Bind(explain);

        Assert.That(bound.Reasons, Is.EqualTo(new[] { nameof(FireAbortReason.RoeHoldFire) }));
    }

    [Test]
    public void Equivalent_abort_strings_fill_remaining_tooltip_slots()
    {
        var explain = Blocked(AbortReasonCatalog.Engage.DLZ_OUT);
        var detail = DetailWithPolicy(
            $"{AbortReasonCatalog.Engage.MOUNT_OFFLINE} {AbortReasonCatalog.Engage.NO_AMMO} {nameof(FireAbortReason.RoeHoldFire)}");

        var bound = WeaponAbortTooltipBinder.Bind(explain, detail);

        Assert.That(bound.Reasons, Is.EqualTo(new[]
        {
            nameof(FireAbortReason.RoeHoldFire),
            AbortReasonCatalog.Engage.MOUNT_OFFLINE,
            AbortReasonCatalog.Engage.DLZ_OUT,
        }));
        Assert.That(bound.Reasons, Has.No.Member(AbortReasonCatalog.Engage.NO_AMMO));
    }

    [Test]
    public void Dlz_zone_label_is_not_an_abort_reason()
    {
        var detail = DetailWithStatus("ENGAGE: DLZ: InZone (Normal) | BLOCKED | —");

        var bound = WeaponAbortTooltipBinder.Bind(EngageExplain.Empty, detail);

        Assert.That(bound.IsDenied, Is.False);
        Assert.That(bound.TooltipText, Does.Not.Contain("DLZ"));
    }

    [Test]
    public void Bind_rows_target_denied_weapon_rows_only()
    {
        var bound = WeaponAbortTooltipBinder.Bind(Blocked(nameof(FireAbortReason.EmconOff)));
        var rows = WeaponAbortTooltipBinder.BindRows(bound);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].ElementName, Is.EqualTo(WeaponAbortTooltipBinder.FireSingleRowName));
        Assert.That(rows[0].OptionId, Is.EqualTo(WeaponAbortTooltipBinder.FireSingleOptionId));
        Assert.That(rows[1].ElementName, Is.EqualTo(WeaponAbortTooltipBinder.FireSalvoRowName));
        Assert.That(rows[1].OptionId, Is.EqualTo(WeaponAbortTooltipBinder.FireSalvoOptionId));
        Assert.That(rows[0].TooltipText, Is.EqualTo(nameof(FireAbortReason.EmconOff)));
        Assert.That(rows[0].IsDenied, Is.True);
        Assert.That(WeaponAbortTooltipBinder.AppliesTo(WeaponAbortTooltipBinder.FireSingleOptionId), Is.True);
        Assert.That(WeaponAbortTooltipBinder.AppliesTo("hold-fire"), Is.False);
    }

    private static EngageExplain Blocked(string code) =>
        new(
            $"ENGAGE: BLOCKED — {code}",
            code,
            $"Engagement blocked ({code}).",
            IsBlocked: true);

    private static CombatDetailPresentation DetailWithPolicy(string policyLine) =>
        CombatDetailPresentation.Empty with { PolicyLine = policyLine };

    private static CombatDetailPresentation DetailWithStatus(string statusLine) =>
        CombatDetailPresentation.Empty with { StatusLine = statusLine };
}
