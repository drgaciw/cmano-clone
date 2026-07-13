using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Agents;

[Collection("CatalogSqlite")]
public sealed class OrchestratorKillChainGateTests
{
    [Fact]
    public void DatabaseIntelligence_pipeline_agent_order_matches_documented_sequence()
    {
        var orchestrator = new DatabaseIntelligenceOrchestrator();
        var result = orchestrator.Run(InMemoryCatalogReader.BalticPatrolFixture());

        Assert.Equal(
            DatabaseIntelligenceOrchestrator.PipelineAgentOrder,
            result.Reports.Select(r => r.AgentId).ToArray());
    }

    [Fact]
    public void ApproveBatch_mount_batch_blocked_when_kill_chain_errors_present()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-kill-chain-gate-mount-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            SeedHypersonicMagazineChain(dbPath);

            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9950));
            var batchId = gate.ProposeMountBatch(
                [new CatalogMount("u1", "vls-aft", MountType: "vls", ReviewState: CatalogReviewStates.Approved)],
                "agent",
                "kill-chain-gate-test");

            var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
            Assert.False(decision.Committed);
            Assert.Contains(decision.Errors, r => r.StartsWith("kill_chain:", StringComparison.Ordinal));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void ApproveBatch_weapon_batch_blocked_when_kill_chain_errors_present()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-kill-chain-gate-weapon-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            SeedHypersonicMagazineChain(dbPath, weaponId: "kc-cli-weapon");

            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9951));
            var batchId = gate.ProposeWeaponBatch(
                [new CatalogWeaponRecord(
                    "kc-cli-weapon",
                    "KC CLI Weapon",
                    MinRangeMeters: 1000,
                    MaxRangeMeters: 250_000,
                    ReviewState: CatalogReviewStates.Approved)],
                "agent",
                "kill-chain-gate-test");

            var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
            Assert.False(decision.Committed);
            Assert.Contains(decision.Errors, r => r.StartsWith("kill_chain:", StringComparison.Ordinal));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Sim_export_slice_excludes_quarantined_sensor_bindings()
    {
        var reader = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, ReviewState: CatalogReviewStates.Approved),
                new CatalogSensorBinding("u1", "radar-quarantine", 0.05, ReviewState: CatalogReviewStates.Provisional),
            ],
            "kill-chain-export",
            [new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0)]);

        var export = new PlatformCatalogExportData(
            Platforms: [new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0)],
            Sensors: reader.GetSortedSensorBindings(),
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms: []);

        var filtered = CatalogTlExportFilter.Apply(export, CatalogTlTier.Tl0);

        Assert.DoesNotContain(filtered.Sensors, s => s.SensorId == "radar-quarantine");
        Assert.Contains(filtered.Sensors, s => s.SensorId == "radar-1");
        Assert.DoesNotContain(reader.GetSortedDependencyEdges(), e => e.SensorId == "radar-quarantine");
    }

    [Fact]
    public void KillChainCommitGate_surfaces_distinct_blocking_codes()
    {
        var reader = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 0.1, ReviewState: CatalogReviewStates.Approved)],
            "kill-chain-gate",
            [new CatalogPlatformEntry("u1", 57.0, 20.0, 50.0)],
            mobility: [new CatalogMobility("u1", MaxSpeedKnots: 32, RangeNm: 4200)],
            mounts: [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Approved)],
            magazines:
            [
                new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.KillChainHypersonic, 4),
            ]);

        var reasons = KillChainCommitGate.GetBlockingReasons(reader);
        Assert.NotEmpty(reasons);
        Assert.All(reasons, r => Assert.StartsWith("kill_chain:", r));
    }

    private static void SeedHypersonicMagazineChain(string dbPath, string weaponId = CatalogWeaponIds.KillChainHypersonic)
    {
        using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9949));
        var mountBatch = gate.ProposeMountBatch(
            [new CatalogMount("u1", "vls-fwd", MountType: "vls", ReviewState: CatalogReviewStates.Approved)],
            "agent",
            "seed");
        Assert.True(gate.ApproveBatch(mountBatch, "human", "seed").Committed);

        var mobilityBatch = gate.ProposeMobilityBatch(
            [new CatalogMobility("u1", MaxSpeedKnots: 32, RangeNm: 4200)],
            "agent",
            "seed");
        Assert.True(gate.ApproveBatch(mobilityBatch, "human", "seed").Committed);

        var loadoutBatch = gate.ProposeLoadoutBatch(
            [new CatalogLoadout("u1", "asuw-default", "ASUW Default", "asuw", IsDefault: true)],
            "agent",
            "seed");
        Assert.True(gate.ApproveBatch(loadoutBatch, "human", "seed").Committed);

        if (!string.Equals(weaponId, CatalogWeaponIds.KillChainHypersonic, StringComparison.Ordinal))
        {
            var weaponBatch = gate.ProposeWeaponBatch(
                [new CatalogWeaponRecord(
                    weaponId,
                    "Seed Weapon",
                    MinRangeMeters: 1000,
                    MaxRangeMeters: 80_000,
                    ReviewState: CatalogReviewStates.Approved)],
                "agent",
                "seed");
            Assert.True(gate.ApproveBatch(weaponBatch, "human", "seed").Committed);
        }

        var magazineBatch = gate.ProposeMagazineBatch(
            [new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", weaponId, 4)],
            "agent",
            "seed");
        Assert.True(gate.ApproveBatch(magazineBatch, "human", "seed").Committed);
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