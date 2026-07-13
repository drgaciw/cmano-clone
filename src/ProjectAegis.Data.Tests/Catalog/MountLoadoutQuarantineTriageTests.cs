using System.Globalization;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>S32-03 mount/loadout quarantine triage + bounded FK repair envelope.</summary>
[Collection("CatalogSqlite")]
public sealed class MountLoadoutQuarantineTriageTests
{
    [Fact]
    public void Audit_identifies_pending_mount_loadout_child_rows_by_domain()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s32-triage-audit-{Guid.NewGuid():N}.db");
        try
        {
            StagePlatformMountLoadout(dbPath, "u-triage-hull", approvePlatformOnly: true);

            var result = MountLoadoutQuarantineTriage.Run(dbPath, dryRun: true, entityHint: "platform");

            var before = Assert.Single(result.Before);
            Assert.Equal(MountLoadoutQuarantineDomain.Platform, before.Domain);
            Assert.Equal(1, before.MountQuarantined);
            Assert.Equal(1, before.LoadoutQuarantined);
            Assert.Equal(2, before.Repairable);
            Assert.Equal(0, before.OutOfEnvelope);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Apply_repairs_in_envelope_rows_via_WriteGate_only()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s32-triage-repair-{Guid.NewGuid():N}.db");
        try
        {
            var batches = StagePlatformMountLoadout(dbPath, "u-triage-repair", approvePlatformOnly: true);

            var before = MountLoadoutQuarantineTriage.Audit(dbPath, entityHint: "platform");
            Assert.Equal(1, before[0].MountQuarantined);
            Assert.Equal(1, before[0].LoadoutQuarantined);

            var result = MountLoadoutQuarantineTriage.Run(
                dbPath,
                dryRun: false,
                entityHint: "platform",
                clock: new FixedCatalogClock(32041));

            Assert.False(result.DryRun);
            Assert.Equal(2, result.RepairedBatchIds.Count);
            Assert.Contains(batches.MountBatchId, result.RepairedBatchIds);
            Assert.Contains(batches.LoadoutBatchId, result.RepairedBatchIds);

            var after = Assert.Single(result.After);
            Assert.Equal(0, after.MountQuarantined);
            Assert.Equal(0, after.LoadoutQuarantined);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.Equal(1, CountRows(connection, "platform_mount", "platform_id = 'u-triage-repair'"));
            Assert.Equal(1, CountRows(connection, "platform_loadout", "platform_id = 'u-triage-repair'"));
            Assert.Equal(0, CountProposedStagingRows(connection, "catalog_staging_mount"));
            Assert.Equal(0, CountProposedStagingRows(connection, "catalog_staging_loadout"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Orphan_mount_rows_remain_quarantined_out_of_envelope()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s32-triage-orphan-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(32051)))
            {
                var mountBatch = gate.ProposeMountBatch(
                    [new CatalogMount("missing-platform", "mount-orphan")],
                    "agent",
                    "orphan-test");
                Assert.False(gate.ApproveBatch(mountBatch, "human", "qa").Committed);
            }

            var result = MountLoadoutQuarantineTriage.Run(dbPath, dryRun: true, entityHint: "platform");
            var row = Assert.Single(result.RemainingQuarantine);
            Assert.Equal("mount", row.ChildKind);
            Assert.Equal(MountLoadoutQuarantineRepairEnvelope.ReasonOrphanPlatform, row.Reason);
            Assert.Null(row.RepairRule);

            var applied = MountLoadoutQuarantineTriage.Run(
                dbPath,
                dryRun: false,
                entityHint: "platform",
                clock: new FixedCatalogClock(32052));
            Assert.Empty(applied.RepairedBatchIds);
            Assert.Equal(1, applied.After[0].OutOfEnvelope);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void ApproveBatch_rejects_orphan_platform_mount_and_loadout_DBI_3_2()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s32-orphan-fk-{Guid.NewGuid():N}.db");
        try
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(32061));
            var mountBatch = gate.ProposeMountBatch(
                [new CatalogMount("missing-platform", "mount-1")],
                "agent",
                "fk-test");
            var mountDecision = gate.ApproveBatch(mountBatch, "human", "qa");
            Assert.False(mountDecision.Committed);
            Assert.Contains(mountDecision.Errors, e => e.Contains("orphan_platform:missing-platform", StringComparison.Ordinal));

            var loadoutBatch = gate.ProposeLoadoutBatch(
                [new CatalogLoadout("missing-platform", "loadout-1", LoadoutName: "Orphan")],
                "agent",
                "fk-test");
            var loadoutDecision = gate.ApproveBatch(loadoutBatch, "human", "qa");
            Assert.False(loadoutDecision.Committed);
            Assert.Contains(loadoutDecision.Errors, e => e.Contains("orphan_platform:missing-platform", StringComparison.Ordinal));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Theory]
    [InlineData("submarine")]
    [InlineData("facility")]
    public void Curated_slice_fixture_triage_reports_zero_pending_child_rows_after_full_approve(string domain)
    {
        var markdown = domain switch
        {
            "submarine" => CmoMarkdownImporter.ResolveSubmarineSlice100FixturePath(),
            "facility" => CmoMarkdownImporter.ResolveFacilitySlice100FixturePath(),
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null),
        };

        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s32-slice-{domain}-{Guid.NewGuid():N}.db");
        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                markdown,
                maxRecords: 12,
                chunkSize: 500,
                clock: new FixedCatalogClock(32071 + domain.Length));

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(32072 + domain.Length)))
            {
                foreach (var batch in proposed.Batches)
                {
                    Assert.True(gate.ApproveBatch(batch.BatchId, "human", $"s32-triage-{domain}").Committed);
                }
            }

            var result = MountLoadoutQuarantineTriage.Run(dbPath, dryRun: true, entityHint: domain);
            var counts = Assert.Single(result.Before);
            Assert.Equal(domain, counts.Domain);
            Assert.Equal(0, counts.MountQuarantined);
            Assert.Equal(0, counts.LoadoutQuarantined);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Repair_envelope_documents_bounded_rules_only()
    {
        Assert.Equal(
            [
                MountLoadoutQuarantineRepairEnvelope.RuleLivePlatformFk,
                MountLoadoutQuarantineRepairEnvelope.RuleStagingPlatformFk,
                MountLoadoutQuarantineRepairEnvelope.RuleBalticSeedFk,
            ],
            MountLoadoutQuarantineRepairEnvelope.RepairRules);
    }

    private static (string PlatformBatchId, string MountBatchId, string LoadoutBatchId) StagePlatformMountLoadout(
        string dbPath,
        string platformId,
        bool approvePlatformOnly)
    {
        using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(32081));
        var platformBatch = gate.ProposePlatformBatch(
            [new CatalogPlatformBinding(platformId, "Triage Hull", Domain: "surface", PlatformClass: "Frigate")],
            "agent",
            "triage-test");
        var mountBatch = gate.ProposeMountBatch(
            [new CatalogMount(platformId, "mount-a", MountType: "vls", Capacity: 8)],
            "agent",
            "triage-test");
        var loadoutBatch = gate.ProposeLoadoutBatch(
            [new CatalogLoadout(platformId, "loadout-a", LoadoutName: "Default", Role: "asuw", IsDefault: true)],
            "agent",
            "triage-test");

        if (approvePlatformOnly)
        {
            Assert.True(gate.ApproveBatch(platformBatch, "human", "qa").Committed);
        }

        return (platformBatch, mountBatch, loadoutBatch);
    }

    private static int CountRows(SqliteConnection connection, string table, string? whereClause = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = whereClause is null
            ? $"SELECT COUNT(*) FROM {table}"
            : $"SELECT COUNT(*) FROM {table} WHERE {whereClause}";
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static int CountProposedStagingRows(SqliteConnection connection, string stagingTable)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"""
             SELECT COUNT(*)
             FROM {stagingTable} AS s
             INNER JOIN catalog_staging_batch AS b ON b.batch_id = s.batch_id
             WHERE b.approval_state = 'proposed'
             """;
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}