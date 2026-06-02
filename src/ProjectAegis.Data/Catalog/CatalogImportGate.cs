namespace ProjectAegis.Data.Catalog;

/// <summary>TRL/TL gate for catalog drops — rejects rows below minimum confidence (ADR-006 MVP).</summary>
public static class CatalogImportGate
{
    public const double DefaultMinimumConfidence = 0.5;

    public static CatalogSensorBinding[] ApplyMinimumConfidence(
        IEnumerable<CatalogSensorBinding> bindings,
        double minimumConfidence = DefaultMinimumConfidence)
    {
        var min = Math.Clamp(minimumConfidence, 0, 1);
        return bindings
            .Where(b => b.Confidence >= min)
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();
    }
}