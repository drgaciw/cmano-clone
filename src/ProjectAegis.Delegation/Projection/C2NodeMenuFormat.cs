namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.C2Nodes;

/// <summary>
/// Presentation-only C2 node menu labels (S122-11 / DRG-189). Does not issue orders.
/// </summary>
public static class C2NodeMenuFormat
{
    public const string ItemIdPrefix = "c2-node-";

    /// <summary>Stable menu row id for a projected element.</summary>
    public static string ItemId(string elementId) => $"{ItemIdPrefix}{elementId}";

    /// <summary>
    /// Operator-facing row: role, platform unit, availability
    /// (e.g. <c>C2 u1 (Available)</c>).
    /// </summary>
    public static string FormatLabel(C2NodeElement element)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        return $"{element.Role} {element.PlatformUnitId} ({element.Availability})";
    }
}
