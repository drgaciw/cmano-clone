namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Engage;

/// <summary>Applies catalog weapon envelope to sim engage context (DATA-4).</summary>
public static class CatalogEngageEnvelope
{
    public static EngageContext Apply(
        EngageContext context,
        ICatalogReader? catalog,
        string weaponId = CatalogWeaponIds.MvpDefault)
    {
        if (catalog == null || !catalog.TryGetWeaponEnvelope(weaponId, out var dto))
        {
            return context;
        }

        return context with
        {
            Envelope = new WeaponEnvelope(dto.MinRangeMeters, dto.MaxRangeMeters),
        };
    }
}