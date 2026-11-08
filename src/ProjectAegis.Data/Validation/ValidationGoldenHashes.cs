namespace ProjectAegis.Data.Validation;

/// <summary>Pinned report hashes for CI golden scenarios (ADR-008).</summary>
public static class ValidationGoldenHashes
{
    /// <summary>InMemory Baltic catalog + <c>golden_strike_unreachable.json</c> (includes DOCTRINE_RESOLVED info).</summary>
    public const string StrikeUnreachable = "114703ff2a4f4b119dc3218edff2f3e0d2b8b9a3ea93bd36ca1bcd548130017f";

    /// <summary>InMemory Baltic catalog + <c>golden_clean.json</c> (includes DOCTRINE_RESOLVED info).</summary>
    public const string CleanPatrol = "dfec4cd9904e9ac942d3163ca572c9ae0c94382abad8f8e04ec84fa32e7cdc6b";

    /// <summary>Phase B workbook fixture with no validation findings (PLE-4.3).</summary>
    public const string PhaseBCleanWorkbook = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>Phase B workbook fixture with deliberate header/FK/enum/sanity errors (PLE-4.3).</summary>
    public const string PhaseBFixtureErrors = "1bad2aad2d2cee480adc802ed6ffd36c7afc867141ad1e1a05d264712bdfa110";

    /// <summary>Phase B damage fixture with deliberate HP/withdraw/flag errors (PLE-4.3 / S25-06).</summary>
    public const string PhaseBDamageFixtureErrors = "c88e90a96af0a13f064b3c533e8675eeae032f62c2cddc15b91515350a4c8ff6";
}