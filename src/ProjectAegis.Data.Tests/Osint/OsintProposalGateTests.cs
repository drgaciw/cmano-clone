using ProjectAegis.Data.Osint;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Osint;

public sealed class OsintProposalGateTests
{
    [Test]
    public void Partition_promotes_records_at_or_above_confidence_threshold()
    {
        var hits = new[]
        {
            new OsintDiscoveryRecord("hypersonic-glide-a", "https://example/a", "snippet-a", 0.64, "09", 6),
            new OsintDiscoveryRecord("hypersonic-glide-b", "https://example/b", "snippet-b", 0.65, "09", 7),
            new OsintDiscoveryRecord("speculative-rail-c", "https://example/c", "snippet-c", 0.9, "10", 4),
        };

        var (proposals, logOnly) = OsintProposalGate.Partition(hits);

        Assert.That(logOnly, Has.Exactly(1).Items);
        Assert.That(logOnly[0].CanonicalId, Is.EqualTo("hypersonic-glide-a"));
        Assert.That(proposals.Length, Is.EqualTo(2));
        Assert.That(proposals[0].CanonicalId, Is.EqualTo("hypersonic-glide-b"));
        Assert.That(proposals[1].CanonicalId, Is.EqualTo("speculative-rail-c"));
    }

    [Test]
    public void Partition_orders_deterministically_by_source_then_canonical_id()
    {
        var hits = new[]
        {
            new OsintDiscoveryRecord("z-last", "https://z.example/2", "s", 0.8, "09", 7),
            new OsintDiscoveryRecord("a-first", "https://a.example/1", "s", 0.8, "09", 7),
        };

        var (proposals, _) = OsintProposalGate.Partition(hits);

        Assert.That(proposals[0].CanonicalId, Is.EqualTo("a-first"));
        Assert.That(proposals[1].CanonicalId, Is.EqualTo("z-last"));
    }
}