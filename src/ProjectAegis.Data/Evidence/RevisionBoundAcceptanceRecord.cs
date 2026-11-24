namespace ProjectAegis.Data.Evidence;

/// <summary>Verification execution layer for VER-07.6 non-substitution checks.</summary>
public enum AcceptanceEvidenceLayer
{
    Headless = 1,
    Proxy = 2,
    Fixture = 3,
    EditorPlayMode = 4,
    OwnerWalk = 5,
}

/// <summary>Explicit owner acceptance state (VER-07.4); CI green never promotes Pending to Accepted.</summary>
public enum OwnerAcceptanceStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
}

/// <summary>SHA-256 hashed input artifact descriptor.</summary>
public sealed record HashedInputFile(string Path, string Sha256);

/// <summary>Revision and local-delta identity bound to an acceptance record (VER-07.3 / VER-07.5).</summary>
public sealed record RevisionIdentity(
    string CommitSha,
    string BuildIdentity,
    string LocalDeltaIdentity,
    IReadOnlyList<string>? UntrackedInputManifest = null);

/// <summary>Scenario, catalog, seed, and policy input digests bound to an acceptance run.</summary>
public sealed record InputDigestManifest(
    string ScenarioId,
    string ScenarioSha256,
    int Seed,
    string CatalogId,
    string CatalogSha256,
    IReadOnlyList<HashedInputFile> Policies);

/// <summary>Strictly separates declared baseline floor from observed execution counts (VER-07.3).</summary>
public sealed record FloorVsObservedCounts(
    int DeclaredFloor,
    int ObservedPassed,
    int ObservedFailed,
    int ObservedSkipped);

/// <summary>Explicit owner acceptance decision separate from automated CI results (VER-07.4).</summary>
public sealed record OwnerAcceptanceDecision(
    OwnerAcceptanceStatus Status,
    string? Owner,
    string? DecisionDateUtc,
    string? DecisionReference,
    RevisionIdentity? AcceptedRevision,
    InputDigestManifest? AcceptedInputs);

/// <summary>
/// Revision-bound acceptance evidence record satisfying VER-07.3..6 and REC-VER-07.3..6.
/// </summary>
public sealed record RevisionBoundAcceptanceRecord(
    string CriterionId,
    string RequirementId,
    RevisionIdentity Revision,
    InputDigestManifest Inputs,
    AcceptanceEvidenceLayer Layer,
    AcceptanceEvidenceLayer RequiredLayer,
    string ExecutionCommandOrSteps,
    string Result,
    string ArtifactLocation,
    string ArtifactSha256,
    string CollectedAtUtc,
    FloorVsObservedCounts Counts,
    OwnerAcceptanceDecision OwnerAcceptance,
    string? Notes = null);
