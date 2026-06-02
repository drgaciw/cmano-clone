namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Engage;
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
        ScenarioEngageDefaults? engageDefaults = null,
        bool allowDualSideControl = false,
        IReadOnlyList<ScenarioContactSeed>? contactSeeds = null,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon = null,
        IReadOnlyList<ScenarioDetectionTrial>? detectionTrials = null,
        IReadOnlyList<ScenarioCatalogDetectionTarget>? catalogDetectionTargets = null,
        IReadOnlyList<ScenarioJammer>? jammers = null,
        ScenarioContactLifecycle? contactLifecycle = null,
        ScenarioReplaySettings? replaySettings = null,
        ScenarioMissionTimeline? missionTimeline = null,
        ScenarioDelegationSettings? delegationSettings = null,
        IReadOnlyList<ScenarioCommsTransition>? commsTransitions = null)
    {
        FriendlyDefault = friendlyDefault;
        OpposingDefault = opposingDefault ?? EffectivePolicy.DefaultFree;
        UnitOverrides = unitOverrides ?? new Dictionary<string, EffectivePolicy>();
        PlayerInfoModel = playerInfoModel;
        PersonalityEditPolicy = personalityEditPolicy;
        EngageDefaults = engageDefaults;
        AllowDualSideControl = allowDualSideControl;
        ContactSeeds = contactSeeds ?? Array.Empty<ScenarioContactSeed>();
        UnitRadarEmcon = unitRadarEmcon ?? new Dictionary<string, EmconState>();
        DetectionTrials = detectionTrials ?? Array.Empty<ScenarioDetectionTrial>();
        CatalogDetectionTargets = catalogDetectionTargets ?? Array.Empty<ScenarioCatalogDetectionTarget>();
        Jammers = jammers ?? Array.Empty<ScenarioJammer>();
        ContactLifecycle = contactLifecycle ?? ScenarioContactLifecycle.Default;
        ReplaySettings = replaySettings ?? ScenarioReplaySettings.Default;
        MissionTimeline = missionTimeline;
        DelegationSettings = delegationSettings ?? ScenarioDelegationSettings.Default;
        CommsTransitions = commsTransitions ?? Array.Empty<ScenarioCommsTransition>();
    }

    public string Id { get; init; } = "";

    public EffectivePolicy FriendlyDefault { get; }

    public EffectivePolicy OpposingDefault { get; }

    public IReadOnlyDictionary<string, EffectivePolicy> UnitOverrides { get; }

    public PlayerInfoModel PlayerInfoModel { get; }

    public PersonalityEditPolicy PersonalityEditPolicy { get; }

    public ScenarioEngageDefaults? EngageDefaults { get; }

    /// <summary>When true, Mixed mode may assign human controllers on both sides (req 03 test sandbox).</summary>
    public bool AllowDualSideControl { get; }

    public IReadOnlyList<ScenarioContactSeed> ContactSeeds { get; }

    public IReadOnlyDictionary<string, EmconState> UnitRadarEmcon { get; }

    public IReadOnlyList<ScenarioDetectionTrial> DetectionTrials { get; }

    /// <summary>Trials resolved via <see cref="DetectionTrialResolver"/> when <see cref="DetectionTrials"/> is empty.</summary>
    public IReadOnlyList<ScenarioCatalogDetectionTarget> CatalogDetectionTargets { get; }

    public IReadOnlyList<ScenarioJammer> Jammers { get; }

    public ScenarioContactLifecycle ContactLifecycle { get; }

    public ScenarioReplaySettings ReplaySettings { get; }

    public ScenarioMissionTimeline? MissionTimeline { get; }

    public ScenarioDelegationSettings DelegationSettings { get; }

    public IReadOnlyList<ScenarioCommsTransition> CommsTransitions { get; }

    public EngageContext ResolveEngageContext()
    {
        var defaults = EngageDefaults ?? ScenarioEngageDefaults.MvpFallback;
        return defaults.ToEngageContext(defaults.DefaultMagazineRounds);
    }

    public EffectivePolicy ResolveForUnit(string unitKey, bool isFriendly)
    {
        if (UnitOverrides.TryGetValue(unitKey, out var over))
        {
            return over;
        }

        return isFriendly ? FriendlyDefault : OpposingDefault;
    }
}