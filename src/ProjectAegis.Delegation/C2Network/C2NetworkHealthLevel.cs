namespace ProjectAegis.Delegation.C2Network;

/// <summary>Aggregate mesh health derived from link rows and comms state.</summary>
public enum C2NetworkHealthLevel
{
    Healthy = 0,
    Degraded = 1,
    Partitioned = 2,
}
