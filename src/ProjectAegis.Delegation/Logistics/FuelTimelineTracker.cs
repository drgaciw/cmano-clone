namespace ProjectAegis.Delegation.Logistics;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

/// <summary>Emits deterministic fuel band transitions when burn model is active (logistics GDD AC-3).</summary>
public sealed class FuelTimelineTracker
{
    private readonly ScenarioLogisticsSettings _logistics;
    private readonly Dictionary<string, string> _lastStateByUnit = new(StringComparer.Ordinal);

    public FuelTimelineTracker(ScenarioLogisticsSettings logistics) => _logistics = logistics;

    public static FuelTimelineTracker? TryCreate(ScenarioPolicyProfile? profile) =>
        profile is { Logistics.UsesFuelBurnModel: true }
            ? new FuelTimelineTracker(profile.Logistics)
            : null;

    public IReadOnlyList<FuelStateChangeRecord> Drain(ulong simTick, double simTime, IEnumerable<TargetId> unitIds)
    {
        var emitted = new List<FuelStateChangeRecord>();
        foreach (var unitId in unitIds.OrderBy(u => u.Value, StringComparer.Ordinal))
        {
            var state = FuelStateProjection.ResolveState(simTime, _logistics);
            if (!_lastStateByUnit.TryGetValue(unitId.Value, out var previous))
            {
                _lastStateByUnit[unitId.Value] = state;
                if (!string.Equals(state, "NOMINAL", StringComparison.Ordinal))
                {
                    emitted.Add(CreateRecord(simTick, simTime, unitId, "NOMINAL", state));
                }

                continue;
            }

            if (string.Equals(previous, state, StringComparison.Ordinal))
            {
                continue;
            }

            emitted.Add(CreateRecord(simTick, simTime, unitId, previous, state));
            _lastStateByUnit[unitId.Value] = state;
        }

        return emitted;
    }

    private FuelStateChangeRecord CreateRecord(
        ulong simTick,
        double simTime,
        TargetId unitId,
        string previous,
        string next) =>
        new(
            0,
            simTime,
            simTick,
            unitId,
            previous,
            next,
            _logistics.RemainingFuelKg(simTime));
}