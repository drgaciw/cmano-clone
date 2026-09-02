namespace ProjectAegis.Data.Snapshots;

using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;

/// <summary>
/// Read-only catalog governance audit (req 06 DBI-GOV-1…4).
/// Additive — does not rewrite <see cref="CatalogWriteGate"/> write paths.
/// </summary>
public static class CatalogGovernanceIntegrity
{
    public const string FindingEmptyContentHash = "DBI-GOV-1";
    public const string FindingInertChangeLog = "DBI-GOV-2";
    public const string FindingSchemaWatermarkDrift = "DBI-GOV-3";
    public const string FindingBlankApprovedReviewer = "DBI-GOV-4";

    /// <summary>Inspects a SQLite catalog for release-train governance theater.</summary>
    public static IReadOnlyList<CatalogGovernanceFinding> Audit(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path required.", nameof(databasePath));
        }
        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException("Catalog database not found.", databasePath);
        }

        var findings = new List<CatalogGovernanceFinding>();
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();

        AuditContentHashes(connection, findings);
        AuditChangeLog(connection, findings);
        AuditSchemaWatermark(connection, findings);
        AuditApprovedReviewers(connection, findings);

        return findings;
    }

    /// <summary>True when <see cref="Audit"/> returns no findings.</summary>
    public static bool IsReleaseReady(string databasePath) => Audit(databasePath).Count == 0;

    private static void AuditContentHashes(SqliteConnection connection, List<CatalogGovernanceFinding> findings)
    {
        if (!TableExists(connection, "catalog_snapshot") ||
            !ColumnExists(connection, "catalog_snapshot", "content_hash_sha256"))
        {
            findings.Add(new CatalogGovernanceFinding(
                FindingEmptyContentHash,
                "catalog_snapshot.content_hash_sha256 column missing — migration 006 not applied."));
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT snapshot_id, IFNULL(content_hash_sha256, '')
            FROM catalog_snapshot
            ORDER BY snapshot_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var snapshotId = reader.GetString(0);
            var hash = reader.GetString(1);
            if (string.IsNullOrWhiteSpace(hash))
            {
                findings.Add(new CatalogGovernanceFinding(
                    FindingEmptyContentHash,
                    $"Snapshot '{snapshotId}' has empty content_hash_sha256."));
            }
        }
    }

    private static void AuditChangeLog(SqliteConnection connection, List<CatalogGovernanceFinding> findings)
    {
        if (!TableExists(connection, "catalog_change_log"))
        {
            findings.Add(new CatalogGovernanceFinding(
                FindingInertChangeLog,
                "catalog_change_log table missing."));
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM catalog_change_log";
        var count = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        if (count == 0)
        {
            findings.Add(new CatalogGovernanceFinding(
                FindingInertChangeLog,
                "catalog_change_log is empty (inert audit trail)."));
        }
    }

    private static void AuditSchemaWatermark(SqliteConnection connection, List<CatalogGovernanceFinding> findings)
    {
        long userVersion;
        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA user_version";
            userVersion = Convert.ToInt64(pragma.ExecuteScalar() ?? 0L);
        }

        if (userVersion == 0)
        {
            findings.Add(new CatalogGovernanceFinding(
                FindingSchemaWatermarkDrift,
                "PRAGMA user_version is 0 — migration watermark not stamped on the file."));
        }

        if (!TableExists(connection, "db_release"))
        {
            findings.Add(new CatalogGovernanceFinding(
                FindingSchemaWatermarkDrift,
                "db_release table missing."));
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT release_version, IFNULL(schema_version, '')
            FROM db_release
            ORDER BY release_version ASC
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var release = reader.GetString(0);
            var schema = reader.GetString(1);
            if (!string.Equals(schema, CatalogTlTier.CatalogSchemaVersion, StringComparison.Ordinal))
            {
                findings.Add(new CatalogGovernanceFinding(
                    FindingSchemaWatermarkDrift,
                    $"Release '{release}' schema_version '{schema}' != CatalogSchemaVersion '{CatalogTlTier.CatalogSchemaVersion}'."));
            }
        }
    }

    private static void AuditApprovedReviewers(SqliteConnection connection, List<CatalogGovernanceFinding> findings)
    {
        if (!TableExists(connection, "sensor") ||
            !ColumnExists(connection, "sensor", "review_state") ||
            !ColumnExists(connection, "sensor", "reviewer_id"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT sensor_id
            FROM sensor
            WHERE review_state = 'approved'
              AND (reviewer_id IS NULL OR TRIM(reviewer_id) = '')
            ORDER BY sensor_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            findings.Add(new CatalogGovernanceFinding(
                FindingBlankApprovedReviewer,
                $"Approved sensor '{reader.GetString(0)}' has blank reviewer_id."));
        }
    }

    private static bool TableExists(SqliteConnection connection, string table)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1";
        cmd.Parameters.AddWithValue("$name", table);
        return cmd.ExecuteScalar() != null;
    }

    private static bool ColumnExists(SqliteConnection connection, string table, string column)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Single governance integrity finding (DBI-GOV-* id).</summary>
public sealed record CatalogGovernanceFinding(string Code, string Message);
