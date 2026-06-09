using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectAegis.Data.Osint.Connectors;

/// <summary>
/// Basic in-memory connector for Sprint 19 OSINT (demo / test fixture).
/// In real would parse file/RSS/HTTP to OsintDiscoveryRecord.
/// Implements IOsintConnector (retrofit for S20-01; stable Fetch always).
/// </summary>
public sealed class InMemoryOsintConnector : IOsintConnector
{
    private readonly OsintDiscoveryRecord[] _records;

    public InMemoryOsintConnector(IEnumerable<OsintDiscoveryRecord>? records = null)
    {
        _records = records?.ToArray() ?? GetDefaultFixture();
    }

    public OsintDiscoveryRecord[] Fetch()
    {
        // Return copy, sorted for determinism
        return _records
            .OrderBy(r => r.SourceUrl, StringComparer.Ordinal)
            .ThenBy(r => r.CanonicalId, StringComparer.Ordinal)
            .ToArray();
    }

    private static OsintDiscoveryRecord[] GetDefaultFixture()
    {
        return new[]
        {
            new OsintDiscoveryRecord("hypersonic-glide-demo", "https://osint.example/hg", "hypersonic boost-glide observed", 0.71, "09", 7),
            new OsintDiscoveryRecord("railgun-spec", "https://osint.example/rg", "speculative railgun mount", 0.82, "10", 5),
            new OsintDiscoveryRecord("low-relevance", "https://osint.example/low", "unconfirmed chatter", 0.40, "11", 2),
        };
    }
}
