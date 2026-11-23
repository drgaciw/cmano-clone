namespace ProjectAegis.Delegation.Resilience;

using System.Globalization;
using System.Text.Json;
using Data.Evidence;

/// <summary>Summary section from <c>tools/qa-gauntlet/saboteur.py</c> <c>report.json</c>.</summary>
public sealed record SaboteurSummary(
    int Caught,
    int Survived,
    int Invalid,
    int CaughtDefects,
    int SurvivedDefects,
    string KillRate);

/// <summary>Per-mutant calibration outcome row from <c>saboteur.py</c> <c>report.json</c>.</summary>
public sealed record SaboteurMutantResult(
    string Id,
    string Role,
    IReadOnlyList<string> ExpectedOracles,
    IReadOnlyList<string> FiredOracles,
    string Outcome,
    string? Mode = null);

/// <summary>Parsed <c>saboteur.py</c> calibration report.</summary>
public sealed record SaboteurCalibrationReport(
    SaboteurSummary Summary,
    IReadOnlyList<SaboteurMutantResult> Results);

/// <summary>Fail-closed VER-05 evaluation of a <see cref="SaboteurCalibrationReport"/>.</summary>
public sealed record SaboteurCalibrationEvaluation(
    SaboteurCalibrationReport Report,
    bool Passed,
    IReadOnlyList<string> Failures,
    IReadOnlyList<DefectFollowUpLink> DefectFollowUps);

/// <summary>
/// DRG-203 / VER-05: parses <c>tools/qa-gauntlet/saboteur.py</c> JSON calibration reports,
/// enforces the role-driven exit matrix (failing closed on invalid mutants, caught controls or
/// expected-misses, and survived defect blind spots), emits <see cref="DefectFollowUpLink"/>s,
/// and records the result into <see cref="EvidenceLedger"/>.
/// </summary>
public static class SaboteurCalibrationLedgerRecorder
{
    /// <summary>Parses a <c>saboteur.py</c> <c>report.json</c> payload (<c>netstandard2.1</c>-safe).</summary>
    public static SaboteurCalibrationReport ParseReportJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Saboteur report JSON is required.", nameof(json));
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Saboteur report root must be a JSON object.");
        }

        if (!root.TryGetProperty("summary", out var summaryEl) || summaryEl.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Saboteur report missing 'summary' object.");
        }

        if (!root.TryGetProperty("results", out var resultsEl) || resultsEl.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Saboteur report missing 'results' array.");
        }

        var summary = new SaboteurSummary(
            Caught: ReadRequiredInt(summaryEl, "caught"),
            Survived: ReadRequiredInt(summaryEl, "survived"),
            Invalid: ReadRequiredInt(summaryEl, "invalid"),
            CaughtDefects: ReadRequiredInt(summaryEl, "caughtDefects"),
            SurvivedDefects: ReadRequiredInt(summaryEl, "survivedDefects"),
            KillRate: ReadRequiredString(summaryEl, "killRate"));

        var results = new List<SaboteurMutantResult>();
        foreach (var item in resultsEl.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Saboteur result entry must be a JSON object.");
            }

            results.Add(new SaboteurMutantResult(
                Id: ReadString(item, "id") ?? string.Empty,
                Role: ReadString(item, "role") ?? string.Empty,
                ExpectedOracles: ReadStringArray(item, "expectedOracles"),
                FiredOracles: ReadStringArray(item, "firedOracles"),
                Outcome: ReadString(item, "outcome") ?? string.Empty,
                Mode: ReadString(item, "mode")));
        }

        return new SaboteurCalibrationReport(summary, results);
    }

    /// <summary>Enforces <c>saboteur.py:exit_code_for</c> and <c>VER-05</c> blind-spot tracking rules.</summary>
    public static SaboteurCalibrationEvaluation Evaluate(SaboteurCalibrationReport report)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        var failures = new List<string>();
        var followUps = new List<DefectFollowUpLink>();

        if (report.Results.Count == 0)
        {
            failures.Add("Saboteur calibration report contains no mutant results.");
        }

        if (report.Summary.Invalid > 0)
        {
            failures.Add($"Invalid mutants present: {report.Summary.Invalid}.");
        }

        var caught = 0;
        var survived = 0;
        var invalid = 0;
        var caughtDefects = 0;
        var survivedDefects = 0;

        for (var i = 0; i < report.Results.Count; i++)
        {
            var r = report.Results[i];
            if (string.IsNullOrWhiteSpace(r.Id))
            {
                failures.Add($"Mutant result at index {i} is missing id.");
            }

            switch (r.Outcome)
            {
                case "caught":
                    caught++;
                    break;
                case "survived":
                    survived++;
                    break;
                case "invalid-mutant":
                    invalid++;
                    failures.Add($"Mutant '{r.Id}' is invalid-mutant.");
                    break;
                default:
                    failures.Add($"Mutant '{r.Id}' has unsupported outcome '{r.Outcome}'.");
                    break;
            }

            if (string.Equals(r.Role, "control", StringComparison.Ordinal))
            {
                if (string.Equals(r.Outcome, "caught", StringComparison.Ordinal))
                {
                    failures.Add($"Control mutant '{r.Id}' was caught (false positive).");
                }
            }
            else if (string.Equals(r.Role, "expected-miss", StringComparison.Ordinal))
            {
                if (string.Equals(r.Outcome, "caught", StringComparison.Ordinal))
                {
                    failures.Add($"Expected-miss mutant '{r.Id}' was caught (promote role to defect).");
                }
            }
            else if (string.Equals(r.Role, "defect", StringComparison.Ordinal))
            {
                if (string.Equals(r.Outcome, "caught", StringComparison.Ordinal))
                {
                    caughtDefects++;
                }
                else if (string.Equals(r.Outcome, "survived", StringComparison.Ordinal))
                {
                    survivedDefects++;
                    failures.Add($"Defect mutant '{r.Id}' survived (oracle blind spot).");
                    followUps.Add(new DefectFollowUpLink(
                        IssueId: $"SABOTAGE-{r.Id}",
                        Title: $"Oracle blind spot: defect mutant {r.Id} survived",
                        Status: "Open",
                        Notes: $"Expected oracles: [{string.Join(", ", r.ExpectedOracles)}]; mode={r.Mode ?? "classic"}"));
                }
            }
            else
            {
                failures.Add($"Mutant '{r.Id}' has unknown role '{r.Role}'.");
            }
        }

        if (caught != report.Summary.Caught
            || survived != report.Summary.Survived
            || invalid != report.Summary.Invalid
            || caughtDefects != report.Summary.CaughtDefects
            || survivedDefects != report.Summary.SurvivedDefects)
        {
            failures.Add("Saboteur summary counts do not match individual mutant result totals.");
        }

        var expectedKillRate = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1}",
            caughtDefects,
            caughtDefects + survivedDefects);
        if (!string.Equals(report.Summary.KillRate, expectedKillRate, StringComparison.Ordinal))
        {
            failures.Add($"Saboteur summary killRate '{report.Summary.KillRate}' does not match '{expectedKillRate}'.");
        }

        return new SaboteurCalibrationEvaluation(
            Report: report,
            Passed: failures.Count == 0,
            Failures: failures,
            DefectFollowUps: followUps);
    }

    /// <summary>Parses, evaluates, and records a <c>saboteur.py</c> calibration report into <see cref="EvidenceLedger"/>.</summary>
    public static EvidenceLedger Record(
        string ledgerId,
        string scenarioId,
        int seed,
        string reportJson,
        RevisionIdentity revision,
        InputDigestManifest inputs,
        string artifactLocation,
        string artifactSha256,
        string collectedAtUtc,
        string executionNote = "curated-fixture")
    {
        if (revision is null)
        {
            throw new ArgumentNullException(nameof(revision));
        }

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        var report = ParseReportJson(reportJson);
        var eval = Evaluate(report);
        var entries = new List<MissionThreadEvidenceRecord>(report.Results.Count + 1);
        var scenarios = new List<string>(report.Results.Count);

        for (var i = 0; i < report.Results.Count; i++)
        {
            var r = report.Results[i];
            var seq = (i + 1).ToString(CultureInfo.InvariantCulture);
            scenarios.Add($"{r.Id}:{r.Role}:{r.Outcome}");
            entries.Add(new MissionThreadEvidenceRecord(
                EvidenceId: $"{scenarioId}:{i}:{r.Id}:assess",
                Kind: MissionThreadEvidenceKind.Assessment,
                SimTick: (ulong)(i + 1),
                SimTime: i + 1.0,
                SequenceRef: seq,
                AuthoritativeSource: "SaboteurCalibrationLedgerRecorder:Mutant",
                Assessment: new AssessmentEvidenceFact(
                    ContactId: r.Id,
                    TargetId: r.Role,
                    AssessmentState: r.Outcome,
                    DamageLevel: $"fired={string.Join(",", r.FiredOracles)}",
                    PreviousHpPct: null,
                    NewHpPct: null,
                    SourceKind: r.Mode ?? "classic")));
        }

        entries.Add(new MissionThreadEvidenceRecord(
            EvidenceId: $"{scenarioId}:summary:auth",
            Kind: MissionThreadEvidenceKind.Authority,
            SimTick: (ulong)(report.Results.Count + 1),
            SimTime: report.Results.Count + 1.0,
            SequenceRef: (report.Results.Count + 1).ToString(CultureInfo.InvariantCulture),
            AuthoritativeSource: "SaboteurCalibrationLedgerRecorder:Summary",
            Authority: new AuthorityEvidenceFact(
                ActorId: "saboteur",
                TargetId: executionNote,
                RoeLevel: $"killRate={report.Summary.KillRate}",
                Disposition: eval.Passed ? "Clean" : "Failed",
                ReasonCode: eval.Passed ? "VER-05_CLEAN" : string.Join("; ", eval.Failures),
                PolicySnapshotId: null,
                RequiredApproval: null)));

        var acceptance = new RevisionBoundAcceptanceRecord(
            CriterionId: "VER-05",
            RequirementId: "VER-05.1..3",
            Revision: revision,
            Inputs: inputs,
            Layer: AcceptanceEvidenceLayer.Headless,
            RequiredLayer: AcceptanceEvidenceLayer.Headless,
            ExecutionCommandOrSteps: $"tools/qa-gauntlet/saboteur.py ({executionNote})",
            Result: eval.Passed ? "PASS" : "FAIL",
            ArtifactLocation: artifactLocation,
            ArtifactSha256: artifactSha256,
            CollectedAtUtc: collectedAtUtc,
            Counts: new FloorVsObservedCounts(
                DeclaredFloor: report.Summary.CaughtDefects + report.Summary.SurvivedDefects,
                ObservedPassed: report.Summary.CaughtDefects,
                ObservedFailed: eval.Failures.Count,
                ObservedSkipped: 0),
            OwnerAcceptance: new OwnerAcceptanceDecision(
                OwnerAcceptanceStatus.Pending,
                Owner: null,
                DecisionDateUtc: null,
                DecisionReference: null,
                AcceptedRevision: null,
                AcceptedInputs: null),
            Notes: $"KillRate={report.Summary.KillRate}; mode={executionNote}.");

        var oracle = new OracleRunCorrelation(
            Profile: "saboteur-calibration",
            Passed: eval.Passed,
            Scenarios: scenarios,
            Failures: eval.Failures,
            Warnings: Array.Empty<string>());

        return EvidenceLedgerBuilder.Build(
            ledgerId: ledgerId,
            scenarioId: scenarioId,
            seed: seed,
            entries: entries,
            oracleEvaluation: oracle,
            defectFollowUps: eval.DefectFollowUps,
            acceptanceRecords: new[] { acceptance });
    }

    private static int ReadRequiredInt(JsonElement obj, string propName)
    {
        if (!obj.TryGetProperty(propName, out var el)
            || el.ValueKind != JsonValueKind.Number
            || !el.TryGetInt32(out var val))
        {
            throw new JsonException($"Saboteur summary missing or invalid integer property '{propName}'.");
        }

        return val;
    }

    private static string ReadRequiredString(JsonElement obj, string propName)
    {
        if (!obj.TryGetProperty(propName, out var el) || el.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Saboteur summary missing or invalid string property '{propName}'.");
        }

        var value = el.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Saboteur summary string property '{propName}' must not be blank.");
        }

        return value!;
    }

    private static string? ReadString(JsonElement obj, string propName) =>
        obj.TryGetProperty(propName, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static IReadOnlyList<string> ReadStringArray(JsonElement obj, string propName)
    {
        if (!obj.TryGetProperty(propName, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var list = new List<string>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString();
                if (!string.IsNullOrEmpty(s))
                {
                    list.Add(s);
                }
            }
        }

        return list;
    }
}
