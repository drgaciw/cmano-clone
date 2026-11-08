namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for APP-6 map symbol LOD clustering (REQ-20 Phase N).
/// Returns cluster list plus reduction stats for presentation / perf gates.
/// </summary>
public static class MapLodApplyState
{
    /// <summary>
    /// Cluster symbols for the given band using <see cref="MapLodPolicy.DefaultGridDivisions"/>.
    /// </summary>
    public static MapLodApplyResult Apply(IReadOnlyList<MapSymbolEntry> symbols, MapLodBand band) =>
        Apply(symbols, band, MapLodPolicy.DefaultGridDivisions(band));

    /// <summary>
    /// Cluster symbols for the given band and grid divisions; emit input/output/reduction stats.
    /// </summary>
    public static MapLodApplyResult Apply(
        IReadOnlyList<MapSymbolEntry> symbols,
        MapLodBand band,
        int gridDivisions)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        var clusters = MapSymbolLodClusterer.Cluster(symbols, band, gridDivisions);
        var inputCount = symbols.Count;
        var outputCount = clusters.Count;
        var reductionRatio = inputCount == 0
            ? 1.0
            : (double)outputCount / inputCount;

        return new MapLodApplyResult(
            Clusters: clusters,
            InputCount: inputCount,
            OutputCount: outputCount,
            ReductionRatio: reductionRatio,
            Band: band,
            GridDivisions: gridDivisions);
    }
}

/// <summary>LOD cluster apply result with reduction statistics.</summary>
public sealed record MapLodApplyResult(
    IReadOnlyList<MapSymbolClusterEntry> Clusters,
    int InputCount,
    int OutputCount,
    double ReductionRatio,
    MapLodBand Band,
    int GridDivisions);
