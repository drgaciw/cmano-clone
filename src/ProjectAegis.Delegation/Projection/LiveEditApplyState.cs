namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

/// <summary>
/// Headless apply path for live-edit findings presentation (CMD-35).
/// Maps <see cref="ValidationReport"/> + mutation outcome into bindable fields;
/// Unity hosts must not re-derive severity counts or status lines.
/// </summary>
public static class LiveEditApplyState
{
    /// <summary>
    /// Projects a validation report and last mutation outcome into presentation state.
    /// </summary>
    /// <param name="report">Live validation report, or null when none yet.</param>
    /// <param name="editVersion">Current scenario edit version token.</param>
    /// <param name="lastMutationOk">True when the last mutation committed successfully.</param>
    /// <param name="lastReason">Mutation error code/message when <paramref name="lastMutationOk"/> is false.</param>
    public static LiveEditSessionPresentation FromValidationReport(
        ValidationReport? report,
        int editVersion,
        bool lastMutationOk,
        string? lastReason)
    {
        var findings = MapFindings(report);
        var blocking = 0;
        var warnings = 0;
        foreach (var f in findings)
        {
            if (string.Equals(f.Severity, "Error", StringComparison.Ordinal))
            {
                blocking++;
            }
            else if (string.Equals(f.Severity, "Warning", StringComparison.Ordinal))
            {
                warnings++;
            }
        }

        var statusLine = BuildStatusLine(editVersion, blocking, warnings, lastMutationOk, lastReason, findings);

        return new LiveEditSessionPresentation(
            EditVersion: editVersion,
            BlockingCount: blocking,
            WarningCount: warnings,
            Findings: findings,
            LastMutationOk: lastMutationOk,
            LastMutationReason: lastMutationOk ? null : lastReason,
            StatusLine: statusLine);
    }

    /// <summary>
    /// Projects a <see cref="ScenarioMutationResult"/> (wraps bus output; does not re-validate).
    /// </summary>
    public static LiveEditSessionPresentation FromMutationResult(ScenarioMutationResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var reason = result.Ok
            ? null
            : result.ErrorCode ?? result.ErrorMessage;
        return FromValidationReport(result.Report, result.EditVersion, result.Ok, reason);
    }

    private static IReadOnlyList<LiveEditFindingEntry> MapFindings(ValidationReport? report)
    {
        if (report is null || report.Findings is null || report.Findings.Count == 0)
        {
            return Array.Empty<LiveEditFindingEntry>();
        }

        var list = new List<LiveEditFindingEntry>(report.Findings.Count);
        foreach (var f in report.Findings)
        {
            if (f is null)
            {
                continue;
            }

            list.Add(new LiveEditFindingEntry(
                Code: f.Code ?? string.Empty,
                Severity: MapSeverity(f.Severity),
                Message: f.Message ?? string.Empty,
                Path: BuildPath(f)));
        }

        return list;
    }

    /// <summary>Maps <see cref="ValidationSeverity"/> to stable presentation strings.</summary>
    public static string MapSeverity(ValidationSeverity severity) =>
        severity switch
        {
            ValidationSeverity.Error => "Error",
            ValidationSeverity.Warning => "Warning",
            _ => "Info",
        };

    /// <summary>
    /// Builds a locator path from entity ids on the finding (mission / unit / target).
    /// Validation findings do not carry a free-form path field.
    /// </summary>
    public static string? BuildPath(ValidationFinding finding)
    {
        if (finding is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(finding.MissionId))
        {
            return $"mission/{finding.MissionId}";
        }

        if (!string.IsNullOrEmpty(finding.UnitId))
        {
            return $"unit/{finding.UnitId}";
        }

        if (!string.IsNullOrEmpty(finding.TargetId))
        {
            return $"target/{finding.TargetId}";
        }

        if (finding.Data is not null
            && finding.Data.TryGetValue("path", out var dataPath)
            && !string.IsNullOrEmpty(dataPath))
        {
            return dataPath;
        }

        return null;
    }

    private static string BuildStatusLine(
        int editVersion,
        int blocking,
        int warnings,
        bool lastMutationOk,
        string? lastReason,
        IReadOnlyList<LiveEditFindingEntry> findings)
    {
        if (!lastMutationOk)
        {
            var code = string.IsNullOrWhiteSpace(lastReason) ? "MUTATION_FAILED" : lastReason!.Trim();
            if (code.Length > 48)
            {
                code = code[..48];
            }

            return $"LIVE EDIT BLOCKED: {code}";
        }

        // Successful mutation with blocking findings: surface first error code for action gate parity.
        if (blocking > 0)
        {
            var firstCode = findings
                .FirstOrDefault(f => string.Equals(f.Severity, "Error", StringComparison.Ordinal))
                ?.Code;
            if (!string.IsNullOrEmpty(firstCode))
            {
                return $"LIVE EDIT BLOCKED: {firstCode}";
            }
        }

        return $"LIVE EDIT v{editVersion} · {blocking} blocking · {warnings} warnings";
    }
}
