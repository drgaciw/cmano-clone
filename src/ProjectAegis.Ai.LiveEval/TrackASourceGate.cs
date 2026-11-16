namespace ProjectAegis.Ai.LiveEval;

/// <summary>
/// Dual-track firewall: refuse Track A / proprietary-derived sources before any provider call.
/// </summary>
public static class TrackASourceGate
{
    /// <summary>
    /// Returns false when the case metadata indicates Track A / db3 / proprietary derivation.
    /// </summary>
    public static bool TryAllowPublicSource(
        IReadOnlyDictionary<string, string>? caseMetadata,
        out string denyReason)
    {
        if (caseMetadata is null)
        {
            denyReason = string.Empty;
            return true;
        }

        foreach (var pair in caseMetadata)
        {
            var key = pair.Key;
            var value = pair.Value ?? string.Empty;
            if (ContainsTrackASignal(key) || ContainsTrackASignal(value))
            {
                denyReason =
                    "Track A / proprietary-derived source refused for LiveEval egress: " + key;
                return false;
            }
        }

        denyReason = string.Empty;
        return true;
    }

    private static bool ContainsTrackASignal(string text) =>
        text.Contains("trackA", StringComparison.OrdinalIgnoreCase)
        || text.Contains("track-a", StringComparison.OrdinalIgnoreCase)
        || text.Contains(".db3", StringComparison.OrdinalIgnoreCase)
        || text.Contains("cmo_db", StringComparison.OrdinalIgnoreCase)
        || text.Contains("proprietary", StringComparison.OrdinalIgnoreCase);
}
