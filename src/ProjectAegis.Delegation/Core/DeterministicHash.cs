namespace ProjectAegis.Delegation.Core;

/// <summary>
/// Cross-process-stable hashing for the deterministic simulation path.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="string.GetHashCode()"/> (and its <c>StringComparison</c> overloads)
/// are randomized per process in .NET — the same string yields a different hash on
/// every application launch. Using that hash to seed simulation RNG breaks the core
/// determinism invariant (controllers must be pure functions of observed state,
/// traits, and seed), because the per-agent RNG stream would differ run-to-run while
/// looking stable within a single process.
/// </para>
/// <para>
/// This helper implements FNV-1a (32-bit) over the little-endian UTF-16 representation
/// of the string. It is deterministic across processes, machines, and runtimes, which
/// is the property the simulation requires. It is NOT a security hash and must not be
/// used for anything that needs collision resistance against adversarial input.
/// </para>
/// </remarks>
public static class DeterministicHash
{
    private const uint Fnv1aOffsetBasis = 2166136261;
    private const uint Fnv1aPrime = 16777619;

    /// <summary>
    /// Returns a process-stable 32-bit FNV-1a hash of <paramref name="value"/>,
    /// reinterpreted as a signed <see cref="int"/>. Deterministic across runs.
    /// </summary>
    public static int OrdinalHash(string value)
    {
        unchecked
        {
            var hash = Fnv1aOffsetBasis;
            foreach (var c in value)
            {
                hash ^= (byte)(c & 0xFF);
                hash *= Fnv1aPrime;
                hash ^= (byte)((c >> 8) & 0xFF);
                hash *= Fnv1aPrime;
            }

            return (int)hash;
        }
    }
}
