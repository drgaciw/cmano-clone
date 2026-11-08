namespace ProjectAegis.Data.Telemetry;

/// <summary>
/// S29-10: advisory-only balance drift evaluation for catalog import/approve diff entity sets.
/// Never mutates catalog state and never bypasses <see cref="WriteGate.IWriteGate"/>.
/// </summary>
public static class CatalogBalanceDriftPipelineEvaluator
{
    public static BalanceDriftReport EvaluateForDiff(
        CatalogBalanceDriftPipelineSettings? settings,
        IReadOnlyList<string> diffEntityIds)
    {
        var effective = settings ?? CatalogBalanceDriftPipelineSettings.Disabled;
        if (!effective.EnableBalanceDrift)
        {
            return BalanceDriftReport.EmptyDisabled;
        }

        if (diffEntityIds is null || diffEntityIds.Count == 0)
        {
            return new BalanceDriftReport(
                DriftDetectionEnabled: true,
                Findings: Array.Empty<BalanceDriftFinding>(),
                StateHash: BalanceTelemetryGoldenHashes.EmptyState);
        }

        var sink = ResolveSink(effective);
        var fullReport = sink.EvaluateDrift();
        var entitySet = diffEntityIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        if (entitySet.Count == 0)
        {
            return new BalanceDriftReport(
                DriftDetectionEnabled: true,
                Findings: Array.Empty<BalanceDriftFinding>(),
                StateHash: fullReport.StateHash);
        }

        var filtered = fullReport.Findings
            .Where(f => entitySet.Contains(f.EntityId))
            .OrderBy(f => f.EntityId, StringComparer.Ordinal)
            .ThenBy(f => (int)f.EntityKind)
            .ThenBy(f => f.Code, StringComparer.Ordinal)
            .ToArray();

        return new BalanceDriftReport(
            DriftDetectionEnabled: true,
            Findings: filtered,
            StateHash: filtered.Length > 0 ? fullReport.StateHash : BalanceTelemetryGoldenHashes.EmptyState);
    }

    public static IReadOnlyList<string> FormatAdvisoryNotes(BalanceDriftReport report)
    {
        if (!report.DriftDetectionEnabled || report.Findings.Count == 0)
        {
            return [];
        }

        return report.Findings
            .Select(f => $"balance_drift_advisory:{f.EntityId}:{f.Code}:{f.Message}")
            .ToArray();
    }

    private static IBalanceTelemetrySink ResolveSink(CatalogBalanceDriftPipelineSettings settings) =>
        settings.TelemetrySink
        ?? BalanceTelemetrySinkFactory.Create(
            new BalanceDriftFeatureFlags { EnableBalanceDrift = true },
            settings.Options);
}