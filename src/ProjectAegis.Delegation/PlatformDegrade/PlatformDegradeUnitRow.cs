namespace ProjectAegis.Delegation.PlatformDegrade;

/// <summary>One own-unit degrade row: unit id, active codes, severity band, sim tick.</summary>
public sealed record PlatformDegradeUnitRow(
    string UnitId,
    IReadOnlyList<string> ActiveDegradeCodes,
    PlatformDegradeSeverityBand SeverityBand,
    ulong SimTick);
