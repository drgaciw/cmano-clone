namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Presentation-only icon sliding between authoritative tick poses (CMD-38 / ADR-010).
/// Does not write sim state; wall-clock <paramref name="t"/> must not feed the order log.
/// </summary>
public static class MapSymbolPresentationLerp
{
    public static IReadOnlyList<MapSymbolEntry> Lerp(
        IReadOnlyList<MapSymbolEntry>? previous,
        IReadOnlyList<MapSymbolEntry> current,
        float t)
    {
        if (current is null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        if (previous is null || previous.Count == 0 || t >= 1f)
        {
            return current;
        }

        var clamped = t < 0f ? 0f : t;
        var byId = new Dictionary<string, MapSymbolEntry>(previous.Count, StringComparer.Ordinal);
        foreach (var symbol in previous)
        {
            byId.TryAdd(symbol.SymbolId, symbol);
        }

        var lerped = new MapSymbolEntry[current.Count];
        for (var i = 0; i < current.Count; i++)
        {
            var to = current[i];
            if (!to.HasAuthoritativePose
                || to.IsDestroyed
                || !byId.TryGetValue(to.SymbolId, out var from)
                || !from.HasAuthoritativePose)
            {
                lerped[i] = to;
                continue;
            }

            lerped[i] = to with
            {
                NormalizedX = Mix(from.NormalizedX, to.NormalizedX, clamped),
                NormalizedY = Mix(from.NormalizedY, to.NormalizedY, clamped),
                Latitude = MixNullable(from.Latitude, to.Latitude, clamped),
                Longitude = MixNullable(from.Longitude, to.Longitude, clamped),
            };
        }

        return lerped;
    }

    private static float Mix(float a, float b, float t) => a + ((b - a) * t);

    private static double? MixNullable(double? a, double? b, float t)
    {
        if (a is double av && b is double bv)
        {
            return av + ((bv - av) * t);
        }

        return b;
    }
}
