namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;

/// <summary>S34-08 / DBI-4.5: read-only deterministic link catalog report.</summary>
public static class CatalogLinkReportCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string? databasePath, TextWriter output)
    {
        using var reader = OpenReader(databasePath, out var resolvedDatabasePath);
        var links = reader.GetSortedLinks();
        var canonicalLines = LinkCatalogReport.BuildCanonicalLines(links);

        var payload = new
        {
            ok = true,
            verb = "catalog_link_report",
            databasePath = resolvedDatabasePath,
            linkCount = links.Count,
            linksHash = LinkCatalogReport.ComputeLinksHash(links),
            canonicalLines,
            links = links.Select(link => new
            {
                link.LinkId,
                link.DisplayName,
                link.LinkType,
                link.LatencyMsNominal,
            }),
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }

    public static void PrintHelp(TextWriter output)
    {
        output.WriteLine("catalog_link_report — deterministic read-only link catalog report");
        output.WriteLine("Usage:");
        output.WriteLine("  catalog_link_report [--db <catalog.db>]");
        output.WriteLine("Notes:");
        output.WriteLine("  Read-only path; no CatalogWriteGate mutation.");
        output.WriteLine("  Emits sorted link_catalog rows for curator review.");
    }

    private static SqliteCatalogReader OpenReader(string? databasePath, out string resolvedDatabasePath)
    {
        if (!string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath))
        {
            resolvedDatabasePath = Path.GetFullPath(databasePath);
            return new SqliteCatalogReader(resolvedDatabasePath, "cli-link-report");
        }

        var resolved = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        if (!string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved))
        {
            resolvedDatabasePath = resolved;
            return new SqliteCatalogReader(resolved, "cli-link-report-baltic");
        }

        throw new ArgumentException("Database path required and must exist.", nameof(databasePath));
    }
}