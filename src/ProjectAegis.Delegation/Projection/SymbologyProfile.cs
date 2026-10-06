namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Map symbology profile (S126 SYM-02 scaffold, DRG-231 / DRG-232).
/// Presentation-only: selecting a profile never changes sim state, orders or replay hashes.
/// </summary>
public enum SymbologyProfile
{
    /// <summary>Pre-S126 affiliation-only APP-6 placeholder (<see cref="App6Sidc"/>); first-run default.</summary>
    Legacy = 0,

    /// <summary>SYM-MIL-01 Military Tactical subset (see <see cref="MilitarySymbology"/>).</summary>
    MilitaryTactical = 1,

    /// <summary>SYM-CIV-01 civilian-friendly profile; reserved, not selectable until the DRG-232 scope decision.</summary>
    CivilianFriendly = 2,
}
