namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.MissionIntent;
using ProjectAegis.Delegation.TaskGroupCoord;

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
}

/// <summary>Authored association between a runtime group and mission package.</summary>
public sealed record CoordinationGroupPackageAssignment(string GroupId, string PackageId);

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
    IReadOnlyList<CoordinationGap> Gaps);

/// <summary>Read-only coordination frame for every registered task group.</summary>
public sealed record CoordinationSnapshot(IReadOnlyList<CoordinationGroupSnapshot> Groups)
{
    public static CoordinationSnapshot Empty { get; } = new(Array.Empty<CoordinationGroupSnapshot>());
}
