namespace ProjectAegis.Data.Catalog;

using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// S32-03 curator triage for quarantined ship/facility/submarine mount/loadout child rows.
/// Repairs only within <see cref="MountLoadoutQuarantineRepairEnvelope"/> via <see cref="CatalogWriteGate"/>.
/// </summary>
public static class MountLoadoutQuarantineTriage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static MountLoadoutQuarantineTriageResult Run(
        string databasePath,
        bool dryRun = true,
        string? entityHint = null,
        string? proposeJsonPath = null,
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path required.", nameof(databasePath));
        }

        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException($"Catalog database not found: {databasePath}");
        }

        SqliteConnection.ClearAllPools();
        var fittingByDomain = LoadFittingQuarantineCounts(proposeJsonPath, entityHint);
        var before = Audit(databasePath, entityHint, fittingByDomain);
        if (dryRun)
        {
            var dryRows = BuildRemainingRows(databasePath, entityHint, fittingByDomain);
            return new MountLoadoutQuarantineTriageResult(
                Ok: true,
                DryRun: true,
                DatabasePath: databasePath,
                Before: before,
                After: before,
                RemainingQuarantine: dryRows,
                RepairedBatchIds: [],
                AdvisoryNotes:
                [
                    "Dry-run only — no WriteGate commits.",
                    "Repair envelope: platform_live_fk, platform_staging_fk, baltic_seed_fk.",
                ]);
        }

        var repaired = new List<string>();
        var notes = new List<string>();
        using (var gate = new CatalogWriteGate(databasePath, clock ?? new FixedCatalogClock(32031)))
        {
            var plan = BuildRepairPlan(databasePath, entityHint, fittingByDomain);
            foreach (var batchId in plan.PlatformBatchesToApprove)
            {
                var decision = gate.ApproveBatch(batchId, "human", "mount-loadout-quarantine-triage");
                if (!decision.Committed)
                {
                    notes.Add($"platform_batch_failed:{batchId}:{string.Join(',', decision.Errors)}");
                    continue;
                }

                repaired.Add(batchId);
            }

            foreach (var batchId in plan.MountLoadoutBatchesToApprove)
            {
                var decision = gate.ApproveBatch(batchId, "human", "mount-loadout-quarantine-triage");
                if (!decision.Committed)
                {
                    notes.Add($"child_batch_failed:{batchId}:{string.Join(',', decision.Errors)}");
                    continue;
                }

                repaired.Add(batchId);
            }
        }

        var after = Audit(databasePath, entityHint, fittingByDomain);
        var remaining = BuildRemainingRows(databasePath, entityHint, fittingByDomain);
        return new MountLoadoutQuarantineTriageResult(
            Ok: remaining.Count == 0 || after.Sum(d => d.MountQuarantined + d.LoadoutQuarantined) <
                before.Sum(d => d.MountQuarantined + d.LoadoutQuarantined),
            DryRun: false,
            DatabasePath: databasePath,
            Before: before,
            After: after,
            RemainingQuarantine: remaining,
            RepairedBatchIds: repaired,
            AdvisoryNotes: notes);
    }

    public static IReadOnlyList<MountLoadoutDomainQuarantineCounts> Audit(
        string databasePath,
        string? entityHint = null,
        IReadOnlyDictionary<string, int>? fittingByDomain = null)
    {
        fittingByDomain ??= new Dictionary<string, int>(StringComparer.Ordinal);
        using var connection = OpenConnection(databasePath);
        var rows = ReadPendingChildRows(connection);
        var domains = ResolveDomains(entityHint);

        return domains
            .Select(domain =>
            {
                var domainRows = rows.Where(row => row.Domain == domain).ToArray();
                var mountRows = domainRows.Where(row => row.ChildKind == "mount").ToArray();
                var loadoutRows = domainRows.Where(row => row.ChildKind == "loadout").ToArray();
                fittingByDomain.TryGetValue(domain, out var fittingCount);
                var repairable = domainRows.Count(row => row.RepairRule is not null);
                var outOfEnvelope = domainRows.Count(row => row.RepairRule is null);
                return new MountLoadoutDomainQuarantineCounts(
                    domain,
                    mountRows.Length,
                    loadoutRows.Length,
                    fittingCount,
                    repairable,
                    outOfEnvelope);
            })
            .ToArray();
    }

    private static IReadOnlyList<MountLoadoutQuarantineRow> BuildRemainingRows(
        string databasePath,
        string? entityHint,
        IReadOnlyDictionary<string, int> fittingByDomain)
    {
        using var connection = OpenConnection(databasePath);
        var rows = ReadPendingChildRows(connection).ToList();

        foreach (var (domain, count) in fittingByDomain.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (count <= 0)
            {
                continue;
            }

            rows.Add(new MountLoadoutQuarantineRow(
                domain,
                "fitting",
                BatchId: "fitting-quarantine",
                PlatformId: "*",
                ChildId: "*",
                Reason: "orphan_weapon_id",
                RepairRule: null));
        }

        if (!string.IsNullOrWhiteSpace(entityHint))
        {
            var hinted = MountLoadoutQuarantineDomain.FromEntityHint(entityHint);
            return rows
                .Where(row => row.Domain == hinted)
                .OrderBy(row => row.Domain, StringComparer.Ordinal)
                .ThenBy(row => row.ChildKind, StringComparer.Ordinal)
                .ThenBy(row => row.PlatformId, StringComparer.Ordinal)
                .ThenBy(row => row.ChildId, StringComparer.Ordinal)
                .ToArray();
        }

        return rows
            .OrderBy(row => row.Domain, StringComparer.Ordinal)
            .ThenBy(row => row.ChildKind, StringComparer.Ordinal)
            .ThenBy(row => row.PlatformId, StringComparer.Ordinal)
            .ThenBy(row => row.ChildId, StringComparer.Ordinal)
            .ToArray();
    }

    private static RepairPlan BuildRepairPlan(
        string databasePath,
        string? entityHint,
        IReadOnlyDictionary<string, int> fittingByDomain)
    {
        using var connection = OpenConnection(databasePath);
        var rows = ReadPendingChildRows(connection);
        if (!string.IsNullOrWhiteSpace(entityHint))
        {
            var hinted = MountLoadoutQuarantineDomain.FromEntityHint(entityHint);
            rows = rows.Where(row => row.Domain == hinted).ToArray();
        }

        var platformBatches = new SortedSet<string>(StringComparer.Ordinal);
        var childBatches = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (row.RepairRule is null)
            {
                continue;
            }

            childBatches.Add(row.BatchId);
            if (string.Equals(row.RepairRule, MountLoadoutQuarantineRepairEnvelope.RuleStagingPlatformFk, StringComparison.Ordinal))
            {
                foreach (var platformBatchId in FindStagingPlatformBatches(connection, row.PlatformId))
                {
                    platformBatches.Add(platformBatchId);
                }
            }
        }

        return new RepairPlan(
            platformBatches.ToArray(),
            childBatches.ToArray(),
            fittingByDomain.Values.Sum());
    }

    private static IReadOnlyList<MountLoadoutQuarantineRow> ReadPendingChildRows(SqliteConnection connection)
    {
        var duplicateLoadouts = FindDuplicateLoadoutKeys(connection);
        var rows = new List<MountLoadoutQuarantineRow>();

        foreach (var (tableName, childKind, childColumn) in new[]
                 {
                     ("catalog_staging_mount", "mount", "mount_id"),
                     ("catalog_staging_loadout", "loadout", "loadout_id"),
                 })
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                $"""
                 SELECT s.batch_id, s.platform_id, s.{childColumn}
                 FROM {tableName} AS s
                 INNER JOIN catalog_staging_batch AS b ON b.batch_id = s.batch_id
                 WHERE b.approval_state = 'proposed'
                 ORDER BY s.platform_id ASC, s.{childColumn} ASC, s.batch_id ASC
                 """;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var batchId = reader.GetString(0);
                var platformId = reader.GetString(1);
                var childId = reader.GetString(2);
                var domain = ResolveDomainForPlatform(connection, platformId);
                string? repairRule = null;
                string reason = MountLoadoutQuarantineRepairEnvelope.ReasonOutOfEnvelope;

                if (duplicateLoadouts.Contains($"{platformId}/{childId}") && childKind == "loadout")
                {
                    reason = MountLoadoutQuarantineRepairEnvelope.ReasonDuplicateLoadout;
                }
                else if (string.Equals(platformId, childId, StringComparison.Ordinal))
                {
                    reason = MountLoadoutQuarantineRepairEnvelope.ReasonCircularFk;
                }
                else
                {
                    var classification = MountLoadoutQuarantineRepairEnvelope.ClassifyPlatformFk(
                        platformId,
                        LivePlatformExists(connection, platformId),
                        StagingPlatformExists(connection, platformId),
                        BalticSeedPlatformExists(platformId));
                    if (classification.Repairable)
                    {
                        repairRule = classification.Rule;
                    }
                    else
                    {
                        reason = classification.Reason ?? MountLoadoutQuarantineRepairEnvelope.ReasonOrphanPlatform;
                    }
                }

                rows.Add(new MountLoadoutQuarantineRow(
                    domain,
                    childKind,
                    batchId,
                    platformId,
                    childId,
                    reason,
                    repairRule));
            }
        }

        return rows;
    }

    private static HashSet<string> FindDuplicateLoadoutKeys(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, loadout_id, COUNT(DISTINCT loadout_name) AS name_variants
            FROM catalog_staging_loadout
            GROUP BY platform_id, loadout_id
            HAVING name_variants > 1
            ORDER BY platform_id ASC, loadout_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var duplicates = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            duplicates.Add($"{reader.GetString(0)}/{reader.GetString(1)}");
        }

        return duplicates;
    }

    private static IReadOnlyList<string> FindStagingPlatformBatches(SqliteConnection connection, string platformId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT DISTINCT s.batch_id
            FROM catalog_staging_platform AS s
            INNER JOIN catalog_staging_batch AS b ON b.batch_id = s.batch_id
            WHERE s.platform_id = $platform AND b.approval_state = 'proposed'
            ORDER BY s.batch_id ASC
            """;
        cmd.Parameters.AddWithValue("$platform", platformId);
        using var reader = cmd.ExecuteReader();
        var batches = new List<string>();
        while (reader.Read())
        {
            batches.Add(reader.GetString(0));
        }

        return batches;
    }

    private static string ResolveDomainForPlatform(SqliteConnection connection, string platformId)
    {
        using (var liveCmd = connection.CreateCommand())
        {
            liveCmd.CommandText =
                """
                SELECT domain
                FROM platform
                WHERE platform_id = $platform
                ORDER BY snapshot_id ASC
                LIMIT 1
                """;
            liveCmd.Parameters.AddWithValue("$platform", platformId);
            var scalar = liveCmd.ExecuteScalar();
            if (scalar is string liveDomain && !string.IsNullOrWhiteSpace(liveDomain))
            {
                return MountLoadoutQuarantineDomain.FromPlatformDomain(liveDomain);
            }
        }

        using var stagingCmd = connection.CreateCommand();
        stagingCmd.CommandText =
            """
            SELECT domain
            FROM catalog_staging_platform
            WHERE platform_id = $platform
            ORDER BY batch_id ASC
            LIMIT 1
            """;
        stagingCmd.Parameters.AddWithValue("$platform", platformId);
        var stagingDomain = stagingCmd.ExecuteScalar();
        if (stagingDomain is string domain && !string.IsNullOrWhiteSpace(domain))
        {
            return MountLoadoutQuarantineDomain.FromPlatformDomain(domain);
        }

        return MountLoadoutQuarantineDomain.Platform;
    }

    private static bool LivePlatformExists(SqliteConnection connection, string platformId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM platform WHERE platform_id = $platform";
        cmd.Parameters.AddWithValue("$platform", platformId);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private static bool StagingPlatformExists(SqliteConnection connection, string platformId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*)
            FROM catalog_staging_platform
            WHERE platform_id = $platform
            """;
        cmd.Parameters.AddWithValue("$platform", platformId);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private static bool BalticSeedPlatformExists(string platformId) =>
        CatalogValidationDefaults.BalticPlatforms()
            .Any(platform => string.Equals(platform.PlatformId, platformId, StringComparison.Ordinal));

    private static IReadOnlyDictionary<string, int> LoadFittingQuarantineCounts(
        string? proposeJsonPath,
        string? entityHint)
    {
        var counts = MountLoadoutQuarantineDomain.ChildRowDomains.ToDictionary(
            domain => domain,
            _ => 0,
            StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(proposeJsonPath) || !File.Exists(proposeJsonPath))
        {
            return counts;
        }

        using var stream = File.OpenRead(proposeJsonPath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        if (!root.TryGetProperty("quarantinedCount", out var quarantinedCountElement))
        {
            return counts;
        }

        var entity = root.TryGetProperty("entity", out var entityElement)
            ? entityElement.GetString()
            : entityHint;
        var domain = MountLoadoutQuarantineDomain.FromEntityHint(entity);
        counts[domain] = quarantinedCountElement.GetInt32();
        return counts;
    }

    private static IReadOnlyList<string> ResolveDomains(string? entityHint)
    {
        if (!string.IsNullOrWhiteSpace(entityHint))
        {
            return [MountLoadoutQuarantineDomain.FromEntityHint(entityHint)];
        }

        return MountLoadoutQuarantineDomain.ChildRowDomains;
    }

    private static SqliteConnection OpenConnection(string databasePath)
    {
        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();
        return connection;
    }

    private sealed record RepairPlan(
        IReadOnlyList<string> PlatformBatchesToApprove,
        IReadOnlyList<string> MountLoadoutBatchesToApprove,
        int FittingQuarantined);
}