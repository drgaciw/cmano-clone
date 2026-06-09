namespace ProjectAegis.Data.Osint.Connectors;

/// <summary>
/// Common contract for OSINT sources (real-time or fixture) per req05 + S20-01/S21.
/// Enables MCP on-demand (search_osint) + digest runner + CLI.
/// All impls (File, Rss, InMemory) must be deterministic (stable Fetch).
/// </summary>
public interface IOsintConnector
{
    OsintDiscoveryRecord[] Fetch();
}
