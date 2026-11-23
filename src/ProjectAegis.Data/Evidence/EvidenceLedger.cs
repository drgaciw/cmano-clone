using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectAegis.Data.Evidence;

/// <summary>Baseline context provenance matching <c>gauntlet-oracle.yml</c> schemaVersion 1.</summary>
public sealed record BaselineContextProvenance(
    int SchemaVersion,
    string CommitSha,
    int Seed,
    int Ticks,
    string? CatalogPath,
    string? CatalogSha256,
    IReadOnlyList<HashedInputFile> Policies,
    HashedInputFile? Results,
    string? LocalDeltaIdentity = null);

/// <summary>Post-batch oracle evaluation summary correlated in <see cref="EvidenceLedger"/>.</summary>
public sealed record OracleRunCorrelation(
    string Profile,
    bool Passed,
    IReadOnlyList<string> Scenarios,
    IReadOnlyList<string> Failures,
    IReadOnlyList<string> Warnings);

/// <summary>CI / Buildkite run correlation metadata.</summary>
public sealed record CiBuildCorrelation(
    string Provider,
    string PipelineOrWorkflow,
    string RunId,
    string? RunUrl,
    string Status);

/// <summary>Linked defect or follow-up tracking issue.</summary>
public sealed record DefectFollowUpLink(
    string IssueId,
    string Title,
    string Status,
    string? Notes = null);

/// <summary>
/// Unified, deterministic DRG-201 evidence ledger correlating scenario runs with replay,
/// oracle evaluation, CI/Buildkite runs, defect follow-ups, typed mission-thread records,
/// and revision-bound acceptance records.
/// </summary>
public sealed record EvidenceLedger(
    int SchemaVersion,
    string LedgerId,
    string ScenarioId,
    int Seed,
    string? ReplayFingerprint,
    string? ReplayWorldHash,
    string? CapabilityManifestRef,
    BaselineContextProvenance? BaselineContext,
    OracleRunCorrelation? OracleEvaluation,
    CiBuildCorrelation? CiCorrelation,
    IReadOnlyList<DefectFollowUpLink> DefectFollowUps,
    IReadOnlyList<MissionThreadEvidenceRecord> Entries,
    IReadOnlyList<RevisionBoundAcceptanceRecord> AcceptanceRecords,
    string ContentDigestSha256);

/// <summary>Canonical, order-invariant builder for <see cref="EvidenceLedger"/>.</summary>
public static class EvidenceLedgerBuilder
{
    /// <summary>Builds a canonically sorted and SHA-256 digested <see cref="EvidenceLedger"/>.</summary>
    public static EvidenceLedger Build(
        string ledgerId,
        string scenarioId,
        int seed,
        IEnumerable<MissionThreadEvidenceRecord> entries,
        string? replayFingerprint = null,
        string? replayWorldHash = null,
        string? capabilityManifestRef = null,
        BaselineContextProvenance? baselineContext = null,
        OracleRunCorrelation? oracleEvaluation = null,
        CiBuildCorrelation? ciCorrelation = null,
        IEnumerable<DefectFollowUpLink>? defectFollowUps = null,
        IEnumerable<RevisionBoundAcceptanceRecord>? acceptanceRecords = null)
    {
        if (string.IsNullOrWhiteSpace(ledgerId))
        {
            throw new ArgumentException("LedgerId is required.", nameof(ledgerId));
        }

        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException("ScenarioId is required.", nameof(scenarioId));
        }

        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var rawEntries = entries as ICollection<MissionThreadEvidenceRecord> ?? entries.ToArray();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in rawEntries)
        {
            if (entry is null)
            {
                throw new ArgumentException("Evidence entry cannot be null.", nameof(entries));
            }

            var validationErrors = entry.Validate();
            if (validationErrors.Count > 0)
            {
                throw new ArgumentException(
                    $"Invalid MissionThreadEvidenceRecord '{entry.EvidenceId}': {string.Join("; ", validationErrors)}",
                    nameof(entries));
            }

            if (!seenIds.Add(entry.EvidenceId))
            {
                throw new ArgumentException(
                    $"Duplicate EvidenceId '{entry.EvidenceId}' is not allowed in EvidenceLedger.",
                    nameof(entries));
            }
        }

        var sortedEntries = rawEntries
            .OrderBy(e => e.SimTick)
            .ThenBy(e => (int)e.Kind)
            .ThenBy(e => e.SequenceRef ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(e => e.EvidenceId, StringComparer.Ordinal)
            .ThenBy(e => e.AuthoritativeSource, StringComparer.Ordinal)
            .ToArray();

        var sortedDefects = (defectFollowUps ?? Array.Empty<DefectFollowUpLink>())
            .OrderBy(d => d.IssueId, StringComparer.Ordinal)
            .ThenBy(d => d.Title, StringComparer.Ordinal)
            .ThenBy(d => d.Status, StringComparer.Ordinal)
            .ThenBy(d => d.Notes ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

        var sortedAcceptance = (acceptanceRecords ?? Array.Empty<RevisionBoundAcceptanceRecord>())
            .OrderBy(a => a.CriterionId, StringComparer.Ordinal)
            .ThenBy(a => a.RequirementId, StringComparer.Ordinal)
            .ThenBy(a => a.CollectedAtUtc, StringComparer.Ordinal)
            .ThenBy(a => a.ArtifactSha256, StringComparer.Ordinal)
            .ThenBy(a => a.ArtifactLocation, StringComparer.Ordinal)
            .ThenBy(a => a.Result, StringComparer.Ordinal)
            .ThenBy(a => a.OwnerAcceptance?.Status ?? OwnerAcceptanceStatus.Pending)
            .ThenBy(a => a.Counts?.ObservedPassed ?? 0)
            .ToArray();

        var normalizedBaseline = baselineContext is null
            ? null
            : baselineContext with
            {
                Policies = (baselineContext.Policies ?? Array.Empty<HashedInputFile>())
                    .OrderBy(p => p.Path, StringComparer.Ordinal)
                    .ThenBy(p => p.Sha256, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
            };

        var normalizedOracle = oracleEvaluation is null
            ? null
            : oracleEvaluation with
            {
                Scenarios = (oracleEvaluation.Scenarios ?? Array.Empty<string>()).OrderBy(s => s, StringComparer.Ordinal).ToArray(),
                Failures = (oracleEvaluation.Failures ?? Array.Empty<string>()).ToArray(),
                Warnings = (oracleEvaluation.Warnings ?? Array.Empty<string>()).ToArray(),
            };

        var draft = new EvidenceLedger(
            SchemaVersion: MissionThreadEvidenceSchema.Version,
            LedgerId: ledgerId,
            ScenarioId: scenarioId,
            Seed: seed,
            ReplayFingerprint: replayFingerprint,
            ReplayWorldHash: replayWorldHash,
            CapabilityManifestRef: capabilityManifestRef,
            BaselineContext: normalizedBaseline,
            OracleEvaluation: normalizedOracle,
            CiCorrelation: ciCorrelation,
            DefectFollowUps: sortedDefects,
            Entries: sortedEntries,
            AcceptanceRecords: sortedAcceptance,
            ContentDigestSha256: string.Empty);

        var canonicalPayload = EvidenceLedgerSerializer.ToCanonicalJson(draft);
        var digest = EvidenceLedgerSerializer.ComputeSha256Hex(canonicalPayload);
        return draft with { ContentDigestSha256 = digest };
    }
}

/// <summary>Canonical JSON and SHA-256 serialization helpers (<c>netstandard2.1</c>-compatible).</summary>
public static class EvidenceLedgerSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        opts.Converters.Add(new JsonStringEnumConverter());
        return opts;
    }

    /// <summary>Serializes <paramref name="ledger"/> deterministically.</summary>
    public static string ToCanonicalJson(EvidenceLedger ledger)
    {
        if (ledger is null)
        {
            throw new ArgumentNullException(nameof(ledger));
        }

        return JsonSerializer.Serialize(ledger, JsonOptions);
    }

    /// <summary>Deserializes <see cref="EvidenceLedger"/> from JSON.</summary>
    public static EvidenceLedger FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        return JsonSerializer.Deserialize<EvidenceLedger>(json, JsonOptions)
            ?? throw new JsonException("Deserialized EvidenceLedger was null.");
    }

    /// <summary>Computes lowercase SHA-256 hex digest using <see cref="SHA256.Create()"/> (<c>netstandard2.1</c>-safe).</summary>
    public static string ComputeSha256Hex(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        using var sha = SHA256.Create();
        return ToHexLower(sha.ComputeHash(bytes));
    }

    /// <summary>Computes lowercase SHA-256 hex digest of a file on disk.</summary>
    public static string ComputeFileSha256Hex(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("File path is required.", nameof(path));
        }

        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return ToHexLower(sha.ComputeHash(stream));
    }

    /// <summary>Parses a <c>baseline-context.json</c> matching <c>.github/workflows/gauntlet-oracle.yml</c> schemaVersion 1.</summary>
    public static bool TryParseBaselineContext(
        string json,
        out BaselineContextProvenance? context,
        out IReadOnlyList<string> errors)
    {
        context = null;
        var err = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            err.Add("baseline-context JSON is empty.");
            errors = err;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                err.Add("baseline-context root must be a JSON object.");
                errors = err;
                return false;
            }

            var schemaVersion = root.TryGetProperty("schemaVersion", out var svEl) && svEl.TryGetInt32(out var sv) ? sv : 0;
            if (schemaVersion != 1)
            {
                err.Add($"Unsupported baseline-context schemaVersion: {schemaVersion}.");
            }

            var commit = ReadString(root, "commit") ?? ReadString(root, "commitSha") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(commit))
            {
                err.Add("baseline-context missing commit.");
            }

            var seed = root.TryGetProperty("seed", out var seedEl) && seedEl.TryGetInt32(out var sVal) ? sVal : 0;
            var ticks = root.TryGetProperty("ticks", out var ticksEl) && ticksEl.TryGetInt32(out var tVal) ? tVal : 0;
            var localDelta = ReadString(root, "localDeltaIdentity");

            string? catalogPath = null;
            string? catalogSha = null;
            if (root.TryGetProperty("catalog", out var catEl) && catEl.ValueKind == JsonValueKind.Object)
            {
                catalogPath = ReadString(catEl, "path");
                catalogSha = ReadString(catEl, "sha256");
            }

            var policies = new List<HashedInputFile>();
            if (root.TryGetProperty("policies", out var polEl) && polEl.ValueKind == JsonValueKind.Array)
            {
                var idx = 0;
                foreach (var item in polEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        err.Add($"baseline-context policies[{idx}] must be a JSON object.");
                        idx++;
                        continue;
                    }

                    var pPath = ReadString(item, "path");
                    var pSha = ReadString(item, "sha256");
                    if (string.IsNullOrWhiteSpace(pPath) || string.IsNullOrWhiteSpace(pSha))
                    {
                        err.Add($"baseline-context policies[{idx}] missing non-empty path or sha256.");
                        idx++;
                        continue;
                    }

                    policies.Add(new HashedInputFile(pPath, pSha));
                    idx++;
                }
            }

            HashedInputFile? results = null;
            if (root.TryGetProperty("results", out var resEl) && resEl.ValueKind == JsonValueKind.Object)
            {
                var rPath = ReadString(resEl, "path");
                var rSha = ReadString(resEl, "sha256");
                if (!string.IsNullOrWhiteSpace(rPath) && !string.IsNullOrWhiteSpace(rSha))
                {
                    results = new HashedInputFile(rPath, rSha);
                }
            }

            if (results is null)
            {
                err.Add("baseline-context missing results.path/sha256.");
            }

            if (err.Count > 0)
            {
                errors = err;
                return false;
            }

            context = new BaselineContextProvenance(
                SchemaVersion: schemaVersion,
                CommitSha: commit,
                Seed: seed,
                Ticks: ticks,
                CatalogPath: catalogPath,
                CatalogSha256: catalogSha,
                Policies: policies,
                Results: results,
                LocalDeltaIdentity: localDelta);
            errors = Array.Empty<string>();
            return true;
        }
        catch (JsonException ex)
        {
            err.Add($"Invalid baseline-context JSON: {ex.Message}");
            errors = err;
            return false;
        }
    }

    /// <summary>Serializes <paramref name="context"/> in <c>gauntlet-oracle.yml</c> <c>schemaVersion: 1</c> format.</summary>
    public static string ToBaselineContextJson(BaselineContextProvenance context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var dto = new Dictionary<string, object?>
        {
            ["schemaVersion"] = context.SchemaVersion,
            ["commit"] = context.CommitSha,
            ["seed"] = context.Seed,
            ["ticks"] = context.Ticks,
            ["catalog"] = new
            {
                path = context.CatalogPath ?? "none",
                sha256 = context.CatalogSha256 ?? "none",
            },
            ["policies"] = context.Policies.Select(p => new { path = p.Path, sha256 = p.Sha256 }).ToArray(),
            ["results"] = context.Results is null
                ? null
                : new { path = context.Results.Path, sha256 = context.Results.Sha256 },
        };

        if (!string.IsNullOrWhiteSpace(context.LocalDeltaIdentity))
        {
            dto["localDeltaIdentity"] = context.LocalDeltaIdentity;
        }

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    private static string? ReadString(JsonElement obj, string propName) =>
        obj.TryGetProperty(propName, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static string ToHexLower(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i * 2] = GetHexNibble(b >> 4);
            chars[i * 2 + 1] = GetHexNibble(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) =>
        (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}

/// <summary>
/// Extracts typed <see cref="MissionThreadEvidenceRecord"/> entries from gauntlet CSV fingerprints
/// without altering <see cref="ProjectAegis.Data.Catalog.GauntletOracleEvaluator"/> pass/fail rules.
/// </summary>
public static class GauntletFingerprintEvidenceExtractor
{
    /// <summary>
    /// Parses space/newline-separated <c>DecisionLog.ComputeFingerprint()</c> wire-format tokens
    /// (<c>Kind|SequenceId|SimTime|SimTick|...</c>) into typed mission-thread evidence records.
    /// </summary>
    public static IReadOnlyList<MissionThreadEvidenceRecord> Extract(string scenarioId, string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return Array.Empty<MissionThreadEvidenceRecord>();
        }

        var list = new List<MissionThreadEvidenceRecord>();
        var rawTokens = fingerprint.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < rawTokens.Length; i++)
        {
            var token = rawTokens[i].Trim();
            var parts = token.Split('|');
            if (parts.Length < 4)
            {
                continue;
            }

            var prefix = parts[0];
            var seq = parts[1];
            var simTime = ParseDouble(parts[2]);
            var idPrefix = $"{scenarioId}:{i}:{seq}";

            if (string.Equals(prefix, "PolicyUpdate", StringComparison.Ordinal) && parts.Length >= 8)
            {
                // PolicyUpdate|seq|time|simTick|policySnapshotId|field|prev|new
                var tick = ParseUlong(parts[3]);
                long? snapshotId = long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSnap)
                    ? parsedSnap
                    : null;
                var field = string.IsNullOrWhiteSpace(parts[5]) ? "policy" : parts[5];
                list.Add(new MissionThreadEvidenceRecord(
                    EvidenceId: $"{idPrefix}:auth",
                    Kind: MissionThreadEvidenceKind.Authority,
                    SimTick: tick,
                    SimTime: simTime,
                    SequenceRef: seq,
                    AuthoritativeSource: "GauntletCsvFingerprint:PolicyUpdate",
                    Authority: new AuthorityEvidenceFact(
                        ActorId: field,
                        TargetId: null,
                        RoeLevel: parts[7],
                        Disposition: "Updated",
                        ReasonCode: $"{parts[6]}->{parts[7]}",
                        PolicySnapshotId: snapshotId,
                        RequiredApproval: null)));
            }
            else if (string.Equals(prefix, "PolicyDenial", StringComparison.Ordinal) && parts.Length >= 6)
            {
                // PolicyDenial|seq|time|targetId|reason|attemptedKind
                list.Add(new MissionThreadEvidenceRecord(
                    EvidenceId: $"{idPrefix}:denial",
                    Kind: MissionThreadEvidenceKind.Authority,
                    SimTick: 0UL,
                    SimTime: simTime,
                    SequenceRef: seq,
                    AuthoritativeSource: "GauntletCsvFingerprint:PolicyDenial",
                    Authority: new AuthorityEvidenceFact(
                        ActorId: parts[3],
                        TargetId: parts[3],
                        RoeLevel: parts[4],
                        Disposition: "Denied",
                        ReasonCode: $"{parts[4]}:{parts[5]}",
                        PolicySnapshotId: null,
                        RequiredApproval: null)));
            }
            else if (string.Equals(prefix, "ContactChange", StringComparison.Ordinal) && parts.Length >= 9)
            {
                // ContactChange|seq|time|simTick|observerId|contactId|targetId|prev|new
                var tick = ParseUlong(parts[3]);
                list.Add(new MissionThreadEvidenceRecord(
                    EvidenceId: $"{idPrefix}:contact",
                    Kind: MissionThreadEvidenceKind.Contact,
                    SimTick: tick,
                    SimTime: simTime,
                    SequenceRef: seq,
                    AuthoritativeSource: "GauntletCsvFingerprint:ContactChange",
                    Contact: new ContactEvidenceFact(
                        ContactId: parts[5],
                        TargetId: parts[6],
                        ObserverId: parts[4],
                        LifecycleState: parts[8],
                        KillChainPhase: parts[8],
                        Confidence: null,
                        QualityFlags: $"{parts[7]}->{parts[8]}")));
            }
            else if (string.Equals(prefix, "CommsStateChange", StringComparison.Ordinal) && parts.Length >= 8)
            {
                // CommsStateChange|seq|time|simTick|nodeId|prev|new|reason
                var tick = ParseUlong(parts[3]);
                var newState = parts[6];
                list.Add(new MissionThreadEvidenceRecord(
                    EvidenceId: $"{idPrefix}:link",
                    Kind: MissionThreadEvidenceKind.DataLink,
                    SimTick: tick,
                    SimTime: simTime,
                    SequenceRef: seq,
                    AuthoritativeSource: "GauntletCsvFingerprint:CommsStateChange",
                    DataLink: new DataLinkEvidenceFact(
                        NetworkOrUnitId: parts[4],
                        CommsState: newState,
                        PreviousState: parts[5],
                        OutOfCommsUnknown: IsOutOfCommsState(newState),
                        ShareLagTicks: null,
                        ReasonCode: parts[7])));
            }
            else if (string.Equals(prefix, "Engagement", StringComparison.Ordinal) && parts.Length >= 8)
            {
                // Engagement|seq|time|simTick|shooter|engId|launched|abortReason
                var tick = ParseUlong(parts[3]);
                var launched = string.Equals(parts[6], "True", StringComparison.OrdinalIgnoreCase);
                list.Add(new MissionThreadEvidenceRecord(
                    EvidenceId: $"{idPrefix}:engage",
                    Kind: MissionThreadEvidenceKind.Engagement,
                    SimTick: tick,
                    SimTime: simTime,
                    SequenceRef: seq,
                    AuthoritativeSource: "GauntletCsvFingerprint:Engagement",
                    Engagement: new EngagementEvidenceFact(
                        ShooterUnitId: parts[4],
                        TargetUnitId: null,
                        EngagementId: ParseUlong(parts[5]),
                        Launched: launched,
                        WeaponFamilyId: null,
                        SalvoSize: 1,
                        AbortReasonCode: launched ? null : parts[7],
                        OutcomeCode: parts[7])));
            }
            else if (string.Equals(prefix, "EngagementOutcome", StringComparison.Ordinal) && parts.Length >= 8)
            {
                // EngagementOutcome|seq|time|simTick|engId|victimTargetId|outcomeCode|pkDraw
                var tick = ParseUlong(parts[3]);
                list.Add(new MissionThreadEvidenceRecord(
                    EvidenceId: $"{idPrefix}:outcome",
                    Kind: MissionThreadEvidenceKind.Engagement,
                    SimTick: tick,
                    SimTime: simTime,
                    SequenceRef: seq,
                    AuthoritativeSource: "GauntletCsvFingerprint:EngagementOutcome",
                    Engagement: new EngagementEvidenceFact(
                        ShooterUnitId: "unknown",
                        TargetUnitId: parts[5],
                        EngagementId: ParseUlong(parts[4]),
                        Launched: true,
                        WeaponFamilyId: null,
                        SalvoSize: 1,
                        AbortReasonCode: null,
                        OutcomeCode: parts[6])));
            }
            else if (string.Equals(prefix, "AgentDecision", StringComparison.Ordinal) && parts.Length >= 7)
            {
                // AgentDecision|seq|time|simTick|agentId|chosenOrderKind|<ScoredIntents...>|rngDraw
                var tick = ParseUlong(parts[3]);
                list.Add(new MissionThreadEvidenceRecord(
                    EvidenceId: $"{idPrefix}:rec",
                    Kind: MissionThreadEvidenceKind.Recommendation,
                    SimTick: tick,
                    SimTime: simTime,
                    SequenceRef: seq,
                    AuthoritativeSource: "GauntletCsvFingerprint:AgentDecision",
                    Recommendation: new RecommendationEvidenceFact(
                        AgentId: parts[4],
                        TargetId: "unspecified",
                        ChosenKind: parts[5],
                        PolicySnapshotId: null,
                        PersonalitySlug: null,
                        Outcome: null,
                        WithheldReasonCode: null)));
            }
            else if (string.Equals(prefix, "PlatformDamageChange", StringComparison.Ordinal) && parts.Length >= 9)
            {
                // PlatformDamageChange|seq|time|simTick|unitId|prevHp|newHp|reason|lvl
                var tick = ParseUlong(parts[3]);
                list.Add(new MissionThreadEvidenceRecord(
                    EvidenceId: $"{idPrefix}:assess",
                    Kind: MissionThreadEvidenceKind.Assessment,
                    SimTick: tick,
                    SimTime: simTime,
                    SequenceRef: seq,
                    AuthoritativeSource: "GauntletCsvFingerprint:PlatformDamageChange",
                    Assessment: new AssessmentEvidenceFact(
                        ContactId: parts[4],
                        TargetId: parts[4],
                        AssessmentState: parts[7],
                        DamageLevel: parts[8],
                        PreviousHpPct: ParseDouble(parts[5]),
                        NewHpPct: ParseDouble(parts[6]),
                        SourceKind: "GauntletCsvFingerprint")));
            }
        }

        return list;
    }

    private static bool IsOutOfCommsState(string? commsState)
    {
        if (string.IsNullOrWhiteSpace(commsState))
        {
            return false;
        }

        if (string.Equals(commsState, "Nominal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Restored", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Connected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Online", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(commsState, "Denied", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Degraded", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Lost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "OutOfComms", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Silent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Partitioned", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Disconnected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Offline", StringComparison.OrdinalIgnoreCase)
            || string.Equals(commsState, "Unknown", StringComparison.OrdinalIgnoreCase);
    }

    private static ulong ParseUlong(string s) =>
        ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0UL;

    private static double ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0.0;
}
