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
        var next = Math.Max(0, previous - burn);
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