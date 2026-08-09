namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>SWARM-22 / B9 + SWARM-20 / C5: pure validation for scenario swarm place/configure.</summary>
public static class SwarmScenarioValidation
{
    public const string CountExceedsMax = "SWARM_COUNT_EXCEEDS_MAX";
    public const string CountInvalid = "SWARM_COUNT_INVALID";
    public const string PlatformMissing = "SWARM_PLATFORM_MISSING";
    public const string HostMissing = "SWARM_HOST_MISSING";
    public const string MissionTypeUnknown = "SWARM_MISSION_TYPE_UNKNOWN";
    public const string ModeUnknown = "SWARM_MODE_UNKNOWN";

    public readonly record struct Result(bool IsValid, string ErrorCode, string Message)
    {
        public static Result Ok() => new(true, "", "");
        public static Result Fail(string code, string message) => new(false, code, message);
    }

    public static Result ValidatePlacement(
        string? platformId,
        int? droneCount,
        int? maxDrones,
        string? hostUnitId,
        bool hostExists,
        string? missionType = null,
        string? mode = null)
    {
        if (string.IsNullOrWhiteSpace(platformId))
        {
            return Result.Fail(PlatformMissing, "Platform id is required for swarm placement.");
        }

        if (droneCount is < 0 or 0)
        {
            return Result.Fail(CountInvalid, "Drone count must be a positive integer when specified.");
        }

        if (droneCount is int count && maxDrones is int max && count > max)
        {
            return Result.Fail(
                CountExceedsMax,
                $"Drone count {count} exceeds maxDrones {max}.");
        }

        if (!string.IsNullOrWhiteSpace(hostUnitId) && !hostExists)
        {
            return Result.Fail(HostMissing, $"Host unit '{hostUnitId}' is not present in ORBAT.");
        }

        if (!string.IsNullOrWhiteSpace(missionType) &&
            !SwarmMissionTypeNames.TryParse(missionType, out _))
        {
            return Result.Fail(
                MissionTypeUnknown,
                $"Unknown swarm mission type '{missionType}'. Expected one of: {string.Join(", ", SwarmMissionTypeNames.All)}.");
        }

        if (!string.IsNullOrWhiteSpace(mode) &&
            !Catalog.CatalogSwarmPlatformDefaults.IsValidMode(mode))
        {
            return Result.Fail(
                ModeUnknown,
                $"Unknown swarm mode '{mode}'. Expected one of: {string.Join(", ", Catalog.CatalogSwarmPlatformDefaults.ValidModes)}.");
        }

        return Result.Ok();
    }

    /// <summary>
    /// Resolves canonical mission type name + effective mode for authoring.
    /// Unknown mission types fail; when mode is omitted, applies <see cref="SwarmMissionDefaults"/>.
    /// </summary>
    public static Result ResolveMissionAssignment(
        string? missionType,
        string? explicitMode,
        out string? canonicalMissionType,
        out string? effectiveMode)
    {
        canonicalMissionType = null;
        effectiveMode = null;

        if (!string.IsNullOrWhiteSpace(missionType))
        {
            if (!SwarmMissionTypeNames.TryParse(missionType, out var parsed))
            {
                return Result.Fail(
                    MissionTypeUnknown,
                    $"Unknown swarm mission type '{missionType}'. Expected one of: {string.Join(", ", SwarmMissionTypeNames.All)}.");
            }

            canonicalMissionType = SwarmMissionTypeNames.ToName(parsed);
        }

        if (!string.IsNullOrWhiteSpace(explicitMode))
        {
            if (!Catalog.CatalogSwarmPlatformDefaults.IsValidMode(explicitMode))
            {
                return Result.Fail(
                    ModeUnknown,
                    $"Unknown swarm mode '{explicitMode}'. Expected one of: {string.Join(", ", Catalog.CatalogSwarmPlatformDefaults.ValidModes)}.");
            }

            effectiveMode = explicitMode.Trim();
        }
        else if (canonicalMissionType is not null)
        {
            effectiveMode = SwarmMissionDefaults.DefaultMode(
                SwarmMissionTypeNames.TryParse(canonicalMissionType, out var mt)
                    ? mt
                    : throw new InvalidOperationException("Canonical mission type failed re-parse."));
        }

        return Result.Ok();
    }
}
