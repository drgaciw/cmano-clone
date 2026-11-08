namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// Detection modality for scenario trials (S111 / DRG-10).
/// Radar uses RF jam + EMCON; Infrared/Visual use optical/thermal env masks and do not fold RF jammers.
/// </summary>
public enum SensorModality
{
    Radar = 0,
    Infrared = 1,
    Visual = 2,
}
