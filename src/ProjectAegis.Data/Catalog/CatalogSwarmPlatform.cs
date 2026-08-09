namespace ProjectAegis.Data.Catalog;

/// <summary>
/// SWARM-01 / SWARM-21 Phase A: catalog row for a first-class drone/UAS swarm platform.
/// Aggregate integrity uses <see cref="MaxDrones"/>; runtime count lives on
/// <see cref="SwarmUnitIntegrity"/>. Distinct from <see cref="SwarmTier"/> (req-09 entity caps).
/// </summary>
public sealed record CatalogSwarmPlatform(
    string PlatformId,
    int MaxDrones,
    bool IsSwarm = true,
    string ArmorClass = CatalogSwarmPlatformDefaults.ArmorClassLightAir,
    string DefaultSensorId = "",
    string DefaultWeaponId = "",
    string ReviewState = CatalogReviewStates.Provisional,
    int TrlLevel = 9,
    string ValueTier = CatalogProvenanceTier.GameplayAbstraction,
    string CitationRef = "");

/// <summary>Starter constants for generic swarm catalog presets (doc 22 non-normative tuning).</summary>
public static class CatalogSwarmPlatformDefaults
{
    /// <summary>Abstract generic swarm platform id (Phase A only — national exemplars are Phase B+).
    /// Sorted after Baltic <c>u1</c> so existing seed/export row-0 assumptions stay stable.</summary>
    public const string GenericSwarmPlatformId = "uas-swarm-generic";

    public const string GenericDisplayName = "Generic UAS Swarm";

    public const string ArmorClassLightAir = "light-air";

    public const string DefaultSensorId = "swarm-eo-ir";

    public const string DefaultWeaponId = "swarm-munition-light";

    /// <summary>Doc 22 starter tuning: maxDrones = 40 logical.</summary>
    public const int GenericMaxDrones = 40;

    public const double GenericCombatRadiusNm = 120.0;

    public const double GenericLatDeg = 57.05;

    public const double GenericLonDeg = 20.05;
}
