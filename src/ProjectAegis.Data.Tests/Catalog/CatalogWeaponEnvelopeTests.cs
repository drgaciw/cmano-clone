namespace ProjectAegis.Data.Tests.Catalog;

using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using Xunit;

[Collection("CatalogSqlite")]
public sealed class CatalogWeaponEnvelopeTests
{
    [Fact]
    public void InMemory_reader_resolves_mvp_default_envelope()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.True(catalog.TryGetWeaponEnvelope(CatalogWeaponIds.MvpDefault, out var envelope));
        Assert.Equal(1_000, envelope.MinRangeMeters);
        Assert.Equal(100_000, envelope.MaxRangeMeters);
    }

    [Fact]
    public void Null_reader_does_not_resolve_weapon_envelope()
    {
        Assert.False(NullCatalogReader.Instance.TryGetWeaponEnvelope("mvp-default", out _));
    }

    [Fact]
    public void Sqlite_reader_returns_weapon_catalog_envelope_then_falls_back_to_defaults()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-weapon-env-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "weapon-env-test");

            Assert.True(reader.TryGetWeaponEnvelope(CatalogWeaponIds.BalticRim66, out var rim));
            Assert.Equal(1_000, rim.MinRangeMeters);
            Assert.Equal(74_000, rim.MaxRangeMeters);

            Assert.True(reader.TryGetWeaponEnvelope(CatalogWeaponIds.BalticOto76, out var gun));
            Assert.Equal(0, gun.MinRangeMeters);
            Assert.Equal(16_000, gun.MaxRangeMeters);

            // Unknown catalog id still resolves via MVP default constants when applicable.
            Assert.True(reader.TryGetWeaponEnvelope(CatalogWeaponIds.MvpDefault, out var mvp));
            Assert.Equal(1_000, mvp.MinRangeMeters);
            Assert.Equal(100_000, mvp.MaxRangeMeters);

            Assert.False(reader.TryGetWeaponEnvelope("definitely-not-a-weapon-id", out _));
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