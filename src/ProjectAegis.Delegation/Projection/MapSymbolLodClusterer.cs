namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Deterministic APP-6-aware grid LOD clusterer for multi-thousand map symbols (REQ-20 Phase N / hub 5k@60).
/// Pure projection — no Unity, no sim mutation.
/// </summary>
public static class MapSymbolLodClusterer
{
    /// <summary>
    /// Cluster map symbols for the given LOD band.
    /// <see cref="MapLodBand.Close"/> returns 1:1 non-cluster entries.
    /// Coarser bands grid-bucket by normalized x/y (or lat/lon when both present);
    /// representative is the lowest <see cref="MapSymbolEntry.SymbolId"/> (ordinal).
    /// Output order is deterministic by ClusterId then RepresentativeSymbolId.
    /// </summary>
    public static IReadOnlyList<MapSymbolClusterEntry> Cluster(
        IReadOnlyList<MapSymbolEntry> symbols,
        MapLodBand band,
        int gridDivisions)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (symbols.Count == 0)
        {
            return Array.Empty<MapSymbolClusterEntry>();
        }

        if (band == MapLodBand.Close)
        {
            return ToIdentityEntries(symbols);
        }

        if (gridDivisions < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(gridDivisions), gridDivisions, "gridDivisions must be >= 1.");
        }

        // Bucket key → members (stable insert order not required; we sort members for pick).
        var buckets = new Dictionary<(int Gx, int Gy), List<MapSymbolEntry>>();
        foreach (var symbol in symbols)
        {
            if (symbol is null)
            {
                continue;
            }

            var (gx, gy) = ResolveGridCell(symbol, gridDivisions);
            var key = (gx, gy);
            if (!buckets.TryGetValue(key, out var list))
            {
                list = new List<MapSymbolEntry>();
                buckets[key] = list;
            }

            list.Add(symbol);
        }

        var results = new List<MapSymbolClusterEntry>(buckets.Count);
        foreach (var kvp in buckets)
        {
            var members = kvp.Value;
            members.Sort(static (a, b) => string.CompareOrdinal(a.SymbolId, b.SymbolId));
            var representative = members[0];
            var count = members.Count;
            var isCluster = count > 1;
            var clusterId = isCluster
                ? $"g:{kvp.Key.Gx}:{kvp.Key.Gy}"
                : $"s:{representative.SymbolId}";

            var (nx, ny) = AverageNormalized(members);
            var (lat, lon) = AverageLatLon(members);
            var affiliation = MajorityAffiliation(members);
            var glyph = ResolveApp6Glyph(representative);

            results.Add(new MapSymbolClusterEntry(
                ClusterId: clusterId,
                RepresentativeSymbolId: representative.SymbolId,
                Count: count,
                NormalizedX: nx,
                NormalizedY: ny,
                Latitude: lat,
                Longitude: lon,
                AffiliationMajority: affiliation,
                App6Glyph: glyph,
                IsCluster: isCluster));
        }

        return results
            .OrderBy(c => c.ClusterId, StringComparer.Ordinal)
            .ThenBy(c => c.RepresentativeSymbolId, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<MapSymbolClusterEntry> ToIdentityEntries(IReadOnlyList<MapSymbolEntry> symbols)
    {
        var results = new List<MapSymbolClusterEntry>(symbols.Count);
        foreach (var symbol in symbols.OrderBy(s => s.SymbolId, StringComparer.Ordinal))
        {
            if (symbol is null)
            {
                continue;
            }

            results.Add(new MapSymbolClusterEntry(
                ClusterId: $"s:{symbol.SymbolId}",
                RepresentativeSymbolId: symbol.SymbolId,
                Count: 1,
                NormalizedX: symbol.NormalizedX,
                NormalizedY: symbol.NormalizedY,
                Latitude: symbol.Latitude,
                Longitude: symbol.Longitude,
                AffiliationMajority: symbol.Affiliation,
                App6Glyph: ResolveApp6Glyph(symbol),
                IsCluster: false));
        }

        return results;
    }

    /// <summary>Prefer lat/lon grid when both present; else normalized x/y in [0,1].</summary>
    private static (int Gx, int Gy) ResolveGridCell(MapSymbolEntry symbol, int gridDivisions)
    {
        if (symbol.Latitude is double lat && symbol.Longitude is double lon)
        {
            var nx = (float)((lon + 180.0) / 360.0);
            var ny = (float)((lat + 90.0) / 180.0);
            return (Bucket(nx, gridDivisions), Bucket(ny, gridDivisions));
        }

        return (Bucket(symbol.NormalizedX, gridDivisions), Bucket(symbol.NormalizedY, gridDivisions));
    }

    private static int Bucket(float normalized, int divisions)
    {
        if (normalized <= 0f)
        {
            return 0;
        }

        if (normalized >= 1f)
        {
            return divisions - 1;
        }

        var cell = (int)(normalized * divisions);
        return cell >= divisions ? divisions - 1 : cell;
    }

    private static (float X, float Y) AverageNormalized(IReadOnlyList<MapSymbolEntry> members)
    {
        double sx = 0;
        double sy = 0;
        for (var i = 0; i < members.Count; i++)
        {
            sx += members[i].NormalizedX;
            sy += members[i].NormalizedY;
        }

        var n = members.Count;
        return ((float)(sx / n), (float)(sy / n));
    }

    private static (double? Lat, double? Lon) AverageLatLon(IReadOnlyList<MapSymbolEntry> members)
    {
        double slat = 0;
        double slon = 0;
        var n = 0;
        for (var i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.Latitude is double lat && m.Longitude is double lon)
            {
                slat += lat;
                slon += lon;
                n++;
            }
        }

        if (n == 0)
        {
            return (null, null);
        }

        return (slat / n, slon / n);
    }

    private static string MajorityAffiliation(IReadOnlyList<MapSymbolEntry> members)
    {
        // Count per affiliation; ties broken by lowest ordinal affiliation string.
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < members.Count; i++)
        {
            var aff = members[i].Affiliation ?? string.Empty;
            counts.TryGetValue(aff, out var c);
            counts[aff] = c + 1;
        }

        string? best = null;
        var bestCount = -1;
        foreach (var kvp in counts.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (kvp.Value > bestCount)
            {
                bestCount = kvp.Value;
                best = kvp.Key;
            }
        }

        return best ?? string.Empty;
    }

    /// <summary>APP-6 glyph via existing ResolveGlyph path (SIDC preferred, affiliation fallback).</summary>
    private static string ResolveApp6Glyph(MapSymbolEntry symbol) =>
        CesiumBillboardProjection.ResolveGlyph(symbol).UnicodeGlyph;
}
