namespace ProjectAegis.Delegation.Skills;

/// <summary>INF-7.1 citation into an existing projection, snapshot, or order-log row.</summary>
public sealed record EvidencePointer(
    EvidenceKind Kind,
    string Id,
    int? SequenceId,
    string? ProjectionType,
    string? Field);

/// <summary>AGC-03: why a later fire might be legal. Not a clearance.</summary>
public sealed record AuthorityBasis(
    string? PolicySnapshotId,
    bool PolicyUnavailable,
    string? Roe,
    string? Emcon,
    TrackSource TrackSource,
    bool FireControlSatisfied,
    bool EngagementAuthorizationImplied);

/// <summary>AGC-03 player countermand path. Submit still goes through <c>C2PlayerCommandBridge.TryIssue</c>.</summary>
public sealed record PlayerOverride(
    string Path,
    string CommandId,
    string ControllerRequirement,
    bool RejectLeavesNoMutation)
{
    public const string SubmitPath = "C2PlayerCommandBridge.TryIssue";
    public const string HumanControllerRequirement = "HumanController";
}

/// <summary>AGC-04 replay provenance. After submit, <see cref="SequenceIdOnSubmit"/> points at the order log.</summary>
public sealed record ReplayProvenance(
    string SkillId,
    string InvocationId,
    ulong SimTick,
    double SimTime,
    string? OrderLogFingerprintBefore,
    int? SequenceIdOnSubmit,
    bool Submitted);

/// <summary>Shared envelope for AGC-01..04 skill invocations. Lane discriminates read vs propose vs submit.</summary>
public sealed record SkillEnvelope(
    SkillLane Lane,
    string SkillId,
    string InvocationId,
    string? ProposalId,
    ulong SimTick,
    double SimTime,
    string? CommandId,
    int? TtlTicks,
    ulong? CreatedSimTick,
    RequiredApproval RequiredApproval,
    IReadOnlyList<EvidencePointer> InputsEvidence,
    IReadOnlyList<string> Assumptions,
    string Rationale,
    AuthorityBasis? AuthorityBasis,
    PlayerOverride? PlayerOverride,
    ReplayProvenance ReplayProvenance);

/// <summary>Catalog row for a discoverable Slice A skill.</summary>
public sealed record SkillDescriptor(
    string SkillId,
    string Name,
    IReadOnlyList<SkillLane> Lanes,
    IReadOnlyList<string> CommandIds);

/// <summary>Contract-only validation result. Does not enqueue orders.</summary>
public sealed record SkillEnvelopeValidation(
    bool Ok,
    string? FailureReason,
    string? ResolvedCommandId);
