using System.Globalization;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>S32-03 mount/loadout quarantine triage + bounded FK repair envelope.</summary>
[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class MountLoadoutQuarantineTriageTests
{
    [Test]
    public void Audit_identifies_pending_mount_loadout_child_rows_by_domain()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s32-triage-audit-{Guid.NewGuid():N}.db");
        try
        {
            StagePlatformMountLoadout(dbPath, "u-triage-hull", approvePlatformOnly: true);

            var result = MountLoadoutQuarantineTriage.Run(dbPath, dryRun: true, entityHint: "platform");

            var before = ProjectAegis.Data.Tests.AssertEx.Single(result.Before);
            Assert.That(before.Domain, Is.EqualTo(MountLoadoutQuarantineDomain.Platform));
            Assert.That(before.MountQuarantined, Is.EqualTo(1));
            Assert.That(before.LoadoutQuarantined, Is.EqualTo(1));
            Assert.That(before.Repairable, Is.EqualTo(2));
            Assert.That(before.OutOfEnvelope, Is.EqualTo(0));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Apply_repairs_in_envelope_rows_via_WriteGate_only()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s32-triage-repair-{Guid.NewGuid():N}.db");
        try
        {
            var batches = StagePlatformMountLoadout(dbPath, "u-triage-repair", approvePlatformOnly: true);

            var before = MountLoadoutQuarantineTriage.Audit(dbPath, entityHint: "platform");
            Assert.That(before[0].MountQuarantined, Is.EqualTo(1));
            Assert.That(before[0].LoadoutQuarantined, Is.EqualTo(1));

            var result = MountLoadoutQuarantineTriage.Run(
                dbPath,
                dryRun: false,
                entityHint: "platform",
                clock: new FixedCatalogClock(32041));

            Assert.That(result.DryRun, Is.False);
            Assert.That(result.RepairedBatchIds.Count, Is.EqualTo(2));
            Assert.That(result.RepairedBatchIds, Does.Contain(batches.MountBatchId));
            Assert.That(result.RepairedBatchIds, Does.Contain(batches.LoadoutBatchId));

            var after = ProjectAegis.Data.Tests.AssertEx.Single(result.After);
            Assert.That(after.MountQuarantined, Is.EqualTo(0));
            Assert.That(after.LoadoutQuarantined, Is.EqualTo(0));

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.That(CountRows(connection, "platform_mount", "platform_id = 'u-triage-repair'"), Is.EqualTo(1));
            Assert.That(CountRows(connection, "platform_loadout", "platform_id = 'u-triage-repair'"), Is.EqualTo(1));
            Assert.That(CountProposedStagingRows(connection, "catalog_staging_mount"), Is.EqualTo(0));
            Assert.That(CountProposedStagingRows(connection, "catalog_staging_loadout"), Is.EqualTo(0));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
                Assert.That(gate.ApproveBatch(mountBatch, "human", "qa").Committed, Is.False);
            }

            var result = MountLoadoutQuarantineTriage.Run(dbPath, dryRun: true, entityHint: "platform");
            var row = ProjectAegis.Data.Tests.AssertEx.Single(result.RemainingQuarantine);
            Assert.That(row.ChildKind, Is.EqualTo("mount"));
            Assert.That(row.Reason, Is.EqualTo(MountLoadoutQuarantineRepairEnvelope.ReasonOrphanPlatform));
            Assert.That(row.RepairRule, Is.Null);

            var applied = MountLoadoutQuarantineTriage.Run(
                dbPath,
                dryRun: false,
                entityHint: "platform",
                clock: new FixedCatalogClock(32052));
            Assert.That(applied.RepairedBatchIds, Is.Empty);
            Assert.That(applied.After[0].OutOfEnvelope, Is.EqualTo(1));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
            Assert.That(mountDecision.Committed, Is.False);
            Assert.That(mountDecision.Errors.Any(e => e.Contains("orphan_platform:missing-platform", StringComparison.Ordinal)), Is.True);

            var loadoutBatch = gate.ProposeLoadoutBatch(
                [new CatalogLoadout("missing-platform", "loadout-1", LoadoutName: "Orphan")],
                "agent",
                "fk-test");
            var loadoutDecision = gate.ApproveBatch(loadoutBatch, "human", "qa");
            Assert.That(loadoutDecision.Committed, Is.False);
            Assert.That(loadoutDecision.Errors.Any(e => e.Contains("orphan_platform:missing-platform", StringComparison.Ordinal)), Is.True);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }
    [TestCase("submarine")]
    [TestCase("facility")]
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
                    Assert.That(gate.ApproveBatch(batch.BatchId, "human", $"s32-triage-{domain}").Committed, Is.True);
                }
            }

            var result = MountLoadoutQuarantineTriage.Run(dbPath, dryRun: true, entityHint: domain);
            var counts = ProjectAegis.Data.Tests.AssertEx.Single(result.Before);
            Assert.That(counts.Domain, Is.EqualTo(domain));
            Assert.That(counts.MountQuarantined, Is.EqualTo(0));
            Assert.That(counts.LoadoutQuarantined, Is.EqualTo(0));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Repair_envelope_documents_bounded_rules_only()
    {
        Assert.That(            MountLoadoutQuarantineRepairEnvelope.RepairRules, Is.EqualTo(            [
                MountLoadoutQuarantineRepairEnvelope.RuleLivePlatformFk,
                MountLoadoutQuarantineRepairEnvelope.RuleStagingPlatformFk,
                MountLoadoutQuarantineRepairEnvelope.RuleBalticSeedFk,
            ]));
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
            Assert.That(gate.ApproveBatch(platformBatch, "human", "qa").Committed, Is.True);
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