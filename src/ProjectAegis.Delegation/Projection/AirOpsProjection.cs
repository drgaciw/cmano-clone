namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Glossary;

/// <summary>
/// Pure projection of unit readiness into Air Ops rows (CMD-24 Phase A).
/// No Unity / orchestrator dependency — callers supply simple tuples.
/// Phase N (LOG-08 timers/launch/abort FSM) is intentionally out of scope.
/// </summary>
public static class AirOpsProjection
{
    public const string StatusReady = "READY";
    public const string MissingLabel = "—";

    /// <summary>Status when <see cref="AirOpsEntry.ReadyForLaunch"/> is false — matches engage abort naming.</summary>
    public static string StatusNotReady { get; } =
        $"NOT READY · {AbortReasonCatalog.Engage.AIR_NOT_READY}";

    /// <summary>Refusal code aligned with engage / validation gate (doc 16 LOG-04).</summary>
    public static string AirNotReadyCode { get; } = AbortReasonCatalog.Engage.AIR_NOT_READY;

    /// <summary>
    /// Project air-ops rows from readiness tuples.
    /// <paramref name="rows"/> items: unitId, readyForLaunch, optional platform type, optional host.
    /// </summary>
    public static IReadOnlyList<AirOpsEntry> Project(
        IReadOnlyList<(string unitId, bool ready, string? platformType, string? host)> rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<AirOpsEntry>();
        }

        var result = new List<AirOpsEntry>(rows.Count);
        foreach (var row in rows
                     .Where(r => !string.IsNullOrWhiteSpace(r.unitId))
                     .OrderBy(r => r.unitId, StringComparer.Ordinal))
        {
            result.Add(ToEntry(row.unitId, row.ready, row.platformType, row.host));
        }

        return result;
    }

    /// <summary>
    /// Walk unit ids with a readiness lookup (host / session wiring without full orchestrator).
    /// </summary>
    public static IReadOnlyList<AirOpsEntry> Project(
        IEnumerable<string> unitIds,
        Func<string, bool> readyForLaunch,
        Func<string, string?>? platformType = null,
        Func<string, string?>? host = null)
    {
        if (unitIds is null)
        {
            return Array.Empty<AirOpsEntry>();
        }

        var list = unitIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        if (list.Count == 0)
        {
            return Array.Empty<AirOpsEntry>();
        }

        var result = new List<AirOpsEntry>(list.Count);
        foreach (var id in list)
        {
            result.Add(ToEntry(
                id,
                readyForLaunch(id),
                platformType?.Invoke(id),
                host?.Invoke(id)));
        }

        return result;
    }

    /// <summary>Aggregate ready count + summary line for group / header status.</summary>
    public static AirOpsAggregate Aggregate(IReadOnlyList<AirOpsEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return AirOpsAggregate.Empty;
        }

        var ready = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].ReadyForLaunch)
            {
                ready++;
            }
        }

        var total = entries.Count;
        return new AirOpsAggregate(ready, total, FormatSummaryLine(ready, total));
    }

    public static string FormatSummaryLine(int readyCount, int totalCount) =>
        $"READY {readyCount}/{totalCount}";

    private static AirOpsEntry ToEntry(
        string unitId,
        bool ready,
        string? platformType,
        string? host)
    {
        var platform = string.IsNullOrWhiteSpace(platformType) ? MissingLabel : platformType!.Trim();
        var hostLabel = string.IsNullOrWhiteSpace(host) ? MissingLabel : host!.Trim();
        var status = ready ? StatusReady : StatusNotReady;
        var refusal = ready ? null : AirNotReadyCode;
        return new AirOpsEntry(
            UnitId: unitId.Trim(),
            PlatformTypeLabel: platform,
            HostLabel: hostLabel,
            ReadyForLaunch: ready,
            StatusLine: status,
            RefusalCode: refusal);
    }
}

/// <summary>Aggregate ready count for Air Ops group status (CMD-24 Phase A).</summary>
public sealed record AirOpsAggregate(int ReadyCount, int TotalCount, string SummaryLine)
{
    public static AirOpsAggregate Empty { get; } = new(0, 0, "READY 0/0");
}
