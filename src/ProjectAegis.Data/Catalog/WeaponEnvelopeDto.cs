namespace ProjectAegis.Data.Catalog;

/// <summary>Catalog weapon min/max range (meters) for engage envelope export (DATA-4).</summary>
public readonly record struct WeaponEnvelopeDto(double MinRangeMeters, double MaxRangeMeters);