namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Optional runtime ledger of per-unit <see cref="AirOpsUnitState"/> (LOG-08).
/// Pure dictionary + FSM wrappers — no Unity / session coupling.
/// </summary>
public sealed class AirOpsStateMap
{
    private readonly Dictionary<string, AirOpsUnitState> _states;
    private readonly int _prepDurationTicks;
    private readonly int _taxiDurationTicks;
    private readonly int _takeoffDurationTicks;

    public AirOpsStateMap(
        int prepDurationTicks = AirOpsFsm.DefaultPrepTicks,
        int taxiDurationTicks = AirOpsFsm.DefaultTaxiTicks,
        int takeoffDurationTicks = AirOpsFsm.DefaultTakeoffTicks)
    {
        _states = new Dictionary<string, AirOpsUnitState>(StringComparer.Ordinal);
        _prepDurationTicks = prepDurationTicks;
        _taxiDurationTicks = taxiDurationTicks;
        _takeoffDurationTicks = takeoffDurationTicks;
    }

    public AirOpsStateMap(
        IEnumerable<AirOpsUnitState> seed,
        int prepDurationTicks = AirOpsFsm.DefaultPrepTicks,
        int taxiDurationTicks = AirOpsFsm.DefaultTaxiTicks,
        int takeoffDurationTicks = AirOpsFsm.DefaultTakeoffTicks)
        : this(prepDurationTicks, taxiDurationTicks, takeoffDurationTicks)
    {
        if (seed is null)
        {
            return;
        }

        foreach (var state in seed)
        {
            if (state is null || string.IsNullOrWhiteSpace(state.UnitId))
            {
                continue;
            }

            _states[state.UnitId] = state;
        }
    }

    public int Count => _states.Count;

    public void Upsert(AirOpsUnitState state)
    {
        if (state is null || string.IsNullOrWhiteSpace(state.UnitId))
        {
            return;
        }

        _states[state.UnitId] = state;
    }

    public bool TryGet(string unitId, out AirOpsUnitState state) =>
        _states.TryGetValue(unitId, out state!);

    public IReadOnlyList<AirOpsUnitState> Snapshot()
    {
        if (_states.Count == 0)
        {
            return Array.Empty<AirOpsUnitState>();
        }

        return _states.Values
            .OrderBy(s => s.UnitId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Advance every unit in stable unit-id order.</summary>
    public void TickAll(int deltaTicks = 1)
    {
        if (_states.Count == 0 || deltaTicks <= 0)
        {
            return;
        }

        foreach (var id in _states.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList())
        {
            _states[id] = AirOpsFsm.Tick(
                _states[id],
                deltaTicks,
                _taxiDurationTicks,
                _takeoffDurationTicks);
        }
    }

    public AirOpsFsmResult TryLaunch(string unitId, bool force = false)
    {
        if (!_states.TryGetValue(unitId, out var state))
        {
            var missing = AirOpsUnitState.OnGround(unitId, readyForLaunch: false);
            return new AirOpsFsmResult(false, missing, AirOpsFsm.ReasonAirNotReady);
        }

        var result = AirOpsFsm.RequestLaunch(state, _prepDurationTicks, force);
        _states[unitId] = result.State;
        return result;
    }

    public AirOpsFsmResult TryAbort(string unitId)
    {
        if (!_states.TryGetValue(unitId, out var state))
        {
            var missing = AirOpsUnitState.OnGround(unitId, readyForLaunch: false);
            return new AirOpsFsmResult(false, missing, AirOpsFsm.ReasonAbortNotActive);
        }

        var result = AirOpsFsm.AbortLaunch(state);
        _states[unitId] = result.State;
        return result;
    }
}
