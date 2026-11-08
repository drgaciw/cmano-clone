namespace ProjectAegis.Data.Scenario;

/// <summary>
/// One scenario-library browse row (CMD-27 Phase 1): metadata + pre-load feasibility.
/// </summary>
public sealed record ScenarioLibraryEntry(
    string ScenarioId,
    string Title,
    string PolicyId,
    string TlBranch,
    ulong Seed,
    string Location,
    string Year,
    string ProvenanceLabel,
    bool Available,
    string? UnavailableReason,
    string Difficulty,
    string Complexity,
    string SourcePath);

/// <summary>Stable pre-load feasibility reason codes (CMD-27.1 / CMD-27.7).</summary>
public static class ScenarioLibraryReasons
{
    public const string FileUnreadable = "FILE_UNREADABLE";
    public const string DbMismatch = "DB_MISMATCH";
    public const string BrokenRef = "BROKEN_REF";
    public const string ValidationBlocked = "VALIDATION_BLOCKED";
    public const string SchemaError = "SCHEMA_ERROR";
}
