namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;

/// <summary>DBI-3.4 / DBI-7.2: block write-gate commits when post-staging catalog has kill-chain errors.</summary>
public static class KillChainCommitGate
{
    public static IReadOnlyList<string> GetBlockingReasons(ICatalogReader catalog) =>
        KillChainRules.Evaluate(catalog)
            .Where(IsBlockingFinding)
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .Select(f => $"kill_chain:{f.Code}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public static bool HasBlockingFindings(ICatalogReader catalog) =>
        KillChainRules.Evaluate(catalog).Any(IsBlockingFinding);

    private static bool IsBlockingFinding(DatabaseAgentFinding finding) =>
        string.Equals(finding.Severity, "error", StringComparison.Ordinal)
        && finding.Code.StartsWith("KILL_CHAIN_", StringComparison.Ordinal);
}