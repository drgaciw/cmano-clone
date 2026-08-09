namespace ProjectAegis.Data.PlatformAssistant;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;

/// <summary>catalog-grounding skill — rank peers from a live export snapshot.</summary>
public static class PlatformPeerScorer
{
    public static IReadOnlyList<PlatformPeerScore> Score(
        PlatformCatalogExportData export,
        PlatformDesignBrief brief)
    {
        if (export is null) throw new ArgumentNullException(nameof(export));
        if (brief is null) throw new ArgumentNullException(nameof(brief));

        var damageById = (export.Damage ?? [])
            .ToDictionary(d => d.PlatformId, StringComparer.Ordinal);
        var mobilityById = (export.Mobility ?? [])
            .ToDictionary(m => m.PlatformId, StringComparer.Ordinal);

        var conceptTokens = Tokenize(brief.Concept + " " + brief.DisplayName + " " + brief.Domain);
        var preferred = brief.PeerPlatformIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        var scores = new List<PlatformPeerScore>(export.Platforms.Count);
        foreach (var platform in export.Platforms)
        {
            if (string.Equals(platform.PlatformId, brief.PlatformId, StringComparison.Ordinal))
            {
                continue;
            }

            damageById.TryGetValue(platform.PlatformId, out var damage);
            mobilityById.TryGetValue(platform.PlatformId, out var mobility);
            var maxHp = damage?.MaxHp ?? 100;
            var speed = mobility?.MaxSpeedKnots ?? 0;
            var reasons = new List<string>(4);
            double score = 0;

            if (preferred is not null && preferred.Count > 0)
            {
                if (preferred.Contains(platform.PlatformId))
                {
                    score += 100;
                    reasons.Add("curator-selected peer");
                }
                else
                {
                    // When curator fixed peers, still allow fallbacks with tiny score.
                    score += 1;
                    reasons.Add("non-selected fallback");
                }
            }
            else
            {
                // Prefer mid-range combat radii as "standard" surface peers unless concept says otherwise.
                score += 20;
                reasons.Add("catalog peer");

                foreach (var token in conceptTokens)
                {
                    if (platform.PlatformId.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 12;
                        reasons.Add($"id match:{token}");
                    }
                }

                // Prefer non-zero combat radius platforms (real units vs placeholders).
                if (platform.CombatRadiusNm > 0)
                {
                    score += 10;
                    reasons.Add("has combat radius");
                }

                if (maxHp > 0)
                {
                    score += 5;
                    reasons.Add("has damage model");
                }
            }

            scores.Add(new PlatformPeerScore(
                platform.PlatformId,
                score,
                reasons,
                platform.CombatRadiusNm,
                maxHp,
                speed));
        }

        return scores
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.PlatformId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split([' ', '-', '_', '/', ',', ';', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
