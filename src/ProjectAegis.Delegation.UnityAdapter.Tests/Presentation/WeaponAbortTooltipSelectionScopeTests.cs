using NUnit.Framework;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// DRG-352: the weapon-abort tooltip reads combat detail scoped to the selected unit's own
/// engagement leg, never the global combat-history inspection key.
/// </summary>
[TestFixture]
public sealed class WeaponAbortTooltipSelectionScopeTests
{
    private const string Selected = "u1";
    private const string Other = "u2";

    [Test]
    public void Inspected_history_key_for_another_unit_does_not_leak_into_selected_tooltip()
    {
        var frame = Frame(
            Refused(Selected, "hostile-1", nameof(FireAbortReason.NoFireControlTrack), correlation: 1, tick: 2),
            Refused(Other, "hostile-2", nameof(FireAbortReason.RoeHoldFire), correlation: 2, tick: 5));
        var otherKey = CombatMapPresenter.KeyFor(frame.Events.Events[1]);
        var explain = Blocked(nameof(FireAbortReason.NoFireControlTrack));

        var leaked = WeaponAbortTooltipBinder.Bind(
            explain,
            CombatSelectionPresenter.Build(frame, otherKey, Selected, null));
        var scoped = WeaponAbortTooltipBinder.BindForSelection(explain, frame, Selected, null);

        Assert.That(leaked.Reasons, Does.Contain(nameof(FireAbortReason.RoeHoldFire)),
            "precondition: the global inspection key surfaces the other unit's history");
        Assert.That(scoped.IsDenied, Is.True);
        Assert.That(scoped.Reasons, Does.Not.Contain(nameof(FireAbortReason.RoeHoldFire)));
        Assert.That(scoped.Reasons, Is.EqualTo(new[] { nameof(FireAbortReason.NoFireControlTrack) }));
    }

    [Test]
    public void Other_unit_history_cannot_displace_selected_denial_from_top_three()
    {
        var frame = Frame(
            Refused(Other, "hostile-2", nameof(FireAbortReason.RoeHoldFire), correlation: 2, tick: 3),
            Refused(Other, "hostile-2", nameof(FireAbortReason.WeaponsTight), correlation: 3, tick: 4),
            Refused(Other, "hostile-2", nameof(FireAbortReason.EmconOff), correlation: 4, tick: 5));
        var explain = Blocked(nameof(FireAbortReason.NoFireControlTrack));

        var scoped = WeaponAbortTooltipBinder.BindForSelection(explain, frame, Selected, null);

        Assert.That(scoped.Reasons, Is.EqualTo(new[] { nameof(FireAbortReason.NoFireControlTrack) }));
    }

    [Test]
    public void Selected_unit_own_history_still_contributes()
    {
        var frame = Frame(
            Refused(Selected, "hostile-1", nameof(FireAbortReason.RoeHoldFire), correlation: 1, tick: 2));

        var scoped = WeaponAbortTooltipBinder.BindForSelection(null, frame, Selected, null);
        var detail = WeaponAbortTooltipBinder.ScopeCombatDetail(frame, Selected, null);

        Assert.That(detail, Is.Not.EqualTo(CombatDetailPresentation.Empty));
        Assert.That(scoped.Reasons, Does.Contain(nameof(FireAbortReason.RoeHoldFire)));
    }

    [Test]
    public void Selected_contact_narrows_detail_to_current_engagement_target()
    {
        var contacts = SliceAContactFrame.Empty with
        {
            Contacts = new[] { new ContactPictureEntry("c-2", "hostile-2", Selected, "Identified", 1, 1) },
        };
        var frame = new CombatPresentationFrame(
            new CombatEventSnapshot(new[]
            {
                Refused(Selected, "hostile-2", nameof(FireAbortReason.NoFireControlTrack), correlation: 1, tick: 1),
                Refused(Selected, "hostile-1", nameof(FireAbortReason.RoeHoldFire), correlation: 2, tick: 6),
            }),
            contacts,
            BdaAssessSnapshot.Empty,
            6);

        var scoped = WeaponAbortTooltipBinder.BindForSelection(null, frame, Selected, "c-2");

        Assert.That(scoped.Reasons, Does.Contain(nameof(FireAbortReason.NoFireControlTrack)));
        Assert.That(scoped.Reasons, Does.Not.Contain(nameof(FireAbortReason.RoeHoldFire)));
    }

    [Test]
    public void No_selected_unit_or_frame_yields_explain_only()
    {
        var frame = Frame(
            Refused(Other, "hostile-2", nameof(FireAbortReason.RoeHoldFire), correlation: 2, tick: 5));
        var explain = Blocked(nameof(FireAbortReason.NoFireControlTrack));

        Assert.That(WeaponAbortTooltipBinder.ScopeCombatDetail(frame, null, null), Is.EqualTo(CombatDetailPresentation.Empty));
        Assert.That(WeaponAbortTooltipBinder.ScopeCombatDetail(null, Selected, null), Is.EqualTo(CombatDetailPresentation.Empty));
        Assert.That(
            WeaponAbortTooltipBinder.BindForSelection(explain, frame, null, null).Reasons,
            Is.EqualTo(new[] { nameof(FireAbortReason.NoFireControlTrack) }));
    }

    [Test]
    public void Clear_explain_still_suppresses_selected_history()
    {
        var frame = Frame(
            Refused(Selected, "hostile-1", nameof(FireAbortReason.RoeHoldFire), correlation: 1, tick: 2));
        var explain = new EngageExplain(
            EngageExplainProjection.CanFireLabel,
            null,
            "Launch is permitted under current DLZ, track, mount, and EMCON constraints.",
            IsBlocked: false);

        Assert.That(
            WeaponAbortTooltipBinder.BindForSelection(explain, frame, Selected, null),
            Is.EqualTo(WeaponAbortTooltipState.Empty));
    }

    private static CombatPresentationFrame Frame(params CombatEvent[] events) =>
        new(new CombatEventSnapshot(events), SliceAContactFrame.Empty, BdaAssessSnapshot.Empty, 6);

    private static CombatEvent Refused(string shooter, string target, string reason, ulong correlation, ulong tick) =>
        new(CombatEventPhase.AuthorizationRefused, shooter, target, "Missile", reason, correlation, tick, tick, $"policy:{reason}");

    private static EngageExplain Blocked(string code) =>
        new($"BLOCKED: {code}", code, code, IsBlocked: true);
}
