namespace ProjectAegis.Data.Catalog;

/// <summary>P0 weapon envelope defaults when catalog has no per-weapon row yet.</summary>
public static class CatalogWeaponDefaults
{
    public static readonly WeaponEnvelopeDto MvpEnvelope = new(1_000, 100_000);

    public static bool TryResolve(string weaponId, out WeaponEnvelopeDto envelope)
    {
        if (string.Equals(weaponId, CatalogWeaponIds.MvpDefault, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(weaponId))
        {
            envelope = MvpEnvelope;
            return true;
        }

        envelope = default;
        return false;
    }
}