namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Policy;

/// <summary>Mission/scenario-level ROE defaults (doc 11 / policy GDD inheritance root).</summary>
public sealed class ScenarioPolicyProfile
{
    public ScenarioPolicyProfile(
        EffectivePolicy friendlyDefault,
        EffectivePolicy? opposingDefault = null,
        IReadOnlyDictionary<string, EffectivePolicy>? unitOverrides = null,
        PlayerInfoModel playerInfoModel = PlayerInfoModel.FullTransparency,
        PersonalityEditPolicy personalityEditPolicy = PersonalityEditPolicy.Anytime,
        bool allowDualSideControl = false)
    {
        FriendlyDefault = friendlyDefault;
        OpposingDefault = opposingDefault ?? EffectivePolicy.DefaultFree;
        UnitOverrides = unitOverrides ?? new Dictionary<string, EffectivePolicy>();
        PlayerInfoModel = playerInfoModel;
        PersonalityEditPolicy = personalityEditPolicy;
        AllowDualSideControl = allowDualSideControl;
    }

    public string Id { get; init; } = "";

    public EffectivePolicy FriendlyDefault { get; }

    public EffectivePolicy OpposingDefault { get; }

    public IReadOnlyDictionary<string, EffectivePolicy> UnitOverrides { get; }

    public PlayerInfoModel PlayerInfoModel { get; }

    public PersonalityEditPolicy PersonalityEditPolicy { get; }

    /// <summary>When true, Mixed mode may assign human controllers on both sides (req 03 test sandbox).</summary>
    public bool AllowDualSideControl { get; }

    public EffectivePolicy ResolveForUnit(string unitKey, bool isFriendly)
    {
        if (UnitOverrides.TryGetValue(unitKey, out var over))
        {
            return over;
        }

        return isFriendly ? FriendlyDefault : OpposingDefault;
    }
}
