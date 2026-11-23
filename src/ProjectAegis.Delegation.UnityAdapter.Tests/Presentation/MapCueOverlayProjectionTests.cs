using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MapCueOverlayProjectionTests
{
    [Test]
    public void Empty_or_null_bridge_returns_empty_overlay_state()
    {
        var state = MapCueOverlayProjection.Project(
            bridge: null,
            symbols: new[] { Symbol("u1", "Friendly", 0.2f, 0.3f) },
            currentSimTick: 10,
            selectedContactId: "c1",
            selectedUnitId: "u1");

        Assert.That(state.Symbols, Is.Empty);
        Assert.That(state.Contacts, Is.Empty);
        Assert.That(state.UnitOrders, Is.Empty);
        Assert.That(state.SelectedContactCue.Visible, Is.False);
        Assert.That(state.SelectedUnitOrderCue.Visible, Is.False);
        Assert.That(state.IsInspectingHistory, Is.False);
    }

    [Test]
    public void Stale_and_datalink_contact_icons_reflect_ContactCueProjection_independently()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var log = bridge.Orchestrator.DecisionLog;

        // Tick 1: dl-hostile-1 (auto-datalink via dl- prefix) and c-organic (explicit Organic).
        log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", "dl-hostile-1", "hostile-1", "Unknown", "Identified"));
        log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", "c-organic", "hostile-2", "Unknown", "Identified"));
        // Tick 10: c-organic refreshed so only dl-hostile-1 is stale at tick 11 with threshold 5.
        log.AppendContactChange(new ContactChangeRecord(0, 10.0, 10, "u1", "c-organic", "hostile-2", "Identified", "Identified"));

        var symbols = new[]
        {
            Symbol("dl-hostile-1", "Hostile", 0.4f, 0.5f),
            Symbol("c-organic", "Hostile", 0.6f, 0.7f),
        };
        var origins = new[]
        {
            new ContactOriginFact("c-organic", ContactInformationOrigin.Organic),
        };

        var state = MapCueOverlayProjection.Project(
            bridge,
            symbols,
            currentSimTick: 11,
            selectedContactId: null,
            selectedUnitId: null,
            origins: origins,
            staleThresholdTicks: 5);

        var dlRow = state.Symbols.Single(s => s.SymbolId == "dl-hostile-1");
        Assert.That(dlRow.IsContact, Is.True);
        Assert.That(dlRow.IsStale, Is.True);
        Assert.That(dlRow.IsDatalinkSourced, Is.True);
        Assert.That(dlRow.NormalizedX, Is.EqualTo(0.4f).Within(0.0001f));
        Assert.That(dlRow.NormalizedY, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(dlRow.IsSelected, Is.False);
        Assert.That(dlRow.TooltipText, Is.Not.Empty);
        Assert.That(dlRow.Origin, Is.EqualTo(ContactInformationOrigin.Datalink));
        Assert.That(dlRow.ContactTokensText, Does.Contain(MapCueOverlayProjection.StaleTokenText));
        Assert.That(dlRow.ContactTokensText, Does.Contain(MapCueOverlayProjection.DatalinkTokenText));
        Assert.That(dlRow.CssClasses, Does.Contain(MapCueOverlayProjection.StaleCssClass));
        Assert.That(dlRow.CssClasses, Does.Contain(MapCueOverlayProjection.DatalinkCssClass));

        var orgRow = state.Symbols.Single(s => s.SymbolId == "c-organic");
        Assert.That(orgRow.IsContact, Is.True);
        Assert.That(orgRow.IsStale, Is.False);
        Assert.That(orgRow.IsDatalinkSourced, Is.False);
        Assert.That(orgRow.Origin, Is.EqualTo(ContactInformationOrigin.Organic));
        Assert.That(orgRow.ContactTokensText, Is.Empty);
        Assert.That(orgRow.CssClasses, Does.Not.Contain(MapCueOverlayProjection.StaleCssClass));
        Assert.That(orgRow.CssClasses, Does.Not.Contain(MapCueOverlayProjection.DatalinkCssClass));
    }

    [Test]
    public void Queued_order_badge_requires_IsGenuinelyPending_and_hides_delayed_rejected_expired_executed()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var uQueued = bridge.Registry.RegisterUnit(new EntityKey(1), "u-queued");
        uQueued.Target.Slot.SetActive(new HumanController());

        var log = bridge.Orchestrator.DecisionLog;
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u-queued"), OrderKind.Hold, ExecuteSimTick: 5));
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u-delayed"), OrderKind.Move, ExecuteSimTick: 12));
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u-rejected"), OrderKind.Engage, ExecuteSimTick: 5));
        log.AppendPolicyDenial(new PolicyDenialRecord(0, 5.0, 5, new AgentId("roe"), new TargetId("u-rejected"), 0, FireAbortReason.WeaponsTight, OrderKind.Engage));
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u-executed"), OrderKind.Engage, ExecuteSimTick: 5));
        log.AppendEngagement(new EngagementRecord(0, 6.0, 6, new TargetId("u-executed"), EngagementId: 11, Launched: true));
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u-expired"), OrderKind.Hold, ExecuteSimTick: 5));

        var symbols = new[]
        {
            Symbol("u-queued", "Friendly", 0.1f, 0.2f),
            Symbol("u-delayed", "Friendly", 0.2f, 0.2f),
            Symbol("u-rejected", "Friendly", 0.3f, 0.2f),
            Symbol("u-executed", "Friendly", 0.4f, 0.2f),
            Symbol("u-expired", "Friendly", 0.5f, 0.2f),
            Symbol("u-none", "Friendly", 0.6f, 0.2f),
        };
        var terminals = new[]
        {
            new UnitOrderTerminalFact("u-expired", 7, UnitOrderDisposition.Expired, OrderKind.Hold),
        };

        var state = MapCueOverlayProjection.Project(
            bridge,
            symbols,
            currentSimTick: 8,
            selectedContactId: null,
            selectedUnitId: "u-queued",
            terminalOrders: terminals);

        var queuedRow = state.Symbols.Single(s => s.SymbolId == "u-queued");
        Assert.That(queuedRow.IsQueuedOrder, Is.True);
        Assert.That(queuedRow.OrderDisposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(queuedRow.OrderKind, Is.EqualTo(OrderKind.Hold));
        Assert.That(queuedRow.IsSelected, Is.True);
        Assert.That(queuedRow.UnitBadgeText, Does.Contain(MapCueOverlayProjection.QueuedBadgeText));
        Assert.That(queuedRow.CssClasses, Does.Contain(MapCueOverlayProjection.QueuedCssClass));
        Assert.That(state.SelectedUnitOrderCue.Visible, Is.True);
        Assert.That(state.SelectedUnitOrderCue.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(state.SelectedUnitOrderCue.OrderKind, Is.EqualTo(OrderKind.Hold));
        Assert.That(state.SelectedUnitOrderCue.Text, Does.Contain("QUEUED"));

        foreach (var nonPendingId in new[] { "u-delayed", "u-rejected", "u-executed", "u-expired", "u-none" })
        {
            var row = state.Symbols.Single(s => s.SymbolId == nonPendingId);
            Assert.That(row.IsQueuedOrder, Is.False, nonPendingId);
            Assert.That(row.UnitBadgeText, Is.Empty, nonPendingId);
            Assert.That(row.CssClasses, Does.Not.Contain(MapCueOverlayProjection.QueuedCssClass), nonPendingId);
        }
    }

    [Test]
    public void Selection_switch_clears_previous_contact_and_unit_cues_with_zero_leak()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var log = bridge.Orchestrator.DecisionLog;
        log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", "dl-stale", "hostile-1", "Unknown", "Suspect"));
        log.AppendContactChange(new ContactChangeRecord(0, 10.0, 10, "u1", "c-fresh", "hostile-2", "Unknown", "Identified"));
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u1"), OrderKind.Hold, ExecuteSimTick: 5));

        var symbols = new[]
        {
            Symbol("dl-stale", "Hostile", 0.2f, 0.2f),
            Symbol("c-fresh", "Hostile", 0.3f, 0.3f),
            Symbol("u1", "Friendly", 0.4f, 0.4f),
            Symbol("u2", "Friendly", 0.5f, 0.5f),
        };
        var origins = new[] { new ContactOriginFact("c-fresh", ContactInformationOrigin.Organic) };

        var first = MapCueOverlayProjection.Project(
            bridge,
            symbols,
            currentSimTick: 11,
            selectedContactId: "dl-stale",
            selectedUnitId: "u1",
            origins: origins,
            staleThresholdTicks: 5);

        Assert.That(first.SelectedContactCue.Visible, Is.True);
        Assert.That(first.SelectedContactCue.ContactId, Is.EqualTo("dl-stale"));
        Assert.That(first.SelectedContactCue.IsStale, Is.True);
        Assert.That(first.SelectedContactCue.IsDatalinkSourced, Is.True);
        Assert.That(first.SelectedContactCue.Text, Does.Contain(MapCueOverlayProjection.StaleTokenText));
        Assert.That(first.SelectedContactCue.Text, Does.Contain(MapCueOverlayProjection.DatalinkTokenText));
        Assert.That(first.SelectedUnitOrderCue.Visible, Is.True);
        Assert.That(first.SelectedUnitOrderCue.UnitId, Is.EqualTo("u1"));

        // Switch to fresh organic contact c-fresh and idle unit u2.
        var switched = MapCueOverlayProjection.Project(
            bridge,
            symbols,
            currentSimTick: 11,
            selectedContactId: "c-fresh",
            selectedUnitId: "u2",
            origins: origins,
            staleThresholdTicks: 5);

        Assert.That(switched.SelectedContactCue.Visible, Is.False);
        Assert.That(switched.SelectedContactCue.Text, Is.Empty);
        Assert.That(switched.SelectedContactCue.FixtureDisclosureText, Is.Empty);
        Assert.That(switched.SelectedContactCue.CssClasses, Is.Empty);
        Assert.That(switched.SelectedUnitOrderCue.Visible, Is.False);
        Assert.That(switched.SelectedUnitOrderCue.Text, Is.Empty);
        Assert.That(switched.SelectedUnitOrderCue.CssClasses, Is.Empty);
    }

    [Test]
    public void Fixture_position_preserves_DRG417_fixture_label_and_never_claims_production_feed()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Orchestrator.DecisionLog.AppendContactChange(
            new ContactChangeRecord(0, 2.0, 2, "u1", "c-prod", "hostile-1", "Unknown", "Identified"));
        bridge.Orchestrator.DecisionLog.AppendContactChange(
            new ContactChangeRecord(0, 2.0, 2, "u1", "c-fix", "hostile-2", "Unknown", "Identified"));

        var positions = new[]
        {
            new ContactPositionSample("c-prod", 2, 100.0, 200.0, IsProductionFeed: true, FixtureLabel: null),
            new ContactPositionSample("c-fix", 2, 300.0, 400.0, IsProductionFeed: false, FixtureLabel: "baltic-smoke-fixture"),
        };
        var symbols = new[]
        {
            Symbol("c-prod", "Hostile", 0.2f, 0.2f),
            Symbol("c-fix", "Hostile", 0.3f, 0.3f),
        };

        var stateProd = MapCueOverlayProjection.Project(
            bridge,
            symbols,
            currentSimTick: 2,
            selectedContactId: "c-prod",
            selectedUnitId: null,
            positions: positions);

        var prodRow = stateProd.Symbols.Single(s => s.SymbolId == "c-prod");
        Assert.That(prodRow.PositionKind, Is.EqualTo(ContactPositionClaimKind.FixtureOnly));
        Assert.That(prodRow.FixtureLabel, Is.EqualTo(ContactCueProjection.ProductionFeedUnavailableLabel));
        Assert.That(stateProd.SelectedContactCue.Visible, Is.True);
        Assert.That(stateProd.SelectedContactCue.FixtureDisclosureText,
            Is.EqualTo(ContactCueProjection.ProductionFeedUnavailableLabel));

        var stateFix = MapCueOverlayProjection.Project(
            bridge,
            symbols,
            currentSimTick: 2,
            selectedContactId: "c-fix",
            selectedUnitId: null,
            positions: positions);

        Assert.That(stateFix.Symbols.Single(s => s.SymbolId == "c-fix").FixtureLabel, Is.EqualTo("baltic-smoke-fixture"));
        Assert.That(stateFix.SelectedContactCue.FixtureDisclosureText, Is.EqualTo("baltic-smoke-fixture"));
    }

    [Test]
    public void Historical_timeline_inspection_hides_live_cues()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Orchestrator.DecisionLog.AppendContactChange(
            new ContactChangeRecord(0, 1.0, 1, "u1", "dl-1", "hostile-1", "Unknown", "Suspect"));
        bridge.Orchestrator.DecisionLog.AppendPlayerOrder(
            new PlayerOrderRecord(0, 1.0, 1, new TargetId("u1"), OrderKind.Hold, ExecuteSimTick: 1));

        var state = MapCueOverlayProjection.Project(
            bridge,
            new[] { Symbol("dl-1", "Hostile", 0.2f, 0.2f), Symbol("u1", "Friendly", 0.3f, 0.3f) },
            currentSimTick: 12,
            selectedContactId: "dl-1",
            selectedUnitId: "u1",
            isInspectingHistory: true);

        Assert.That(state.IsInspectingHistory, Is.True);
        Assert.That(state.Symbols, Is.Empty);
        Assert.That(state.SelectedContactCue.Visible, Is.False);
        Assert.That(state.SelectedUnitOrderCue.Visible, Is.False);
    }

    private static MapSymbolEntry Symbol(string id, string affiliation, float x, float y) =>
        new(id, affiliation, "◆", id, x, y, IsDestroyed: false);
}
