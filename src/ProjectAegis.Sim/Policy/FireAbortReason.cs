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
}
