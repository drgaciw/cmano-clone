namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Engage;

/// <summary>Scenario-level defaults for MVP engage priming (data/scenarios/*.policy.json).</summary>
public sealed class ScenarioEngageDefaults
{
    public ScenarioEngageDefaults(
        double rangeMeters,
        double envelopeMinMeters,
        double envelopeMaxMeters,
        int defaultMagazineRounds,
        bool hasFireControlTrack,
        double pkBase = 0.85,
        double pkIntercept = 0.0,
        double pkKill = 1.0,
        int salvoSize = 1,
        int weaponTechnologyLevel = 0,
        bool weaponRequiresBlackProject = false,
        DlzPersonality dlzPersonality = DlzPersonality.Normal,
        CombatDomain combatDomain = CombatDomain.Air,
        bool mountOnline = true,
        bool contactIdentified = true,
        bool combatDomainsEnabled = false)
    {
        RangeMeters = rangeMeters;
        EnvelopeMinMeters = envelopeMinMeters;
        EnvelopeMaxMeters = envelopeMaxMeters;
        DefaultMagazineRounds = defaultMagazineRounds;
        HasFireControlTrack = hasFireControlTrack;
        PkBase = pkBase;
        PkIntercept = pkIntercept;
        PkKill = pkKill;
        SalvoSize = Math.Max(1, salvoSize);
        WeaponTechnologyLevel = Math.Clamp(weaponTechnologyLevel, 0, 5);
        WeaponRequiresBlackProject = weaponRequiresBlackProject;
        DlzPersonality = dlzPersonality;
        CombatDomain = combatDomain;
        MountOnline = mountOnline;
        ContactIdentified = contactIdentified;
        CombatDomainsEnabled = combatDomainsEnabled;
    }

    public double RangeMeters { get; }

    public double EnvelopeMinMeters { get; }

    public double EnvelopeMaxMeters { get; }

    public int DefaultMagazineRounds { get; }

    public bool HasFireControlTrack { get; }

    public double PkBase { get; }

    public double PkIntercept { get; }

    public double PkKill { get; }

    public int SalvoSize { get; }

    public int WeaponTechnologyLevel { get; }

    public bool WeaponRequiresBlackProject { get; }

    public DlzPersonality DlzPersonality { get; }

    public CombatDomain CombatDomain { get; }

    public bool MountOnline { get; }

    public bool ContactIdentified { get; }

    /// <summary>ADR-009: when false (default), registry validators are not invoked on engage path.</summary>
    public bool CombatDomainsEnabled { get; }

    public EngageContext ToEngageContext(int roundsRemaining) =>
        new(
            RangeMeters,
            new WeaponEnvelope(EnvelopeMinMeters, EnvelopeMaxMeters),
            roundsRemaining,
            HasFireControlTrack,
            PkBase: PkBase,
            PkIntercept: PkIntercept,
            PkKill: PkKill,
            SalvoSize: SalvoSize,
            WeaponTechnologyLevel: WeaponTechnologyLevel,
            WeaponRequiresBlackProject: WeaponRequiresBlackProject,
            DlzPersonality: DlzPersonality,
            CombatDomain: CombatDomain,
            MountOnline: MountOnline,
            ContactIdentified: ContactIdentified);

    public static ScenarioEngageDefaults MvpFallback { get; } = new(
        50_000,
        1_000,
        100_000,
        defaultMagazineRounds: 2,
        hasFireControlTrack: true);
}