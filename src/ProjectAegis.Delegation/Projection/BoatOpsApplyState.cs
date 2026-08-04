namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for Boat Ops presentation (CMD-25 / LOG-09…11).
/// Unity hosts bind <see cref="BoatOpsPresentation.Lines"/> onto Labels without re-formatting.
/// </summary>
public static class BoatOpsApplyState
{
    /// <summary>Honest empty-state when session has no boat-ops map.</summary>
    public const string NoBoatOpsDataLine = "NO BOAT OPS DATA";

    public static BoatOpsPresentation Apply(IReadOnlyList<BoatOpsEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return BoatOpsPresentation.Empty;
        }

        var aggregate = BoatOpsProjection.Aggregate(entries);
        var rows = new List<BoatOpsDisplayRow>(entries.Count);
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            var line = FormatLine(entry);
            lines.Add(line);
            rows.Add(new BoatOpsDisplayRow(
                entry.CraftId,
                entry.HostLabel,
                entry.PhaseLabel,
                entry.StatusLine,
                entry.RefusalCode,
                line,
                entry.SeaStateDouglas,
                entry.MaxLaunchSeaState,
                entry.MaxRecoverySeaState,
                entry.SeaStateLaunchOk,
                entry.SeaStateRecoveryOk,
                entry.PersonnelEmbarked,
                entry.PersonnelCapacity,
                entry.CargoTons,
                entry.CargoCapacityTons,
                entry.TimeToReadyTicks,
                entry.CanLaunch,
                entry.CanRecover,
                entry.CanAbort,
                entry.LaunchDisabledReason,
                entry.RecoverDisabledReason));
        }

        var sea = entries[0].SeaStateDouglas;
        return new BoatOpsPresentation(
            rows,
            lines,
            aggregate,
            HeaderLine: $"BOAT OPS  ·  {aggregate.SummaryLine}  ·  sea={sea}",
            EmptyStateLine: string.Empty,
            HasBoatOpsData: true,
            SeaStateDouglas: sea);
    }

    /// <summary>
    /// Apply when the host knows whether boat-ops data exists.
    /// Missing data yields honest <see cref="NoBoatOpsDataLine"/> rather than fabricated rows.
    /// </summary>
    public static BoatOpsPresentation Apply(
        IReadOnlyList<BoatOpsEntry>? entries,
        bool hasBoatOpsData)
    {
        if (!hasBoatOpsData)
        {
            return BoatOpsPresentation.NoBoatOpsData;
        }

        return Apply(entries);
    }

    public static string FormatLine(BoatOpsEntry entry)
    {
        var host = string.IsNullOrWhiteSpace(entry.HostLabel)
            ? BoatOpsProjection.MissingLabel
            : entry.HostLabel;
        var phase = string.IsNullOrWhiteSpace(entry.PhaseLabel)
            ? BoatOpsProjection.MissingLabel
            : entry.PhaseLabel;
        var seaGate = entry.SeaStateLaunchOk
            ? (entry.SeaStateRecoveryOk ? "sea=OK" : "sea=LAUNCH-OK/RECOVERY-BLOCKED")
            : "sea=LAUNCH-BLOCKED";
        return
            $"{entry.CraftId}  host={host}  phase={phase}  eta={entry.TimeToReadyTicks}  " +
            $"{seaGate}  load={entry.PersonnelEmbarked}/{entry.PersonnelCapacity}p " +
            $"{entry.CargoTons:0.##}/{entry.CargoCapacityTons:0.##}t  [{entry.StatusLine}]";
    }
}

/// <summary>One bound list row for the Boat Ops ListView (CMD-25).</summary>
public sealed record BoatOpsDisplayRow(
    string CraftId,
    string HostLabel,
    string PhaseLabel,
    string StatusLine,
    string? RefusalCode,
    string DisplayLine,
    int SeaStateDouglas = 0,
    int MaxLaunchSeaState = 0,
    int MaxRecoverySeaState = 0,
    bool SeaStateLaunchOk = true,
    bool SeaStateRecoveryOk = true,
    int PersonnelEmbarked = 0,
    int PersonnelCapacity = 0,
    double CargoTons = 0,
    double CargoCapacityTons = 0,
    int TimeToReadyTicks = 0,
    bool CanLaunch = false,
    bool CanRecover = false,
    bool CanAbort = false,
    string? LaunchDisabledReason = null,
    string? RecoverDisabledReason = null);

/// <summary>Presentation bundle for hosts / tests.</summary>
public sealed record BoatOpsPresentation(
    IReadOnlyList<BoatOpsDisplayRow> Rows,
    IReadOnlyList<string> Lines,
    BoatOpsAggregate Aggregate,
    string HeaderLine,
    string EmptyStateLine,
    bool HasBoatOpsData,
    int SeaStateDouglas = 0)
{
    public static BoatOpsPresentation Empty { get; } = new(
        Array.Empty<BoatOpsDisplayRow>(),
        Array.Empty<string>(),
        BoatOpsAggregate.Empty,
        HeaderLine: "BOAT OPS  ·  READY 0/0  ·  sea=0",
        EmptyStateLine: string.Empty,
        HasBoatOpsData: true,
        SeaStateDouglas: 0);

    /// <summary>Honest zero-state when session has no boat-ops map.</summary>
    public static BoatOpsPresentation NoBoatOpsData { get; } = new(
        Array.Empty<BoatOpsDisplayRow>(),
        Array.Empty<string>(),
        BoatOpsAggregate.Empty,
        HeaderLine: "BOAT OPS  ·  NO BOAT OPS DATA",
        EmptyStateLine: BoatOpsApplyState.NoBoatOpsDataLine,
        HasBoatOpsData: false,
        SeaStateDouglas: 0);
}
