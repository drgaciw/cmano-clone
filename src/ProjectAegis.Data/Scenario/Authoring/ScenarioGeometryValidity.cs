namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Pure geometry validity helper for map reference points (point / line / corridor / polygon / circle).
/// Used by the map invalid-mark contract only — not a Validation Engine rule.
/// </summary>
public static class ScenarioGeometryValidity
{
    /// <summary>
    /// Returns whether <paramref name="rp"/> has geometry that satisfies the type's vertex/radius rules.
    /// On failure, <paramref name="reason"/> describes the invalidity (type name included when relevant).
    /// </summary>
    /// <param name="rp">Reference point DTO; null is invalid.</param>
    /// <param name="reason">Human-readable invalid reason, or null when valid.</param>
    /// <returns>True when geometry is valid for the declared type.</returns>
    public static bool IsValid(ScenarioReferencePointDto? rp, out string? reason)
    {
        if (rp is null)
        {
            reason = "reference point is null.";
            return false;
        }

        var vertexCount = rp.Geometry?.Count ?? 0;
        var type = rp.Type ?? string.Empty;

        if (string.Equals(type, "point", StringComparison.OrdinalIgnoreCase))
        {
            if (vertexCount < 1)
            {
                reason = "point requires at least 1 vertex.";
                return false;
            }

            reason = null;
            return true;
        }

        if (string.Equals(type, "line", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "corridor", StringComparison.OrdinalIgnoreCase))
        {
            if (vertexCount < 2)
            {
                reason = $"{type.ToLowerInvariant()} requires at least 2 vertices.";
                return false;
            }

            reason = null;
            return true;
        }

        if (string.Equals(type, "polygon", StringComparison.OrdinalIgnoreCase))
        {
            if (vertexCount < 3)
            {
                reason = "polygon requires at least 3 vertices.";
                return false;
            }

            reason = null;
            return true;
        }

        if (string.Equals(type, "circle", StringComparison.OrdinalIgnoreCase))
        {
            if (vertexCount < 1)
            {
                reason = "circle requires a center vertex.";
                return false;
            }

            if (rp.RadiusNm is not > 0)
            {
                reason = "circle requires RadiusNm > 0.";
                return false;
            }

            reason = null;
            return true;
        }

        reason = $"unknown reference point type '{type}'.";
        return false;
    }
}
