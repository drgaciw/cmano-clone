using System.Text.Json;

namespace ProjectAegis.Data.Osint.Connectors;

/// <summary>
/// Sprint 20: File-based OSINT connector (local JSON fixture or directory of facts).
/// Deterministic: always returns stable sort by SourceUrl then CanonicalId.
/// No network, no wall-clock in hot path. Simple parser for test fixtures (array of objects).
/// Implements IOsintConnector for runner + CLI + MCP integration.
/// </summary>
public sealed class FileOsintConnector : IOsintConnector
{
    private readonly string _path;

    public FileOsintConnector(string path)
    {
        _path = path ?? string.Empty;
    }

    public OsintDiscoveryRecord[] Fetch()
    {
        if (string.IsNullOrWhiteSpace(_path) || !File.Exists(_path))
        {
            return Array.Empty<OsintDiscoveryRecord>();
        }

        try
        {
            var json = File.ReadAllText(_path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<OsintDiscoveryRecord>();
            }

            var list = new List<OsintDiscoveryRecord>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;

                string id = TryGetString(el, "canonicalId") ?? TryGetString(el, "CanonicalId") ?? "unknown";
                string url = TryGetString(el, "sourceUrl") ?? TryGetString(el, "SourceUrl") ?? string.Empty;
                string snip = TryGetString(el, "snippet") ?? TryGetString(el, "Snippet") ?? string.Empty;
                double score = TryGetDouble(el, "relevanceScore") ?? TryGetDouble(el, "RelevanceScore") ?? 0.5;
                string target = TryGetString(el, "targetDoc") ?? TryGetString(el, "TargetDoc") ?? "10";
                int trl = (int)(TryGetDouble(el, "proposedTrl") ?? TryGetDouble(el, "ProposedTrl") ?? 5);

                list.Add(new OsintDiscoveryRecord(id, url, snip, score, target, trl));
            }

            // Stable deterministic order (same as gate + InMemory + runner)
            return list
                .OrderBy(r => r.SourceUrl, StringComparer.Ordinal)
                .ThenBy(r => r.CanonicalId, StringComparer.Ordinal)
                .ToArray();
        }
        catch
        {
            // On any parse error return empty (deterministic, no throw in connector)
            return Array.Empty<OsintDiscoveryRecord>();
        }
    }

    private static string? TryGetString(JsonElement el, string name)
    {
        return el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
    }

    private static double? TryGetDouble(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var p))
        {
            if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var d)) return d;
            if (p.ValueKind == JsonValueKind.String && double.TryParse(p.GetString(), out var ds)) return ds;
        }
        return null;
    }
}
