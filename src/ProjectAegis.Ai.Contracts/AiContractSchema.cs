namespace ProjectAegis.Ai.Contracts;

/// <summary>
/// Frozen schema identity for Wave 0 contracts. Additive changes must bump
/// <see cref="Version"/>; breaking renames are forbidden while consumers exist.
/// </summary>
public static class AiContractSchema
{
    /// <summary>Current artifact / evidence schema version (integer; bump on additive shape changes).</summary>
    public const int Version = 1;

    /// <summary>Stable string form used in envelopes and transport headers.</summary>
    public const string VersionString = "1";
}
