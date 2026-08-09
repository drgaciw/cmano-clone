namespace ProjectAegis.Data.PlatformAssistant;

using ProjectAegis.Data.Catalog;

/// <summary>Peer used as relative-scaling basis.</summary>
public sealed record PlatformPeerScore(
    string PlatformId,
    double Score,
    IReadOnlyList<string> Reasons,
    double CombatRadiusNm,
    double MaxHp,
    double MaxSpeedKnots);

/// <summary>One scaled field with peer basis citation.</summary>
public sealed record PlatformFieldBasis(
    string Field,
    double Value,
    IReadOnlyList<string> PeerIds,
    string Method);

/// <summary>Deterministic draft from catalog peers (not yet staged).</summary>
public sealed record PlatformDesignProposal(
    CatalogPlatformBinding Binding,
    CatalogPlatformDamage Damage,
    CatalogMobility Mobility,
    double CombatRadiusNm,
    double LatDeg,
    double LonDeg,
    IReadOnlyList<PlatformPeerScore> Peers,
    IReadOnlyList<PlatformFieldBasis> Basis,
    IReadOnlyList<string> Outliers,
    IReadOnlyList<string> SkillsApplied,
    string Summary,
    bool WhatIf);

/// <summary>Outcome of staging a design proposal through CatalogWriteGate.</summary>
public sealed record PlatformDesignProposeResult(
    PlatformDesignProposal Proposal,
    string PlatformBatchId,
    string DamageBatchId,
    string? MobilityBatchId,
    IReadOnlyList<string> Notes)
{
    public IReadOnlyList<string> BatchIds
    {
        get
        {
            var ids = new List<string>(3) { PlatformBatchId, DamageBatchId };
            if (!string.IsNullOrWhiteSpace(MobilityBatchId))
            {
                ids.Add(MobilityBatchId);
            }

            return ids;
        }
    }
}
