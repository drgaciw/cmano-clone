namespace ProjectAegis.Data.Evidence;

/// <summary>Schema versioning for DRG-201 typed mission-thread evidence records.</summary>
public static class MissionThreadEvidenceSchema
{
    /// <summary>Current typed mission-thread evidence schema version.</summary>
    public const int Version = 1;
}

/// <summary>Six typed mission-thread evidence categories required by DRG-201.</summary>
public enum MissionThreadEvidenceKind
{
    Contact = 1,
    Authority = 2,
    DataLink = 3,
    Recommendation = 4,
    Engagement = 5,
    Assessment = 6,
}

/// <summary>Typed contact lifecycle and kill-chain observation fact.</summary>
public sealed record ContactEvidenceFact(
    string ContactId,
    string TargetId,
    string ObserverId,
    string LifecycleState,
    string? KillChainPhase,
    string? Confidence,
    string? QualityFlags);

/// <summary>Typed ROE/authority policy disposition fact.</summary>
public sealed record AuthorityEvidenceFact(
    string ActorId,
    string? TargetId,
    string RoeLevel,
    string? Disposition,
    string? ReasonCode,
    long? PolicySnapshotId,
    string? RequiredApproval);

/// <summary>Typed data-link / comms health and sharing latency fact.</summary>
public sealed record DataLinkEvidenceFact(
    string NetworkOrUnitId,
    string CommsState,
    string? PreviousState,
    bool OutOfCommsUnknown,
    int? ShareLagTicks,
    string? ReasonCode);

/// <summary>Typed agent recommendation / decision provenance fact.</summary>
public sealed record RecommendationEvidenceFact(
    string AgentId,
    string TargetId,
    string ChosenKind,
    string? PolicySnapshotId,
    string? PersonalitySlug,
    string? Outcome,
    string? WithheldReasonCode);

/// <summary>Typed engagement execution / abort fact.</summary>
public sealed record EngagementEvidenceFact(
    string ShooterUnitId,
    string? TargetUnitId,
    ulong EngagementId,
    bool Launched,
    string? WeaponFamilyId,
    int SalvoSize,
    string? AbortReasonCode,
    string? OutcomeCode);

/// <summary>Typed BDA / platform damage assessment fact.</summary>
public sealed record AssessmentEvidenceFact(
    string ContactId,
    string TargetId,
    string AssessmentState,
    string? DamageLevel,
    double? PreviousHpPct,
    double? NewHpPct,
    string? SourceKind);

/// <summary>
/// Immutable, observation-only typed mission-thread evidence record.
/// Preserves simulation authority and never re-derives tactical or combat rules.
/// </summary>
public sealed record MissionThreadEvidenceRecord(
    string EvidenceId,
    MissionThreadEvidenceKind Kind,
    ulong SimTick,
    double SimTime,
    string? SequenceRef,
    string AuthoritativeSource,
    ContactEvidenceFact? Contact = null,
    AuthorityEvidenceFact? Authority = null,
    DataLinkEvidenceFact? DataLink = null,
    RecommendationEvidenceFact? Recommendation = null,
    EngagementEvidenceFact? Engagement = null,
    AssessmentEvidenceFact? Assessment = null)
{
    /// <summary>Typed evidence records are strictly observational and preserve simulation authority.</summary>
    public bool PreservesSimAuthority => true;

    /// <summary>True when the payload property matching <see cref="Kind"/> is populated and identifiers are valid.</summary>
    public bool HasValidPayload() => Validate().Count == 0;

    /// <summary>Validates that required identifiers and the typed payload matching <see cref="Kind"/> are present.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(EvidenceId))
        {
            errors.Add("EvidenceId is required.");
        }

        if (string.IsNullOrWhiteSpace(AuthoritativeSource))
        {
            errors.Add("AuthoritativeSource is required.");
        }

        switch (Kind)
        {
            case MissionThreadEvidenceKind.Contact:
                if (Contact is null)
                {
                    errors.Add("Contact payload is required when Kind is Contact.");
                }
                break;
            case MissionThreadEvidenceKind.Authority:
                if (Authority is null)
                {
                    errors.Add("Authority payload is required when Kind is Authority.");
                }
                break;
            case MissionThreadEvidenceKind.DataLink:
                if (DataLink is null)
                {
                    errors.Add("DataLink payload is required when Kind is DataLink.");
                }
                break;
            case MissionThreadEvidenceKind.Recommendation:
                if (Recommendation is null)
                {
                    errors.Add("Recommendation payload is required when Kind is Recommendation.");
                }
                break;
            case MissionThreadEvidenceKind.Engagement:
                if (Engagement is null)
                {
                    errors.Add("Engagement payload is required when Kind is Engagement.");
                }
                break;
            case MissionThreadEvidenceKind.Assessment:
                if (Assessment is null)
                {
                    errors.Add("Assessment payload is required when Kind is Assessment.");
                }
                break;
            default:
                errors.Add($"Unsupported MissionThreadEvidenceKind: {Kind}.");
                break;
        }

        return errors;
    }
}
