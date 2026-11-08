namespace ProjectAegis.Sim.Swarm.SoftKill;

/// <summary>
/// SWARM-18 / DRG-107 external soft-kill applicator.
/// EMP freezes mode switches for N sim-seconds (tracked here; optional Scatter via IssueMode).
/// Jam degrades/loses C2 link via SetLinkState. Does not rewrite SwarmController internals.
/// Deterministic; pure freeze/link mapping delegated to evaluators.
/// </summary>
public sealed class SwarmSoftKillApplicator
{
    private readonly SwarmController _controller;
    private readonly Dictionary<string, double> _modeFreezeUntilByUnit = new(StringComparer.Ordinal);
    private readonly List<SwarmSoftKillEvent> _events = new();
    private ulong _sequence = 1;

    public SwarmSoftKillApplicator(SwarmController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    /// <summary>Append-only soft-kill event log with explicit reason strings.</summary>
    public IReadOnlyList<SwarmSoftKillEvent> EventLog => _events;

    /// <summary>Whether mode switches are frozen for <paramref name="unitId"/> at <paramref name="simTime"/>.</summary>
    public bool IsModeFrozen(string unitId, double simTime)
    {
        if (!TryNormalizeUnitId(unitId, out var id))
        {
            return false;
        }

        if (!_modeFreezeUntilByUnit.TryGetValue(id, out var until))
        {
            return false;
        }

        return SwarmEmpEvaluator.IsModeFrozen(simTime, until);
    }

    /// <summary>Freeze-until simTime for unit, or 0 when no freeze recorded.</summary>
    public double GetModeFreezeUntil(string unitId)
    {
        if (!TryNormalizeUnitId(unitId, out var id))
        {
            return 0;
        }

        return _modeFreezeUntilByUnit.TryGetValue(id, out var until) ? until : 0;
    }

    /// <summary>
    /// Apply EMP soft-kill: freeze mode switches until simTime + duration.
    /// Optionally recommends Scatter via <see cref="SwarmController.IssueMode"/> when freeze is applied.
    /// </summary>
    /// <returns>True when freeze was recorded (unit known).</returns>
    public bool ApplyEmp(
        string unitId,
        ulong simTick,
        double simTime,
        double freezeDurationSeconds = SwarmEmpEvaluator.DefaultFreezeDurationSeconds,
        bool recommendScatter = true)
    {
        if (!TryNormalizeUnitId(unitId, out var id) || !_controller.Contains(id))
        {
            return false;
        }

        var candidate = SwarmEmpEvaluator.ComputeFreezeUntil(simTime, freezeDurationSeconds);
        var existing = _modeFreezeUntilByUnit.TryGetValue(id, out var prior) ? prior : simTime;
        var freezeUntil = SwarmEmpEvaluator.MergeFreezeUntil(existing, candidate);
        _modeFreezeUntilByUnit[id] = freezeUntil;

        AppendEvent(simTick, simTime, id, SwarmSoftKillKind.Emp, SwarmEmpEvaluator.ReasonModeFreeze);

        if (recommendScatter && freezeDurationSeconds > 0)
        {
            // Scatter is a soft recommendation at EMP onset; only when orders are accepted (link not Lost).
            if (_controller.GetLinkState(id) != SwarmLinkState.Lost)
            {
                if (_controller.GetMode(id) != SwarmOperationalMode.Scatter)
                {
                    _controller.IssueMode(id, SwarmOperationalMode.Scatter, simTick, simTime);
                    AppendEvent(
                        simTick,
                        simTime,
                        id,
                        SwarmSoftKillKind.Emp,
                        SwarmEmpEvaluator.ReasonRecommendScatter);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Apply jam soft-kill: SetLinkState(Degraded) or Lost at higher severity.
    /// </summary>
    public bool ApplyJam(
        string unitId,
        ulong simTick,
        double simTime,
        SwarmJamSeverity severity)
    {
        if (!TryNormalizeUnitId(unitId, out var id) || !_controller.Contains(id))
        {
            return false;
        }

        if (severity == SwarmJamSeverity.None)
        {
            return ClearJam(id, simTick, simTime);
        }

        var link = SwarmJamEvaluator.LinkStateForSeverity(severity);
        var reason = SwarmJamEvaluator.ReasonForSeverity(severity);
        _controller.SetLinkState(id, link);
        AppendEvent(simTick, simTime, id, SwarmSoftKillKind.Jam, reason);
        return true;
    }

    /// <summary>
    /// Clear jam: restore Connected via SetLinkState, or re-evaluate geometry via RefreshLinkState.
    /// </summary>
    public bool ClearJam(
        string unitId,
        ulong simTick,
        double simTime,
        bool refreshFromGeometry = false,
        bool jammed = false)
    {
        if (!TryNormalizeUnitId(unitId, out var id) || !_controller.Contains(id))
        {
            return false;
        }

        if (refreshFromGeometry)
        {
            _controller.RefreshLinkState(id, jammed: jammed);
        }
        else
        {
            _controller.SetLinkState(id, SwarmLinkState.Connected);
        }

        AppendEvent(simTick, simTime, id, SwarmSoftKillKind.ClearJam, SwarmJamEvaluator.ReasonClear);
        return true;
    }

    /// <summary>Clear EMP mode freeze early (recovery path).</summary>
    public bool ClearEmpFreeze(string unitId, ulong simTick, double simTime)
    {
        if (!TryNormalizeUnitId(unitId, out var id) || !_controller.Contains(id))
        {
            return false;
        }

        _modeFreezeUntilByUnit.Remove(id);
        AppendEvent(simTick, simTime, id, SwarmSoftKillKind.ClearEmp, SwarmEmpEvaluator.ReasonClear);
        return true;
    }

    /// <summary>
    /// Issue operational mode respecting EMP freeze window.
    /// Returns false (no mutation) while frozen; true when IssueMode succeeds.
    /// </summary>
    public bool TryIssueMode(
        string unitId,
        SwarmOperationalMode mode,
        ulong simTick,
        double simTime,
        out string? rejectReason)
    {
        rejectReason = null;
        if (!TryNormalizeUnitId(unitId, out var id) || !_controller.Contains(id))
        {
            rejectReason = "unknown-unit";
            return false;
        }

        if (IsModeFrozen(id, simTime))
        {
            rejectReason = SwarmEmpEvaluator.ReasonModeBlocked;
            AppendEvent(simTick, simTime, id, SwarmSoftKillKind.ModeBlocked, SwarmEmpEvaluator.ReasonModeBlocked);
            return false;
        }

        _controller.IssueMode(id, mode, simTick, simTime);
        return true;
    }

    private void AppendEvent(
        ulong simTick,
        double simTime,
        string unitId,
        SwarmSoftKillKind kind,
        string reason)
    {
        var seq = _sequence++;
        _events.Add(new SwarmSoftKillEvent(seq, simTick, simTime, unitId, kind, reason));
    }

    private static bool TryNormalizeUnitId(string unitId, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        id = unitId.Trim();
        return true;
    }
}
