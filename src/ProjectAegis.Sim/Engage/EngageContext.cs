namespace ProjectAegis.Sim.Engage;

/// <summary>Inputs used to resolve one engagement attempt.</summary>
/// <param name="RangeMeters">Range from shooter to target in meters.</param>
/// <param name="Envelope">Weapon engagement envelope.</param>
/// <param name="RoundsRemaining">Rounds remaining before this attempt.</param>
/// <param name="HasFireControlTrack">Whether a fire-control track is available.</param>
/// <param name="RadarEmconActive">Whether radar emission control is active.</param>
/// <param name="PkBase">Base probability of kill.</param>
/// <param name="PkIntercept">Probability that the shot is intercepted.</param>
/// <param name="PkKill">Conditional probability that a hit kills.</param>
/// <param name="SalvoSize">Requested salvo size.</param>
/// <param name="WeaponTechnologyLevel">Weapon technology level.</param>
/// <param name="WeaponRequiresBlackProject">Whether the weapon requires black-project authorization.</param>
/// <param name="DlzPersonality">Dynamic launch-zone personality.</param>
/// <param name="CombatDomain">Target combat domain.</param>
/// <param name="MountOnline">Whether the firing mount is online.</param>
/// <param name="ContactIdentified">Whether the target contact is identified.</param>
/// <param name="AirOperationsReady">Whether air operations are ready.</param>
/// <param name="IsHypersonicTarget">Whether the target is hypersonic.</param>
/// <param name="HasHypersonicDefenseLayer">Whether a hypersonic defense layer is available.</param>
/// <param name="TrackSpoofed">Whether the target track is spoofed.</param>
/// <param name="AirAspectInEnvelope">Whether the air aspect is in envelope.</param>
/// <param name="SurfaceAspectInEnvelope">Whether the surface aspect is in envelope.</param>
/// <param name="SubsurfaceAspectInEnvelope">Whether the subsurface aspect is in envelope.</param>
/// <param name="LandAspectInEnvelope">Whether the land aspect is in envelope.</param>
/// <param name="MineAspectInEnvelope">Whether the mine aspect is in envelope.</param>
/// <param name="FacilityAspectInEnvelope">Whether the facility aspect is in envelope.</param>
/// <param name="CatalogDamageWithdrawBlocked">Whether catalog damage blocks engagement through withdrawal.</param>
/// <param name="LogisticsBingoBlocked">Whether logistics bingo state blocks engagement.</param>
/// <param name="ShotgunRoundsThreshold">Rounds threshold for shotgun behavior.</param>
/// <param name="ShooterMaxDrones">When greater than zero, the shooter is a swarm and <paramref name="PkBase"/> is scaled by living integrity.</param>
/// <param name="ShooterDroneCount">Current living shooter drone count.</param>
/// <param name="TargetMaxDrones">When greater than zero, the target is a swarm and hit or kill outcomes apply aggregate integrity loss.</param>
/// <param name="TargetDroneCount">Current living target drone count.</param>
/// <param name="TargetAaProfile">Target anti-air profile.</param>
/// <param name="PointFireDronesLostPerHit">Scenario-tunable point-fire drones lost per hit; zero uses <see cref="SwarmHardCounterAa"/> defaults.</param>
/// <param name="AreaAaDronesLostPerHit">Scenario-tunable area-AA drones lost per hit; zero uses defaults.</param>
/// <param name="UsesRemoteCecTrack">Whether the shooter is attempting engagement using a remote CEC composite track.</param>
/// <param name="CecRemoteFireControlEligible">Whether the world has determined remote CEC fire control is currently eligible for this shot.</param>
/// <param name="ShooterCecCapable">Whether the shooter catalog platform is CEC-capable.</param>
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
    int ShooterMaxDrones = 0,
    int ShooterDroneCount = 0,
    int TargetMaxDrones = 0,
    int TargetDroneCount = 0,
    SwarmAaProfileKind TargetAaProfile = SwarmAaProfileKind.PointFire,
    int PointFireDronesLostPerHit = 0,
    int AreaAaDronesLostPerHit = 0,
    bool UsesRemoteCecTrack = false,
    bool CecRemoteFireControlEligible = false,
    bool ShooterCecCapable = false);
