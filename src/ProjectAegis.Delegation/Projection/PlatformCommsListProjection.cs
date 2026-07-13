namespace ProjectAegis.Delegation.Projection;

using System.Globalization;
using ProjectAegis.Data.Catalog;

/// <summary>ADR-011 Phase G/H: read-only comms fitting list lines for platform catalog viewer.</summary>
public static class PlatformCommsListProjection
{
    public static string FormatRow(CatalogCommsBinding binding) =>
        FormatRow(binding, linkDisplayNames: null);

    public static string FormatRow(
        CatalogCommsBinding binding,
        IReadOnlyDictionary<string, string>? linkDisplayNames) =>
        $"{FormatLinkLabel(binding.LinkId, linkDisplayNames)} role={binding.Role} satcom={FormatBool(binding.SatcomCapable)}";

    public static IReadOnlyList<string> FormatRows(IEnumerable<CatalogCommsBinding> bindings) =>
        FormatRows(bindings, linkDisplayNames: null);

    public static IReadOnlyList<string> FormatRows(
        IEnumerable<CatalogCommsBinding> bindings,
        IReadOnlyDictionary<string, string>? linkDisplayNames) =>
        bindings
            .OrderBy(b => b.LinkId, StringComparer.Ordinal)
            .Select(binding => FormatRow(binding, linkDisplayNames))
            .ToArray();

    private static string FormatBool(bool value) =>
        value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();

    private static string FormatLinkLabel(
        string linkId,
        IReadOnlyDictionary<string, string>? linkDisplayNames)
    {
        if (linkDisplayNames is not null
            && linkDisplayNames.TryGetValue(linkId, out var displayName)
            && !string.IsNullOrWhiteSpace(displayName))
        {
            return $"{linkId} ({displayName})";
        }

        return linkId;
    }
}