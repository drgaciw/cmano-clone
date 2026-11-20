using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogPublicCorpusTests
{
    [TestCase("aegis_public_corpus")]
    [TestCase("public-corpus")]
    public void TryResolvePublicCorpusDbRef_maps_enterprise_refs(string dbRef)
    {
        Assert.That(CatalogValidationDefaults.TryResolvePublicCorpusDbRef(dbRef, out var snapshotId), Is.True);
        Assert.That(snapshotId, Is.EqualTo(CatalogValidationDefaults.PublicCorpusSnapshotId));
    }

    [Test]
    public void TryResolveBalticDbRef_does_not_capture_public_corpus_ref()
    {
        Assert.That(CatalogValidationDefaults.TryResolveBalticDbRef(
            CatalogValidationDefaults.PublicCorpusSnapshotId,
            out _), Is.False);
    }

    [Test]
    public void ReadSensorBindings_public_corpus_uses_catalog_platform_placeholder()
    {
        var previous = Environment.GetEnvironmentVariable("AEGIS_PUBLIC_CORPUS");
        try
        {
            Environment.SetEnvironmentVariable("AEGIS_PUBLIC_CORPUS", "1");
            var path = CmoMarkdownImporter.ResolveMiniFixturePath();
            var bindings = CmoMarkdownImporter.ReadSensorBindings(path, maxRecords: 3);

            Assert.That(bindings.Count, Is.EqualTo(3));
            foreach (var b in                 bindings)
{
Assert.That(                    b.PlatformId, Is.EqualTo(                    CatalogValidationDefaults.PublicCorpusSensorCatalogPlatformId));
}
        }
        finally
        {
            Environment.SetEnvironmentVariable("AEGIS_PUBLIC_CORPUS", previous);
        }
    }

    [Test]
    public void EnsureSchemaOnly_seeds_public_corpus_sensor_catalog_platform()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-public-corpus-schema-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.EnsureSchemaOnly(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "test");
            Assert.That(reader.TryGetCombatRadiusNm(
                CatalogValidationDefaults.PublicCorpusSensorCatalogPlatformId,
                out var radius), Is.True);
            Assert.That(radius > 0, Is.True);
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

    [Test]
    public void ScenarioPackage_resolves_public_corpus_dbRef()
    {
        var meta = new ScenarioMetadataDto
        {
            PolicyId = "enterprise-smoke",
            DbRef = "aegis_public_corpus",
        };
        var binding = ScenarioPackage.ResolveBinding(meta, catalog: null);
        Assert.That(binding.DbSnapshotId, Is.EqualTo(CatalogValidationDefaults.PublicCorpusSnapshotId));
        Assert.That(binding.DbRef, Is.EqualTo("aegis_public_corpus"));
    }

    [Test]
    public void CatalogReaderFactory_resolves_public_corpus_database_path()
    {
        var path = CatalogReaderFactory.ResolvePublicCorpusDatabasePath();
        Assert.That(path, Does.EndWith(CatalogValidationDefaults.PublicCorpusDatabaseFileName).IgnoreCase);
    }

    [Test]
    public void ResolveDatabasePathForDbRef_selects_public_corpus_for_enterprise_ref()
    {
        var path = CatalogReaderFactory.ResolveDatabasePathForDbRef("public-corpus");
        Assert.That(path, Does.Contain(CatalogValidationDefaults.PublicCorpusDatabaseFileName).IgnoreCase);
    }

    [Test]
    public void Smoke_scenario_file_resolves_public_corpus_dbRef_to_enterprise_path()
    {
        var scenarioPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine(
                "assets",
                "data",
                "scenarios",
                "smoke",
                "enterprise-public-corpus-smoke.scenario.json"));
        Assert.That(File.Exists(scenarioPath), Is.True, $"Missing smoke scenario: {scenarioPath}");

        var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        Assert.That(document.Metadata.DbRef, Is.EqualTo("aegis_public_corpus"));

        var package = ScenarioPackage.FromDocument("enterprise-public-corpus-smoke", document);
        Assert.That(package.DbSnapshotId, Is.EqualTo(CatalogValidationDefaults.PublicCorpusSnapshotId));
        Assert.That(package.DbRef, Is.EqualTo("aegis_public_corpus"));

        var dbPath = CatalogReaderFactory.ResolveDatabasePathForDbRef(package.DbRef);
        Assert.That(            Path.GetFullPath(dbPath), Is.EqualTo(            Path.GetFullPath(CatalogReaderFactory.ResolvePublicCorpusDatabasePath())));
        Assert.That(            dbPath, Does.EndWith(            CatalogValidationDefaults.PublicCorpusDatabaseFileName).IgnoreCase);

        // When the promoted enterprise DB is present (not a Git LFS pointer stub),
        // OOB platform_ids should resolve. Pointer stubs arrive when checkout omits LFS.
        if (File.Exists(dbPath) && LooksLikeSqliteDatabase(dbPath))
        {
            using var reader = new SqliteCatalogReader(dbPath, "enterprise-smoke");
            Assert.That(reader.TryResolveDbRef("aegis_public_corpus", out var snap), Is.True);
            Assert.That(snap, Is.EqualTo(CatalogValidationDefaults.PublicCorpusSnapshotId));
            Assert.That(document.Orbat, Is.Not.Null);
            Assert.That(document.Orbat!.Units.Count, Is.EqualTo(2));
            foreach (var unit in document.Orbat.Units)
            {
                Assert.That(                    reader.TryGetCombatRadiusNm(unit.PlatformId, out _), Is.True,                     $"Corpus missing platform_id used by smoke OOB: {unit.PlatformId}");
            }
        }
    }

    private static bool LooksLikeSqliteDatabase(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[16];
            var read = stream.Read(header);
            // SQLite magic prefix: "SQLite format 3"
            ReadOnlySpan<byte> magic = "SQLite format 3"u8;
            return read >= magic.Length && header[..magic.Length].SequenceEqual(magic);
        }
        catch (IOException)
        {
            return false;
        }
    }
}
