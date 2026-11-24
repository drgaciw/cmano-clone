using System.Globalization;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint.Connectors;
using ProjectAegis.Data.WriteGate;

namespace ProjectAegis.Data.Osint;

/// <summary>Provenance-enriched OSINT staging or discovery proposal row (DRG-253 / ADR-006).</summary>
public sealed record OsintStagingProposalRow(
    string BatchId,
    string PlatformId,
    string SensorId,
    double Confidence,
    string CitationRef,
    string Snippet,
    string SourceFactId,
    string SourceFile,
    int TrlLevel,
    string ReviewState,
    string ActorType,
    string ActorId,
    long ProposedUtcTicks,
    string Rationale,
    bool IsLogOnly,
    IReadOnlyList<string> AdditionalCitations);

/// <summary>Partitioned snapshot of pending staging proposals and sub-threshold log-only items.</summary>
public sealed record OsintStagingSnapshot(
    IReadOnlyList<OsintStagingProposalRow> Proposals,
    IReadOnlyList<OsintStagingProposalRow> LogOnly,
    string SourceKind);

/// <summary>Result of a write-gate proposal, approve, or reject operation.</summary>
public sealed record OsintStagingDecisionResult(
    bool Succeeded,
    bool Committed,
    bool Rejected,
    string BatchId,
    IReadOnlyList<string> Errors);

/// <summary>
/// Read-only SQLite provenance reader + write-gate delegating service for OSINT staging review (DRG-253).
/// Opens SQLite in <see cref="SqliteOpenMode.ReadOnly"/> for inspection so shipped DBs remain byte-identical,
/// and delegates all mutations through <see cref="CatalogWriteGate"/> without modifying it.
/// </summary>
public sealed class OsintStagingReviewService
{
    public const string ReasonBelowConfidenceThreshold = "BELOW_CONFIDENCE_THRESHOLD";
    public const string ReasonMissingCitation = "MISSING_CITATION";

    public static bool IsConfidencePermitted(
        double confidence,
        double minimumConfidence = OsintProposalGate.DefaultProposalConfidenceThreshold)
    {
        if (double.IsNaN(confidence) || double.IsInfinity(confidence) || confidence < 0.0 || confidence > 1.0)
        {
            return false;
        }

        if (double.IsNaN(minimumConfidence) || double.IsInfinity(minimumConfidence))
        {
            return false;
        }

        var min = Math.Clamp(minimumConfidence, 0d, 1d);
        return confidence >= min;
    }

    public static OsintStagingSnapshot LoadStagingSnapshot(
        string? databasePath,
        string? fixturePath = null,
        double minimumConfidence = OsintProposalGate.DefaultProposalConfidenceThreshold)
    {
        var fixtureLookup = LoadFixtureLookup(fixturePath);

        if (!string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath))
        {
            var dbRows = TryReadPendingStagingRows(databasePath, fixtureLookup, minimumConfidence);
            if (dbRows.Count > 0)
            {
                var proposals = dbRows.Where(r => !r.IsLogOnly).ToArray();
                var logOnly = dbRows.Where(r => r.IsLogOnly).ToArray();
                return new OsintStagingSnapshot(proposals, logOnly, "staging-db");
            }
        }

        if (!string.IsNullOrWhiteSpace(fixturePath) && File.Exists(fixturePath))
        {
            var records = new FileOsintConnector(fixturePath).Fetch();
            if (records.Length > 0)
            {
                var ordered = records
                    .OrderBy(d => d.SourceUrl, StringComparer.Ordinal)
                    .ThenBy(d => d.CanonicalId, StringComparer.Ordinal);
                var proposals = new List<OsintStagingProposalRow>();
                var logOnly = new List<OsintStagingProposalRow>();
                foreach (var record in ordered)
                {
                    if (IsConfidencePermitted(record.RelevanceScore, minimumConfidence))
                    {
                        proposals.Add(FromDiscovery(record, fixturePath, isLogOnly: false));
                    }
                    else
                    {
                        logOnly.Add(FromDiscovery(record, fixturePath, isLogOnly: true));
                    }
                }

                return new OsintStagingSnapshot(proposals.ToArray(), logOnly.ToArray(), "fixture");
            }
        }

        return new OsintStagingSnapshot(
            Array.Empty<OsintStagingProposalRow>(),
            Array.Empty<OsintStagingProposalRow>(),
            "empty");
    }

    public static OsintStagingDecisionResult ProposeDiscovery(
        string databasePath,
        OsintDiscoveryRecord? record,
        string actorType,
        string actorId,
        string sourceFile = "osint_facts.json",
        bool markApprovedForHumanReview = false,
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            return new OsintStagingDecisionResult(false, false, false, string.Empty, new[] { "INVALID_DATABASE_PATH" });
        }

        if (record is null)
        {
            return new OsintStagingDecisionResult(false, false, false, string.Empty, new[] { "MISSING_RECORD" });
        }

        var validationErrors = ValidateDiscoveryForStaging(record, OsintProposalGate.DefaultProposalConfidenceThreshold);
        if (validationErrors.Count > 0)
        {
            return new OsintStagingDecisionResult(false, false, false, string.Empty, validationErrors);
        }

        var binding = OsintCatalogMapper.ToSensorBinding(record) with
        {
            SourceFile = string.IsNullOrWhiteSpace(sourceFile) ? record.SourceUrl : sourceFile,
            ReviewState = markApprovedForHumanReview ? CatalogReviewStates.Approved : CatalogReviewStates.Provisional,
            ReviewerId = markApprovedForHumanReview ? actorId : "osint-digest",
        };

        using var gate = new CatalogWriteGate(databasePath, clock);
        var batchId = gate.ProposeSensorBatch(
            new[] { binding },
            actorType,
            actorId,
            string.IsNullOrWhiteSpace(record.Snippet) ? record.CanonicalId : record.Snippet);

        return new OsintStagingDecisionResult(true, false, false, batchId, Array.Empty<string>());
    }

    public static OsintStagingDecisionResult ApproveBatch(
        string databasePath,
        string? batchId,
        string actorType,
        string actorId,
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath) || string.IsNullOrWhiteSpace(batchId))
        {
            return new OsintStagingDecisionResult(false, false, false, batchId ?? string.Empty, new[] { "INVALID_BATCH_OR_DB" });
        }

        using var gate = new CatalogWriteGate(databasePath, clock);
        var decision = gate.ApproveBatch(batchId, actorType, actorId);
        var succeeded = decision.Committed && decision.Errors.Count == 0;
        return new OsintStagingDecisionResult(succeeded, decision.Committed, false, decision.BatchId, decision.Errors);
    }

    public static OsintStagingDecisionResult RejectBatch(
        string databasePath,
        string? batchId,
        string actorType,
        string actorId,
        string rationale = "",
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath) || string.IsNullOrWhiteSpace(batchId))
        {
            return new OsintStagingDecisionResult(false, false, false, batchId ?? string.Empty, new[] { "INVALID_BATCH_OR_DB" });
        }

        using var gate = new CatalogWriteGate(databasePath, clock);
        var decision = gate.RejectBatch(batchId, actorType, actorId, rationale);
        var succeeded = decision.Errors.Count == 0;
        return new OsintStagingDecisionResult(succeeded, false, succeeded, decision.BatchId, decision.Errors);
    }

    public static OsintStagingDecisionResult ProposeAndApproveDiscovery(
        string databasePath,
        OsintDiscoveryRecord record,
        string actorType,
        string actorId,
        string sourceFile = "osint_facts.json",
        ICatalogClock? clock = null)
    {
        var proposed = ProposeDiscovery(
            databasePath,
            record,
            actorType,
            actorId,
            sourceFile,
            markApprovedForHumanReview: true,
            clock: clock);
        if (!proposed.Succeeded)
        {
            return proposed;
        }

        return ApproveBatch(databasePath, proposed.BatchId, actorType, actorId, clock);
    }

    private static IReadOnlyList<string> ValidateDiscoveryForStaging(OsintDiscoveryRecord record, double minConfidence)
    {
        var errors = new List<string>();
        if (!IsConfidencePermitted(record.RelevanceScore, minConfidence))
        {
            errors.Add(ReasonBelowConfidenceThreshold);
        }

        if (string.IsNullOrWhiteSpace(record.SourceUrl) && string.IsNullOrWhiteSpace(record.Snippet))
        {
            errors.Add(ReasonMissingCitation);
        }

        return errors;
    }

    private static List<OsintStagingProposalRow> TryReadPendingStagingRows(
        string databasePath,
        IReadOnlyDictionary<string, OsintDiscoveryRecord> fixtureLookup,
        double minimumConfidence)
    {
        var results = new List<OsintStagingProposalRow>();
        try
        {
            var connString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ConnectionString;

            using var conn = new SqliteConnection(connString);
            conn.Open();

            if (!TableExists(conn, "catalog_staging_batch") || !TableExists(conn, "catalog_staging_sensor"))
            {
                return results;
            }

            var citationsByBatch = TableExists(conn, "catalog_staging_evidence_cite")
                ? ReadEvidenceCitations(conn)
                : new Dictionary<string, List<(string Citation, string Assumption)>>(StringComparer.Ordinal);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT
                    b.batch_id,
                    s.platform_id,
                    s.sensor_id,
                    s.confidence,
                    s.citation_ref,
                    s.source_fact_id,
                    s.source_file,
                    s.trl_level,
                    s.review_state,
                    b.actor_type,
                    b.actor_id,
                    b.proposed_utc_ticks,
                    b.rationale
                FROM catalog_staging_batch b
                INNER JOIN catalog_staging_sensor s ON s.batch_id = b.batch_id
                WHERE b.approval_state = 'proposed'
                ORDER BY b.proposed_utc_ticks ASC, b.batch_id ASC, s.platform_id ASC, s.sensor_id ASC;
                """;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var batchId = reader.GetString(0);
                var platformId = reader.GetString(1);
                var sensorId = reader.GetString(2);
                var confidence = reader.IsDBNull(3) ? double.NaN : reader.GetDouble(3);
                var citationRef = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                var sourceFactId = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                var sourceFile = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
                var trlLevel = reader.GetInt32(7);
                var reviewState = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
                var actorType = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
                var actorId = reader.IsDBNull(10) ? string.Empty : reader.GetString(10);
                var proposedUtcTicks = reader.GetInt64(11);
                var rationale = reader.IsDBNull(12) ? string.Empty : reader.GetString(12);

                citationsByBatch.TryGetValue(batchId, out var cites);
                var additional = cites is { Count: > 0 }
                    ? cites.Select(c => c.Citation).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal).ToArray()
                    : Array.Empty<string>();

                var snippet = ResolveSnippet(cites, fixtureLookup, citationRef, sensorId, rationale, sourceFactId, sourceFile);
                var isLogOnly = !IsConfidencePermitted(confidence, minimumConfidence);

                results.Add(new OsintStagingProposalRow(
                    batchId,
                    platformId,
                    sensorId,
                    confidence,
                    citationRef,
                    snippet,
                    sourceFactId,
                    sourceFile,
                    trlLevel,
                    reviewState,
                    actorType,
                    actorId,
                    proposedUtcTicks,
                    rationale,
                    isLogOnly,
                    additional));
            }
        }
        catch (SqliteException)
        {
            return new List<OsintStagingProposalRow>();
        }

        return results;
    }

    private static string ResolveSnippet(
        List<(string Citation, string Assumption)>? cites,
        IReadOnlyDictionary<string, OsintDiscoveryRecord> fixtureLookup,
        string citationRef,
        string sensorId,
        string rationale,
        string sourceFactId,
        string sourceFile)
    {
        if (cites is not null)
        {
            foreach (var cite in cites)
            {
                if (!string.IsNullOrWhiteSpace(cite.Assumption))
                {
                    return cite.Assumption;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(citationRef)
            && fixtureLookup.TryGetValue(citationRef, out var byUrl)
            && !string.IsNullOrWhiteSpace(byUrl.Snippet))
        {
            return byUrl.Snippet;
        }

        if (!string.IsNullOrWhiteSpace(sensorId)
            && fixtureLookup.TryGetValue(sensorId, out var bySensor)
            && !string.IsNullOrWhiteSpace(bySensor.Snippet))
        {
            return bySensor.Snippet;
        }

        if (!string.IsNullOrWhiteSpace(rationale))
        {
            return rationale;
        }

        if (!string.IsNullOrWhiteSpace(sourceFactId) || !string.IsNullOrWhiteSpace(sourceFile))
        {
            return $"fact={sourceFactId}; source={sourceFile}";
        }

        return string.Empty;
    }

    private static Dictionary<string, List<(string Citation, string Assumption)>> ReadEvidenceCitations(SqliteConnection conn)
    {
        var map = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT batch_id, source_id, source_hash, revision, assumption FROM catalog_staging_evidence_cite ORDER BY batch_id, field, source_id;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var batchId = reader.GetString(0);
            var sourceId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var revision = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            var assumption = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            if (!map.TryGetValue(batchId, out var list))
            {
                list = new List<(string, string)>();
                map[batchId] = list;
            }

            var formatted = string.IsNullOrWhiteSpace(sourceId)
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, "{0} (rev {1})", sourceId, revision);
            list.Add((formatted, assumption));
        }

        return map;
    }

    private static bool TableExists(SqliteConnection conn, string tableName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
        cmd.Parameters.AddWithValue("$name", tableName);
        return cmd.ExecuteScalar() is not null;
    }

    private static Dictionary<string, OsintDiscoveryRecord> LoadFixtureLookup(string? fixturePath)
    {
        var dict = new Dictionary<string, OsintDiscoveryRecord>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(fixturePath) || !File.Exists(fixturePath))
        {
            return dict;
        }

        foreach (var record in new FileOsintConnector(fixturePath).Fetch())
        {
            if (!string.IsNullOrWhiteSpace(record.SourceUrl))
            {
                dict[record.SourceUrl] = record;
            }

            var sensorId = "osint-" + record.CanonicalId.Replace(" ", "-").ToLowerInvariant();
            dict[sensorId] = record;
        }

        return dict;
    }

    private static OsintStagingProposalRow FromDiscovery(
        OsintDiscoveryRecord record,
        string fixturePath,
        bool isLogOnly)
    {
        var binding = OsintCatalogMapper.ToSensorBinding(record);
        return new OsintStagingProposalRow(
            BatchId: record.CanonicalId,
            PlatformId: binding.PlatformId,
            SensorId: binding.SensorId,
            Confidence: record.RelevanceScore,
            CitationRef: record.SourceUrl,
            Snippet: record.Snippet,
            SourceFactId: binding.SourceFactId,
            SourceFile: Path.GetFileName(fixturePath),
            TrlLevel: binding.TrlLevel,
            ReviewState: binding.ReviewState,
            ActorType: "osint",
            ActorId: "fixture",
            ProposedUtcTicks: record.ObservedUtcTicks,
            Rationale: record.Snippet,
            IsLogOnly: isLogOnly,
            AdditionalCitations: Array.Empty<string>());
    }
}
