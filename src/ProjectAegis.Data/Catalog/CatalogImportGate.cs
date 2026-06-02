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

    public const int DefaultMinimumTrl = 4;

    public static CatalogSensorBinding[] ApplyTlReviewGate(
        IEnumerable<CatalogSensorBinding> bindings,
        int minimumTrl = DefaultMinimumTrl,
        bool requireApproved = true)
    {
        var minTrl = Math.Clamp(minimumTrl, 1, 9);
        return bindings
            .Where(b => b.TrlLevel >= minTrl)
            .Where(b => !requireApproved ||
                        string.Equals(b.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();
    }

    public static CatalogSensorBinding[] ApplyAllGates(
        IEnumerable<CatalogSensorBinding> bindings,
        double minimumConfidence = DefaultMinimumConfidence,
        int minimumTrl = DefaultMinimumTrl,
        bool requireApproved = true) =>
        ApplyTlReviewGate(
            ApplyMinimumConfidence(bindings, minimumConfidence),
            minimumTrl,
            requireApproved);

    public static (CatalogSensorBinding[] Approved, QuarantinedCatalogBinding[] Quarantined) PartitionForImport(
        IEnumerable<CatalogSensorBinding> bindings,
        double minimumConfidence = DefaultMinimumConfidence,
        int minimumTrl = DefaultMinimumTrl,
        bool requireApproved = true)
    {
        var approved = new List<CatalogSensorBinding>();
        var quarantined = new List<QuarantinedCatalogBinding>();
        foreach (var binding in bindings.OrderBy(b => b.PlatformId, StringComparer.Ordinal)
                     .ThenBy(b => b.SensorId, StringComparer.Ordinal))
        {
            var reason = GetRejectionReason(binding, minimumConfidence, minimumTrl, requireApproved);
            if (reason == null)
            {
                approved.Add(binding);
            }
            else
            {
                quarantined.Add(new QuarantinedCatalogBinding(binding, reason));
            }
        }

        return (approved.ToArray(), quarantined.ToArray());
    }

    private static string? GetRejectionReason(
        CatalogSensorBinding binding,
        double minimumConfidence,
        int minimumTrl,
        bool requireApproved)
    {
        if (binding.Confidence < Math.Clamp(minimumConfidence, 0, 1))
        {
            return "confidence_below_minimum";
        }

        if (binding.TrlLevel < Math.Clamp(minimumTrl, 1, 9))
        {
            return "trl_below_minimum";
        }

        if (requireApproved &&
            !string.Equals(binding.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase))
        {
            return $"review_state_{binding.ReviewState}";
        }

        return null;
    }
}