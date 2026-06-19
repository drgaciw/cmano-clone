using System.Diagnostics;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// Headless wall-clock proxy for Unity C2 panel selection bind latency (Req 20: &lt; 100 ms).
/// Unity Profiler frame capture is deferred to Editor host — see
/// <c>production/perf/unity-c2-frame-baseline-s35-2026-06-19.md</c>.
/// </summary>
[TestFixture]
public sealed class C2PanelBindTimingTests
{
    private const int WarmupIterations = 3;
    private const int MeasuredIterations = 20;
    private const int Req20PanelBindBudgetMs = 100;

    [Test]
    public void C2_panel_selection_bind_path_completes_under_100ms_budget()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10, mvpEngagement: false);
        var oob = new[] { new OobTreeEntry("u1", true) };
        var symbols = MapPictureProjection.Project(oob, result.SensorC2.Contacts, layoutSeed: 7);
        var hostile = symbols.First(s => s.Affiliation == "Hostile");

        for (var i = 0; i < WarmupIterations; i++)
        {
            _ = RunSelectionBindPath(result.SensorC2, oob, symbols, hostile.SymbolId);
        }

        var samplesMs = new double[MeasuredIterations];
        for (var i = 0; i < MeasuredIterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            _ = RunSelectionBindPath(result.SensorC2, oob, symbols, hostile.SymbolId);
            stopwatch.Stop();
            samplesMs[i] = stopwatch.Elapsed.TotalMilliseconds;
        }

        Array.Sort(samplesMs);
        var meanMs = samplesMs.Average();
        var p95Index = (int)Math.Ceiling(MeasuredIterations * 0.95) - 1;
        var p95Ms = samplesMs[p95Index];
        var maxMs = samplesMs[^1];

        TestContext.WriteLine(
            $"C2 panel selection bind: mean={meanMs:F3} ms p95={p95Ms:F3} ms max={maxMs:F3} ms (n={MeasuredIterations})");

        Assert.That(
            p95Ms,
            Is.LessThan(Req20PanelBindBudgetMs),
            $"Req 20 panel-bind budget is {Req20PanelBindBudgetMs} ms; headless p95 was {p95Ms:F3} ms.");
        Assert.That(
            maxMs,
            Is.LessThan(Req20PanelBindBudgetMs),
            $"Worst-case headless bind sample must stay under {Req20PanelBindBudgetMs} ms (max={maxMs:F3} ms).");
    }

    private static SelectionBindOutcome RunSelectionBindPath(
        SensorC2Snapshot sensorC2,
        IReadOnlyList<OobTreeEntry> oob,
        IReadOnlyList<MapSymbolEntry> symbols,
        string hostileSymbolId)
    {
        var defaultUnit = C2SelectionResolver.ResolveDefaultFriendlyUnit(oob);

        var mapDefault = MapPanelBinder.Bind(symbols, "baltic-patrol-classify", defaultUnit, null);
        var oobDefault = OobTreePanelBinder.Bind(oob, defaultUnit);

        Assert.That(mapDefault.Symbols.Single(s => s.SymbolId == defaultUnit).IsSelected, Is.True);
        Assert.That(oobDefault.UnitRows.Single(r => r.UnitId == defaultUnit).IsSelected, Is.True);

        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol(hostileSymbolId, symbols, out var contactId),
            Is.True);

        var summary = ContactSummaryProjection.Project(contactId, sensorC2.Contacts);
        Assert.That(summary, Is.Not.Null);

        var mapContact = MapPanelBinder.Bind(symbols, "baltic-patrol-classify", null, contactId);
        var drawerContacts = SensorC2PanelBinder.Bind(sensorC2);

        return new SelectionBindOutcome(
            mapContact,
            drawerContacts,
            summary!,
            contactId);
    }

    private sealed record SelectionBindOutcome(
        MapPanelState MapContact,
        SensorC2PanelState DrawerContacts,
        ContactSummaryEntry Summary,
        string ContactId);
}