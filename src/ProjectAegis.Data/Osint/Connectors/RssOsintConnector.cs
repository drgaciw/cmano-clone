using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProjectAegis.Data.Osint;

namespace ProjectAegis.Data.Osint.Connectors;

/// <summary>
/// Sprint 20: RSS/HTTP-style OSINT connector (stub for real sources; fixture-driven for tests/CLI).
/// Supports JSON array fixtures (same shape as File connector) when path provided; otherwise returns
/// deterministic demo record for MCP/demo scenarios.
/// Deterministic: stable sort by SourceUrl then CanonicalId. No network in hot path.
/// Implements IOsintConnector (retrofit complete for S20-01).
/// </summary>
public sealed class RssOsintConnector : IOsintConnector
{
    private readonly string _path;

    public RssOsintConnector(string path = "")
    {
        _path = path;
    }

    public OsintDiscoveryRecord[] Fetch()
    {
        if (string.IsNullOrWhiteSpace(_path))
        {
            // No source specified: deterministic demo record for on-demand/MCP (fixture-driven tests use explicit path)
            return new[]
            {
                new OsintDiscoveryRecord("rss-demo-hypersonic", "https://rss.ex/h", "rss observed boost-glide", 0.76, "10", 6)
            }
            .OrderBy(r => r.SourceUrl, StringComparer.Ordinal)
            .ThenBy(r => r.CanonicalId, StringComparer.Ordinal)
            .ToArray();
        }

        if (!File.Exists(_path))
        {
            // Explicit path provided but unavailable: empty (deterministic, like File connector). Caller intent was "this source".
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

                // Robust parser (tolerant of casing / missing, mirrors FileOsintConnector)
                string id = TryGetString(el, "canonicalId") ?? TryGetString(el, "CanonicalId") ?? "rss-unknown";
                string url = TryGetString(el, "sourceUrl") ?? TryGetString(el, "SourceUrl") ?? string.Empty;
                string snip = TryGetString(el, "snippet") ?? TryGetString(el, "Snippet") ?? string.Empty;
                double score = TryGetDouble(el, "relevanceScore") ?? TryGetDouble(el, "RelevanceScore") ?? 0.5;
                string target = TryGetString(el, "targetDoc") ?? TryGetString(el, "TargetDoc") ?? "10";
                int trl = (int)(TryGetDouble(el, "proposedTrl") ?? TryGetDouble(el, "ProposedTrl") ?? 5);

                list.Add(new OsintDiscoveryRecord(id, url, snip, score, target, trl));
            }

            return list
                .OrderBy(r => r.SourceUrl, StringComparer.Ordinal)
                .ThenBy(r => r.CanonicalId, StringComparer.Ordinal)
                .ToArray();
        }
        catch
        {
            // Deterministic: never throw, empty on error
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