using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;
using Xunit;

namespace ProjectAegis.Data.Tests.Snapshots;

[Collection("CatalogSqlite")]
public sealed class CatalogGovernanceIntegrityTests
{
    [Fact]
    public void Audit_seeded_baltic_reports_dbi_gov_findings()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-gov-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var findings = CatalogGovernanceIntegrity.Audit(dbPath);

            Assert.Contains(findings, f => f.Code == CatalogGovernanceIntegrity.FindingEmptyContentHash);
            Assert.Contains(findings, f => f.Code == CatalogGovernanceIntegrity.FindingInertChangeLog);
            Assert.Contains(findings, f => f.Code == CatalogGovernanceIntegrity.FindingSchemaWatermarkDrift);
            Assert.False(CatalogGovernanceIntegrity.IsReleaseReady(dbPath));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void RecordRelease_rejects_empty_content_hash_dbi_gov_1()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-gov-hash-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var store = new DbSnapshotStore(dbPath);
            Assert.Throws<ArgumentException>(() =>
                store.RecordRelease(
                    "gov-integrity-reject",
                    CatalogValidationDefaults.BalticSnapshotId,
                    contentHashSha256: " ",
                    createdUtcTicks: 1));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void Audit_after_hash_backfill_clears_empty_hash_finding_for_snapshot()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-gov-ok-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            const string hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordRelease(
                    "gov-integrity-ok",
                    CatalogValidationDefaults.BalticSnapshotId,
                    hash,
                    createdUtcTicks: 42,
                    schemaVersion: CatalogTlTier.CatalogSchemaVersion);
            }

            var findings = CatalogGovernanceIntegrity.Audit(dbPath);
            Assert.DoesNotContain(
                findings,
                f => f.Code == CatalogGovernanceIntegrity.FindingEmptyContentHash
                     && f.Message.Contains(CatalogValidationDefaults.BalticSnapshotId, StringComparison.Ordinal));

            using var verify = new DbSnapshotStore(dbPath);
            Assert.True(verify.TryGetContentHash(CatalogValidationDefaults.BalticSnapshotId, out var recorded));
            Assert.Equal(hash, recorded);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}
