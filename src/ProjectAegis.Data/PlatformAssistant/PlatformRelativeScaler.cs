namespace ProjectAegis.Data.PlatformAssistant;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;

/// <summary>relative-scaling + archetype-schema + provenance skills.</summary>
public static class PlatformRelativeScaler
{
    public const string SkillCatalogGrounding = "catalog-grounding";
    public const string SkillArchetypeSchema = "archetype-schema";
    public const string SkillRelativeScaling = "relative-scaling";
    public const string SkillProvenance = "provenance";
    public const string SkillGatePolicy = "gate-policy";
    public const string SkillWorkbookEmit = "workbook-emit";
    public const string SkillWhatIf = "what-if";

    public static PlatformDesignProposal Scale(
        PlatformCatalogExportData export,
        PlatformDesignBrief brief)
    {
        if (export is null) throw new ArgumentNullException(nameof(export));
        if (brief is null) throw new ArgumentNullException(nameof(brief));
        if (string.IsNullOrWhiteSpace(brief.PlatformId))
        {
            throw new ArgumentException("PlatformId is required.", nameof(brief));
        }

        var existingIds = export.Platforms
            .Select(p => p.PlatformId)
            .ToHashSet(StringComparer.Ordinal);

        var ranked = PlatformPeerScorer.Score(export, brief);
        IReadOnlyList<PlatformPeerScore> peers;
        if (brief.PeerPlatformIds is { Count: > 0 })
        {
            var selected = brief.PeerPlatformIds
                .Where(id => ranked.Any(r => string.Equals(r.PlatformId, id, StringComparison.Ordinal)))
                .Select(id => ranked.First(r => string.Equals(r.PlatformId, id, StringComparison.Ordinal)))
                .ToArray();
            peers = selected.Length > 0 ? selected : ranked.Take(Math.Min(3, ranked.Count)).ToArray();
        }
        else
        {
            peers = ranked.Take(Math.Min(4, ranked.Count)).ToArray();
        }

        if (peers.Count == 0)
        {
            // Degenerate empty catalog — deterministic defaults so callers still get a draft.
            peers =
            [
                new PlatformPeerScore("synthetic-peer", 0, ["empty-catalog fallback"], 100, 100, 20),
            ];
        }

        var weight = RoleWeight(brief.RoleWeight);
        var peerIds = peers.Select(p => p.PlatformId).ToArray();

        var combatRadius = WeightedToward(peers.Select(p => p.CombatRadiusNm).ToArray(), weight, decimals: 2);
        var maxHp = WeightedToward(peers.Select(p => p.MaxHp).ToArray(), weight, decimals: 0);
        var withdraw = WeightedToward(
            peers.Select(p =>
            {
                var d = (export.Damage ?? []).FirstOrDefault(x =>
                    string.Equals(x.PlatformId, p.PlatformId, StringComparison.Ordinal));
                return d?.WithdrawThresholdPct ?? 0;
            }).ToArray(),
            1.0 - weight,
            decimals: 2);
        var speed = WeightedToward(peers.Select(p => p.MaxSpeedKnots).ToArray(), weight, decimals: 1);

        // Lat/lon: average of peers that exist in export (skip synthetic).
        var geoPeers = export.Platforms
            .Where(p => peerIds.Contains(p.PlatformId, StringComparer.Ordinal))
            .ToArray();
        var lat = geoPeers.Length == 0 ? 0 : geoPeers.Average(p => p.LatDeg);
        var lon = geoPeers.Length == 0 ? 0 : geoPeers.Average(p => p.LonDeg);

        var platformId = UniqueId(brief.PlatformId.Trim(), existingIds);
        var citation = $"assistant:{string.Join(",", peerIds)}";
        var trl = brief.WhatIf ? 5 : 7;

        var binding = new CatalogPlatformBinding(
            PlatformId: platformId,
            DisplayName: string.IsNullOrWhiteSpace(brief.DisplayName) ? platformId : brief.DisplayName.Trim(),
            Domain: NormalizeDomain(brief.Domain),
            PlatformClass: brief.RoleWeight,
            Nationality: "",
            GameTechnologyLevel: 0,
            ReviewState: CatalogReviewStates.Provisional,
            TrlLevel: trl,
            ValueTier: CatalogProvenanceTier.GameplayAbstraction,
            CitationRef: citation,
            SourceFactId: "platform-design-assistant",
            ImportBatchId: "",
            SourceFile: "platform-design-assistant");

        var damage = new CatalogPlatformDamage(
            PlatformId: platformId,
            MaxHp: maxHp,
            WithdrawThresholdPct: withdraw,
            CriticalFlags: 0,
            ReviewState: CatalogReviewStates.Provisional,
            TrlLevel: trl,
            ValueTier: CatalogProvenanceTier.GameplayAbstraction,
            CitationRef: citation,
            Resilience: 1.0);

        var mobility = new CatalogMobility(
            PlatformId: platformId,
            MaxSpeedKnots: speed,
            CruiseSpeedKnots: Math.Round(speed * 0.75, 1),
            MaxAltitudeFt: NormalizeDomain(brief.Domain) == "air" ? 30000 : 0,
            MaxDepthM: NormalizeDomain(brief.Domain) == "subsurface" ? 300 : 0,
            FuelCapacity: 0,
            RangeNm: Math.Round(combatRadius * 2, 1),
            EnduranceHr: 0,
            ReviewState: CatalogReviewStates.Provisional,
            TrlLevel: trl,
            ValueTier: CatalogProvenanceTier.GameplayAbstraction,
            CitationRef: citation);

        var basis = new PlatformFieldBasis[]
        {
            new("CombatRadiusNm", combatRadius, peerIds, $"relative-scaling role={brief.RoleWeight}"),
            new("MaxHp", maxHp, peerIds, $"relative-scaling role={brief.RoleWeight}"),
            new("WithdrawThresholdPct", withdraw, peerIds, $"relative-scaling inverse-role={brief.RoleWeight}"),
            new("MaxSpeedKnots", speed, peerIds, $"relative-scaling role={brief.RoleWeight}"),
        };

        var outliers = new List<string>();
        if (export.Platforms.Count > 0)
        {
            var domainHp = (export.Damage ?? []).Select(d => d.MaxHp).DefaultIfEmpty(100).ToArray();
            var maxDomainHp = domainHp.Max();
            var minDomainHp = domainHp.Min();
            if (maxHp > maxDomainHp * 1.15)
            {
                outliers.Add($"MaxHp {maxHp} above domain max {maxDomainHp}");
            }

            if (maxHp < minDomainHp * 0.5)
            {
                outliers.Add($"MaxHp {maxHp} far below domain min {minDomainHp}");
            }
        }

        var skills = new List<string>
        {
            SkillCatalogGrounding,
            SkillArchetypeSchema,
            SkillRelativeScaling,
            SkillProvenance,
            SkillGatePolicy,
            SkillWorkbookEmit,
        };
        if (brief.WhatIf)
        {
            skills.Add(SkillWhatIf);
        }

        var summary = brief.WhatIf
            ? $"What-if draft '{binding.DisplayName}' ({platformId}) scaled from {peers.Count} peer(s); staged only until ApproveBatch."
            : $"Proposal '{binding.DisplayName}' ({platformId}) ready for extend-only staging from {peers.Count} peer(s).";

        return new PlatformDesignProposal(
            Binding: binding,
            Damage: damage,
            Mobility: mobility,
            CombatRadiusNm: combatRadius,
            LatDeg: Math.Round(lat, 4),
            LonDeg: Math.Round(lon, 4),
            Peers: peers,
            Basis: basis,
            Outliers: outliers,
            SkillsApplied: skills,
            Summary: summary,
            WhatIf: brief.WhatIf);
    }

    public static double RoleWeight(string role) =>
        role.Trim().ToLowerInvariant() switch
        {
            "light" => 0.25,
            "heavy" => 0.75,
            _ => 0.5,
        };

    public static double WeightedToward(IReadOnlyList<double> values, double weight, int decimals)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var min = values.Min();
        var max = values.Max();
        var mid = Median(values);
        var t = weight <= 0.5 ? weight * 2 : (weight - 0.5) * 2;
        var from = weight <= 0.5 ? min : mid;
        var to = weight <= 0.5 ? mid : max;
        var v = from + (to - from) * t;
        var f = Math.Pow(10, decimals);
        return Math.Round(v * f) / f;
    }

    public static string UniqueId(string desired, ISet<string> existing)
    {
        if (!existing.Contains(desired))
        {
            return desired;
        }

        var n = 2;
        while (existing.Contains($"{desired}-{n}"))
        {
            n++;
        }

        return $"{desired}-{n}";
    }

    private static double Median(IReadOnlyList<double> values)
    {
        var s = values.OrderBy(v => v).ToArray();
        var mid = s.Length / 2;
        return s.Length % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid];
    }

    private static string NormalizeDomain(string domain) =>
        domain.Trim().ToLowerInvariant() switch
        {
            "air" => "air",
            "subsurface" or "sub" or "undersea" => "subsurface",
            "land" or "ground" => "land",
            _ => "surface",
        };
}
