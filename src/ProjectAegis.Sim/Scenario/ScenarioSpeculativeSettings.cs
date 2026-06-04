namespace ProjectAegis.Sim.Scenario;

/// <summary>TL / black-project gates for speculative weapons (req 10).</summary>
public sealed class ScenarioSpeculativeSettings
{
    public ScenarioSpeculativeSettings(bool blackProjectMode = false, int maxTechnologyLevel = 2)
    {
        BlackProjectMode = blackProjectMode;
        MaxTechnologyLevel = Math.Clamp(maxTechnologyLevel, 0, 5);
    }

    public bool BlackProjectMode { get; }

    public int MaxTechnologyLevel { get; }

    public static ScenarioSpeculativeSettings CampaignDefault { get; } = new(blackProjectMode: false, maxTechnologyLevel: 2);
}