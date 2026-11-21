namespace ProjectAegis.Data.Agents;


/// <summary>Req-06 diff agent — surfaces staged write-gate batches (propose-only output).</summary>
public sealed class CatalogDiffProposalAgent : IDatabaseIntelligenceAgent
{
    public string AgentId => "diff_proposal";

    public DatabaseAgentReport Run(DatabaseAgentContext context)
    {
        if (string.IsNullOrWhiteSpace(context.DatabasePath) || !File.Exists(context.DatabasePath))
        {
            return new DatabaseAgentReport(AgentId, true,
            [
                new DatabaseAgentFinding(
                    "DIFF_SKIPPED",
                    "No database path — diff agent requires SQLite catalog for staging inspection",
                    "info"),
            ]);
        }

        // UAT D19: CatalogWriteGate migrates on open, so shipped catalogs are inspected read-only.
        var pending = Catalog.SqliteCatalogReader.IsShippedCatalogDatabase(context.DatabasePath)
            ? ListPendingBatchesReadOnly(context.DatabasePath)
            : ListPendingBatchesViaGate(context.DatabasePath);
        var findings = pending
            .Select(b => new DatabaseAgentFinding(
                "STAGED_BATCH_PENDING",
                $"{b.BatchId} records={b.RecordCount} actor={b.ActorType}:{b.ActorId}",
                "info"))
            .ToList();

        if (findings.Count == 0)
        {
            findings.Add(new DatabaseAgentFinding("DIFF_CLEAN", "No pending staging batches", "info"));
        }

        return new DatabaseAgentReport(AgentId, true, findings);
    }

    private static IReadOnlyList<WriteGate.CatalogStagingBatchSummary> ListPendingBatchesViaGate(string databasePath)
    {
        using var gate = new WriteGate.CatalogWriteGate(databasePath);
        return gate.ListPendingBatches();
    }

    private static IReadOnlyList<WriteGate.CatalogStagingBatchSummary> ListPendingBatchesReadOnly(string databasePath)
    {
        var list = new List<WriteGate.CatalogStagingBatchSummary>();
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ConnectionString);
        connection.Open();
        using var exists = connection.CreateCommand();
        exists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'catalog_staging_batch'";
        if (Convert.ToInt32(exists.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 0)
        {
            return list;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT batch_id, actor_type, actor_id, record_count, approval_state, proposed_utc_ticks
            FROM catalog_staging_batch
            WHERE approval_state = 'proposed'
            ORDER BY proposed_utc_ticks ASC, batch_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new WriteGate.CatalogStagingBatchSummary(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetString(4),
                reader.GetInt64(5)));
        }

        return list;
    }
}
