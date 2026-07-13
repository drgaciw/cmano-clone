namespace ProjectAegis.Data.Import;

using ProjectAegis.Data.Telemetry;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// S31-09: advisory-only balance drift evaluation for nightly approve summary JSON.
/// Never mutates catalog state and never bypasses <see cref="WriteGate.IWriteGate"/>.
/// </summary>
public static class NightlyApproveBalanceDriftSummary
{
    public static BalanceDriftReport EvaluateForBatch(
        string databasePath,
        string batchId,
        CatalogBalanceDriftPipelineSettings? settings,
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        if (string.IsNullOrWhiteSpace(batchId))
        {
            return BalanceDriftReport.EmptyDisabled;
        }

        var effective = settings ?? CatalogBalanceDriftPipelineSettings.Disabled;
        if (!effective.EnableBalanceDrift)
        {
            return BalanceDriftReport.EmptyDisabled;
        }

        using var gate = new CatalogWriteGate(databasePath, clock ?? new FixedCatalogClock(31090));
        var entityIds = gate.ListStagingEntityIds(batchId);
        return CatalogBalanceDriftPipelineEvaluator.EvaluateForDiff(effective, entityIds);
    }

    public static BalanceDriftReport EvaluateForBatches(
        string databasePath,
        IReadOnlyList<string> batchIds,
        CatalogBalanceDriftPipelineSettings? settings,
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        var effective = settings ?? CatalogBalanceDriftPipelineSettings.Disabled;
        if (!effective.EnableBalanceDrift)
        {
            return BalanceDriftReport.EmptyDisabled;
        }

        if (batchIds is null || batchIds.Count == 0)
        {
            return new BalanceDriftReport(
                DriftDetectionEnabled: true,
                Findings: Array.Empty<BalanceDriftFinding>(),
                StateHash: BalanceTelemetryGoldenHashes.EmptyState);
        }

        var entityIds = new SortedSet<string>(StringComparer.Ordinal);
        using (var gate = new CatalogWriteGate(databasePath, clock ?? new FixedCatalogClock(31091)))
        {
            foreach (var batchId in batchIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
            {
                foreach (var entityId in gate.ListStagingEntityIds(batchId))
                {
                    entityIds.Add(entityId);
                }
            }
        }

        return CatalogBalanceDriftPipelineEvaluator.EvaluateForDiff(effective, entityIds.ToArray());
    }

    public static NightlyApproveBalanceDriftAdvisoryDto ToDto(BalanceDriftReport report)
    {
        if (!report.DriftDetectionEnabled)
        {
            return NightlyApproveBalanceDriftAdvisoryDto.Disabled;
        }

        return new NightlyApproveBalanceDriftAdvisoryDto(
            DriftDetectionEnabled: true,
            FindingCount: report.Findings.Count,
            Findings: report.Findings
                .Select(ToFindingDto)
                .ToArray(),
            StateHash: report.StateHash,
            AdvisoryNotes: CatalogBalanceDriftPipelineEvaluator.FormatAdvisoryNotes(report));
    }

    public static NightlyApproveBalanceDriftAdvisoryDto? ToDtoOrNull(BalanceDriftReport report) =>
        report.DriftDetectionEnabled ? ToDto(report) : null;

    private static NightlyApproveBalanceDriftFindingDto ToFindingDto(BalanceDriftFinding finding) =>
        new(
            finding.Code,
            finding.EntityId,
            finding.EntityKind.ToString(),
            finding.SampleRuns,
            finding.ExpectedWinRate,
            finding.ActualWinRate,
            finding.DriftDelta,
            finding.Message);
}

public sealed record NightlyApproveBalanceDriftAdvisoryDto(
    bool DriftDetectionEnabled,
    int FindingCount,
    IReadOnlyList<NightlyApproveBalanceDriftFindingDto> Findings,
    string StateHash,
    IReadOnlyList<string> AdvisoryNotes)
{
    public static readonly NightlyApproveBalanceDriftAdvisoryDto Disabled = new(
        DriftDetectionEnabled: false,
        FindingCount: 0,
        Findings: Array.Empty<NightlyApproveBalanceDriftFindingDto>(),
        StateHash: BalanceTelemetryGoldenHashes.EmptyState,
        AdvisoryNotes: Array.Empty<string>());
}

public sealed record NightlyApproveBalanceDriftFindingDto(
    string Code,
    string EntityId,
    string EntityKind,
    int SampleRuns,
    double ExpectedWinRate,
    double ActualWinRate,
    double DriftDelta,
    string Message);