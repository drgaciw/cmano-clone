namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using C2Nodes;
using MissionIntent;
using TaskGroupCoord;

/// <summary>Optional authored facts consumed by the runtime coordination projection.</summary>
public interface ICoordinationFacts
{
    /// <summary>Mission-package definitions available to this scenario.</summary>
    IReadOnlyList<PackageDefinition> Packages { get; }

    /// <summary>Explicit coverage ownership facts. Missing rows remain unknown.</summary>
    IReadOnlyList<CoverageFact> Coverage { get; }

    /// <summary>Explicit mission intent by group or unit scope.</summary>
    IReadOnlyList<MissionIntentInput> Intents { get; }

    /// <summary>Explicit group-to-package association; overlap alone never assigns a package.</summary>
    IReadOnlyList<CoordinationGroupPackageAssignment> Assignments =>
        Array.Empty<CoordinationGroupPackageAssignment>();

    /// <summary>Optional authored escort links; unauthored falls back strictly to package escort elements.</summary>
    IReadOnlyList<CoordinationEscortFact> Escorts =>
        Array.Empty<CoordinationEscortFact>();

    /// <summary>Optional authored shared-targeting commitments.</summary>
    IReadOnlyList<CoordinationSharedTargetFact> SharedTargets =>
        Array.Empty<CoordinationSharedTargetFact>();
}

/// <summary>Authored association between a runtime group and mission package.</summary>
public sealed record CoordinationGroupPackageAssignment(string GroupId, string PackageId);

/// <summary>Optional authored escort assignment for a task group.</summary>
public sealed record CoordinationEscortFact(
    string GroupId,
    string EscortUnitId,
    string ProtectedUnitId,
    string CapabilityScope);

/// <summary>Optional authored shared-targeting commitment for a task group.</summary>
public sealed record CoordinationSharedTargetFact(
    string GroupId,
    string TargetId,
    IReadOnlyList<string> AssignedUnitIds,
    bool HasFireControlTrack,
    string SourceRef);

/// <summary>Projected escort responsibility within a task group.</summary>
public sealed record CoordinationEscortAssignment(
    string EscortUnitId,
    string ProtectedUnitId,
    string CapabilityScope,
    CoordinationEffectState State);

/// <summary>Projected sector responsibility derived from package coverage facts.</summary>
public sealed record CoordinationSectorResponsibility(
    string CoverageId,
    string Label,
    string OwnerUnitId,
    string ElementId,
    C2NodeRole? Role,
    CoverageStatus Status,
    string? SourceRef,
    string? ReasonCode);

/// <summary>Projected shared-targeting commitment across group members.</summary>
public sealed record CoordinationSharedTargetAssignment(
    string TargetId,
    IReadOnlyList<string> AssignedUnitIds,
    bool HasFireControlTrack,
    string SourceKind,
    string SourceRef);

/// <summary>Extended group coordination metadata (escorting, sectors, shared targeting, controller posture).</summary>
public sealed record CoordinationGroupDetails(
    string ControllerKind,
    Core.AutonomyLevel DelegationAutonomy,
    bool PendingReplan,
    bool IsOffGrid,
    IReadOnlyList<CoordinationEscortAssignment> Escorts,
    IReadOnlyList<CoordinationSectorResponsibility> Sectors,
    IReadOnlyList<CoordinationSharedTargetAssignment> SharedTargets)
{
    /// <summary>Empty group coordination details when unauthored.</summary>
    public static CoordinationGroupDetails Empty { get; } = new(
        ControllerKind: "None",
        DelegationAutonomy: Core.AutonomyLevel.Manual,
        PendingReplan: false,
        IsOffGrid: false,
        Escorts: Array.Empty<CoordinationEscortAssignment>(),
        Sectors: Array.Empty<CoordinationSectorResponsibility>(),
        SharedTargets: Array.Empty<CoordinationSharedTargetAssignment>());
}

/// <summary>Observed result of one responsibility on one unit.</summary>
public enum CoordinationEffectState
{
    Available = 0,
    Lost = 1,
    Detached = 2,
    UnknownRole = 3,
    /// <summary>Role is known but current availability is only last-known.</summary>
    UnknownAvailability = 4,
}

/// <summary>One package responsibility and its observed group-to-unit effect.</summary>
public sealed record CoordinationEffect(
    string GroupId,
    string UnitId,
    string ElementId,
    C2NodeRole? Role,
    CoordinationEffectState State,
    CoverageAssessment Coverage);

/// <summary>Named coordination gap retained for review instead of silently dropping a capability.</summary>
public sealed record CoordinationGap(
    string Code,
    string UnitId,
    C2NodeRole? Role,
    string Detail);

/// <summary>Runtime group coordination, mission intent, responsibility, and gap projection.</summary>
public sealed record CoordinationGroupSnapshot(
    TaskGroupCoordSnapshot Coordination,
    MissionIntentSnapshot Intent,
    IReadOnlyList<CoordinationEffect> Effects,
    IReadOnlyList<CoordinationGap> Gaps,
    CoordinationGroupDetails? Details = null);

/// <summary>Read-only coordination frame for every registered task group.</summary>
public sealed record CoordinationSnapshot(IReadOnlyList<CoordinationGroupSnapshot> Groups)
{
    public static CoordinationSnapshot Empty { get; } = new(Array.Empty<CoordinationGroupSnapshot>());
}
