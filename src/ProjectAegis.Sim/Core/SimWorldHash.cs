namespace ProjectAegis.Sim.Core;

/// <summary>ADR-004 unified world-state hash layers (core → detection → engage).</summary>
public static class SimWorldHash
{
    public const byte LayerCore = 1;
    public const byte LayerDetection = 2;
    public const byte LayerEngage = 3;

    public static ulong MixLayer(ulong composite, ulong layer, byte tag) =>
        Fold(composite ^ Fold(layer ^ ((ulong)tag << 56)));

    public static ulong Combine(ulong coreHash, ulong detectionHash, ulong engageMix) =>
        MixLayer(MixLayer(coreHash, detectionHash, LayerDetection), engageMix, LayerEngage);

    public static ulong Fold(ulong x)
    {
        x ^= x >> 33;
        x *= 0xff51_afd7_ed55_8ccdUL;
        x ^= x >> 33;
        x *= 0xc4ce_b9fe_1a85_ec53UL;
        x ^= x >> 33;
        return x;
    }
}