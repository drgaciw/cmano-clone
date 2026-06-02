namespace ProjectAegis.Sim.Engage;

public readonly record struct EngageContext(
    double RangeMeters,
    WeaponEnvelope Envelope,
    int RoundsRemaining,
    bool HasFireControlTrack,
    bool RadarEmconActive = true);
