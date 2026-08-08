using System.Diagnostics;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// APP-6 map symbol LOD clustering (REQ-20 Phase N / hub 5k@60 north-star).
/// Pure headless tests — determinism, identity at Close, reduction at Overview, 5k bench.
/// </summary>
[TestFixture]
public sealed class MapSymbolLodClustererTests
{
    private const int Synthetic5k = 5000;
    private const int OverviewGridDivisions = 16;
    private const int WarmupIterations = 3;
    private const int MeasuredIterations = 20;
    private const double Cluster5kBudgetMs = 50.0;

    [Test]
    public void ResolveBand_uses_altitude_thresholds()
    {
        Assert.That(MapLodPolicy.ResolveBand(0), Is.EqualTo(MapLodBand.Close));
        Assert.That(MapLodPolicy.ResolveBand(MapLodPolicy.CloseMaxAltitudeMeters - 1), Is.EqualTo(MapLodBand.Close));
        Assert.That(MapLodPolicy.ResolveBand(MapLodPolicy.CloseMaxAltitudeMeters), Is.EqualTo(MapLodBand.Tactical));
        Assert.That(MapLodPolicy.ResolveBand(MapLodPolicy.TacticalMaxAltitudeMeters), Is.EqualTo(MapLodBand.Theater));
        Assert.That(MapLodPolicy.ResolveBand(MapLodPolicy.TheaterMaxAltitudeMeters), Is.EqualTo(MapLodBand.Overview));
        Assert.That(MapLodPolicy.ResolveBand(2_000_000), Is.EqualTo(MapLodBand.Overview));
    }

    [Test]
    public void Cluster_Close_is_identity_one_to_one()
    {
        var symbols = CreateSymbols(12, seed: 7);
        var clustered = MapSymbolLodClusterer.Cluster(symbols, MapLodBand.Close, gridDivisions: 8);

        Assert.That(clustered.Count, Is.EqualTo(symbols.Count));
        Assert.That(clustered.All(c => !c.IsCluster), Is.True);
        Assert.That(clustered.All(c => c.Count == 1), Is.True);

        var inputIds = symbols.Select(s => s.SymbolId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var outIds = clustered.Select(c => c.RepresentativeSymbolId).ToArray();
        Assert.That(outIds, Is.EqualTo(inputIds));
    }

    [Test]
    public void Cluster_is_deterministic_for_same_inputs()
    {
        var symbols = CreateSymbols(200, seed: 11);
        var a = MapSymbolLodClusterer.Cluster(symbols, MapLodBand.Overview, OverviewGridDivisions);
        var b = MapSymbolLodClusterer.Cluster(symbols, MapLodBand.Overview, OverviewGridDivisions);

        Assert.That(b.Count, Is.EqualTo(a.Count));
        for (var i = 0; i < a.Count; i++)
        {
            Assert.That(b[i], Is.EqualTo(a[i]), $"Cluster index {i} must match across runs.");
        }
    }

    [Test]
    public void Cluster_Overview_reduces_symbol_count()
    {
        var symbols = CreateSymbols(500, seed: 3);
        var clustered = MapSymbolLodClusterer.Cluster(symbols, MapLodBand.Overview, OverviewGridDivisions);

        Assert.That(clustered.Count, Is.LessThan(symbols.Count));
        Assert.That(clustered.Count, Is.LessThanOrEqualTo(OverviewGridDivisions * OverviewGridDivisions));
        Assert.That(clustered.Sum(c => c.Count), Is.EqualTo(symbols.Count));
        Assert.That(clustered.Any(c => c.IsCluster), Is.True);
    }

    [Test]
    public void Cluster_representative_is_lowest_symbol_id_ordinal()
    {
        // Force same grid cell via identical normalized position.
        var symbols = new[]
        {
            MakeSymbol("z-unit", "Hostile", 0.5f, 0.5f),
            MakeSymbol("a-unit", "Friendly", 0.5f, 0.5f),
            MakeSymbol("m-unit", "Neutral", 0.5f, 0.5f),
        };

        var clustered = MapSymbolLodClusterer.Cluster(symbols, MapLodBand.Theater, gridDivisions: 4);
        Assert.That(clustered.Count, Is.EqualTo(1));
        Assert.That(clustered[0].RepresentativeSymbolId, Is.EqualTo("a-unit"));
        Assert.That(clustered[0].Count, Is.EqualTo(3));
        Assert.That(clustered[0].IsCluster, Is.True);
        // Majority: one of each affiliation — ordinal tie-break picks "Friendly" first among equal counts? 
        // Each has count 1; OrderBy affiliation then pick max count → first max is "Friendly" (lowest ordinal among scanned after sort).
        Assert.That(clustered[0].AffiliationMajority, Is.EqualTo("Friendly"));
    }

    [Test]
    public void Cluster_App6Glyph_uses_representative_resolution()
    {
        var friendly = MakeSymbol("f1", "Friendly", 0.2f, 0.2f);
        var hostile = MakeSymbol("h1", "Hostile", 0.2f, 0.2f);
        // Representative is f1 (lower ordinal) → friendly glyph.
        var clustered = MapSymbolLodClusterer.Cluster([friendly, hostile], MapLodBand.Overview, 8);
        Assert.That(clustered.Count, Is.EqualTo(1));
        Assert.That(clustered[0].RepresentativeSymbolId, Is.EqualTo("f1"));
        Assert.That(clustered[0].App6Glyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(clustered[0].App6Glyph, Is.EqualTo(CesiumBillboardProjection.ResolveGlyph(friendly).UnicodeGlyph));
    }

    [Test]
    public void Apply_reports_reduction_stats()
    {
        var symbols = CreateSymbols(100, seed: 5);
        var result = MapLodApplyState.Apply(symbols, MapLodBand.Overview, OverviewGridDivisions);

        Assert.That(result.InputCount, Is.EqualTo(100));
        Assert.That(result.OutputCount, Is.EqualTo(result.Clusters.Count));
        Assert.That(result.OutputCount, Is.LessThan(result.InputCount));
        Assert.That(result.ReductionRatio, Is.EqualTo((double)result.OutputCount / result.InputCount).Within(1e-9));
        Assert.That(result.Band, Is.EqualTo(MapLodBand.Overview));
    }

    [Test]
    public void Cluster_5k_Overview_reduces_well_below_input()
    {
        var symbols = CreateSymbols(Synthetic5k, seed: 7);
        var result = MapLodApplyState.Apply(symbols, MapLodBand.Overview, OverviewGridDivisions);

        Assert.That(result.InputCount, Is.EqualTo(Synthetic5k));
        // 16×16 grid → at most 256 cells; assert strong reduction vs 5k.
        Assert.That(result.OutputCount, Is.LessThan(500));
        Assert.That(result.OutputCount, Is.LessThanOrEqualTo(OverviewGridDivisions * OverviewGridDivisions));
        Assert.That(result.Clusters.Sum(c => c.Count), Is.EqualTo(Synthetic5k));
        TestContext.WriteLine(
            $"5k Overview cluster: in={result.InputCount} out={result.OutputCount} ratio={result.ReductionRatio:F4}");
    }

    [Test]
    public void Cluster_5k_Overview_wall_clock_p95_under_budget()
    {
        var symbols = CreateSymbols(Synthetic5k, seed: 7);

        for (var i = 0; i < WarmupIterations; i++)
        {
            _ = MapSymbolLodClusterer.Cluster(symbols, MapLodBand.Overview, OverviewGridDivisions);
        }

        var samplesMs = new double[MeasuredIterations];
        for (var i = 0; i < MeasuredIterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var clustered = MapSymbolLodClusterer.Cluster(symbols, MapLodBand.Overview, OverviewGridDivisions);
            sw.Stop();
            samplesMs[i] = sw.Elapsed.TotalMilliseconds;
            Assert.That(clustered.Count, Is.LessThan(500));
        }

        Array.Sort(samplesMs);
        var mean = samplesMs.Average();
        var p95Index = (int)Math.Ceiling(MeasuredIterations * 0.95) - 1;
        if (p95Index < 0)
        {
            p95Index = 0;
        }

        if (p95Index >= MeasuredIterations)
        {
            p95Index = MeasuredIterations - 1;
        }

        var p95 = samplesMs[p95Index];
        var max = samplesMs[^1];
        TestContext.WriteLine(
            $"MapSymbolLodClusterer 5k Overview: mean={mean:F3} ms p95={p95:F3} ms max={max:F3} ms (n={MeasuredIterations}) budget={Cluster5kBudgetMs} ms");

        Assert.That(
            p95,
            Is.LessThan(Cluster5kBudgetMs),
            $"5k cluster p95 must stay under {Cluster5kBudgetMs} ms; was {p95:F3} ms.");
    }

    [Test]
    public void MapPanelPresentation_LodOutputCount_is_additive_default()
    {
        var symbols = CreateSymbols(10, seed: 1);
        var applied = MapPanelApplyState.BindAndApply(symbols, "BALTIC");
        Assert.That(applied.LodOutputCount, Is.EqualTo(applied.SymbolCount));

        var lod = MapLodApplyState.Apply(symbols, MapLodBand.Overview, 4);
        var withLod = MapPanelApplyState.BindAndApply(
            symbols,
            "BALTIC",
            selectedUnitId: null,
            selectedContactId: null,
            envelopeRings: null,
            datalinkEdges: null,
            doctrineOverlay: null,
            lodOutputCount: lod.OutputCount);
        Assert.That(withLod.LodOutputCount, Is.EqualTo(lod.OutputCount));
        Assert.That(withLod.LodOutputCount, Is.LessThanOrEqualTo(withLod.SymbolCount));
    }

    private static IReadOnlyList<MapSymbolEntry> CreateSymbols(int count, int seed)
    {
        var list = new List<MapSymbolEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var id = $"sym-{i:D5}";
            var affiliation = (i % 5) switch
            {
                0 => "Hostile",
                1 => "Neutral",
                2 => "Suspect",
                3 => "Pending",
                _ => "Friendly",
            };
            var (x, y) = MapPictureProjection.Place(id, seed);
            var resolution = App6Sidc.ResolveMapGlyph(affiliation);
            list.Add(new MapSymbolEntry(
                id,
                affiliation,
                resolution.UnicodeGlyph,
                id,
                x,
                y,
                IsDestroyed: false,
                resolution.Sidc,
                resolution.UssFrameId));
        }

        return list;
    }

    private static MapSymbolEntry MakeSymbol(string id, string affiliation, float x, float y)
    {
        var resolution = App6Sidc.ResolveMapGlyph(affiliation);
        return new MapSymbolEntry(
            id,
            affiliation,
            resolution.UnicodeGlyph,
            id,
            x,
            y,
            IsDestroyed: false,
            resolution.Sidc,
            resolution.UssFrameId);
    }
}
