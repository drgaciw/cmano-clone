namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;

/// <summary>Req-21 / DBI-3.1 detect-only link catalog integrity rules.</summary>
public static class LinkCatalogRules
{
    public const string OrphanCommsCode = "LINK_ORPHAN_COMMS";
    public const string TypeInvalidCode = "LINK_TYPE_INVALID";
    public const string LatencyInvalidCode = "LINK_LATENCY_INVALID";

    /// <summary>Minimum inclusive nominal latency (ms). Negative values are invalid.</summary>
    public const int MinLatencyMsNominal = 0;

    /// <summary>Maximum inclusive nominal latency (ms) for tactical/voice/satcom/strategic envelopes.</summary>
    public const int MaxLatencyMsNominal = 300_000;

    public static IReadOnlyList<DatabaseAgentFinding> Evaluate(ICatalogReader catalog)
    {
        var findings = new List<DatabaseAgentFinding>();
        var knownLinks = BuildLinkLookup(catalog.GetSortedLinks());

        foreach (var comms in catalog.GetSortedComms())
        {
            EvaluateOrphanComms(comms, knownLinks, findings);
        }

        foreach (var link in catalog.GetSortedLinks())
        {
            EvaluateLinkType(link, findings);
            EvaluateLinkLatency(link, findings);
        }

        return SortFindings(findings);
    }

    public static string ComputeFindingsHash(IReadOnlyList<DatabaseAgentFinding> findings)
    {
        var sorted = SortFindings(findings);
        var sb = new System.Text.StringBuilder();
        foreach (var finding in sorted)
        {
            sb.Append(finding.Code).Append('|')
                .Append(finding.Severity).Append('|')
                .Append(finding.Message).Append('\n');
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        using var sha = System.Security.Cryptography.SHA256.Create();
        return ToHexLower(sha.ComputeHash(bytes));
    }

    private static void EvaluateOrphanComms(
        CatalogCommsBinding comms,
        IReadOnlyDictionary<string, CatalogLinkEntry> knownLinks,
        List<DatabaseAgentFinding> findings)
    {
        if (knownLinks.ContainsKey(comms.LinkId))
        {
            return;
        }

        findings.Add(new DatabaseAgentFinding(
            OrphanCommsCode,
            $"{comms.PlatformId}/{comms.LinkId}: link '{comms.LinkId}' missing from link_catalog",
            "error"));
    }

    private static void EvaluateLinkType(CatalogLinkEntry link, List<DatabaseAgentFinding> findings)
    {
        if (CatalogLinkTypes.IsValid(link.LinkType))
        {
            return;
        }

        findings.Add(new DatabaseAgentFinding(
            TypeInvalidCode,
            $"{link.LinkId}: link_type '{link.LinkType}' not in CatalogLinkTypes",
            "error"));
    }

    private static void EvaluateLinkLatency(CatalogLinkEntry link, List<DatabaseAgentFinding> findings)
    {
        if (link.LatencyMsNominal >= MinLatencyMsNominal &&
            link.LatencyMsNominal <= MaxLatencyMsNominal)
        {
            return;
        }

        findings.Add(new DatabaseAgentFinding(
            LatencyInvalidCode,
            $"{link.LinkId}: latency_ms_nominal={link.LatencyMsNominal} out of bounds [{MinLatencyMsNominal},{MaxLatencyMsNominal}]",
            "error"));
    }

    private static IReadOnlyDictionary<string, CatalogLinkEntry> BuildLinkLookup(
        IReadOnlyList<CatalogLinkEntry> links)
    {
        var lookup = new Dictionary<string, CatalogLinkEntry>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            lookup[link.LinkId] = link;
        }

        return lookup;
    }

    private static IReadOnlyList<DatabaseAgentFinding> SortFindings(IReadOnlyList<DatabaseAgentFinding> findings) =>
        findings
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToArray();

    private static string ToHexLower(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = GetHexNibble(bytes[i] >> 4);
            chars[i * 2 + 1] = GetHexNibble(bytes[i] & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) => (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}