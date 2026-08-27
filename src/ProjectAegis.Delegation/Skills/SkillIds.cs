namespace ProjectAegis.Delegation.Skills;

/// <summary>DRG-196 / AGC-01: stable dotted skill identities. Source: production/docs/skills/agent-c2-skill-contract/catalog.json.</summary>
public static class SkillIds
{
    public const string TrackAssess = "c2.track.assess";
    public const string DatalinkReason = "c2.datalink.reason";
    public const string PairingRecommend = "c2.pairing.recommend";
    public const string Explain = "c2.explain";

    /// <summary>Host verb over an approved proposal. Not a Slice A catalog row.</summary>
    public const string Submit = "c2.skill.submit";
}

/// <summary>AGC-02 lanes: projection read, bounded proposal, approved command submit.</summary>
public enum SkillLane
{
    Read = 0,
    Propose = 1,
    Submit = 2,
}

/// <summary>ADR-018 track origin for authority basis. Shared SA is not fire-control.</summary>
public enum TrackSource
{
    Organic = 0,
    DatalinkShared = 1,
    FusedWithoutOrganicFc = 2,
    Unknown = 3,
}

/// <summary>AGC-03 required approval. Propose never uses <see cref="None"/> when a command is named.</summary>
public enum RequiredApproval
{
    None = 0,
    Operator = 1,
    WeaponsRelease = 2,
}

/// <summary>INF-7.1 evidence pointer kinds.</summary>
public enum EvidenceKind
{
    Contact = 0,
    Unit = 1,
    Policy = 2,
    OrderLog = 3,
    Projection = 4,
    Snapshot = 5,
}
