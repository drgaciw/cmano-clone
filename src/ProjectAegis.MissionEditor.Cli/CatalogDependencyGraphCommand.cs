namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;

/// <summary>S33-08 / DBI-1.5: read-only deterministic dependency graph export.</summary>
public static class CatalogDependencyGraphCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string? databasePath, TextWriter output)
    {
        using var reader = OpenReader(databasePath);
        var edges = reader.GetSortedDependencyEdges();
        var canonicalLines = edges
            .Select(FormatCanonicalLine)
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        var payload = new
        {
            ok = true,
            verb = "catalog_dependency_graph",
            databasePath = databasePath ?? CatalogReaderFactory.ResolveBalticPatrolDatabasePath(),
            edgeCount = edges.Count,
            canonicalLines,
            edges = edges.Select(edge => new
            {
                kind = edge.Kind.ToString(),
                edge.PlatformId,
                edge.MountId,
                edge.WeaponId,
                edge.SensorId,
            }),
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }

    public static void PrintHelp(TextWriter output)
    {
        output.WriteLine("catalog_dependency_graph — deterministic read-only kill-chain dependency graph");
        output.WriteLine("Usage:");
        output.WriteLine("  catalog_dependency_graph [--db <catalog.db>]");
        output.WriteLine("Notes:");
        output.WriteLine("  Read-only path; no CatalogWriteGate mutation.");
        output.WriteLine("  Emits sorted platform→mount→weapon and platform→sensor edges.");
    }

    private static SqliteCatalogReader OpenReader(string? databasePath)
    {
        if (!string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath))
        {
            return new SqliteCatalogReader(Path.GetFullPath(databasePath), "cli-dependency-graph");
        }

        var resolved = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        if (!string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved))
        {
            return new SqliteCatalogReader(resolved, "cli-dependency-graph-baltic");
        }

        throw new ArgumentException("Database path required and must exist.", nameof(databasePath));
    }

    private static string FormatCanonicalLine(CatalogDependencyEdge edge) =>
        edge.Kind switch
        {
            CatalogDependencyEdgeKind.PlatformToSensor =>
                $"sensor:{edge.PlatformId}:{edge.SensorId}",
            CatalogDependencyEdgeKind.PlatformToMountToWeapon =>
                $"weapon:{edge.PlatformId}:{edge.MountId}:{edge.WeaponId}",
            _ => $"mount:{edge.PlatformId}:{edge.MountId}",
        };
}