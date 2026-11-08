namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Optional runtime ledger of per-craft <see cref="BoatOpsUnitState"/> (LOG-09…11).
/// Pure dictionary + FSM wrappers — no Unity / session coupling.
/// Scenario sea state is held on the map for launch/recovery gates (LOG-10).
/// </summary>
public sealed class BoatOpsStateMap
{
    private readonly Dictionary<string, BoatOpsUnitState> _states;
    private readonly int _prepDurationTicks;
    private readonly int _launchDurationTicks;
    private readonly int _returnDurationTicks;
    private readonly int _alongsideDurationTicks;
    private readonly int _recoverDurationTicks;
    private int _seaStateDouglas;

    public BoatOpsStateMap(
        int seaStateDouglas = 0,
        int prepDurationTicks = BoatOpsFsm.DefaultPrepTicks,
        int launchDurationTicks = BoatOpsFsm.DefaultLaunchTicks,
        int returnDurationTicks = BoatOpsFsm.DefaultReturnTicks,
        int alongsideDurationTicks = BoatOpsFsm.DefaultAlongsideTicks,
        int recoverDurationTicks = BoatOpsFsm.DefaultRecoverTicks)
    {
        _states = new Dictionary<string, BoatOpsUnitState>(StringComparer.Ordinal);
        _seaStateDouglas = ScenarioSeaState.Clamp(seaStateDouglas);
        _prepDurationTicks = prepDurationTicks;
        _launchDurationTicks = launchDurationTicks;
        _returnDurationTicks = returnDurationTicks;
        _alongsideDurationTicks = alongsideDurationTicks;
        _recoverDurationTicks = recoverDurationTicks;
    }

    public BoatOpsStateMap(
        IEnumerable<BoatOpsUnitState> seed,
        int seaStateDouglas = 0,
        int prepDurationTicks = BoatOpsFsm.DefaultPrepTicks,
        int launchDurationTicks = BoatOpsFsm.DefaultLaunchTicks,
        int returnDurationTicks = BoatOpsFsm.DefaultReturnTicks,
        int alongsideDurationTicks = BoatOpsFsm.DefaultAlongsideTicks,
        int recoverDurationTicks = BoatOpsFsm.DefaultRecoverTicks)
        : this(
            seaStateDouglas,
            prepDurationTicks,
            launchDurationTicks,
            returnDurationTicks,
            alongsideDurationTicks,
            recoverDurationTicks)
    {
        if (seed is null)
        {
            return;
        }

        foreach (var state in seed)
        {
            if (state is null || string.IsNullOrWhiteSpace(state.CraftId))
            {
                continue;
            }

            _states[state.CraftId] = state;
        }
    }

    public int Count => _states.Count;

    /// <summary>Current scenario sea state (Douglas 0–9).</summary>
    public int SeaStateDouglas => _seaStateDouglas;

    public ScenarioSeaState SeaState => ScenarioSeaState.Of(_seaStateDouglas);

    public void SetSeaState(int douglasScale) =>
        _seaStateDouglas = ScenarioSeaState.Clamp(douglasScale);

    public void Upsert(BoatOpsUnitState state)
    {
        if (state is null || string.IsNullOrWhiteSpace(state.CraftId))
        {
            return;
        }

        _states[state.CraftId] = state;
    }

    public bool TryGet(string craftId, out BoatOpsUnitState state) =>
        _states.TryGetValue(craftId, out state!);

    public IReadOnlyList<BoatOpsUnitState> Snapshot()
    {
        if (_states.Count == 0)
        {
            return Array.Empty<BoatOpsUnitState>();
        }

        return _states.Values
            .OrderBy(s => s.CraftId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Advance every craft in stable craft-id order.</summary>
    public void TickAll(int deltaTicks = 1)
    {
        if (_states.Count == 0 || deltaTicks <= 0)
        {
            return;
        }

        foreach (var id in _states.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList())
        {
            _states[id] = BoatOpsFsm.Tick(
                _states[id],
                deltaTicks,
                _launchDurationTicks,
                _alongsideDurationTicks,
                _recoverDurationTicks);
        }
    }

    public BoatOpsFsmResult TryLaunch(string craftId)
    {
        if (!_states.TryGetValue(craftId, out var state))
        {
            var missing = BoatOpsUnitState.Stowed(craftId);
            return new BoatOpsFsmResult(false, missing, BoatOpsFsm.ReasonBoatNotReady);
        }

        var result = BoatOpsFsm.RequestLaunch(state, _seaStateDouglas, _prepDurationTicks);
        _states[craftId] = result.State;
        return result;
    }

    public BoatOpsFsmResult TryRecover(string craftId)
    {
        if (!_states.TryGetValue(craftId, out var state))
        {
            var missing = BoatOpsUnitState.Stowed(craftId);
            return new BoatOpsFsmResult(false, missing, BoatOpsFsm.ReasonNotWaterborne);
        }

        var result = BoatOpsFsm.RequestRecover(state, _seaStateDouglas, _returnDurationTicks);
        _states[craftId] = result.State;
        return result;
    }

    public BoatOpsFsmResult TryAbort(string craftId)
    {
        if (!_states.TryGetValue(craftId, out var state))
        {
            var missing = BoatOpsUnitState.Stowed(craftId);
            return new BoatOpsFsmResult(false, missing, BoatOpsFsm.ReasonAbortNotActive);
        }

        var result = BoatOpsFsm.AbortLaunch(state);
        _states[craftId] = result.State;
        return result;
    }

    /// <summary>Mark craft stranded when host departs while waterborne (LOG-11).</summary>
    public BoatOpsFsmResult TryHostDepart(string craftId)
    {
        if (!_states.TryGetValue(craftId, out var state))
        {
            var missing = BoatOpsUnitState.Stowed(craftId);
            return new BoatOpsFsmResult(false, missing, BoatOpsFsm.ReasonHostDepartNotWaterborne);
        }

        var result = BoatOpsFsm.HostDepartWhileWaterborne(state);
        _states[craftId] = result.State;
        return result;
    }
}
