namespace ProjectAegis.Data.Osint;

/// <summary>Normalized OSINT digest or on-demand hit (doc 05 DSA-1.x).</summary>
public sealed record OsintDiscoveryRecord(
    string CanonicalId,
    string SourceUrl,
    string Snippet,
    double RelevanceScore,
    string TargetDoc,
    int ProposedTrl,
    long ObservedUtcTicks = 0);