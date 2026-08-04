namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for Air Ops presentation (CMD-24 Phase A + Phase N lifecycle).
/// Unity hosts bind <see cref="AirOpsPresentation.Lines"/> onto Labels without re-formatting.
/// </summary>
public static class AirOpsApplyState
{
    /// <summary>Honest empty-state when session has no <c>UnitReadiness</c> map.</summary>
    public const string NoReadinessDataLine = "NO READINESS DATA";

    public static AirOpsPresentation Apply(IReadOnlyList<AirOpsEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return AirOpsPresentation.Empty;
        }

        var aggregate = AirOpsProjection.Aggregate(entries);
        var rows = new List<AirOpsDisplayRow>(entries.Count);
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            var line = FormatLine(entry);
            lines.Add(line);
            rows.Add(new AirOpsDisplayRow(
                entry.UnitId,
                entry.PlatformTypeLabel,
                entry.HostLabel,
                entry.ReadyForLaunch,
                entry.StatusLine,
                entry.RefusalCode,
                line,
                entry.PhaseLabel,
                entry.TimeToReadyTicks,
                entry.CanLaunch,
                entry.CanAbort,
                entry.LaunchDisabledReason));
        }

        return new AirOpsPresentation(
            rows,
            lines,
            aggregate,
            HeaderLine: $"AIR OPS  ·  {aggregate.SummaryLine}",
            EmptyStateLine: string.Empty,
            HasReadinessData: true);
    }

    /// <summary>
    /// Apply when the host knows whether readiness data exists.
    /// Missing readiness yields honest <see cref="NoReadinessDataLine"/> rather than fabricated ready rows.
    /// </summary>
    public static AirOpsPresentation Apply(
        IReadOnlyList<AirOpsEntry>? entries,
        bool hasReadinessData)
    {
        if (!hasReadinessData)
        {
            return AirOpsPresentation.NoReadinessData;
        }

        return Apply(entries);
    }

    public static string FormatLine(AirOpsEntry entry)
    {
        var platform = string.IsNullOrWhiteSpace(entry.PlatformTypeLabel)
            ? AirOpsProjection.MissingLabel
            : entry.PlatformTypeLabel;
        var host = string.IsNullOrWhiteSpace(entry.HostLabel)
            ? AirOpsProjection.MissingLabel
            : entry.HostLabel;
        var phase = string.IsNullOrWhiteSpace(entry.PhaseLabel)
            ? AirOpsProjection.MissingLabel
            : entry.PhaseLabel;
        return
            $"{entry.UnitId}  type={platform}  host={host}  phase={phase}  eta={entry.TimeToReadyTicks}  [{entry.StatusLine}]";
    }
}

/// <summary>One bound list row for the Air Ops ListView (Phase A + Phase N flags).</summary>
public sealed record AirOpsDisplayRow(
    string UnitId,
    string PlatformTypeLabel,
    string HostLabel,
    bool ReadyForLaunch,
    string StatusLine,
    string? RefusalCode,
    string DisplayLine,
    string PhaseLabel = "OnGround",
    int TimeToReadyTicks = 0,
    bool CanLaunch = false,
    bool CanAbort = false,
    string? LaunchDisabledReason = null);

/// <summary>Presentation bundle for hosts / tests.</summary>
public sealed record AirOpsPresentation(
    IReadOnlyList<AirOpsDisplayRow> Rows,
    IReadOnlyList<string> Lines,
    AirOpsAggregate Aggregate,
    string HeaderLine,
    string EmptyStateLine,
    bool HasReadinessData)
{
    public static AirOpsPresentation Empty { get; } = new(
        Array.Empty<AirOpsDisplayRow>(),
        Array.Empty<string>(),
        AirOpsAggregate.Empty,
        HeaderLine: "AIR OPS  ·  READY 0/0",
        EmptyStateLine: string.Empty,
        HasReadinessData: true);

    /// <summary>Honest zero-state when session has no UnitReadiness map.</summary>
    public static AirOpsPresentation NoReadinessData { get; } = new(
        Array.Empty<AirOpsDisplayRow>(),
        Array.Empty<string>(),
        AirOpsAggregate.Empty,
        HeaderLine: "AIR OPS  ·  NO READINESS DATA",
        EmptyStateLine: AirOpsApplyState.NoReadinessDataLine,
        HasReadinessData: false);
}
