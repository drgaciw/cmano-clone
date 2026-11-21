using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using ProjectAegis.Data.Catalog;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// UAT D19: the public factory entry points must never rewrite the committed catalog files.
/// Files are snapshotted up front and restored in TearDown so a failing run leaves them byte-identical.
/// </summary>
[Runner.CatalogSqliteCollection]
public sealed class CatalogReaderFactoryShippedBytesTests
{
    private readonly Dictionary<string, byte[]> _originals = new(StringComparer.Ordinal);

    [SetUp]
    public void SnapshotShippedCatalogs()
    {
        _originals.Clear();
        foreach (var path in new[]
                 {
                     CatalogReaderFactory.ResolveBalticPatrolDatabasePath(),
                     CatalogReaderFactory.ResolvePublicCorpusDatabasePath(),
                 })
        {
            if (File.Exists(path))
            {
                _originals[path] = File.ReadAllBytes(path);
            }
        }
    }

    [TearDown]
    public void RestoreShippedCatalogs()
    {
        SqliteConnection.ClearAllPools();
        foreach (var (path, bytes) in _originals)
        {
            if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
            {
                File.WriteAllBytes(path, bytes);
            }

            File.Delete(path + "-wal");
            File.Delete(path + "-shm");
            File.Delete(path + "-journal");
        }
    }

    [Test]
    public void Factory_baltic_patrol_reader_leaves_shipped_bytes_unchanged()
    {
        var path = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        Assert.That(File.Exists(path), Is.True, $"Catalog DB missing: {path}");
        var before = Hash(path);

        using (var reader = (IDisposable?)CatalogReaderFactory.TryCreateBalticPatrolReader())
        {
            Assert.That(reader, Is.Not.Null);
            Assert.That(((ICatalogReader)reader!).GetSortedSensorBindings().Count, Is.GreaterThan(0));
        }

        SqliteConnection.ClearAllPools();
        Assert.That(Hash(path), Is.EqualTo(before));
    }

    [Test]
    public void Factory_public_corpus_reader_leaves_shipped_bytes_unchanged()
    {
        var path = CatalogReaderFactory.ResolvePublicCorpusDatabasePath();
        Assert.That(File.Exists(path), Is.True, $"Catalog DB missing: {path}");
        var before = Hash(path);

        using (var reader = (IDisposable?)CatalogReaderFactory.TryCreatePublicCorpusReader())
        {
            Assert.That(reader, Is.Not.Null);
            _ = ((ICatalogReader)reader!).GetSortedSensorBindings();
        }

        SqliteConnection.ClearAllPools();
        Assert.That(Hash(path), Is.EqualTo(before));
    }

    [Test]
    public void Intelligence_run_over_shipped_baltic_catalog_leaves_bytes_unchanged()
    {
        var path = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        Assert.That(File.Exists(path), Is.True, $"Catalog DB missing: {path}");
        var before = Hash(path);

        using (var reader = (IDisposable?)CatalogReaderFactory.TryCreateBalticPatrolReader())
        {
            Assert.That(reader, Is.Not.Null);
            _ = new ProjectAegis.Data.Agents.DatabaseIntelligenceOrchestrator().Run((ICatalogReader)reader!, path);
        }

        SqliteConnection.ClearAllPools();
        Assert.That(Hash(path), Is.EqualTo(before));
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
