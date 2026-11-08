namespace ProjectAegis.Data.Catalog;

/// <summary>
/// S32-03 bounded FK repair envelope for mount/loadout child rows (DBI-3.2 / PLE-2.3).
/// No scope creep beyond these documented rules.
/// </summary>
public static class MountLoadoutQuarantineRepairEnvelope
{
    public const string RuleLivePlatformFk = "platform_live_fk";
    public const string RuleStagingPlatformFk = "platform_staging_fk";
    public const string RuleBalticSeedFk = "baltic_seed_fk";

    public const string ReasonOrphanPlatform = "orphan_platform";
    public const string ReasonCircularFk = "circular_fk";
    public const string ReasonDuplicateLoadout = "duplicate_loadout_key";
    public const string ReasonOutOfEnvelope = "out_of_envelope";

    public static readonly IReadOnlyList<string> RepairRules =
    [
        RuleLivePlatformFk,
        RuleStagingPlatformFk,
        RuleBalticSeedFk,
    ];

    public static MountLoadoutRepairClassification ClassifyPlatformFk(
        string platformId,
        bool livePlatformExists,
        bool stagingPlatformExists,
        bool balticSeedExists)
    {
        if (string.IsNullOrWhiteSpace(platformId))
        {
            return new MountLoadoutRepairClassification(
                Repairable: false,
                Rule: null,
                Reason: ReasonOrphanPlatform);
        }

        if (livePlatformExists)
        {
            return new MountLoadoutRepairClassification(true, RuleLivePlatformFk, null);
        }

        if (stagingPlatformExists)
        {
            return new MountLoadoutRepairClassification(true, RuleStagingPlatformFk, null);
        }

        if (balticSeedExists)
        {
            return new MountLoadoutRepairClassification(true, RuleBalticSeedFk, null);
        }

        return new MountLoadoutRepairClassification(false, null, ReasonOrphanPlatform);
    }
}

public sealed record MountLoadoutRepairClassification(
    bool Repairable,
    string? Rule,
    string? Reason);