namespace ProjectAegis.Data.Validation;

/// <summary>Pinned report hashes for CI golden scenarios (ADR-008).</summary>
public static class ValidationGoldenHashes
{
    /// <summary>InMemory Baltic catalog + <c>golden_strike_unreachable.json</c>.</summary>
    public const string StrikeUnreachable = "4a46d4db3b84f9d8bcce796b11a2359035aad114193d597fefba30969e405e23";

    /// <summary>InMemory Baltic catalog + <c>golden_clean.json</c>.</summary>
    public const string CleanPatrol = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
}