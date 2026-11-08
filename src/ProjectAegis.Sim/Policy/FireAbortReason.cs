namespace ProjectAegis.Sim.Policy;

public enum FireAbortReason
{
    None = 0,
    RoeHoldFire = 1,
    WeaponsTight = 2,
    WraRange = 3,
    WraSalvo = 4,
    EmconOff = 5,
    NoFireControlTrack = 6,
    CommsDenied = 7,
    AirAspectBlock = 8,
    SurfaceAspectBlock = 9,
    SubsurfaceAspectBlock = 10,
    LandAspectBlock = 11,
    MineAspectBlock = 12,
    FacilityAspectBlock = 13,
    /// <summary>SWARM-15: auto-engage posture not authorized by doctrine.</summary>
    AutoEngageDenied = 14,
    /// <summary>SWARM-15/19: expend/kamikaze pulse not authorized by doctrine.</summary>
    ExpendUnauthorized = 15,
    /// <summary>C3-01 / DRG-390: unit is off its side's comms grid and cannot accept new direct orders.</summary>
    OffGrid = 16,
}
