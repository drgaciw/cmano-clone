namespace ProjectAegis.Sim.Scenario;

/// <summary>Scenario-tunable fuel (logistics GDD § Fuel & endurance).</summary>
public sealed class ScenarioLogisticsSettings
{
    public static ScenarioLogisticsSettings Default { get; } = new(300, 600);

    public ScenarioLogisticsSettings(
        double jokerSimSeconds,
        double bingoSimSeconds,
        double fuelCapacityKg = 0,
        double burnRateKgPerSecond = 0,
        double jokerFuelFraction = 0.25,
        double bingoFuelFraction = 0.10)
    {
        if (jokerSimSeconds < 0 || bingoSimSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jokerSimSeconds), "Fuel thresholds must be non-negative.");
        }

        if (bingoSimSeconds < jokerSimSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(bingoSimSeconds), "Bingo threshold must be >= Joker.");
        }

        if (fuelCapacityKg < 0 || burnRateKgPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fuelCapacityKg), "Fuel capacity and burn must be non-negative.");
        }

        if (jokerFuelFraction is <= 0 or > 1 || bingoFuelFraction is <= 0 or > 1 || bingoFuelFraction > jokerFuelFraction)
        {
            throw new ArgumentOutOfRangeException(nameof(jokerFuelFraction), "Fuel fractions must be in (0,1] with bingo <= joker.");
        }

        JokerSimSeconds = jokerSimSeconds;
        BingoSimSeconds = bingoSimSeconds;
        FuelCapacityKg = fuelCapacityKg;
        BurnRateKgPerSecond = burnRateKgPerSecond;
        JokerFuelFraction = jokerFuelFraction;
        BingoFuelFraction = bingoFuelFraction;
    }

    public double JokerSimSeconds { get; }

    public double BingoSimSeconds { get; }

    public double FuelCapacityKg { get; }

    public double BurnRateKgPerSecond { get; }

    public double JokerFuelFraction { get; }

    public double BingoFuelFraction { get; }

    public bool UsesFuelBurnModel => FuelCapacityKg > 0 && BurnRateKgPerSecond > 0;

    public double RemainingFuelKg(double simTimeSeconds) =>
        Math.Max(0, FuelCapacityKg - BurnRateKgPerSecond * simTimeSeconds);

    public double RemainingFuelFraction(double simTimeSeconds) =>
        FuelCapacityKg <= 0 ? 1 : RemainingFuelKg(simTimeSeconds) / FuelCapacityKg;
}