namespace ProjectAegis.Delegation.Projection;

using System.Globalization;
using ProjectAegis.Data.Catalog;

/// <summary>ADR-011 Phase H: read-only link catalog list lines for platform catalog viewer.</summary>
public static class PlatformLinkListProjection
{
    public static string FormatRow(CatalogLinkEntry link) =>
        $"{link.LinkId} display={FormatDisplay(link.DisplayName)} type={link.LinkType} latency={link.LatencyMsNominal}ms";

    public static IReadOnlyList<string> FormatRows(IEnumerable<CatalogLinkEntry> links) =>
        links
            .OrderBy(link => link.LinkId, StringComparer.Ordinal)
            .Select(FormatRow)
            .ToArray();

    private static string FormatDisplay(string displayName) =>
        string.IsNullOrWhiteSpace(displayName) ? "—" : displayName;
}