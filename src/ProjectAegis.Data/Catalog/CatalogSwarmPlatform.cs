namespace ProjectAegis.Data.Catalog;

/// <summary>
/// SWARM-01 / SWARM-21: catalog row for a first-class drone/UAS swarm platform.
/// Aggregate integrity uses <see cref="MaxDrones"/>; runtime count lives on
/// <see cref="SwarmUnitIntegrity"/>. Distinct from <see cref="SwarmTier"/> (req-09 entity caps).
/// Phase B: default mode, host constraints, <see cref="CecCapable"/> (SWARM-31 catalog gate).
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
    string CitationRef = "",
    string DefaultMode = CatalogSwarmPlatformDefaults.ModeHold,
    bool RequiresHost = false,
    string AllowedHostClasses = "",
    bool CecCapable = false);

/// <summary>Starter constants for generic swarm catalog presets (doc 22 non-normative tuning).</summary>
public static class CatalogSwarmPlatformDefaults
{
    /// <summary>Abstract generic swarm platform id (Phase A only — national exemplars are Phase B+).
    /// Sorted after Baltic <c>u1</c> so existing seed/export row-0 assumptions stay stable.</summary>
    public const string GenericSwarmPlatformId = "uas-swarm-generic";

    /// <summary>USN abstract CEC-capable UAS swarm exemplar (SWARM-31 Phase B). Sorts after generic.</summary>
    public const string UsnCecSwarmPlatformId = "usn-uas-swarm-cec";

    public const string GenericDisplayName = "Generic UAS Swarm";

    public const string UsnCecDisplayName = "USN CEC UAS Swarm";

    public const string ArmorClassLightAir = "light-air";

    public const string DefaultSensorId = "swarm-eo-ir";

    public const string DefaultWeaponId = "swarm-munition-light";

    public const string UsnCecSensorId = "usn-swarm-cec-radar";

    public const string UsnCecWeaponId = "usn-swarm-munition";

    /// <summary>Doc 22 starter tuning: maxDrones = 40 logical.</summary>
    public const int GenericMaxDrones = 40;

    public const double GenericCombatRadiusNm = 120.0;

    public const double GenericLatDeg = 57.05;

    public const double GenericLonDeg = 20.05;

    public const double UsnCecLatDeg = 57.15;

    public const double UsnCecLonDeg = 20.15;

    public const double UsnCecCombatRadiusNm = 150.0;

    public const string ModeHold = "Hold";
    public const string ModeAssault = "Assault";
    public const string ModeScreen = "Screen";
    public const string ModeScatter = "Scatter";
    public const string ModeRejoin = "Rejoin";

    public static readonly string[] ValidModes =
    [
        ModeHold, ModeAssault, ModeScreen, ModeScatter, ModeRejoin
    ];

    public static bool IsValidMode(string? mode) =>
        !string.IsNullOrWhiteSpace(mode) &&
        ValidModes.Contains(mode.Trim(), StringComparer.Ordinal);
}
