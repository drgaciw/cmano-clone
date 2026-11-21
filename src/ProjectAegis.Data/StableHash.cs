namespace ProjectAegis.Data;

/// <summary>
/// Process-stable string hash. <see cref="string.GetHashCode()"/> is randomized per process in .NET,
/// so it must never feed ids, ordering, or keys that are expected to be reproducible (UAT CLI-5).
/// </summary>
public static class StableHash
{
    /// <summary>
    /// FNV-1a 32-bit over the UTF-16 code units of <paramref name="value"/> (low byte then high byte).
    /// Identical to <c>ProjectAegis.Delegation.Core.DeterministicHash.OrdinalHash</c>, which this
    /// lower-layer assembly cannot reference. Never throws for any input.
    /// </summary>
    public static int Fnv1A32(string value)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in value)
            {
                hash ^= (byte)(c & 0xFF);
                hash *= 16777619u;
                hash ^= (byte)((c >> 8) & 0xFF);
                hash *= 16777619u;
            }

            return (int)hash;
        }
    }
}
