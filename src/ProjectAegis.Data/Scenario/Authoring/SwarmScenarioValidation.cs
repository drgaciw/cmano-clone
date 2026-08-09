namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>SWARM-22 / B9: pure validation for scenario swarm place/configure.</summary>
public static class SwarmScenarioValidation
{
    public const string CountExceedsMax = "SWARM_COUNT_EXCEEDS_MAX";
    public const string CountInvalid = "SWARM_COUNT_INVALID";
    public const string PlatformMissing = "SWARM_PLATFORM_MISSING";
    public const string HostMissing = "SWARM_HOST_MISSING";

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
        bool hostExists)
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

        return Result.Ok();
    }
}
