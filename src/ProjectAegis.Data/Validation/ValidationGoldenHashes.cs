namespace ProjectAegis.Data.Validation;

/// <summary>Pinned report hashes for CI golden scenarios (ADR-008).</summary>
public static class ValidationGoldenHashes
{
    /// <summary>InMemory Baltic catalog + <c>golden_strike_unreachable.json</c>.</summary>
    public const string StrikeUnreachable = "6e250abf5f12dca784eb6baca0393586cd8d4a055e1b8d811196823f794d18d5";

    /// <summary>InMemory Baltic catalog + <c>golden_clean.json</c>.</summary>
    public const string CleanPatrol = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>Phase B workbook fixture with no validation findings (PLE-4.3).</summary>
    public const string PhaseBCleanWorkbook = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>Phase B workbook fixture with deliberate header/FK/enum/sanity errors (PLE-4.3).</summary>
    public const string PhaseBFixtureErrors = "1bad2aad2d2cee480adc802ed6ffd36c7afc867141ad1e1a05d264712bdfa110";
}