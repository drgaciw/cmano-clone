namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Logistics;

/// <summary>
/// Pure projection of unit readiness / air-ops FSM into Air Ops rows (CMD-24).
/// Phase A: readiness tuples. Phase N: <see cref="ProjectLifecycle"/> from LOG-08 states.
/// No Unity / orchestrator dependency — callers supply simple tuples or domain states.
/// </summary>
public static class AirOpsProjection
{
    public const string StatusReady = "READY";
    public const string MissingLabel = "—";

    /// <summary>Status when <see cref="AirOpsEntry.ReadyForLaunch"/> is false — matches engage abort naming.</summary>
    public static string StatusNotReady { get; } =
        $"NOT READY · {AbortReasonCatalog.Engage.AIR_NOT_READY}";

    /// <summary>Refusal code aligned with engage / validation gate (doc 16 LOG-04).</summary>
    public static string AirNotReadyCode { get; } = AbortReasonCatalog.Engage.AIR_NOT_READY;

    /// <summary>
    /// Project air-ops rows from readiness tuples.
    /// <paramref name="rows"/> items: unitId, readyForLaunch, optional platform type, optional host.
    /// </summary>
    public static IReadOnlyList<AirOpsEntry> Project(
        IReadOnlyList<(string unitId, bool ready, string? platformType, string? host)> rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<AirOpsEntry>();
        }

        var result = new List<AirOpsEntry>(rows.Count);
        foreach (var row in rows
                     .Where(r => !string.IsNullOrWhiteSpace(r.unitId))
                     .OrderBy(r => r.unitId, StringComparer.Ordinal))
        {
            result.Add(ToEntry(row.unitId, row.ready, row.platformType, row.host));
        }

        return result;
    }

    /// <summary>
    /// Walk unit ids with a readiness lookup (host / session wiring without full orchestrator).
    /// </summary>
    public static IReadOnlyList<AirOpsEntry> Project(
        IEnumerable<string> unitIds,
        Func<string, bool> readyForLaunch,
        Func<string, string?>? platformType = null,
        Func<string, string?>? host = null)
    {
        if (unitIds is null)
        {
            return Array.Empty<AirOpsEntry>();
        }

        var list = unitIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        if (list.Count == 0)
        {
            return Array.Empty<AirOpsEntry>();
        }

        var result = new List<AirOpsEntry>(list.Count);
        foreach (var id in list)
        {
            result.Add(ToEntry(
                id,
                readyForLaunch(id),
                platformType?.Invoke(id),
                host?.Invoke(id)));
        }

        return result;
    }

    /// <summary>
    /// Phase N: project LOG-08 <see cref="AirOpsUnitState"/> rows with phase / timer / launch-abort flags.
    /// </summary>
    public static IReadOnlyList<AirOpsEntry> ProjectLifecycle(
        IReadOnlyList<AirOpsUnitState>? states,
        Func<string, string?>? platformType = null)
    {
        if (states is null || states.Count == 0)
        {
            return Array.Empty<AirOpsEntry>();
        }

        var result = new List<AirOpsEntry>(states.Count);
        foreach (var state in states
                     .Where(s => s is not null && !string.IsNullOrWhiteSpace(s.UnitId))
                     .OrderBy(s => s.UnitId, StringComparer.Ordinal))
        {
            result.Add(ToLifecycleEntry(state, platformType?.Invoke(state.UnitId)));
        }

        return result;
    }

    /// <summary>Aggregate ready count + summary line for group / header status.</summary>
    public static AirOpsAggregate Aggregate(IReadOnlyList<AirOpsEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return AirOpsAggregate.Empty;
        }

        var ready = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].ReadyForLaunch)
            {
                ready++;
            }
        }

        var total = entries.Count;
        return new AirOpsAggregate(ready, total, FormatSummaryLine(ready, total));
    }

    public static string FormatSummaryLine(int readyCount, int totalCount) =>
        $"READY {readyCount}/{totalCount}";

    /// <summary>Phase label for a domain phase (stable string for UI / tests).</summary>
    public static string FormatPhaseLabel(AirOpsPhase phase) => phase.ToString();

    /// <summary>
    /// Resolve launch-disabled reason from FSM phase + readiness (null when launchable).
    /// </summary>
    public static string? ResolveLaunchDisabledReason(AirOpsUnitState state)
    {
        if (state.Phase == AirOpsPhase.OnGround && state.ReadyForLaunch)
        {
            return null;
        }

        return state.Phase switch
        {
            AirOpsPhase.Maintenance => AirOpsFsm.ReasonInMaintenance,
            AirOpsPhase.Airborne or AirOpsPhase.Landing => AirOpsFsm.ReasonAlreadyAirborne,
            AirOpsPhase.Prepping or AirOpsPhase.Taxiing or AirOpsPhase.TakingOff =>
                AirOpsFsm.ReasonLaunchInProgress,
            AirOpsPhase.OnGround when !state.ReadyForLaunch => AirOpsFsm.ReasonAirNotReady,
            _ => AirOpsFsm.ReasonAirNotReady,
        };
    }

    private static AirOpsEntry ToEntry(
        string unitId,
        bool ready,
        string? platformType,
        string? host)
    {
        var platform = string.IsNullOrWhiteSpace(platformType) ? MissingLabel : platformType!.Trim();
        var hostLabel = string.IsNullOrWhiteSpace(host) ? MissingLabel : host!.Trim();
        var status = ready ? StatusReady : StatusNotReady;
        var refusal = ready ? null : AirNotReadyCode;
        return new AirOpsEntry(
            UnitId: unitId.Trim(),
            PlatformTypeLabel: platform,
            HostLabel: hostLabel,
            ReadyForLaunch: ready,
            StatusLine: status,
            RefusalCode: refusal,
            PhaseLabel: FormatPhaseLabel(AirOpsPhase.OnGround),
            TimeToReadyTicks: 0,
            CanLaunch: ready,
            CanAbort: false,
            LaunchDisabledReason: ready ? null : AirNotReadyCode);
    }

    private static AirOpsEntry ToLifecycleEntry(AirOpsUnitState state, string? platformType)
    {
        var platform = string.IsNullOrWhiteSpace(platformType) ? MissingLabel : platformType!.Trim();
        var hostLabel = string.IsNullOrWhiteSpace(state.HostId) ? MissingLabel : state.HostId!.Trim();
        var phaseLabel = FormatPhaseLabel(state.Phase);
        var canLaunch = state.Phase == AirOpsPhase.OnGround && state.ReadyForLaunch;
        var canAbort = state.CanAbortLaunch
            || state.Phase is AirOpsPhase.Prepping or AirOpsPhase.Taxiing or AirOpsPhase.TakingOff;
        var launchDisabled = canLaunch ? null : ResolveLaunchDisabledReason(state);

        // Status: prefer lifecycle phase label; keep READY / NOT READY for ground readiness.
        string status;
        string? refusal;
        if (state.Phase == AirOpsPhase.OnGround)
        {
            status = state.ReadyForLaunch ? StatusReady : StatusNotReady;
            refusal = state.ReadyForLaunch ? null : AirNotReadyCode;
        }
        else if (state.Phase == AirOpsPhase.Maintenance)
        {
            status = $"MAINT · {AirOpsFsm.ReasonInMaintenance}";
            refusal = AirOpsFsm.ReasonInMaintenance;
        }
        else if (state.Phase == AirOpsPhase.Airborne)
        {
            status = "AIRBORNE";
            refusal = null;
        }
        else if (state.Phase == AirOpsPhase.Landing)
        {
            status = "LANDING";
            refusal = null;
        }
        else
        {
            // Launch pipeline — show phase + remaining ticks
            status = $"{phaseLabel.ToUpperInvariant()} · eta={state.TimeToReadyTicks}";
            refusal = null;
        }

        // ReadyForLaunch presentation: true only when on ground and ready (engage gate aligned).
        var readyForLaunch = state.Phase == AirOpsPhase.OnGround && state.ReadyForLaunch;

        return new AirOpsEntry(
            UnitId: state.UnitId.Trim(),
            PlatformTypeLabel: platform,
            HostLabel: hostLabel,
            ReadyForLaunch: readyForLaunch,
            StatusLine: status,
            RefusalCode: refusal,
            PhaseLabel: phaseLabel,
            TimeToReadyTicks: state.TimeToReadyTicks,
            CanLaunch: canLaunch,
            CanAbort: canAbort,
            LaunchDisabledReason: launchDisabled);
    }
}

/// <summary>Aggregate ready count for Air Ops group status (CMD-24 Phase A).</summary>
public sealed record AirOpsAggregate(int ReadyCount, int TotalCount, string SummaryLine)
{
    public static AirOpsAggregate Empty { get; } = new(0, 0, "READY 0/0");
}
