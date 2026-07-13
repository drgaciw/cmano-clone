namespace ProjectAegis.Sim.Catalog;

/// <summary>Stable entity id for mine transit hazard RNG draws.</summary>
public static class MineTransitHazardEntityId
{
    public static ulong FromTrial(string platformId, string mineId)
    {
        ulong hash = 0xcbf29ce484222325UL;
        HashSegment(ref hash, platformId);
        HashSegment(ref hash, mineId);
        return hash;
    }

    private static void HashSegment(ref ulong hash, string value)
    {
        foreach (var ch in value)
        {
            hash ^= ch;
            hash *= 0x100000001b3UL;
        }
    }
}