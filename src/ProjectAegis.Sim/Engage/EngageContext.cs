namespace ProjectAegis.Sim.Engage;

public readonly record struct EngageContext(
    double RangeMeters,
    WeaponEnvelope Envelope,
    int RoundsRemaining,
    bool HasFireControlTrack,
    bool RadarEmconActive = true,
    double PkBase = 0.85,
    double PkIntercept = 0.0,
    double PkKill = 1.0);
