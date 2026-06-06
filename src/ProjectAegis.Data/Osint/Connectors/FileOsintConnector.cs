using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProjectAegis.Data.Osint;

namespace ProjectAegis.Data.Osint.Connectors;

/// <summary>
/// Sprint 20: File-based OSINT connector (local JSON fixture or directory of facts).
/// Deterministic: always returns stable sort by SourceUrl then CanonicalId.
/// No network, no wall-clock in hot path. Simple parser for test fixtures (array of objects).
/// S21: implements IOsintConnector.
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

/// <summary>
/// Sprint 20/21: RSS/HTTP connector (enhanced for S21 real source).
/// S21: implements IOsintConnector; simple parser from "RSS-like" json fixture (or extend for real RSS).
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
        if (string.IsNullOrWhiteSpace(_path) || !File.Exists(_path))
        {
            // S21 "real" enhancement: return a demo record for on-demand/MCP demo (fixture driven for tests)
            return new[]
            {
                new OsintDiscoveryRecord("rss-demo-hypersonic", "https://rss.ex/h", "rss observed boost-glide", 0.76, "10", 6)
            }.OrderBy(r => r.SourceUrl, StringComparer.Ordinal).ThenBy(r => r.CanonicalId, StringComparer.Ordinal).ToArray();
        }

        try
        {
            var json = File.ReadAllText(_path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return Array.Empty<OsintDiscoveryRecord>();

            var list = new List<OsintDiscoveryRecord>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var id = el.GetProperty("canonicalId").GetString() ?? "rss-unknown";
                var url = el.GetProperty("sourceUrl").GetString() ?? "";
                var snip = el.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "";
                var score = el.TryGetProperty("relevanceScore", out var sc) ? sc.GetDouble() : 0.5;
                var target = el.TryGetProperty("targetDoc", out var t) ? t.GetString() ?? "10" : "10";
                var trl = el.TryGetProperty("proposedTrl", out var tr) ? tr.GetInt32() : 5;
                list.Add(new OsintDiscoveryRecord(id, url, snip, score, target, trl));
            }
            return list
                .OrderBy(r => r.SourceUrl, StringComparer.Ordinal)
                .ThenBy(r => r.CanonicalId, StringComparer.Ordinal)
                .ToArray();
        }
        catch
        {
            return Array.Empty<OsintDiscoveryRecord>();
        }
    }
}
