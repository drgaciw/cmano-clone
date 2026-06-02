namespace ProjectAegis.Sim.Scenario;

/// <summary>Scenario-tunable fuel warning thresholds (logistics GDD § Fuel & endurance).</summary>
public sealed class ScenarioLogisticsSettings
{
    public static ScenarioLogisticsSettings Default { get; } = new(300, 600);

    public ScenarioLogisticsSettings(double jokerSimSeconds, double bingoSimSeconds)
    {
        if (jokerSimSeconds < 0 || bingoSimSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jokerSimSeconds), "Fuel thresholds must be non-negative.");
        }

        if (bingoSimSeconds < jokerSimSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(bingoSimSeconds), "Bingo threshold must be >= Joker.");
        }

        JokerSimSeconds = jokerSimSeconds;
        BingoSimSeconds = bingoSimSeconds;
    }

    public double JokerSimSeconds { get; }

    public double BingoSimSeconds { get; }
}