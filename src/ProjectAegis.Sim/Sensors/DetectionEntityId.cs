namespace ProjectAegis.Sim.Sensors;

/// <summary>Stable entity id for detection RNG draws.</summary>
public static class DetectionEntityId
{
    public static ulong FromTrial(string observerId, string sensorId, string targetId)
    {
        ulong hash = 0xcbf29ce484222325UL;
        HashSegment(ref hash, observerId);
        HashSegment(ref hash, sensorId);
        HashSegment(ref hash, targetId);
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