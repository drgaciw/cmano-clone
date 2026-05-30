namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Policy;

/// <summary>Mission/scenario-level ROE defaults (doc 11 / policy GDD inheritance root).</summary>
public sealed class ScenarioPolicyProfile
{
    public ScenarioPolicyProfile(
        EffectivePolicy friendlyDefault,
        EffectivePolicy? opposingDefault = null,
        IReadOnlyDictionary<string, EffectivePolicy>? unitOverrides = null)
    {
        FriendlyDefault = friendlyDefault;
        OpposingDefault = opposingDefault ?? EffectivePolicy.DefaultFree;
        UnitOverrides = unitOverrides ?? new Dictionary<string, EffectivePolicy>();
    }

    public string Id { get; init; } = "";

    public EffectivePolicy FriendlyDefault { get; }

    public EffectivePolicy OpposingDefault { get; }

    public IReadOnlyDictionary<string, EffectivePolicy> UnitOverrides { get; }

    public EffectivePolicy ResolveForUnit(string unitKey, bool isFriendly)
    {
        if (UnitOverrides.TryGetValue(unitKey, out var over))
        {
            return over;
        }

        return isFriendly ? FriendlyDefault : OpposingDefault;
    }
}
