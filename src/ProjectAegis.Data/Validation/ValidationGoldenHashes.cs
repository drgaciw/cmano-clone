namespace ProjectAegis.Data.Validation;

/// <summary>Pinned report hashes for CI golden scenarios (ADR-008).</summary>
public static class ValidationGoldenHashes
{
    /// <summary>InMemory Baltic catalog + <c>golden_strike_unreachable.json</c>.</summary>
    public const string StrikeUnreachable = "6e250abf5f12dca784eb6baca0393586cd8d4a055e1b8d811196823f794d18d5";

    /// <summary>InMemory Baltic catalog + <c>golden_clean.json</c>.</summary>
    public const string CleanPatrol = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
}