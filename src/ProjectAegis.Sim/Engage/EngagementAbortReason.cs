namespace ProjectAegis.Sim.Engage;

public enum EngagementAbortReason
{
    None = 0,
    OutOfEnvelope = 1,
    DlzOut = 2,
    MagazineEmpty = 3,
    NoFireControlTrack = 4,
    EmconOff = 5,
    RoeHoldFire = 6,
    WeaponsTight = 7,
    TargetDestroyed = 8,
    WraSalvo = 9,
    BlackProjectRequired = 10,
    TechnologyLevelExceeded = 11,
    MountOffline = 12,
    DomainNoSolution = 13,
    AirNotReady = 14,
    TrackSpoofed = 15,
    AirAspectBlock = 16,
    SurfaceAspectBlock = 17,
    SubsurfaceAspectBlock = 18,
    LandAspectBlock = 19,
    DamageWithdrawRecommended = 20,
    MineAspectBlock = 21,
    FacilityAspectBlock = 22,

    /// <summary>
    /// The shooter itself was destroyed earlier in the run. Distinct from
    /// <see cref="TargetDestroyed"/>: that gate stops shots *at* a dead unit, this one stops
    /// shots *from* one. See BUG-engagement-resolver-shooter-liveness.
    /// </summary>
    ShooterDestroyed = 23,

    /// <summary>Shooter fuel band is Bingo — offensive engage denied (logistics gate v1).</summary>
    BingoFuel = 24,

    /// <summary>Shotgun band: multi-salvo engage denied (soft logistics gate).</summary>
    ShotgunOrdnance = 25,

    /// <summary>Winchester band: out of weapons — offensive engage denied (logistics hard gate).</summary>
    WinchesterOrdnance = 26,

    /// <summary>
    /// SWARM-31 / B6b: remote engage-on-remote-data path required a CEC composite FC track
    /// but mesh was lost/jammed or FC quality was unavailable. Distinct from
    /// <see cref="NoFireControlTrack"/> so callers can tell organic-only failure from mesh drop.
    /// </summary>
    CecRemoteTrackUnavailable = 27,
}
