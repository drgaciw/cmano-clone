namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Optimistic concurrency for MCP mutating tools (TR-editor-004 / ADR-008).</summary>
public static class ScenarioEditVersionGuard
{
    public const string ConflictCode = "CONFLICT";

    public static ScenarioEditConflictException? TryCheck(int expectedEditVersion, int currentEditVersion, string? fileHash = null)
    {
        if (expectedEditVersion == currentEditVersion)
        {
            return null;
        }

        return new ScenarioEditConflictException(
            ConflictCode,
            $"editVersion mismatch: expected {expectedEditVersion}, current {currentEditVersion}.",
            currentEditVersion,
            fileHash);
    }
}

public sealed class ScenarioEditConflictException : Exception
{
    public ScenarioEditConflictException(string code, string message, int currentEditVersion, string? fileHash)
        : base(message)
    {
        Code = code;
        CurrentEditVersion = currentEditVersion;
        FileHash = fileHash;
    }

    public string Code { get; }

    public int CurrentEditVersion { get; }

    public string? FileHash { get; }
}