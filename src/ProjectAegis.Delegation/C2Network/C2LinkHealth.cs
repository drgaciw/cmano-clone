namespace ProjectAegis.Delegation.C2Network;

/// <summary>Per-link health for headless C2 network projection (DRG-214).</summary>
public enum C2LinkHealth
{
    Healthy = 0,
    Degraded = 1,
    Partitioned = 2,
}
