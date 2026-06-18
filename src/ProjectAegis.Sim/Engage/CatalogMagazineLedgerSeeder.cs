namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;

/// <summary>Seeds <see cref="MagazineLedger"/> from catalog loadout/magazine rows (read-only ADR-006).</summary>
public static class CatalogMagazineLedgerSeeder
{
    public static bool TrySeedInitialRounds(
        MagazineLedger ledger,
        ICatalogReader? catalog,
        string platformId,
        ulong shooterUnitId,
        ulong mountId,
        int fallbackRounds,
        out int seededRounds)
    {
        if (catalog != null)
        {
            var readiness = CatalogMagazineResolver.EvaluateInitialMagazine(platformId, catalog);
            if (readiness.CatalogResolved)
            {
                if (readiness.TotalRounds > 0)
                {
                    ledger.EnsureInitialRounds(shooterUnitId, mountId, readiness.TotalRounds);
                }

                seededRounds = readiness.TotalRounds;
                return true;
            }
        }

        if (fallbackRounds > 0)
        {
            ledger.EnsureInitialRounds(shooterUnitId, mountId, fallbackRounds);
            seededRounds = fallbackRounds;
            return false;
        }

        seededRounds = 0;
        return false;
    }
}