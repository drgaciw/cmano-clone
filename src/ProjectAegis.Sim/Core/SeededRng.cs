namespace ProjectAegis.Sim.Core;

/// <summary>Deterministic unit float in [0,1) from seed, domain, entity, tick, and draw index.</summary>
public static class SeededRng
{
    public static double UnitFloat(
        SimSeed seed,
        RngDomain domain,
        ulong entityId,
        ulong simTick,
        int drawIndex)
    {
        ulong hash = Mix(seed.Value, (ulong)domain, entityId, simTick, (ulong)(uint)drawIndex);
        return (hash & 0xFFFF_FFFF) / (double)uint.MaxValue;
    }

    private static ulong Mix(ulong a, ulong b, ulong c, ulong d, ulong e)
    {
        ulong x = a ^ (b << 17) ^ (c << 31) ^ d ^ (e << 11);
        x ^= x >> 33;
        x *= 0xff51_afd7_ed55_8ccdUL;
        x ^= x >> 33;
        x *= 0xc4ce_b9fe_1a85_ec53UL;
        x ^= x >> 33;
        return x;
    }
}
