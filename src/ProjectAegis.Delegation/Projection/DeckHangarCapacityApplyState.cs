namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for deck/hangar capacity presentation (CMD-24 Air Facilities).
/// </summary>
public static class DeckHangarCapacityApplyState
{
    /// <summary>Honest empty-state when session has no capacity feed.</summary>
    public const string NoCapacityDataLine = "NO CAPACITY DATA";

    public static DeckHangarCapacityPresentation Apply(IReadOnlyList<DeckHangarCapacityEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return DeckHangarCapacityPresentation.Empty;
        }

        var aggregate = DeckHangarCapacityProjection.Aggregate(entries);
        var rows = new List<DeckHangarCapacityDisplayRow>(entries.Count);
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            var line = FormatLine(entry);
            lines.Add(line);
            rows.Add(new DeckHangarCapacityDisplayRow(
                entry.HostId,
                entry.DeckSpotsTotal,
                entry.DeckSpotsOccupied,
                entry.HangarSpotsTotal,
                entry.HangarSpotsOccupied,
                entry.ReadyOnDeck,
                entry.SortieRatePerHour,
                entry.CapacityBand,
                entry.DeckOccupancyPct,
                entry.HangarOccupancyPct,
                line));
        }

        return new DeckHangarCapacityPresentation(
            rows,
            lines,
            aggregate,
            HeaderLine: $"DECK/HANGAR  ·  {aggregate.SummaryLine}",
            EmptyStateLine: string.Empty,
            HasCapacityData: true);
    }

    /// <summary>
    /// Apply when the host knows whether capacity data exists.
    /// Missing data yields honest <see cref="NoCapacityDataLine"/>.
    /// </summary>
    public static DeckHangarCapacityPresentation Apply(
        IReadOnlyList<DeckHangarCapacityEntry>? entries,
        bool hasCapacityData)
    {
        if (!hasCapacityData)
        {
            return DeckHangarCapacityPresentation.NoCapacityData;
        }

        return Apply(entries);
    }

    public static string FormatLine(DeckHangarCapacityEntry entry)
    {
        var deck = $"{entry.DeckSpotsOccupied}/{entry.DeckSpotsTotal}";
        var hangar = $"{entry.HangarSpotsOccupied}/{entry.HangarSpotsTotal}";
        var sortie = entry.SortieRatePerHour is int rate
            ? rate.ToString()
            : DeckHangarCapacityProjection.MissingLabel;
        return
            $"{entry.HostId}  deck={deck}  hangar={hangar}  ready={entry.ReadyOnDeck}  sortie/h={sortie}  [{entry.CapacityBand}]";
    }
}

/// <summary>One bound list row for the deck/hangar ListView.</summary>
public sealed record DeckHangarCapacityDisplayRow(
    string HostId,
    int DeckSpotsTotal,
    int DeckSpotsOccupied,
    int HangarSpotsTotal,
    int HangarSpotsOccupied,
    int ReadyOnDeck,
    int? SortieRatePerHour,
    string CapacityBand,
    double DeckOccupancyPct,
    double HangarOccupancyPct,
    string DisplayLine);

/// <summary>Presentation bundle for hosts / tests.</summary>
public sealed record DeckHangarCapacityPresentation(
    IReadOnlyList<DeckHangarCapacityDisplayRow> Rows,
    IReadOnlyList<string> Lines,
    DeckHangarCapacityAggregate Aggregate,
    string HeaderLine,
    string EmptyStateLine,
    bool HasCapacityData)
{
    public static DeckHangarCapacityPresentation Empty { get; } = new(
        Array.Empty<DeckHangarCapacityDisplayRow>(),
        Array.Empty<string>(),
        DeckHangarCapacityAggregate.Empty,
        HeaderLine: "DECK/HANGAR  ·  OPEN 0  CONGESTED 0  FULL 0  ·  ready 0",
        EmptyStateLine: string.Empty,
        HasCapacityData: true);

    /// <summary>Honest zero-state when session has no capacity feed.</summary>
    public static DeckHangarCapacityPresentation NoCapacityData { get; } = new(
        Array.Empty<DeckHangarCapacityDisplayRow>(),
        Array.Empty<string>(),
        DeckHangarCapacityAggregate.Empty,
        HeaderLine: "DECK/HANGAR  ·  NO CAPACITY DATA",
        EmptyStateLine: DeckHangarCapacityApplyState.NoCapacityDataLine,
        HasCapacityData: false);
}
