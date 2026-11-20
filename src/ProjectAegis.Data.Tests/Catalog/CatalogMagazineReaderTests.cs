using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>Req-16 read path: loadout/magazine rows via ICatalogReader (write-gate seeded).</summary>
[Runner.CatalogSqliteCollection]
public sealed class CatalogMagazineReaderTests
{
    [Test]
    public void SqliteCatalogReader_exposes_magazine_rows_after_write_gate_approve()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s28-magazine-read-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(28001)))
            {
                var mountBatch = gate.ProposeMountBatch(
                    [new CatalogMount("u1", "vls-fwd", "vls", 360, 32)],
                    "agent",
                    "s28-read");
                var loadoutBatch = gate.ProposeLoadoutBatch(
                    [new CatalogLoadout("u1", "asuw-default", "ASUW Default", "asuw", IsDefault: true)],
                    "agent",
                    "s28-read");
                var magazineBatch = gate.ProposeMagazineBatch(
                    [new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, 12)],
                    "agent",
                    "s28-read");

                Assert.That(gate.ApproveBatch(mountBatch, "human", "s28-read").Committed, Is.True);
                Assert.That(gate.ApproveBatch(loadoutBatch, "human", "s28-read").Committed, Is.True);
                Assert.That(gate.ApproveBatch(magazineBatch, "human", "s28-read").Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "s28-read-test");
            Assert.That(reader.GetSortedLoadouts(), Has.Exactly(1).Items);
            Assert.That(reader.GetSortedMagazines(), Has.Exactly(1).Items);
            Assert.That(reader.GetSortedMagazines()[0].Quantity, Is.EqualTo(12));
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
}