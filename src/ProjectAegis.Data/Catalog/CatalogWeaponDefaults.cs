namespace ProjectAegis.Data.Catalog;

/// <summary>P0 weapon envelope defaults when catalog has no per-weapon row yet.</summary>
public static class CatalogWeaponDefaults
{
    public static readonly WeaponEnvelopeDto MvpEnvelope = new(1_000, 100_000);

    /// <summary>S33-03 fixture: long-range weapon for kill-chain reach/sensor tests.</summary>
    public static readonly WeaponEnvelopeDto KillChainLongRangeEnvelope = new(1_000, 200_000);

    /// <summary>S33-03 fixture: short-range weapon within Baltic sensor/platform envelopes.</summary>
    public static readonly WeaponEnvelopeDto KillChainShortRangeEnvelope = new(500, 50_000);

    public static bool TryResolve(string weaponId, out WeaponEnvelopeDto envelope)
    {
        if (string.Equals(weaponId, CatalogWeaponIds.MvpDefault, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(weaponId))
        {
            envelope = MvpEnvelope;
            return true;
        }

        if (string.Equals(weaponId, CatalogWeaponIds.KillChainLongRange, StringComparison.OrdinalIgnoreCase))
        {
            envelope = KillChainLongRangeEnvelope;
            return true;
        }

        if (string.Equals(weaponId, CatalogWeaponIds.KillChainShortRange, StringComparison.OrdinalIgnoreCase))
        {
            envelope = KillChainShortRangeEnvelope;
            return true;
        }

        if (string.Equals(weaponId, CatalogWeaponIds.KillChainHypersonic, StringComparison.OrdinalIgnoreCase))
        {
            envelope = KillChainLongRangeEnvelope;
            return true;
        }

        envelope = default;
        return false;
    }
}