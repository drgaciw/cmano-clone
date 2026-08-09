namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Selected-unit sensor/weapon envelope ring for map overlay (CMD-21/34).
/// RingKind: "Sensor" | "Weapon". Domain: "Air" | "Surface" | "Underwater" | "Land" | "Unknown".
/// </summary>
public sealed record EnvelopeRingEntry(
    string UnitId,
    string RingKind,
    string Domain,
    double RangeNm,
    bool IsSelectedUnit);
