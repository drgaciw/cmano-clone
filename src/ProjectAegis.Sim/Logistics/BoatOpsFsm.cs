namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Result of a pure boat-ops transition attempt (LOG-09…11).
/// <see cref="State"/> is always the post-attempt state (unchanged when refused).
/// </summary>
public readonly record struct BoatOpsFsmResult(bool Accepted, BoatOpsUnitState State, string? RefusalReason);

/// <summary>
/// Pure deterministic boat-ops FSM (doc 16 LOG-09…11 spine).
/// Stowed → Prepping → Launching → Waterborne;
/// Waterborne → Returning → Alongside → Recovering → Stowed;
/// Host-depart while waterborne → Stranded.
/// Launch gated by maxLaunchSeaState; recovery by maxRecoverySeaState (recovery is stricter).
/// No random, no wall-clock, no Unity.
/// </summary>
public static class BoatOpsFsm
{
    public const int DefaultPrepTicks = 2;
    public const int DefaultLaunchTicks = 2;
    public const int DefaultReturnTicks = 2;
    public const int DefaultAlongsideTicks = 1;
    public const int DefaultRecoverTicks = 2;

    /// <summary>Stable refusal when sea state / readiness blocks launch or recovery (doc 16 LOG-09).</summary>
    public const string ReasonBoatNotReady = "BOAT_NOT_READY";

    public const string ReasonEmbarkedOverload = "EMBARKED_OVERLOAD";
    public const string ReasonAlreadyWaterborne = "BOAT_ALREADY_WATERBORNE";
    public const string ReasonInMaintenance = "BOAT_IN_MAINTENANCE";
    public const string ReasonAbortTooLate = "BOAT_ABORT_TOO_LATE";
    public const string ReasonAbortNotActive = "BOAT_ABORT_NOT_ACTIVE";
    public const string ReasonLaunchInProgress = "BOAT_LAUNCH_IN_PROGRESS";
    public const string ReasonRecoveryInProgress = "BOAT_RECOVERY_IN_PROGRESS";
    public const string ReasonNotWaterborne = "BOAT_NOT_WATERBORNE";
    public const string ReasonStranded = "BOAT_STRANDED";
    public const string ReasonHostDepartNotWaterborne = "BOAT_HOST_DEPART_NOT_WATERBORNE";

    /// <summary>
    /// Refusal text for launch sea-state gate: includes the exceeded limit name and value.
    /// </summary>
    public static string FormatLaunchSeaStateRefusal(int maxLaunchSeaState) =>
        $"{ReasonBoatNotReady}:maxLaunchSeaState={ScenarioSeaState.Clamp(maxLaunchSeaState)}";

    /// <summary>
    /// Refusal text for recovery sea-state gate: includes the exceeded limit name and value.
    /// </summary>
    public static string FormatRecoverySeaStateRefusal(int maxRecoverySeaState) =>
        $"{ReasonBoatNotReady}:maxRecoverySeaState={ScenarioSeaState.Clamp(maxRecoverySeaState)}";

    /// <summary>
    /// Begin prepping from Stowed. Gates: sea ≤ maxLaunch, embarked within capacity.
    /// Launch may proceed when sea > maxRecovery but ≤ maxLaunch (recovery asymmetry).
    /// </summary>
    public static BoatOpsFsmResult BeginPrep(
        BoatOpsUnitState craft,
        int seaStateDouglas,
        int durationTicks = DefaultPrepTicks)
    {
        if (craft.Phase != BoatOpsPhase.Stowed)
        {
            return RefuseLaunchFromPhase(craft);
        }

        if (craft.IsEmbarkedOverloaded)
        {
            return new BoatOpsFsmResult(false, craft, ReasonEmbarkedOverload);
        }

        var sea = ScenarioSeaState.Clamp(seaStateDouglas);
        if (sea > craft.MaxLaunchSeaState)
        {
            return new BoatOpsFsmResult(false, craft, FormatLaunchSeaStateRefusal(craft.MaxLaunchSeaState));
        }

        var ticks = durationTicks < 0 ? 0 : durationTicks;
        var next = craft with
        {
            Phase = BoatOpsPhase.Prepping,
            TimeToReadyTicks = ticks,
            CanAbortLaunch = true,
        };
        return new BoatOpsFsmResult(true, next, null);
    }

    /// <summary>
    /// Request launch: starts prep when Stowed and sea/load gates pass.
    /// </summary>
    public static BoatOpsFsmResult RequestLaunch(
        BoatOpsUnitState state,
        int seaStateDouglas,
        int prepDurationTicks = DefaultPrepTicks)
    {
        return state.Phase switch
        {
            BoatOpsPhase.Stowed => BeginPrep(state, seaStateDouglas, prepDurationTicks),
            BoatOpsPhase.Maintenance => new BoatOpsFsmResult(false, state, ReasonInMaintenance),
            BoatOpsPhase.Waterborne or BoatOpsPhase.Returning or BoatOpsPhase.Alongside
                or BoatOpsPhase.Recovering =>
                new BoatOpsFsmResult(false, state, ReasonAlreadyWaterborne),
            BoatOpsPhase.Prepping or BoatOpsPhase.Launching =>
                new BoatOpsFsmResult(false, state, ReasonLaunchInProgress),
            BoatOpsPhase.Stranded => new BoatOpsFsmResult(false, state, ReasonStranded),
            _ => new BoatOpsFsmResult(false, state, ReasonBoatNotReady),
        };
    }

    /// <summary>
    /// Begin recovery from Waterborne. Refused when sea > maxRecovery (names the limit).
    /// </summary>
    public static BoatOpsFsmResult RequestRecover(
        BoatOpsUnitState state,
        int seaStateDouglas,
        int returnDurationTicks = DefaultReturnTicks)
    {
        if (state.Phase != BoatOpsPhase.Waterborne)
        {
            return state.Phase switch
            {
                BoatOpsPhase.Returning or BoatOpsPhase.Alongside or BoatOpsPhase.Recovering =>
                    new BoatOpsFsmResult(false, state, ReasonRecoveryInProgress),
                BoatOpsPhase.Stranded => new BoatOpsFsmResult(false, state, ReasonStranded),
                BoatOpsPhase.Stowed or BoatOpsPhase.Prepping or BoatOpsPhase.Launching
                    or BoatOpsPhase.Maintenance =>
                    new BoatOpsFsmResult(false, state, ReasonNotWaterborne),
                _ => new BoatOpsFsmResult(false, state, ReasonNotWaterborne),
            };
        }

        var sea = ScenarioSeaState.Clamp(seaStateDouglas);
        if (sea > state.MaxRecoverySeaState)
        {
            return new BoatOpsFsmResult(
                false,
                state,
                FormatRecoverySeaStateRefusal(state.MaxRecoverySeaState));
        }

        var ticks = returnDurationTicks < 0 ? 0 : returnDurationTicks;
        var next = state with
        {
            Phase = BoatOpsPhase.Returning,
            TimeToReadyTicks = ticks,
            CanAbortLaunch = false,
        };
        return new BoatOpsFsmResult(true, next, null);
    }

    /// <summary>
    /// Advance timers by <paramref name="deltaTicks"/> (default 1).
    /// Launch pipeline: Prepping → Launching → Waterborne.
    /// Recovery pipeline: Returning → Alongside → Recovering → Stowed.
    /// </summary>
    public static BoatOpsUnitState Tick(
        BoatOpsUnitState state,
        int deltaTicks = 1,
        int launchDurationTicks = DefaultLaunchTicks,
        int alongsideDurationTicks = DefaultAlongsideTicks,
        int recoverDurationTicks = DefaultRecoverTicks)
    {
        if (deltaTicks <= 0)
        {
            return state;
        }

        if (IsLaunchPipeline(state.Phase))
        {
            return TickLaunchPipeline(state, deltaTicks, launchDurationTicks);
        }

        if (IsRecoveryPipeline(state.Phase))
        {
            return TickRecoveryPipeline(
                state,
                deltaTicks,
                alongsideDurationTicks,
                recoverDurationTicks);
        }

        return state;
    }

    /// <summary>
    /// Abort launch while still in Prepping | Launching → back to Stowed.
    /// Waterborne refuses with <see cref="ReasonAbortTooLate"/>.
    /// </summary>
    public static BoatOpsFsmResult AbortLaunch(BoatOpsUnitState state)
    {
        if (state.Phase is BoatOpsPhase.Prepping or BoatOpsPhase.Launching)
        {
            var next = state with
            {
                Phase = BoatOpsPhase.Stowed,
                TimeToReadyTicks = 0,
                CanAbortLaunch = false,
            };
            return new BoatOpsFsmResult(true, next, null);
        }

        if (state.Phase is BoatOpsPhase.Waterborne
            or BoatOpsPhase.Returning
            or BoatOpsPhase.Alongside
            or BoatOpsPhase.Recovering
            or BoatOpsPhase.Stranded)
        {
            return new BoatOpsFsmResult(false, state, ReasonAbortTooLate);
        }

        return new BoatOpsFsmResult(false, state, ReasonAbortNotActive);
    }

    /// <summary>
    /// Host departs while craft is away from stow → Stranded (LOG-11).
    /// Applies to Waterborne and recovery-pipeline phases still off-ship.
    /// </summary>
    public static BoatOpsFsmResult HostDepartWhileWaterborne(BoatOpsUnitState state)
    {
        if (state.Phase is BoatOpsPhase.Waterborne
            or BoatOpsPhase.Returning
            or BoatOpsPhase.Alongside)
        {
            var next = state with
            {
                Phase = BoatOpsPhase.Stranded,
                TimeToReadyTicks = 0,
                CanAbortLaunch = false,
            };
            return new BoatOpsFsmResult(true, next, null);
        }

        return new BoatOpsFsmResult(false, state, ReasonHostDepartNotWaterborne);
    }

    private static BoatOpsUnitState TickLaunchPipeline(
        BoatOpsUnitState state,
        int deltaTicks,
        int launchDurationTicks)
    {
        var phase = state.Phase;
        var timer = state.TimeToReadyTicks;
        var remaining = deltaTicks;

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
                case BoatOpsPhase.Prepping:
                    phase = BoatOpsPhase.Launching;
                    timer = launchDurationTicks < 0 ? 0 : launchDurationTicks;
                    break;
                case BoatOpsPhase.Launching:
                    phase = BoatOpsPhase.Waterborne;
                    timer = 0;
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
            CanAbortLaunch = IsLaunchPipeline(phase),
        };
    }

    private static BoatOpsUnitState TickRecoveryPipeline(
        BoatOpsUnitState state,
        int deltaTicks,
        int alongsideDurationTicks,
        int recoverDurationTicks)
    {
        var phase = state.Phase;
        var timer = state.TimeToReadyTicks;
        var remaining = deltaTicks;

        while (remaining > 0 && IsRecoveryPipeline(phase))
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
                case BoatOpsPhase.Returning:
                    phase = BoatOpsPhase.Alongside;
                    timer = alongsideDurationTicks < 0 ? 0 : alongsideDurationTicks;
                    break;
                case BoatOpsPhase.Alongside:
                    phase = BoatOpsPhase.Recovering;
                    timer = recoverDurationTicks < 0 ? 0 : recoverDurationTicks;
                    break;
                case BoatOpsPhase.Recovering:
                    phase = BoatOpsPhase.Stowed;
                    timer = 0;
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
            CanAbortLaunch = false,
        };
    }

    private static bool IsLaunchPipeline(BoatOpsPhase phase) =>
        phase is BoatOpsPhase.Prepping or BoatOpsPhase.Launching;

    private static bool IsRecoveryPipeline(BoatOpsPhase phase) =>
        phase is BoatOpsPhase.Returning or BoatOpsPhase.Alongside or BoatOpsPhase.Recovering;

    private static BoatOpsFsmResult RefuseLaunchFromPhase(BoatOpsUnitState unit) =>
        unit.Phase switch
        {
            BoatOpsPhase.Maintenance => new BoatOpsFsmResult(false, unit, ReasonInMaintenance),
            BoatOpsPhase.Waterborne or BoatOpsPhase.Returning or BoatOpsPhase.Alongside
                or BoatOpsPhase.Recovering =>
                new BoatOpsFsmResult(false, unit, ReasonAlreadyWaterborne),
            BoatOpsPhase.Prepping or BoatOpsPhase.Launching =>
                new BoatOpsFsmResult(false, unit, ReasonLaunchInProgress),
            BoatOpsPhase.Stranded => new BoatOpsFsmResult(false, unit, ReasonStranded),
            _ => new BoatOpsFsmResult(false, unit, ReasonBoatNotReady),
        };
}
