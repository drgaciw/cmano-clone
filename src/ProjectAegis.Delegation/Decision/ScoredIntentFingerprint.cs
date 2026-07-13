namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Deterministic scored-intent serialization for order-log replay (GDD order-log-replay).</summary>
public static class ScoredIntentFingerprint
{
    public static string Format(IReadOnlyList<ScoredIntent> alternatives)
    {
        if (alternatives.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "|",
            alternatives
                .OrderBy(a => a.Kind)
                .ThenBy(a => a.Score)
                .Select(a => $"{a.Kind}:{FingerprintFloat.Format(a.Score)}:{a.Risk}"));
    }
}