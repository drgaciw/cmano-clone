namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Logistics;

/// <summary>
/// Pure projection of boat-ops FSM into Boat Ops rows (CMD-25 / LOG-09…11).
/// No Unity / orchestrator dependency — callers supply domain states + scenario sea state.
/// </summary>
public static class BoatOpsProjection
{
    public const string StatusStowed = "STOWED";
    public const string StatusWaterborne = "WATERBORNE";
    public const string StatusStranded = "STRANDED";
    public const string MissingLabel = "—";

    public static string BoatNotReadyCode { get; } = BoatOpsFsm.ReasonBoatNotReady;

    /// <summary>
    /// Project boat-ops rows from domain states under a scenario sea-state scalar.
    /// </summary>
    public static IReadOnlyList<BoatOpsEntry> Project(
        IReadOnlyList<BoatOpsUnitState>? states,
        int seaStateDouglas = 0)
    {
        if (states is null || states.Count == 0)
        {
            return Array.Empty<BoatOpsEntry>();
        }

        var sea = ScenarioSeaState.Clamp(seaStateDouglas);
        var result = new List<BoatOpsEntry>(states.Count);
        foreach (var state in states
                     .Where(s => s is not null && !string.IsNullOrWhiteSpace(s.CraftId))
                     .OrderBy(s => s.CraftId, StringComparer.Ordinal))
        {
            result.Add(ToEntry(state, sea));
        }

        return result;
    }

    /// <summary>Aggregate stowed-ready count + summary line for group / header status.</summary>
    public static BoatOpsAggregate Aggregate(IReadOnlyList<BoatOpsEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return BoatOpsAggregate.Empty;
        }

        var ready = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].CanLaunch)
            {
                ready++;
            }
        }

        var total = entries.Count;
        return new BoatOpsAggregate(ready, total, FormatSummaryLine(ready, total));
    }

    public static string FormatSummaryLine(int readyCount, int totalCount) =>
        $"READY {readyCount}/{totalCount}";

    public static string FormatPhaseLabel(BoatOpsPhase phase) => phase.ToString();

    /// <summary>
    /// Resolve launch-disabled reason from FSM phase + sea + load (null when launchable).
    /// </summary>
    public static string? ResolveLaunchDisabledReason(BoatOpsUnitState state, int seaStateDouglas)
    {
        if (state.Phase == BoatOpsPhase.Stowed
            && !state.IsEmbarkedOverloaded
            && ScenarioSeaState.Clamp(seaStateDouglas) <= state.MaxLaunchSeaState)
        {
            return null;
        }

        if (state.IsEmbarkedOverloaded && state.Phase == BoatOpsPhase.Stowed)
        {
            return BoatOpsFsm.ReasonEmbarkedOverload;
        }

        var sea = ScenarioSeaState.Clamp(seaStateDouglas);
        if (state.Phase == BoatOpsPhase.Stowed && sea > state.MaxLaunchSeaState)
        {
            return BoatOpsFsm.FormatLaunchSeaStateRefusal(state.MaxLaunchSeaState);
        }

        return state.Phase switch
        {
            BoatOpsPhase.Maintenance => BoatOpsFsm.ReasonInMaintenance,
            BoatOpsPhase.Waterborne or BoatOpsPhase.Returning or BoatOpsPhase.Alongside
                or BoatOpsPhase.Recovering =>
                BoatOpsFsm.ReasonAlreadyWaterborne,
            BoatOpsPhase.Prepping or BoatOpsPhase.Launching =>
                BoatOpsFsm.ReasonLaunchInProgress,
            BoatOpsPhase.Stranded => BoatOpsFsm.ReasonStranded,
            _ => BoatOpsFsm.ReasonBoatNotReady,
        };
    }

    /// <summary>
    /// Resolve recover-disabled reason (null when recoverable from Waterborne under sea limit).
    /// </summary>
    public static string? ResolveRecoverDisabledReason(BoatOpsUnitState state, int seaStateDouglas)
    {
        if (state.Phase == BoatOpsPhase.Waterborne
            && ScenarioSeaState.Clamp(seaStateDouglas) <= state.MaxRecoverySeaState)
        {
            return null;
        }

        if (state.Phase == BoatOpsPhase.Waterborne
            && ScenarioSeaState.Clamp(seaStateDouglas) > state.MaxRecoverySeaState)
        {
            return BoatOpsFsm.FormatRecoverySeaStateRefusal(state.MaxRecoverySeaState);
        }

        return state.Phase switch
        {
            BoatOpsPhase.Returning or BoatOpsPhase.Alongside or BoatOpsPhase.Recovering =>
                BoatOpsFsm.ReasonRecoveryInProgress,
            BoatOpsPhase.Stranded => BoatOpsFsm.ReasonStranded,
            _ => BoatOpsFsm.ReasonNotWaterborne,
        };
    }

    private static BoatOpsEntry ToEntry(BoatOpsUnitState state, int sea)
    {
        var hostLabel = string.IsNullOrWhiteSpace(state.HostId) ? MissingLabel : state.HostId!.Trim();
        var phaseLabel = FormatPhaseLabel(state.Phase);
        var launchOk = sea <= state.MaxLaunchSeaState;
        var recoveryOk = sea <= state.MaxRecoverySeaState;
        var canLaunch = state.Phase == BoatOpsPhase.Stowed
            && !state.IsEmbarkedOverloaded
            && launchOk;
        var canRecover = state.Phase == BoatOpsPhase.Waterborne && recoveryOk;
        var canAbort = state.CanAbortLaunch
            || state.Phase is BoatOpsPhase.Prepping or BoatOpsPhase.Launching;
        var launchDisabled = canLaunch ? null : ResolveLaunchDisabledReason(state, sea);
        var recoverDisabled = canRecover ? null : ResolveRecoverDisabledReason(state, sea);

        string status;
        string? refusal;
        if (state.Phase == BoatOpsPhase.Stowed)
        {
            if (state.IsEmbarkedOverloaded)
            {
                status = $"OVERLOAD · {BoatOpsFsm.ReasonEmbarkedOverload}";
                refusal = BoatOpsFsm.ReasonEmbarkedOverload;
            }
            else if (!launchOk)
            {
                status = $"NOT READY · {BoatOpsFsm.FormatLaunchSeaStateRefusal(state.MaxLaunchSeaState)}";
                refusal = BoatOpsFsm.ReasonBoatNotReady;
            }
            else
            {
                status = StatusStowed;
                refusal = null;
            }
        }
        else if (state.Phase == BoatOpsPhase.Maintenance)
        {
            status = $"MAINT · {BoatOpsFsm.ReasonInMaintenance}";
            refusal = BoatOpsFsm.ReasonInMaintenance;
        }
        else if (state.Phase == BoatOpsPhase.Waterborne)
        {
            status = recoveryOk
                ? StatusWaterborne
                : $"{StatusWaterborne} · recovery-blocked · {BoatOpsFsm.FormatRecoverySeaStateRefusal(state.MaxRecoverySeaState)}";
            refusal = recoveryOk ? null : BoatOpsFsm.ReasonBoatNotReady;
        }
        else if (state.Phase == BoatOpsPhase.Stranded)
        {
            status = StatusStranded;
            refusal = BoatOpsFsm.ReasonStranded;
        }
        else
        {
            status = $"{phaseLabel.ToUpperInvariant()} · eta={state.TimeToReadyTicks}";
            refusal = null;
        }

        return new BoatOpsEntry(
            CraftId: state.CraftId.Trim(),
            HostLabel: hostLabel,
            PhaseLabel: phaseLabel,
            StatusLine: status,
            RefusalCode: refusal,
            SeaStateDouglas: sea,
            MaxLaunchSeaState: state.MaxLaunchSeaState,
            MaxRecoverySeaState: state.MaxRecoverySeaState,
            SeaStateLaunchOk: launchOk,
            SeaStateRecoveryOk: recoveryOk,
            PersonnelEmbarked: state.PersonnelEmbarked,
            PersonnelCapacity: state.PersonnelCapacity,
            CargoTons: state.CargoTons,
            CargoCapacityTons: state.CargoCapacityTons,
            TimeToReadyTicks: state.TimeToReadyTicks,
            CanLaunch: canLaunch,
            CanRecover: canRecover,
            CanAbort: canAbort,
            LaunchDisabledReason: launchDisabled,
            RecoverDisabledReason: recoverDisabled);
    }
}

/// <summary>Aggregate ready count for Boat Ops group status (CMD-25).</summary>
public sealed record BoatOpsAggregate(int ReadyCount, int TotalCount, string SummaryLine)
{
    public static BoatOpsAggregate Empty { get; } = new(0, 0, "READY 0/0");
}
