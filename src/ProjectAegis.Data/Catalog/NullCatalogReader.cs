namespace ProjectAegis.Data.Catalog;

public sealed class NullCatalogReader : ICatalogReader
{
    public static readonly NullCatalogReader Instance = new();

    private static readonly CatalogSensorBinding[] Empty = Array.Empty<CatalogSensorBinding>();

    private NullCatalogReader()
    {
    }

    public string LayerVersion => "p0-scaffold";

    public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => Empty;

    public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
    {
        basePd = 0;
        return false;
    }

    public bool TryGetWeaponEnvelope(string weaponId, out WeaponEnvelopeDto envelope)
    {
        envelope = default;
        return false;
    }

    public IReadOnlyList<CatalogMobility> GetSortedMobility() => [];

    public IReadOnlyList<CatalogSignature> GetSortedSignatures() => [];

    public IReadOnlyList<CatalogEmcon> GetSortedEmcon() => [];

    public IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamage() => [];

    public bool TryGetMobility(string platformId, out CatalogMobility mobility)
    {
        mobility = new CatalogMobility(platformId);
        return false;
    }

    public bool TryGetSignature(string platformId, out CatalogSignature signature)
    {
        signature = new CatalogSignature(platformId);
        return false;
    }

    public bool TryGetEmcon(string platformId, string condition, string emitterId, out CatalogEmcon emcon)
    {
        emcon = new CatalogEmcon(platformId, condition, emitterId);
        return false;
    }

    public bool TryGetPlatformDamage(string platformId, out CatalogPlatformDamage damage)
    {
        damage = new CatalogPlatformDamage(platformId);
        return false;
    }
}
