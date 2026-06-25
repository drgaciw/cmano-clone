namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Snapshots;

/// <summary>S32-07 / DBI-4.5 / S65-03 (Baltic v2): read-only deterministic diff between two release versions.
/// Hardened/extended for Baltic v2 corpus (10 v2 policies, 9 v2 goldens from S64); v2 domain hashes/scenarios in output.
/// Extend-only; ZERO DelegationBridge. Cite: production/release-train-scope-boundary-2026-06-24.md §4 §8, roadmap §5/§7.
/// </summary>
public static class CatalogReleaseDiffCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string databasePath, string fromReleaseVersion, string toReleaseVersion, TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path required.", nameof(databasePath));
        }

        if (string.IsNullOrWhiteSpace(fromReleaseVersion))
        {
            throw new ArgumentException("From release version required.", nameof(fromReleaseVersion));
        }

        if (string.IsNullOrWhiteSpace(toReleaseVersion))
        {
            throw new ArgumentException("To release version required.", nameof(toReleaseVersion));
        }

        using var store = new DbSnapshotStore(databasePath);
        var report = UnifiedReleaseTrainDiffComparer.Compare(store, fromReleaseVersion, toReleaseVersion);
        var payload = new
        {
            ok = true,
            verb = "catalog_release_diff",
            databasePath,
            fromReleaseVersion = report.FromReleaseVersion,
            toReleaseVersion = report.ToReleaseVersion,
            isEmpty = report.IsEmpty,
            diffCount = report.Rows.Count,
            canonicalLines = report.ToSortedCanonicalLines(),
            rows = report.Rows
                .OrderBy(r => r.Kind)
                .ThenBy(r => r.Domain, StringComparer.Ordinal)
                .ThenBy(r => r.FromReleaseVersion, StringComparer.Ordinal)
                .ThenBy(r => r.ToReleaseVersion, StringComparer.Ordinal)
                .Select(row => new
                {
                    kind = row.Kind.ToString(),
                    row.Domain,
                    row.FromReleaseVersion,
                    row.ToReleaseVersion,
                    row.FromSnapshotId,
                    row.ToSnapshotId,
                    row.FromContentHashSha256,
                    row.ToContentHashSha256,
                }),
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }

    public static void PrintHelp(TextWriter output)
    {
        output.WriteLine("catalog_release_diff — deterministic read-only diff between two ReleaseVersion values");
        output.WriteLine("Usage:");
        output.WriteLine("  catalog_release_diff --db <catalog.db> --from <releaseVersion> --to <releaseVersion>");
        output.WriteLine("  catalog_release_diff --db <catalog.db> <fromReleaseVersion> <toReleaseVersion>");
        output.WriteLine("Notes:");
        output.WriteLine("  Read-only path; no CatalogWriteGate mutation.");
        output.WriteLine("  Compares unified manifests or per-domain nightly drops.");
        output.WriteLine("  Baltic v2 corpus supported (S65-03): v2-named releases (e.g. *-baltic-v2-*) and scenario refs emit in canonicalLines/rows.");
        output.WriteLine("  Timestamp-only metadata changes are excluded from semantic equality.");
        output.WriteLine("  Cite: release-train-scope-boundary-2026-06-24.md + roadmap §7 invariants.");
    }
}