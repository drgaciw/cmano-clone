namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure projection of deck/hangar spot counts into capacity bands (CMD-24 Air Facilities).
/// </summary>
public static class DeckHangarCapacityProjection
{
    public const string BandOpen = "Open";
    public const string BandCongested = "Congested";
    public const string BandFull = "Full";
    public const string MissingLabel = "—";

    /// <summary>Occupancy at or above this fraction (and below full) is <see cref="BandCongested"/>.</summary>
    public const double CongestedOccupancyThreshold = 0.75;

    /// <summary>
    /// Project capacity rows from host spot tuples.
    /// Items: hostId, deckTotal, deckOccupied, hangarTotal, hangarOccupied, readyOnDeck, optional sortieRate.
    /// </summary>
    public static IReadOnlyList<DeckHangarCapacityEntry> Project(
        IReadOnlyList<(
            string hostId,
            int deckTotal,
            int deckOccupied,
            int hangarTotal,
            int hangarOccupied,
            int readyOnDeck,
            int? sortieRatePerHour)>? rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<DeckHangarCapacityEntry>();
        }

        var result = new List<DeckHangarCapacityEntry>(rows.Count);
        foreach (var row in rows
                     .Where(r => !string.IsNullOrWhiteSpace(r.hostId))
                     .OrderBy(r => r.hostId, StringComparer.Ordinal))
        {
            result.Add(ToEntry(
                row.hostId,
                row.deckTotal,
                row.deckOccupied,
                row.hangarTotal,
                row.hangarOccupied,
                row.readyOnDeck,
                row.sortieRatePerHour));
        }

        return result;
    }

    /// <summary>Project from domain capacity states.</summary>
    public static IReadOnlyList<DeckHangarCapacityEntry> Project(
        IReadOnlyList<DeckHangarCapacityState>? states)
    {
        if (states is null || states.Count == 0)
        {
            return Array.Empty<DeckHangarCapacityEntry>();
        }

        var tuples = states
            .Where(s => s is not null)
            .Select(s => (
                s.HostId,
                s.DeckSpotsTotal,
                s.DeckSpotsOccupied,
                s.HangarSpotsTotal,
                s.HangarSpotsOccupied,
                s.ReadyOnDeck,
                s.SortieRatePerHour))
            .ToList();

        return Project(tuples);
    }

    /// <summary>
    /// Capacity band from occupancy ratio: Full (≥1 or free=0 with capacity), Congested (≥75%), else Open.
    /// </summary>
    public static string ResolveCapacityBand(int occupied, int total)
    {
        if (total <= 0)
        {
            return BandOpen;
        }

        var occ = Math.Clamp(occupied, 0, int.MaxValue);
        if (occ >= total)
        {
            return BandFull;
        }

        var ratio = (double)occ / total;
        if (ratio >= CongestedOccupancyThreshold)
        {
            return BandCongested;
        }

        return BandOpen;
    }

    /// <summary>Combined deck+hangar occupancy band (worst of the two when both have capacity).</summary>
    public static string ResolveCombinedBand(
        int deckOccupied,
        int deckTotal,
        int hangarOccupied,
        int hangarTotal)
    {
        var deckBand = ResolveCapacityBand(deckOccupied, deckTotal);
        var hangarBand = ResolveCapacityBand(hangarOccupied, hangarTotal);

        if (deckTotal <= 0 && hangarTotal <= 0)
        {
            return BandOpen;
        }

        if (deckTotal <= 0)
        {
            return hangarBand;
        }

        if (hangarTotal <= 0)
        {
            return deckBand;
        }

        return WorseBand(deckBand, hangarBand);
    }

    public static double ComputeOccupancyPct(int occupied, int total)
    {
        if (total <= 0)
        {
            return 0.0;
        }

        return Math.Clamp(100.0 * Math.Max(0, occupied) / total, 0.0, 100.0);
    }

    public static DeckHangarCapacityAggregate Aggregate(IReadOnlyList<DeckHangarCapacityEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return DeckHangarCapacityAggregate.Empty;
        }

        var open = 0;
        var congested = 0;
        var full = 0;
        var ready = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            ready += Math.Max(0, e.ReadyOnDeck);
            if (string.Equals(e.CapacityBand, BandFull, StringComparison.Ordinal))
            {
                full++;
            }
            else if (string.Equals(e.CapacityBand, BandCongested, StringComparison.Ordinal))
            {
                congested++;
            }
            else
            {
                open++;
            }
        }

        return new DeckHangarCapacityAggregate(
            open,
            congested,
            full,
            entries.Count,
            ready,
            FormatSummaryLine(open, congested, full, ready));
    }

    public static string FormatSummaryLine(int open, int congested, int full, int readyOnDeck) =>
        $"OPEN {open}  CONGESTED {congested}  FULL {full}  ·  ready {readyOnDeck}";

    private static string WorseBand(string a, string b)
    {
        if (string.Equals(a, BandFull, StringComparison.Ordinal) ||
            string.Equals(b, BandFull, StringComparison.Ordinal))
        {
            return BandFull;
        }

        if (string.Equals(a, BandCongested, StringComparison.Ordinal) ||
            string.Equals(b, BandCongested, StringComparison.Ordinal))
        {
            return BandCongested;
        }

        return BandOpen;
    }

    private static DeckHangarCapacityEntry ToEntry(
        string hostId,
        int deckTotal,
        int deckOccupied,
        int hangarTotal,
        int hangarOccupied,
        int readyOnDeck,
        int? sortieRatePerHour)
    {
        var dTotal = Math.Max(0, deckTotal);
        var dOcc = Math.Clamp(deckOccupied, 0, dTotal > 0 ? dTotal : Math.Max(0, deckOccupied));
        var hTotal = Math.Max(0, hangarTotal);
        var hOcc = Math.Clamp(hangarOccupied, 0, hTotal > 0 ? hTotal : Math.Max(0, hangarOccupied));
        // Allow occupied to report raw when total is 0 (no capacity declared) — band stays Open.
        if (dTotal > 0)
        {
            dOcc = Math.Clamp(deckOccupied, 0, dTotal);
        }
        else
        {
            dOcc = Math.Max(0, deckOccupied);
        }

        if (hTotal > 0)
        {
            hOcc = Math.Clamp(hangarOccupied, 0, hTotal);
        }
        else
        {
            hOcc = Math.Max(0, hangarOccupied);
        }

        var band = ResolveCombinedBand(dOcc, dTotal, hOcc, hTotal);
        var ready = Math.Max(0, readyOnDeck);

        return new DeckHangarCapacityEntry(
            HostId: hostId.Trim(),
            DeckSpotsTotal: dTotal,
            DeckSpotsOccupied: dOcc,
            HangarSpotsTotal: hTotal,
            HangarSpotsOccupied: hOcc,
            ReadyOnDeck: ready,
            SortieRatePerHour: sortieRatePerHour is int s && s >= 0 ? s : null,
            CapacityBand: band,
            DeckOccupancyPct: ComputeOccupancyPct(dOcc, dTotal),
            HangarOccupancyPct: ComputeOccupancyPct(hOcc, hTotal));
    }
}

/// <summary>One projected deck/hangar capacity row for UI binding.</summary>
public sealed record DeckHangarCapacityEntry(
    string HostId,
    int DeckSpotsTotal,
    int DeckSpotsOccupied,
    int HangarSpotsTotal,
    int HangarSpotsOccupied,
    int ReadyOnDeck,
    int? SortieRatePerHour,
    string CapacityBand,
    double DeckOccupancyPct,
    double HangarOccupancyPct);

/// <summary>Aggregate host capacity for panel header.</summary>
public sealed record DeckHangarCapacityAggregate(
    int OpenCount,
    int CongestedCount,
    int FullCount,
    int TotalHosts,
    int TotalReadyOnDeck,
    string SummaryLine)
{
    public static DeckHangarCapacityAggregate Empty { get; } =
        new(0, 0, 0, 0, 0, "OPEN 0  CONGESTED 0  FULL 0  ·  ready 0");
}
