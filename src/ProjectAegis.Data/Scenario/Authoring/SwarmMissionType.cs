namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// SWARM-20 / C5: mission types that assign default operational modes for swarm tasking.
/// Stored as strings on scenario ORBAT units for JSON round-trip.
/// </summary>
public enum SwarmMissionType
{
    Patrol = 0,
    Support = 1,
    Strike = 2,
}

/// <summary>JSON/authoring string constants for <see cref="SwarmMissionType"/>.</summary>
public static class SwarmMissionTypeNames
{
    public const string Patrol = "Patrol";
    public const string Support = "Support";
    public const string Strike = "Strike";

    public static readonly string[] All =
    [
        Patrol,
        Support,
        Strike,
    ];

    public static bool TryParse(string? value, out SwarmMissionType missionType)
    {
        missionType = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (string.Equals(value.Trim(), Patrol, StringComparison.OrdinalIgnoreCase))
        {
            missionType = SwarmMissionType.Patrol;
            return true;
        }

        if (string.Equals(value.Trim(), Support, StringComparison.OrdinalIgnoreCase))
        {
            missionType = SwarmMissionType.Support;
            return true;
        }

        if (string.Equals(value.Trim(), Strike, StringComparison.OrdinalIgnoreCase))
        {
            missionType = SwarmMissionType.Strike;
            return true;
        }

        return false;
    }

    public static string ToName(SwarmMissionType missionType) => missionType switch
    {
        SwarmMissionType.Patrol => Patrol,
        SwarmMissionType.Support => Support,
        SwarmMissionType.Strike => Strike,
        _ => throw new ArgumentOutOfRangeException(nameof(missionType), missionType, "Unknown swarm mission type."),
    };
}

/// <summary>
/// SWARM-20: maps mission types to Phase B operational modes
/// (Hold, Assault, Screen, Scatter, Rejoin — see catalog defaults).
/// </summary>
public static class SwarmMissionDefaults
{
    /// <summary>Patrol missions default to Hold (stationary/area presence).</summary>
    public const string PatrolMode = Catalog.CatalogSwarmPlatformDefaults.ModeHold;

    /// <summary>Support missions default to Screen (host escort / defensive screen).</summary>
    public const string SupportMode = Catalog.CatalogSwarmPlatformDefaults.ModeScreen;

    /// <summary>Strike missions default to Assault.</summary>
    public const string StrikeMode = Catalog.CatalogSwarmPlatformDefaults.ModeAssault;

    /// <summary>Returns the default Phase B mode string for a mission type.</summary>
    public static string DefaultMode(SwarmMissionType missionType) => missionType switch
    {
        SwarmMissionType.Patrol => PatrolMode,
        SwarmMissionType.Support => SupportMode,
        SwarmMissionType.Strike => StrikeMode,
        _ => throw new ArgumentOutOfRangeException(nameof(missionType), missionType, "Unknown swarm mission type."),
    };

    /// <summary>
    /// Returns the default mode for a mission type name, or null when
    /// <paramref name="missionType"/> is null/empty. Throws when the name is unknown.
    /// </summary>
    public static string? DefaultMode(string? missionType)
    {
        if (string.IsNullOrWhiteSpace(missionType))
        {
            return null;
        }

        if (!SwarmMissionTypeNames.TryParse(missionType, out var parsed))
        {
            throw new ArgumentException(
                $"Unknown swarm mission type '{missionType}'. Expected one of: {string.Join(", ", SwarmMissionTypeNames.All)}.",
                nameof(missionType));
        }

        return DefaultMode(parsed);
    }

    /// <summary>
    /// When <paramref name="missionType"/> is set and <paramref name="explicitMode"/> is null/whitespace,
    /// returns the mission default mode; otherwise returns the trimmed explicit mode (or null).
    /// </summary>
    public static string? ResolveMode(string? missionType, string? explicitMode)
    {
        if (!string.IsNullOrWhiteSpace(explicitMode))
        {
            return explicitMode.Trim();
        }

        return DefaultMode(missionType);
    }
}
