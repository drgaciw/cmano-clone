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
    bool TrackSpoofed = false);
