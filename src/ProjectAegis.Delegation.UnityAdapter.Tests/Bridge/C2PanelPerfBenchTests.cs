using System.Diagnostics;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// CMD-36: expanded headless wall-clock bench for the richer C2 panel bind path
/// (map apply + unit/contact detail apply + agent roster + tactical envelopes + message log).
/// Req 20 budget: p95 and max < 100 ms (n=20 after warmup). Unity Profiler deferred to Editor.
/// </summary>
[TestFixture]
public sealed class C2PanelPerfBenchTests
{
    private const int WarmupIterations = 3;
    private const int MeasuredIterations = 20;
    private const int Req20PanelBindBudgetMs = 100;

    [Test]
    public void C2_rich_panel_bind_path_completes_under_100ms_budget()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10, mvpEngagement: false);
        var oob = new[] { new OobTreeEntry("u1", true) };
        var symbols = MapPictureProjection.Project(oob, result.SensorC2.Contacts, layoutSeed: 7);
        var defaultUnit = C2SelectionResolver.ResolveDefaultFriendlyUnit(oob) ?? "u1";
        var hostile = symbols.First(s => s.Affiliation == "Hostile");
        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol(hostile.SymbolId, symbols, out var contactId),
            Is.True);

        // Untimed fixture data reused across iterations (projection inputs only).
        var unitEntry = new UnitDetailEntry(
            defaultUnit,
            IsAlive: true,
            StatusLabel: "READY",
            MagazineLabel: "MAGAZINE: —",
            EmconLabel: "EMCON: —",
            DoctrineLabel: "DOCTRINE: —",
            FuelLabel: "FUEL: —",
            EngagePreviewLabel: "ENGAGE: —",
            AttackOptionsLabel: "ATTACK: —",
            AttackMenu: Array.Empty<EngageAttackOptions.AttackOption>(),
            CommsLabel: "COMMS: —");
        var rosterEntries = AgentRosterProjection.Project(
        [
            (defaultUnit, "Agent", "agent-1", "FullAutonomous"),
            ("u2", "Human", null, "Manual"),
        ]);
        var contactLine = $"CONTACT: {contactId}";
        var currentSimTick = (ulong)result.Ticks;
        var messages = result.Messages;

        for (var i = 0; i < WarmupIterations; i++)
        {
            _ = RunRichPanelBindPath(
                symbols,
                defaultUnit,
                contactId,
                unitEntry,
                contactLine,
                result.SensorC2,
                currentSimTick,
                rosterEntries,
                messages);
        }

        var samplesMs = new double[MeasuredIterations];
        for (var i = 0; i < MeasuredIterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            _ = RunRichPanelBindPath(
                symbols,
                defaultUnit,
                contactId,
                unitEntry,
                contactLine,
                result.SensorC2,
                currentSimTick,
                rosterEntries,
                messages);
            stopwatch.Stop();
            samplesMs[i] = stopwatch.Elapsed.TotalMilliseconds;
        }

        var sample = new C2PanelPerfSample(samplesMs);
        TestContext.WriteLine(sample.FormatReport("C2 rich panel bind"));

        Assert.That(
            sample.P95Ms,
            Is.LessThan(Req20PanelBindBudgetMs),
            $"Req 20 panel-bind budget is {Req20PanelBindBudgetMs} ms; headless p95 was {sample.P95Ms:F3} ms.");
        Assert.That(
            sample.MaxMs,
            Is.LessThan(Req20PanelBindBudgetMs),
            $"Worst-case headless bind sample must stay under {Req20PanelBindBudgetMs} ms (max={sample.MaxMs:F3} ms).");
    }

    /// <summary>
    /// Richer bind path covering wave-1 apply-state surfaces + overlays (CMD-36).
    /// </summary>
    private static RichPanelBindOutcome RunRichPanelBindPath(
        IReadOnlyList<MapSymbolEntry> symbols,
        string selectedUnitId,
        string contactId,
        UnitDetailEntry unitEntry,
        string contactLine,
        SensorC2Snapshot sensorC2,
        ulong currentSimTick,
        IReadOnlyList<AgentRosterEntry> rosterEntries,
        IReadOnlyList<MessageLogLine> messages)
    {
        // Tactical overlay envelopes for selected unit (CMD-21/34).
        var envelopes = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(
            selectedUnitId,
            sensorRangeNm: 40,
            weaponRangeNm: 20,
            domain: TacticalOverlayProjection.DomainSurface);

        // Map panel bind + apply with symbols and envelope rings.
        var mapApplied = MapPanelApplyState.BindAndApply(
            symbols,
            theaterLabel: "baltic-patrol-classify",
            selectedUnitId: selectedUnitId,
            selectedContactId: null,
            envelopeRings: envelopes,
            datalinkEdges: null);

        // Unit detail bind + apply.
        var unitApplied = UnitDetailApplyState.BindAndApply(unitEntry, contactLine);

        // Contact detail projection sample → apply.
        var contactDetail = ContactDetailProjection.Project(
            contactId,
            sensorC2.Contacts,
            currentSimTick);
        var contactApplied = ContactDetailApplyState.Apply(contactDetail);

        // Agent roster apply.
        var rosterApplied = AgentRosterApplyState.Apply(rosterEntries);

        // Message log binder (cheap HUD row map).
        var messagePanel = MessageLogPanelBinder.Bind(messages);

        return new RichPanelBindOutcome(
            mapApplied,
            unitApplied,
            contactApplied,
            rosterApplied,
            envelopes,
            messagePanel);
    }

    private sealed record RichPanelBindOutcome(
        MapPanelPresentation Map,
        UnitDetailPresentation Unit,
        ContactDetailPresentation Contact,
        AgentRosterPresentation Roster,
        IReadOnlyList<EnvelopeRingEntry> Envelopes,
        MessageLogPanelState Messages);
}
