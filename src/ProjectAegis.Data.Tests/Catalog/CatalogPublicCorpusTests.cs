using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogPublicCorpusTests
{
    [Theory]
    [InlineData("aegis_public_corpus")]
    [InlineData("public-corpus")]
    public void TryResolvePublicCorpusDbRef_maps_enterprise_refs(string dbRef)
    {
        Assert.True(CatalogValidationDefaults.TryResolvePublicCorpusDbRef(dbRef, out var snapshotId));
        Assert.Equal(CatalogValidationDefaults.PublicCorpusSnapshotId, snapshotId);
    }

    [Fact]
    public void TryResolveBalticDbRef_does_not_capture_public_corpus_ref()
    {
        Assert.False(CatalogValidationDefaults.TryResolveBalticDbRef(
            CatalogValidationDefaults.PublicCorpusSnapshotId,
            out _));
    }

    [Fact]
    public void ReadSensorBindings_public_corpus_uses_catalog_platform_placeholder()
    {
        var previous = Environment.GetEnvironmentVariable("AEGIS_PUBLIC_CORPUS");
        try
        {
            Environment.SetEnvironmentVariable("AEGIS_PUBLIC_CORPUS", "1");
            var path = CmoMarkdownImporter.ResolveMiniFixturePath();
            var bindings = CmoMarkdownImporter.ReadSensorBindings(path, maxRecords: 3);

            Assert.Equal(3, bindings.Count);
            Assert.All(
                bindings,
                b => Assert.Equal(
                    CatalogValidationDefaults.PublicCorpusSensorCatalogPlatformId,
                    b.PlatformId));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AEGIS_PUBLIC_CORPUS", previous);
        }
    }

    [Fact]
    public void EnsureSchemaOnly_seeds_public_corpus_sensor_catalog_platform()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-public-corpus-schema-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.EnsureSchemaOnly(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "test");
            Assert.True(reader.TryGetCombatRadiusNm(
                CatalogValidationDefaults.PublicCorpusSensorCatalogPlatformId,
                out var radius));
            Assert.True(radius > 0);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void ScenarioPackage_resolves_public_corpus_dbRef()
    {
        var meta = new ScenarioMetadataDto
        {
            PolicyId = "enterprise-smoke",
            DbRef = "aegis_public_corpus",
        };
        var binding = ScenarioPackage.ResolveBinding(meta, catalog: null);
        Assert.Equal(CatalogValidationDefaults.PublicCorpusSnapshotId, binding.DbSnapshotId);
        Assert.Equal("aegis_public_corpus", binding.DbRef);
    }

    [Fact]
    public void CatalogReaderFactory_resolves_public_corpus_database_path()
    {
        var path = CatalogReaderFactory.ResolvePublicCorpusDatabasePath();
        Assert.EndsWith(CatalogValidationDefaults.PublicCorpusDatabaseFileName, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDatabasePathForDbRef_selects_public_corpus_for_enterprise_ref()
    {
        var path = CatalogReaderFactory.ResolveDatabasePathForDbRef("public-corpus");
        Assert.Contains(CatalogValidationDefaults.PublicCorpusDatabaseFileName, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Smoke_scenario_file_resolves_public_corpus_dbRef_to_enterprise_path()
    {
        var scenarioPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine(
                "assets",
                "data",
                "scenarios",
                "smoke",
                "enterprise-public-corpus-smoke.scenario.json"));
        Assert.True(File.Exists(scenarioPath), $"Missing smoke scenario: {scenarioPath}");

        var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        Assert.Equal("aegis_public_corpus", document.Metadata.DbRef);

        var package = ScenarioPackage.FromDocument("enterprise-public-corpus-smoke", document);
        Assert.Equal(CatalogValidationDefaults.PublicCorpusSnapshotId, package.DbSnapshotId);
        Assert.Equal("aegis_public_corpus", package.DbRef);

        var dbPath = CatalogReaderFactory.ResolveDatabasePathForDbRef(package.DbRef);
        Assert.Equal(
            Path.GetFullPath(CatalogReaderFactory.ResolvePublicCorpusDatabasePath()),
            Path.GetFullPath(dbPath));
        Assert.EndsWith(
            CatalogValidationDefaults.PublicCorpusDatabaseFileName,
            dbPath,
            StringComparison.OrdinalIgnoreCase);

        // When the promoted enterprise DB is present, OOB platform_ids should resolve.
        if (File.Exists(dbPath))
        {
            using var reader = new SqliteCatalogReader(dbPath, "enterprise-smoke");
            Assert.True(reader.TryResolveDbRef("aegis_public_corpus", out var snap));
            Assert.Equal(CatalogValidationDefaults.PublicCorpusSnapshotId, snap);
            Assert.NotNull(document.Orbat);
            Assert.Equal(2, document.Orbat!.Units.Count);
            foreach (var unit in document.Orbat.Units)
            {
                Assert.True(
                    reader.TryGetCombatRadiusNm(unit.PlatformId, out _),
                    $"Corpus missing platform_id used by smoke OOB: {unit.PlatformId}");
            }
        }
    }
}
