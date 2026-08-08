namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Result of a pure air-ops transition attempt (LOG-08).
/// <see cref="State"/> is always the post-attempt state (unchanged when refused).
/// </summary>
public readonly record struct AirOpsFsmResult(bool Accepted, AirOpsUnitState State, string? RefusalReason);

/// <summary>
/// Per-unit group-launch result (stable unit id + transition result).
/// </summary>
public readonly record struct AirOpsGroupLaunchResult(string UnitId, AirOpsFsmResult Result);

/// <summary>
/// Pure deterministic air-ops FSM (doc 16 LOG-08 spine).
/// OnGround → Prepping → Taxiing → TakingOff → Airborne; abort windows; refusal codes.
/// No random, no wall-clock, no Unity.
/// </summary>
public static class AirOpsFsm
{
    public const int DefaultPrepTicks = 3;
    public const int DefaultTaxiTicks = 2;
    public const int DefaultTakeoffTicks = 1;

    /// <summary>Aligns with engage / logistics abort catalog (doc 16 LOG-04).</summary>
    public const string ReasonAirNotReady = "AIR_NOT_READY";

    public const string ReasonAlreadyAirborne = "AIR_ALREADY_AIRBORNE";
    public const string ReasonInMaintenance = "AIR_IN_MAINTENANCE";
    public const string ReasonAbortTooLate = "AIR_ABORT_TOO_LATE";
    public const string ReasonAbortNotActive = "AIR_ABORT_NOT_ACTIVE";
    public const string ReasonLaunchInProgress = "AIR_LAUNCH_IN_PROGRESS";

    /// <summary>
    /// Begin prepping from OnGround. Requires <see cref="AirOpsUnitState.ReadyForLaunch"/> unless
    /// <paramref name="force"/> is true (test / override policy).
    /// </summary>
    public static AirOpsFsmResult BeginPrep(AirOpsUnitState unit, int durationTicks, bool force = false)
    {
        if (unit.Phase != AirOpsPhase.OnGround)
        {
            return RefuseLaunchFromPhase(unit);
        }

        if (!unit.ReadyForLaunch && !force)
        {
            return new AirOpsFsmResult(false, unit, ReasonAirNotReady);
        }

        var ticks = durationTicks < 0 ? 0 : durationTicks;
        var next = unit with
        {
            Phase = AirOpsPhase.Prepping,
            TimeToReadyTicks = ticks,
            CanAbortLaunch = true,
        };
        return new AirOpsFsmResult(true, next, null);
    }

    /// <summary>
    /// Advance timers by <paramref name="deltaTicks"/> (default 1).
    /// Prepping → Taxiing → TakingOff → Airborne when each phase timer reaches 0.
    /// Durations for taxi/takeoff are overridable; prep duration is set at <see cref="BeginPrep"/>.
    /// </summary>
    public static AirOpsUnitState Tick(
        AirOpsUnitState state,
        int deltaTicks = 1,
        int taxiDurationTicks = DefaultTaxiTicks,
        int takeoffDurationTicks = DefaultTakeoffTicks)
    {
        if (deltaTicks <= 0 || !IsLaunchPipeline(state.Phase))
        {
            return state;
        }

        var phase = state.Phase;
        var timer = state.TimeToReadyTicks;
        var remaining = deltaTicks;
        var ready = state.ReadyForLaunch;

        while (remaining > 0 && IsLaunchPipeline(phase))
        {
            if (timer > remaining)
            {
                timer -= remaining;
                remaining = 0;
                break;
            }

            remaining -= timer;

            switch (phase)
            {
                case AirOpsPhase.Prepping:
                    phase = AirOpsPhase.Taxiing;
                    timer = taxiDurationTicks < 0 ? 0 : taxiDurationTicks;
                    break;
                case AirOpsPhase.Taxiing:
                    phase = AirOpsPhase.TakingOff;
                    timer = takeoffDurationTicks < 0 ? 0 : takeoffDurationTicks;
                    break;
                case AirOpsPhase.TakingOff:
                    phase = AirOpsPhase.Airborne;
                    timer = 0;
                    ready = false;
                    remaining = 0;
                    break;
                default:
                    remaining = 0;
                    break;
            }
        }

        return state with
        {
            Phase = phase,
            TimeToReadyTicks = timer,
            ReadyForLaunch = ready,
            CanAbortLaunch = IsLaunchPipeline(phase),
        };
    }

    /// <summary>
    /// Request launch: starts prep when OnGround + ready; otherwise refuses with a stable reason.
    /// </summary>
    public static AirOpsFsmResult RequestLaunch(
        AirOpsUnitState state,
        int prepDurationTicks = DefaultPrepTicks,
        bool force = false)
    {
        return state.Phase switch
        {
            AirOpsPhase.OnGround => BeginPrep(state, prepDurationTicks, force),
            AirOpsPhase.Maintenance => new AirOpsFsmResult(false, state, ReasonInMaintenance),
            AirOpsPhase.Airborne or AirOpsPhase.Landing =>
                new AirOpsFsmResult(false, state, ReasonAlreadyAirborne),
            AirOpsPhase.Prepping or AirOpsPhase.Taxiing or AirOpsPhase.TakingOff =>
                new AirOpsFsmResult(false, state, ReasonLaunchInProgress),
            _ => new AirOpsFsmResult(false, state, ReasonAirNotReady),
        };
    }

    /// <summary>
    /// Abort launch while still in Prepping | Taxiing | TakingOff → back to OnGround.
    /// Airborne refuses with <see cref="ReasonAbortTooLate"/>.
    /// </summary>
    public static AirOpsFsmResult AbortLaunch(AirOpsUnitState state)
    {
        if (state.Phase is AirOpsPhase.Prepping or AirOpsPhase.Taxiing or AirOpsPhase.TakingOff)
        {
            // Abort returns the airframe to the deck ready to re-launch.
            var next = state with
            {
                Phase = AirOpsPhase.OnGround,
                TimeToReadyTicks = 0,
                CanAbortLaunch = false,
                ReadyForLaunch = true,
            };
            return new AirOpsFsmResult(true, next, null);
        }

        if (state.Phase is AirOpsPhase.Airborne or AirOpsPhase.Landing)
        {
            return new AirOpsFsmResult(false, state, ReasonAbortTooLate);
        }

        return new AirOpsFsmResult(false, state, ReasonAbortNotActive);
    }

    /// <summary>
    /// Group launch: one result per input unit, ordered by unit id (deterministic).
    /// </summary>
    public static IReadOnlyList<AirOpsGroupLaunchResult> RequestGroupLaunch(
        IEnumerable<AirOpsUnitState> states,
        int prepDurationTicks = DefaultPrepTicks,
        bool force = false)
    {
        if (states is null)
        {
            return Array.Empty<AirOpsGroupLaunchResult>();
        }

        var ordered = states
            .Where(s => s is not null && !string.IsNullOrWhiteSpace(s.UnitId))
            .OrderBy(s => s.UnitId, StringComparer.Ordinal)
            .ToList();

        var results = new List<AirOpsGroupLaunchResult>(ordered.Count);
        foreach (var state in ordered)
        {
            var result = RequestLaunch(state, prepDurationTicks, force);
            results.Add(new AirOpsGroupLaunchResult(state.UnitId, result));
        }

        return results;
    }

    private static bool IsLaunchPipeline(AirOpsPhase phase) =>
        phase is AirOpsPhase.Prepping or AirOpsPhase.Taxiing or AirOpsPhase.TakingOff;

    private static AirOpsFsmResult RefuseLaunchFromPhase(AirOpsUnitState unit) =>
        unit.Phase switch
        {
            AirOpsPhase.Maintenance => new AirOpsFsmResult(false, unit, ReasonInMaintenance),
            AirOpsPhase.Airborne or AirOpsPhase.Landing =>
                new AirOpsFsmResult(false, unit, ReasonAlreadyAirborne),
            AirOpsPhase.Prepping or AirOpsPhase.Taxiing or AirOpsPhase.TakingOff =>
                new AirOpsFsmResult(false, unit, ReasonLaunchInProgress),
            _ => new AirOpsFsmResult(false, unit, ReasonAirNotReady),
        };
}
