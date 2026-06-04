namespace ProjectAegis.Data.Osint;

/// <summary>Doc 05 confidence gate — staging proposal vs log-only (DSA-2.1).</summary>
public static class OsintProposalGate
{
    public const double DefaultProposalConfidenceThreshold = 0.65;

    public static (OsintDiscoveryRecord[] Proposals, OsintDiscoveryRecord[] LogOnly) Partition(
        IEnumerable<OsintDiscoveryRecord> discoveries,
        double minimumConfidence = DefaultProposalConfidenceThreshold)
    {
        var min = Math.Clamp(minimumConfidence, 0, 1);
        var proposals = new List<OsintDiscoveryRecord>();
        var logOnly = new List<OsintDiscoveryRecord>();

        foreach (var item in discoveries
                     .OrderBy(d => d.SourceUrl, StringComparer.Ordinal)
                     .ThenBy(d => d.CanonicalId, StringComparer.Ordinal))
        {
            if (item.RelevanceScore >= min)
            {
                proposals.Add(item);
            }
            else
            {
                logOnly.Add(item);
            }
        }

        return (proposals.ToArray(), logOnly.ToArray());
    }
}