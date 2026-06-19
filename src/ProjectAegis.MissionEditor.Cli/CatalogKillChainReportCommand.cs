namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;

/// <summary>S33-08 / DBI-4.5: read-only deterministic kill-chain rule report.</summary>
public static class CatalogKillChainReportCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string? databasePath, TextWriter output)
    {
        using var reader = OpenReader(databasePath);
        var findings = KillChainRules.Evaluate(reader);
        var canonicalLines = findings
            .Select(f => $"{f.Code}|{f.Severity}|{f.Message}")
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        var payload = new
        {
            ok = true,
            verb = "catalog_kill_chain_report",
            databasePath = databasePath ?? CatalogReaderFactory.ResolveBalticPatrolDatabasePath(),
            isEmpty = findings.Count == 0,
            findingCount = findings.Count,
            findingsHash = KillChainRules.ComputeFindingsHash(findings),
            canonicalLines,
            findings = findings.Select(f => new { f.Code, f.Message, f.Severity }),
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }

    public static void PrintHelp(TextWriter output)
    {
        output.WriteLine("catalog_kill_chain_report — deterministic read-only kill-chain impossibility report");
        output.WriteLine("Usage:");
        output.WriteLine("  catalog_kill_chain_report [--db <catalog.db>]");
        output.WriteLine("Notes:");
        output.WriteLine("  Read-only path; no CatalogWriteGate mutation.");
        output.WriteLine("  Detect-only DBI-3.5 rules; sorted stdout for curator review.");
    }

    private static SqliteCatalogReader OpenReader(string? databasePath)
    {
        if (!string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath))
        {
            return new SqliteCatalogReader(Path.GetFullPath(databasePath), "cli-kill-chain-report");
        }

        var resolved = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        if (!string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved))
        {
            return new SqliteCatalogReader(resolved, "cli-kill-chain-report-baltic");
        }

        throw new ArgumentException("Database path required and must exist.", nameof(databasePath));
    }
}