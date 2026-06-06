namespace ProjectAegis.Data.Osint.Connectors;

/// <summary>
/// Sprint 21: Common contract for OSINT sources (real-time or fixture) per req05 + S21 kickoff.
/// Enables MCP on-demand (search_osint) + digest runner.
/// All impls must be deterministic (stable Fetch).
/// </summary>
public interface IOsintConnector
{
    OsintDiscoveryRecord[] Fetch();
}
