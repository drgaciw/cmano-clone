namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Scenario-level static sea-state scalar (Douglas 0–9) for LOG-10 MVP.
/// Deterministic by construction — no weather model, no wall-clock.
/// </summary>
public readonly record struct ScenarioSeaState
{
    public const int MinDouglas = 0;
    public const int MaxDouglas = 9;

    /// <summary>Douglas sea state, always clamped to 0–9.</summary>
    public int DouglasScale { get; }

    public ScenarioSeaState(int douglasScale) =>
        DouglasScale = Clamp(douglasScale);

    /// <summary>Clamp any integer into the Douglas 0–9 band.</summary>
    public static int Clamp(int douglasScale)
    {
        if (douglasScale < MinDouglas)
        {
            return MinDouglas;
        }

        if (douglasScale > MaxDouglas)
        {
            return MaxDouglas;
        }

        return douglasScale;
    }

    /// <summary>Factory that clamps into range.</summary>
    public static ScenarioSeaState Of(int douglasScale) => new(douglasScale);

    public static ScenarioSeaState Calm { get; } = new(0);

    public override string ToString() => $"Douglas {DouglasScale}";
}
