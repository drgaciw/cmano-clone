using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.PlatformAssistant;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.PlatformAssistant;

/// <summary>
/// DRG-73 / PDA-04: headless golden for Platform Design Assistant against Baltic SQLite fixture.
/// </summary>
[Collection("CatalogSqlite")]
public sealed class PlatformDesignAssistantTests
{
    private readonly PlatformDesignAssistant _assistant = new();
    private readonly PlatformWorkbookWriteService _writeService = new();

    [Fact]
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

            Assert.NotEmpty(proposal.Peers);
            Assert.All(proposal.Peers, p => Assert.False(string.IsNullOrWhiteSpace(p.PlatformId)));
            Assert.Contains(PlatformRelativeScaler.SkillCatalogGrounding, proposal.SkillsApplied);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Propose_stages_extend_only_batches_without_mutating_live_sensors()
    {
        var dbPath = CreateTempDbPath("pda-propose");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var catalog = new SqliteCatalogReader(dbPath, "pda-propose");
            Assert.True(catalog.TryGetBasePd("u1", "radar-1", out var originalPd));

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

            Assert.False(string.IsNullOrWhiteSpace(result.PlatformBatchId));
            Assert.False(string.IsNullOrWhiteSpace(result.DamageBatchId));
            Assert.StartsWith("assistant:", result.Proposal.Binding.CitationRef, StringComparison.Ordinal);
            Assert.Contains(result.PlatformBatchId, result.BatchIds);
            Assert.Contains(result.DamageBatchId, result.BatchIds);

            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(12_002));
            var pending = gate.ListPendingBatches();
            Assert.Contains(pending, b => b.BatchId == result.PlatformBatchId);
            Assert.Contains(pending, b => b.BatchId == result.DamageBatchId);

            // Live catalog not mutated until ApproveBatch.
            using var live = new SqliteCatalogReader(dbPath, "pda-propose-live");
            Assert.True(live.TryGetBasePd("u1", "radar-1", out var stillPd));
            Assert.Equal(originalPd, stillPd, precision: 6);
            Assert.False(live.TryGetPlatformDamage("opv-baltic-scout", out _));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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

            Assert.True(result.Proposal.Binding.ApplyCorePosition);
            Assert.True(result.Proposal.CombatRadiusNm > 0);
            Assert.Equal(result.Proposal.CombatRadiusNm, result.Proposal.Binding.CombatRadiusNm, precision: 4);
            Assert.Equal(result.Proposal.LatDeg, result.Proposal.Binding.LatDeg, precision: 4);
            Assert.Equal(result.Proposal.LonDeg, result.Proposal.Binding.LonDeg, precision: 4);

            var write = new PlatformWorkbookWriteService();
            Assert.True(write.ApproveBatches(
                dbPath,
                [result.PlatformBatchId],
                new FixedCatalogClock(16_002),
                "human",
                "qa-reviewer").AllCommitted);

            using var live = new SqliteCatalogReader(dbPath, "pda-core-pos-live");
            Assert.True(live.TryGetCombatRadiusNm("opv-core-pos", out var radius));
            Assert.Equal(result.Proposal.CombatRadiusNm, radius, precision: 4);
            Assert.True(live.TryGetPlatformPosition("opv-core-pos", out var lat, out var lon));
            Assert.Equal(result.Proposal.LatDeg, lat, precision: 4);
            Assert.Equal(result.Proposal.LonDeg, lon, precision: 4);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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
            Assert.True(approvedPlatform.AllCommitted);

            var approvedDamage = write.ApproveBatches(
                dbPath,
                [result.DamageBatchId],
                new FixedCatalogClock(13_003),
                "human",
                "qa-reviewer");
            Assert.True(approvedDamage.AllCommitted);

            if (!string.IsNullOrWhiteSpace(result.MobilityBatchId))
            {
                var approvedMobility = write.ApproveBatches(
                    dbPath,
                    [result.MobilityBatchId!],
                    new FixedCatalogClock(13_004),
                    "human",
                    "qa-reviewer");
                Assert.True(approvedMobility.AllCommitted);
            }

            using var live = new SqliteCatalogReader(dbPath, "pda-approve-live");
            Assert.True(live.TryGetPlatformDamage("fac-red-01", out var damage));
            Assert.True(damage.MaxHp > 0);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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

            Assert.True(result.Import.Plan.SnapshotResolved);
            Assert.False(result.Import.Plan.HasChanges);
            Assert.False(result.Proposed);
            Assert.Empty(result.BatchIds);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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
        Assert.NotNull(platforms);
        Assert.Contains(platforms!.Rows, r => r.Count > 0 && r[0] == proposal.Binding.PlatformId);
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
