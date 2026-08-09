namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for magazine loadout presentation (CMD-24 loadout depth).
/// Unity hosts bind <see cref="MagazineLoadoutPresentation.Lines"/> onto Labels without re-formatting.
/// </summary>
public static class MagazineLoadoutApplyState
{
    /// <summary>Honest empty-state when session has no magazine feed.</summary>
    public const string NoMagazineDataLine = "NO MAGAZINE DATA";

    public static MagazineLoadoutPresentation Apply(IReadOnlyList<MagazineLoadoutEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return MagazineLoadoutPresentation.Empty;
        }

        var aggregate = MagazineLoadoutProjection.Aggregate(entries);
        var rows = new List<MagazineLoadoutDisplayRow>(entries.Count);
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            var line = FormatLine(entry);
            lines.Add(line);
            rows.Add(new MagazineLoadoutDisplayRow(
                entry.UnitId,
                entry.PlatformId,
                entry.MountId,
                entry.WeaponId,
                entry.WeaponLabel,
                entry.Remaining,
                entry.Capacity,
                entry.FillPct,
                entry.StatusLine,
                line));
        }

        return new MagazineLoadoutPresentation(
            rows,
            lines,
            aggregate,
            HeaderLine: $"MAGAZINE  ·  {aggregate.SummaryLine}",
            EmptyStateLine: string.Empty,
            HasMagazineData: true);
    }

    /// <summary>
    /// Apply when the host knows whether magazine data exists.
    /// Missing data yields honest <see cref="NoMagazineDataLine"/> rather than fabricated stock.
    /// </summary>
    public static MagazineLoadoutPresentation Apply(
        IReadOnlyList<MagazineLoadoutEntry>? entries,
        bool hasMagazineData)
    {
        if (!hasMagazineData)
        {
            return MagazineLoadoutPresentation.NoMagazineData;
        }

        return Apply(entries);
    }

    public static string FormatLine(MagazineLoadoutEntry entry)
    {
        var weapon = string.IsNullOrWhiteSpace(entry.WeaponLabel)
            ? MagazineLoadoutProjection.MissingLabel
            : entry.WeaponLabel;
        var mount = string.IsNullOrWhiteSpace(entry.MountId)
            ? MagazineLoadoutProjection.MissingLabel
            : entry.MountId;
        var fill = entry.FillPct.ToString("0");
        return
            $"{entry.UnitId}  mount={mount}  weapon={weapon}  {entry.Remaining}/{entry.Capacity} ({fill}%)  [{entry.StatusLine}]";
    }

    /// <summary>Feasibility line for panel footer (how many airframes can arm from stock).</summary>
    public static string FormatFeasibilityLine(int remainingRounds, int roundsPerAirframe)
    {
        var n = MagazineLoadoutProjection.HowManyAirframesCanArm(remainingRounds, roundsPerAirframe);
        return $"ARMABLE AIRFRAMES  {n}  (stock={Math.Max(0, remainingRounds)}  cost={Math.Max(0, roundsPerAirframe)}/ac)";
    }
}

/// <summary>One bound list row for the magazine loadout ListView.</summary>
public sealed record MagazineLoadoutDisplayRow(
    string UnitId,
    string PlatformId,
    string MountId,
    string WeaponId,
    string WeaponLabel,
    int Remaining,
    int Capacity,
    double FillPct,
    string StatusLine,
    string DisplayLine);

/// <summary>Presentation bundle for hosts / tests.</summary>
public sealed record MagazineLoadoutPresentation(
    IReadOnlyList<MagazineLoadoutDisplayRow> Rows,
    IReadOnlyList<string> Lines,
    MagazineLoadoutAggregate Aggregate,
    string HeaderLine,
    string EmptyStateLine,
    bool HasMagazineData)
{
    public static MagazineLoadoutPresentation Empty { get; } = new(
        Array.Empty<MagazineLoadoutDisplayRow>(),
        Array.Empty<string>(),
        MagazineLoadoutAggregate.Empty,
        HeaderLine: "MAGAZINE  ·  OK 0  LOW 0  EMPTY 0  ·  0 mounts",
        EmptyStateLine: string.Empty,
        HasMagazineData: true);

    /// <summary>Honest zero-state when session has no magazine feed.</summary>
    public static MagazineLoadoutPresentation NoMagazineData { get; } = new(
        Array.Empty<MagazineLoadoutDisplayRow>(),
        Array.Empty<string>(),
        MagazineLoadoutAggregate.Empty,
        HeaderLine: "MAGAZINE  ·  NO MAGAZINE DATA",
        EmptyStateLine: MagazineLoadoutApplyState.NoMagazineDataLine,
        HasMagazineData: false);
}
