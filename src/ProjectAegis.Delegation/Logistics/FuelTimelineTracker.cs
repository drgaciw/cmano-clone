namespace ProjectAegis.Delegation.Logistics;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Logistics;
using ProjectAegis.Sim.Scenario;

/// <summary>Ledger-backed fuel burn and band transitions (logistics GDD AC-3).</summary>
public sealed class FuelTimelineTracker
{
    private readonly ScenarioLogisticsSettings _logistics;
    private readonly FuelLedger _ledger;
    private readonly Dictionary<string, string> _lastStateByUnit = new(StringComparer.Ordinal);

    public FuelTimelineTracker(ScenarioLogisticsSettings logistics)
    {
        _logistics = logistics;
        _ledger = new FuelLedger(logistics.FuelCapacityKg, logistics.BurnRateKgPerSecond);
    }

    public static FuelTimelineTracker? TryCreate(ScenarioPolicyProfile? profile) =>
        profile is { Logistics.UsesFuelBurnModel: true }
            ? new FuelTimelineTracker(profile.Logistics)
            : null;

    public FuelTickDrainResult Drain(
        ulong simTick,
        double simTime,
        double deltaSeconds,
        IEnumerable<TargetId> unitIds)
    {
        var burns = new List<FuelBurnRecord>();
        var bands = new List<FuelStateChangeRecord>();
        foreach (var unitId in unitIds.OrderBy(u => u.Value, StringComparer.Ordinal))
        {
            _ledger.EnsureUnit(unitId.Value);
            var (deltaKg, remainingKg) = _ledger.AdvanceTick(unitId.Value, deltaSeconds);
            if (_logistics.LogTickBurn)
            {
                burns.Add(new FuelBurnRecord(0, simTime, simTick, unitId, deltaKg, remainingKg));
            }

            var state = _ledger.ResolveBand(
                unitId.Value,
                _logistics.JokerFuelFraction,
                _logistics.BingoFuelFraction);
            if (!_lastStateByUnit.TryGetValue(unitId.Value, out var previous))
            {
                _lastStateByUnit[unitId.Value] = state;
                if (!string.Equals(state, "NOMINAL", StringComparison.Ordinal))
                {
                    bands.Add(CreateBandRecord(simTick, simTime, unitId, "NOMINAL", state, remainingKg));
                }

                continue;
            }

            if (string.Equals(previous, state, StringComparison.Ordinal))
            {
                continue;
            }

            bands.Add(CreateBandRecord(simTick, simTime, unitId, previous, state, remainingKg));
            _lastStateByUnit[unitId.Value] = state;
        }

        return new FuelTickDrainResult(burns, bands);
    }

    private static FuelStateChangeRecord CreateBandRecord(
        ulong simTick,
        double simTime,
        TargetId unitId,
        string previous,
        string next,
        double remainingKg) =>
        new(0, simTime, simTick, unitId, previous, next, remainingKg);
}

public sealed record FuelTickDrainResult(
    IReadOnlyList<FuelBurnRecord> Burns,
    IReadOnlyList<FuelStateChangeRecord> BandChanges);