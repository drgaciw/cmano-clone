namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure tactical map overlay projections (CMD-21/34 selected-unit envelope rings).
/// Callers supply sensor/weapon ranges from catalog or engage context when available.
/// </summary>
public static class TacticalOverlayProjection
{
    public const string RingKindSensor = "Sensor";
    public const string RingKindWeapon = "Weapon";

    public const string DomainAir = "Air";
    public const string DomainSurface = "Surface";
    public const string DomainUnderwater = "Underwater";
    public const string DomainLand = "Land";
    public const string DomainUnknown = "Unknown";

    /// <summary>
    /// Projects envelope rings for the selected unit. Returns empty when
    /// <paramref name="selectedUnitId"/> is null or whitespace.
    /// </summary>
    public static IReadOnlyList<EnvelopeRingEntry> ProjectSelectedUnitEnvelopes(
        string? selectedUnitId,
        double sensorRangeNm,
        double weaponRangeNm,
        string domain = DomainUnknown)
    {
        if (string.IsNullOrWhiteSpace(selectedUnitId))
        {
            return Array.Empty<EnvelopeRingEntry>();
        }

        var domainLabel = string.IsNullOrWhiteSpace(domain) ? DomainUnknown : domain;
        return
        [
            new EnvelopeRingEntry(
                selectedUnitId,
                RingKindSensor,
                domainLabel,
                sensorRangeNm,
                IsSelectedUnit: true),
            new EnvelopeRingEntry(
                selectedUnitId,
                RingKindWeapon,
                domainLabel,
                weaponRangeNm,
                IsSelectedUnit: true),
        ];
    }
}
