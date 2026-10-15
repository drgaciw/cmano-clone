using NUnit.Framework;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// DRG-407: the right-unit panel memoizes combat detail and the engage explanation
/// keyed by the <see cref="WeaponAbortTooltipBinder.BindForSelection"/> inputs.
/// </summary>
[TestFixture]
public sealed class RightUnitCombatDetailCacheTests
{
    private const string Selected = "u1";

    [Test]
    public void Same_bind_for_selection_inputs_reuse_explain_detail_and_tooltip()
    {
        var frame = Frame(
            Refused(Selected, "hostile-1", nameof(FireAbortReason.NoFireControlTrack), correlation: 1, tick: 2));
        var preview = BlockedPreview(nameof(FireAbortReason.NoFireControlTrack));
        var cache = new RightUnitCombatDetailCache();

        var first = Bind(cache, preview, frame, Selected, null);
        var second = Bind(cache, Copy(preview), frame, Selected, null);

        Assert.That(cache.MissCount, Is.EqualTo(1));
        Assert.That(second, Is.SameAs(first));
        Assert.That(second.Explain, Is.SameAs(first.Explain));
        Assert.That(second.Detail, Is.SameAs(first.Detail));
        Assert.That(second.Tooltip, Is.SameAs(first.Tooltip));
        var direct = WeaponAbortTooltipBinder.BindForSelection(first.Explain, frame, Selected, null);
        Assert.That(second.Tooltip.TooltipText, Is.EqualTo(direct.TooltipText));
        Assert.That(second.Tooltip.IsDenied, Is.EqualTo(direct.IsDenied));
        Assert.That(second.Tooltip.Reasons, Is.EqualTo(direct.Reasons));
        Assert.That(second.Detail, Is.EqualTo(
            WeaponAbortTooltipBinder.ScopeCombatDetail(frame, Selected, null)));
    }

    [Test]
    public void New_combat_frame_rebinds_even_when_the_preview_is_unchanged()
    {
        var preview = BlockedPreview(nameof(FireAbortReason.RoeHoldFire));
        var cache = new RightUnitCombatDetailCache();
        var firstFrame = Frame(
            Refused(Selected, "hostile-1", nameof(FireAbortReason.RoeHoldFire), correlation: 1, tick: 2));
        var rebuilt = Frame(
            Refused(Selected, "hostile-1", nameof(FireAbortReason.WeaponsTight), correlation: 4, tick: 6));

        var first = Bind(cache, preview, firstFrame, Selected, null);
        var second = Bind(cache, preview, rebuilt, Selected, null);

        Assert.That(cache.MissCount, Is.EqualTo(2));
        Assert.That(second.Detail, Is.Not.SameAs(first.Detail));
        Assert.That(second.Explain, Is.Not.SameAs(first.Explain));
        var direct = WeaponAbortTooltipBinder.BindForSelection(second.Explain, rebuilt, Selected, null);
        Assert.That(second.Tooltip.Reasons, Is.EqualTo(direct.Reasons));
        Assert.That(second.Tooltip.Reasons, Does.Contain(nameof(FireAbortReason.WeaponsTight)));
        Assert.That(first.Tooltip.Reasons, Does.Not.Contain(nameof(FireAbortReason.WeaponsTight)));
    }

    [Test]
    public void Selected_unit_or_contact_change_rebinds()
    {
        var contacts = SliceAContactFrame.Empty with
        {
            Contacts = new[]
            {
                new ContactPictureEntry("c-1", "hostile-1", Selected, "Identified", 1, 1),
                new ContactPictureEntry("c-2", "hostile-2", Selected, "Identified", 1, 1),
            },
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
        var preview = BlockedPreview(nameof(FireAbortReason.NoFireControlTrack));
        var cache = new RightUnitCombatDetailCache();

        var selected = Bind(cache, preview, frame, Selected, "c-2");
        var otherUnit = Bind(cache, preview, frame, "u2", "c-2");
        var otherContact = Bind(cache, preview, frame, Selected, "c-1");

        Assert.That(cache.MissCount, Is.EqualTo(3));
        Assert.That(selected.Tooltip.Reasons, Does.Contain(nameof(FireAbortReason.NoFireControlTrack)));
        Assert.That(selected.Tooltip.Reasons, Does.Not.Contain(nameof(FireAbortReason.RoeHoldFire)));
        Assert.That(otherUnit.Detail, Is.EqualTo(CombatDetailPresentation.Empty));
        Assert.That(otherContact.Tooltip.Reasons, Does.Contain(nameof(FireAbortReason.RoeHoldFire)));
    }

    [Test]
    public void Changed_engage_preview_rebinds_explanation()
    {
        var frame = Frame(
            Refused(Selected, "hostile-1", nameof(FireAbortReason.RoeHoldFire), correlation: 1, tick: 2));
        var cache = new RightUnitCombatDetailCache();

        var blocked = Bind(cache, BlockedPreview(nameof(FireAbortReason.NoFireControlTrack)), frame, Selected, null);
        var clear = Bind(cache, new EngagePreview("DLZ: IN", true, null), frame, Selected, null);

        Assert.That(cache.MissCount, Is.EqualTo(2));
        Assert.That(blocked.Explain.IsBlocked, Is.True);
        Assert.That(blocked.Tooltip.Reasons, Does.Contain(nameof(FireAbortReason.NoFireControlTrack)));
        Assert.That(clear.Explain.StatusLine, Is.EqualTo(EngageExplainProjection.CanFireLabel));
        Assert.That(clear.Tooltip, Is.EqualTo(WeaponAbortTooltipState.Empty));
        Assert.That(clear.Detail, Is.Not.EqualTo(CombatDetailPresentation.Empty));
    }

    [Test]
    public void Empty_inputs_match_bind_for_selection_and_then_hit()
    {
        var cache = new RightUnitCombatDetailCache();

        var first = Bind(cache, null, null, null, null);
        var second = Bind(cache, null, null, null, null);

        Assert.That(cache.MissCount, Is.EqualTo(1));
        Assert.That(second, Is.SameAs(first));
        Assert.That(first.Explain, Is.EqualTo(EngageExplain.Empty));
        Assert.That(first.Detail, Is.EqualTo(CombatDetailPresentation.Empty));
        Assert.That(first.Tooltip, Is.EqualTo(
            WeaponAbortTooltipBinder.BindForSelection(EngageExplain.Empty, null, null, null)));
    }

    [Test]
    public void Right_unit_panel_checks_the_cache_before_bind_for_selection()
    {
        var source = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        var refresh = source.IndexOf("private void Refresh()", StringComparison.Ordinal);
        Assert.That(refresh, Is.GreaterThanOrEqualTo(0));
        var body = source.Substring(refresh);
        var cacheCheck = body.IndexOf("_combatDetailCache.TryGet", StringComparison.Ordinal);
        var bind = body.IndexOf("WeaponAbortTooltipBinder.BindForSelection", StringComparison.Ordinal);

        Assert.That(source, Does.Contain("RightUnitCombatDetailCache"));
        Assert.That(cacheCheck, Is.GreaterThanOrEqualTo(0));
        Assert.That(bind, Is.GreaterThan(cacheCheck));
        Assert.That(body, Does.Contain("EngageExplainProjection.Project"));
        Assert.That(body, Does.Contain("WeaponAbortTooltipBinder.ScopeCombatDetail"));
        Assert.That(body, Does.Contain("bridgeHost.LastCombatFrame"));
        Assert.That(body, Does.Contain("bridgeHost.SelectedContactId"));
        Assert.That(body, Does.Not.Contain("ProjectCombatDetail"));
        Assert.That(body, Does.Not.Contain("CombatInspection"));
    }

    private static RightUnitCombatDetailCache.Result Bind(
        RightUnitCombatDetailCache cache,
        EngagePreview? preview,
        CombatPresentationFrame? frame,
        string? selectedUnitId,
        string? selectedContactId)
    {
        if (cache.TryGet(preview, frame, selectedUnitId, selectedContactId, out var cached))
        {
            return cached;
        }

        var explain = EngageExplainProjection.Project(preview);
        return cache.Remember(
            preview,
            frame,
            selectedUnitId,
            selectedContactId,
            explain,
            WeaponAbortTooltipBinder.ScopeCombatDetail(frame, selectedUnitId, selectedContactId),
            WeaponAbortTooltipBinder.BindForSelection(explain, frame, selectedUnitId, selectedContactId));
    }

    private static EngagePreview BlockedPreview(string code) =>
        new("DLZ: OUT", false, code);

    private static EngagePreview Copy(EngagePreview preview) =>
        new(preview.DlzLabel, preview.CanFire, preview.AbortPreviewCode);

    private static CombatPresentationFrame Frame(params CombatEvent[] events) =>
        new(new CombatEventSnapshot(events), SliceAContactFrame.Empty, BdaAssessSnapshot.Empty, 6);

    private static CombatEvent Refused(string shooter, string target, string reason, ulong correlation, ulong tick) =>
        new(CombatEventPhase.AuthorizationRefused, shooter, target, "Missile", reason, correlation, tick, tick, $"policy:{reason}");
}
