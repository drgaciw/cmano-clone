namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class BalticReplayHarnessCatalogTests
{
    [Test]
    public void Catalog_scenario_produces_deterministic_detection_hash()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol-catalog", ticks: 4, catalog: InMemoryCatalogReader.BalticPatrolFixture());
        var b = BalticReplayHarness.Run(42, "baltic-patrol-catalog", ticks: 4, catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(a.DetectionWorldHash, Is.Not.EqualTo(0UL));
    }

    [Test]
    public void Default_catalog_reader_matches_in_memory_fixture_hash()
    {
        var memory = BalticReplayHarness.Run(42, "baltic-patrol-catalog", ticks: 4, catalog: InMemoryCatalogReader.BalticPatrolFixture());
        if (CatalogReaderFactory.TryCreateBalticPatrolReader() is not SqliteCatalogReader sqlite)
        {
            Assert.Inconclusive("Repo-root catalog database path not available from test output directory.");
            return;
        }

        using (sqlite)
        {
            var seeded = BalticReplayHarness.Run(42, "baltic-patrol-catalog", ticks: 4, catalog: sqlite);
            Assert.That(seeded.DetectionWorldHash, Is.EqualTo(memory.DetectionWorldHash));
        }
    }
}