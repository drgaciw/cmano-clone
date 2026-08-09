namespace ProjectAegis.Sim.Engage;

public readonly record struct EngageContext(
    double RangeMeters,
    WeaponEnvelope Envelope,
    int RoundsRemaining,
    bool HasFireControlTrack,
    bool RadarEmconActive = true,
    double PkBase = 0.85,
    double PkIntercept = 0.0,
    double PkKill = 1.0,
    int SalvoSize = 1,
    int WeaponTechnologyLevel = 0,
    bool WeaponRequiresBlackProject = false,
    DlzPersonality DlzPersonality = DlzPersonality.Normal,
    CombatDomain CombatDomain = CombatDomain.Air,
    bool MountOnline = true,
    bool ContactIdentified = true,
    bool AirOperationsReady = true,
    bool IsHypersonicTarget = false,
    bool HasHypersonicDefenseLayer = false,
    bool TrackSpoofed = false,
    bool AirAspectInEnvelope = true,
    bool SurfaceAspectInEnvelope = true,
    bool SubsurfaceAspectInEnvelope = true,
    bool LandAspectInEnvelope = true,
    bool MineAspectInEnvelope = true,
    bool FacilityAspectInEnvelope = true,
    bool CatalogDamageWithdrawBlocked = false,
    bool LogisticsBingoBlocked = false,
    int ShotgunRoundsThreshold = 0,
    /// <summary>When > 0, shooter is a swarm: PkBase is scaled by living integrity (SWARM-04).</summary>
    int ShooterMaxDrones = 0,
    int ShooterDroneCount = 0,
    /// <summary>When > 0, target is a swarm: hit/kill applies aggregate integrity loss (SWARM-08).</summary>
    int TargetMaxDrones = 0,
    int TargetDroneCount = 0,
    SwarmAaProfileKind TargetAaProfile = SwarmAaProfileKind.PointFire,
    /// <summary>Scenario-tunable point-fire drones lost per hit (0 = use <see cref="SwarmHardCounterAa"/> defaults).</summary>
    int PointFireDronesLostPerHit = 0,
    /// <summary>Scenario-tunable area-AA drones lost per hit (0 = use defaults).</summary>
    int AreaAaDronesLostPerHit = 0);
