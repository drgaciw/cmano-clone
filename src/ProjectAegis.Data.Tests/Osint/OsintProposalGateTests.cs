using ProjectAegis.Data.Osint;
using Xunit;

namespace ProjectAegis.Data.Tests.Osint;

public sealed class OsintProposalGateTests
{
    [Fact]
    public void Partition_promotes_records_at_or_above_confidence_threshold()
    {
        var hits = new[]
        {
            new OsintDiscoveryRecord("hypersonic-glide-a", "https://example/a", "snippet-a", 0.64, "09", 6),
            new OsintDiscoveryRecord("hypersonic-glide-b", "https://example/b", "snippet-b", 0.65, "09", 7),
            new OsintDiscoveryRecord("speculative-rail-c", "https://example/c", "snippet-c", 0.9, "10", 4),
        };

        var (proposals, logOnly) = OsintProposalGate.Partition(hits);

        Assert.Single(logOnly);
        Assert.Equal("hypersonic-glide-a", logOnly[0].CanonicalId);
        Assert.Equal(2, proposals.Length);
        Assert.Equal("hypersonic-glide-b", proposals[0].CanonicalId);
        Assert.Equal("speculative-rail-c", proposals[1].CanonicalId);
    }

    [Fact]
    public void Partition_orders_deterministically_by_source_then_canonical_id()
    {
        var hits = new[]
        {
            new OsintDiscoveryRecord("z-last", "https://z.example/2", "s", 0.8, "09", 7),
            new OsintDiscoveryRecord("a-first", "https://a.example/1", "s", 0.8, "09", 7),
        };

        var (proposals, _) = OsintProposalGate.Partition(hits);

        Assert.Equal("a-first", proposals[0].CanonicalId);
        Assert.Equal("z-last", proposals[1].CanonicalId);
    }
}