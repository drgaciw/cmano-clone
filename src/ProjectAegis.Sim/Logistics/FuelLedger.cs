namespace ProjectAegis.Sim.Logistics;

/// <summary>Per-unit fuel remaining with deterministic tick burn (logistics GDD F' = F - burn * Δt).</summary>
public sealed class FuelLedger
{
    private readonly Dictionary<string, double> _remainingKg = new(StringComparer.Ordinal);
    private readonly double _capacityKg;
    private readonly double _burnRateKgPerSecond;

    public FuelLedger(double capacityKg, double burnRateKgPerSecond)
    {
        if (capacityKg < 0 || burnRateKgPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityKg), "Capacity and burn must be non-negative.");
        }

        _capacityKg = capacityKg;
        _burnRateKgPerSecond = burnRateKgPerSecond;
    }

    public void EnsureUnit(string unitId)
    {
        if (!_remainingKg.ContainsKey(unitId))
        {
            _remainingKg[unitId] = _capacityKg;
        }
    }

    public (double DeltaKg, double RemainingKg) AdvanceTick(string unitId, double deltaSeconds)
    {
        EnsureUnit(unitId);
        var burn = _burnRateKgPerSecond * deltaSeconds;
        var previous = _remainingKg[unitId];
        // Clamp to [0, capacity]: burn is normally non-negative, but a negative
        // deltaSeconds (e.g. an out-of-order or clock-corrected tick from a replay
        // re-sync) must never leave the tank holding more fuel than it can carry.
        var next = Math.Clamp(previous - burn, 0, _capacityKg);
        _remainingKg[unitId] = next;
        return (next - previous, next);
    }

    public double GetRemainingKg(string unitId) =>
        _remainingKg.TryGetValue(unitId, out var kg) ? kg : _capacityKg;

    public string ResolveBand(string unitId, double jokerFuelFraction, double bingoFuelFraction)
    {
        var frac = _capacityKg <= 0 ? 1 : GetRemainingKg(unitId) / _capacityKg;
        if (frac <= bingoFuelFraction)
        {
            return "BINGO";
        }

        if (frac <= jokerFuelFraction)
        {
            return "JOKER";
        }

        return "NOMINAL";
    }
}