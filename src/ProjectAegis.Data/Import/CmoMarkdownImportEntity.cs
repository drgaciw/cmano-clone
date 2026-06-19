namespace ProjectAegis.Data.Import;

/// <summary>CMO markdown import target entity (S26-02..04 Phase 2 weapon/platform CLI; S30-11 entity slices).</summary>
public enum CmoMarkdownImportEntity
{
    Sensor = 0,
    Weapon = 1,
    Platform = 2,
    Aircraft = 3,
    Submarine = 4,
    Facility = 5,
}