namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Comms;

/// <summary>One off-grid unit row for the map / unit panel (C3-01 / DRG-390). Read-only projection.</summary>
public readonly record struct OffGridUnitRow(
    string UnitId,
    ulong OffGridSinceTick,
    bool HasLastReport,
    ulong LastReportTick,
    double LastLatitudeDeg,
    double LastLongitudeDeg,
    string Label);

/// <summary>
/// Projects Sim comms-grid state for presentation. The UI shows the last-reported position for
/// off-grid units; it never computes grid membership itself.
/// </summary>
public static class CommsGridProjection
{
    public const string OffGridLabelPrefix = "OFF GRID";

    public static IReadOnlyList<OffGridUnitRow> Project(CommsGridRegistry? registry, ulong simTick)
    {
        if (registry == null)
        {
            return Array.Empty<OffGridUnitRow>();
        }

        var ids = registry.OffGridUnitIds;
        if (ids.Count == 0)
        {
            return Array.Empty<OffGridUnitRow>();
        }

        var rows = new OffGridUnitRow[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            registry.TryGetOffGridSince(id, out var since);
            var hasReport = registry.TryGetLastReport(id, out var report);
            var label = hasReport
                ? $"{OffGridLabelPrefix} — last report T-{simTick - Math.Min(simTick, report.ReportedAtTick)}"
                : $"{OffGridLabelPrefix} — no position report";
            rows[i] = new OffGridUnitRow(
                hasReport ? report.UnitId : id,
                since,
                hasReport,
                hasReport ? report.ReportedAtTick : 0,
                hasReport ? report.LatitudeDeg : 0,
                hasReport ? report.LongitudeDeg : 0,
                label);
        }

        return rows;
    }

    /// <summary>Stable one-line message-log text for a grid change.</summary>
    public static string FormatChange(CommsGridChange change) =>
        string.IsNullOrEmpty(change.Reason)
            ? $"T{change.SimTick} {change.UnitId} {change.From}→{change.To}"
            : $"T{change.SimTick} {change.UnitId} {change.From}→{change.To} ({change.Reason})";
}
