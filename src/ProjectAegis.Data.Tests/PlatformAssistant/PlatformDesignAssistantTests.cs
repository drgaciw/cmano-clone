using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.PlatformAssistant;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.PlatformAssistant;

/// <summary>
/// DRG-73 / PDA-04: headless golden for Platform Design Assistant against Baltic SQLite fixture.
/// </summary>
[Runner.CatalogSqliteCollection]
public sealed class PlatformDesignAssistantTests
{
    private readonly PlatformDesignAssistant _assistant = new();
    private readonly PlatformWorkbookWriteService _writeService = new();

    [Test]
    public void Draft_peer_grounding_uses_export_platforms_on_baltic()
    {
        var dbPath = CreateTempDbPath("pda-draft-peers");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var catalog = new SqliteCatalogReader(dbPath, "pda-draft");
            var proposal = _assistant.Draft(
                catalog,
                new PlatformDesignBrief("opv-scout", "OPV Scout", Domain: "surface", RoleWeight: "light"));

            Assert.That(proposal.Peers, Is.Not.Empty);
            foreach (var p in proposal.Peers)
{
Assert.That(string.IsNullOrWhiteSpace(p.PlatformId), Is.False);
}
            Assert.That(proposal.SkillsApplied, Does.Contain(PlatformRelativeScaler.SkillCatalogGrounding));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Propose_stages_extend_only_batches_without_mutating_live_sensors()
    {
        var dbPath = CreateTempDbPath("pda-propose");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var catalog = new SqliteCatalogReader(dbPath, "pda-propose");
            Assert.That(catalog.TryGetBasePd("u1", "radar-1", out var originalPd), Is.True);

            var result = _assistant.Propose(
                dbPath,
                catalog,
                new PlatformDesignBrief(
                    "opv-baltic-scout",
                    "OPV Baltic Scout",
                    Domain: "surface",
                    RoleWeight: "light",
                    Concept: "patrol coastal",
                    WhatIf: true),
                new FixedCatalogClock(12_001));

            Assert.That(string.IsNullOrWhiteSpace(result.PlatformBatchId), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(result.DamageBatchId), Is.False);
            Assert.That(result.Proposal.Binding.CitationRef, Does.StartWith("assistant:"));
            Assert.That(result.BatchIds, Does.Contain(result.PlatformBatchId));
            Assert.That(result.BatchIds, Does.Contain(result.DamageBatchId));

            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(12_002));
            var pending = gate.ListPendingBatches();
            Assert.That(pending.Any(b => b.BatchId == result.PlatformBatchId), Is.True);
            Assert.That(pending.Any(b => b.BatchId == result.DamageBatchId), Is.True);

            // Live catalog not mutated until ApproveBatch.
            using var live = new SqliteCatalogReader(dbPath, "pda-propose-live");
            Assert.That(live.TryGetBasePd("u1", "radar-1", out var stillPd), Is.True);
            Assert.That(stillPd, Is.EqualTo(originalPd).Within(0.000005));
            Assert.That(live.TryGetPlatformDamage("opv-baltic-scout", out _), Is.False);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Propose_then_approve_preserves_scaled_combat_radius_and_lat_lon()
    {
        var dbPath = CreateTempDbPath("pda-core-pos");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var catalog = new SqliteCatalogReader(dbPath, "pda-core-pos");
            var result = _assistant.Propose(
                dbPath,
                catalog,
                new PlatformDesignBrief(
                    "opv-core-pos",
                    "OPV Core Pos",
                    Domain: "surface",
                    RoleWeight: "standard",
                    WhatIf: false),
                new FixedCatalogClock(16_001));

            Assert.That(result.Proposal.Binding.ApplyCorePosition, Is.True);
            Assert.That(result.Proposal.CombatRadiusNm > 0, Is.True);
            Assert.That(result.Proposal.Binding.CombatRadiusNm, Is.EqualTo(result.Proposal.CombatRadiusNm).Within(0.0005));
            Assert.That(result.Proposal.Binding.LatDeg, Is.EqualTo(result.Proposal.LatDeg).Within(0.0005));
            Assert.That(result.Proposal.Binding.LonDeg, Is.EqualTo(result.Proposal.LonDeg).Within(0.0005));

            var write = new PlatformWorkbookWriteService();
            Assert.That(write.ApproveBatches(
                dbPath,
                [result.PlatformBatchId],
                new FixedCatalogClock(16_002),
                "human",
                "qa-reviewer").AllCommitted, Is.True);

            using var live = new SqliteCatalogReader(dbPath, "pda-core-pos-live");
            Assert.That(live.TryGetCombatRadiusNm("opv-core-pos", out var radius), Is.True);
            Assert.That(radius, Is.EqualTo(result.Proposal.CombatRadiusNm).Within(0.0005));
            Assert.That(live.TryGetPlatformPosition("opv-core-pos", out var lat, out var lon), Is.True);
            Assert.That(lat, Is.EqualTo(result.Proposal.LatDeg).Within(0.0005));
            Assert.That(lon, Is.EqualTo(result.Proposal.LonDeg).Within(0.0005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Propose_then_approve_platform_before_damage_commits_extend_only()
    {
        var dbPath = CreateTempDbPath("pda-approve");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var catalog = new SqliteCatalogReader(dbPath, "pda-approve");
            var result = _assistant.Propose(
                dbPath,
                catalog,
                new PlatformDesignBrief("fac-red-01", "FAC Red", Domain: "surface", RoleWeight: "light", WhatIf: false),
                new FixedCatalogClock(13_001));

            var write = new PlatformWorkbookWriteService();
            var approvedPlatform = write.ApproveBatches(
                dbPath,
                [result.PlatformBatchId],
                new FixedCatalogClock(13_002),
                "human",
                "qa-reviewer");
            Assert.That(approvedPlatform.AllCommitted, Is.True);

            var approvedDamage = write.ApproveBatches(
                dbPath,
                [result.DamageBatchId],
                new FixedCatalogClock(13_003),
                "human",
                "qa-reviewer");
            Assert.That(approvedDamage.AllCommitted, Is.True);

            if (!string.IsNullOrWhiteSpace(result.MobilityBatchId))
            {
                var approvedMobility = write.ApproveBatches(
                    dbPath,
                    [result.MobilityBatchId!],
                    new FixedCatalogClock(13_004),
                    "human",
                    "qa-reviewer");
                Assert.That(approvedMobility.AllCommitted, Is.True);
            }

            using var live = new SqliteCatalogReader(dbPath, "pda-approve-live");
            Assert.That(live.TryGetPlatformDamage("fac-red-01", out var damage), Is.True);
            Assert.That(damage.MaxHp > 0, Is.True);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Unedited_workbook_empty_diff_golden_still_green_after_assistant_code()
    {
        var dbPath = CreateTempDbPath("pda-empty-diff");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var exported = _writeService.ExportFromDatabase(
                dbPath,
                CatalogValidationDefaults.BalticSnapshotId,
                new FixedCatalogClock(14_000));

            var result = _writeService.Propose(
                dbPath,
                exported,
                new FixedCatalogClock(14_001),
                "human",
                "drgamtd",
                "empty diff regression after PDA");

            Assert.That(result.Import.Plan.SnapshotResolved, Is.True);
            Assert.That(result.Import.Plan.HasChanges, Is.False);
            Assert.That(result.Proposed, Is.False);
            Assert.That(result.BatchIds, Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Workbook_emitter_appends_platform_row()
    {
        var export = new PlatformCatalogExportData(
            Platforms: [new CatalogPlatformEntry("u1", 57, 20, 100)],
            Sensors: [],
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms: [],
            Mobility: [new CatalogMobility("u1", MaxSpeedKnots: 20)],
            Damage: [new CatalogPlatformDamage("u1", MaxHp: 100)]);

        var proposal = PlatformRelativeScaler.Scale(
            export,
            new PlatformDesignBrief("new-opv", "New OPV", PeerPlatformIds: ["u1"]));

        var book = PlatformDesignWorkbookEmitter.Emit(
            export,
            proposal,
            CatalogValidationDefaults.BalticSnapshotId,
            new FixedCatalogClock(15_000));

        var platforms = book.FindSheet("Platforms");
        Assert.That(platforms, Is.Not.Null);
        Assert.That(platforms!.Rows.Any(r => r.Count > 0 && r[0] == proposal.Binding.PlatformId), Is.True);
    }

    private static string CreateTempDbPath(string label) =>
        Path.Combine(Path.GetTempPath(), $"aegis-{label}-{Guid.NewGuid():N}.db");

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
