using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>PE-UX-W5: read-only catalog health strip (no write path).</summary>
[TestFixture]
public sealed class PlatformCatalogHealthProjectionTests
{
    [Test]
    public void Format_zero_counts_reports_ok()
    {
        var line = PlatformCatalogHealthProjection.Format(
            blockedFindingCount: 0,
            pendingDiffCount: 0,
            dependencyEdgeCount: 12);

        Assert.That(line, Is.EqualTo("Health: OK · edges 12 · pending 0 · blocked 0"));
    }

    [Test]
    public void Format_blocked_and_pending_surface_counts()
    {
        var line = PlatformCatalogHealthProjection.Format(
            blockedFindingCount: 2,
            pendingDiffCount: 5,
            dependencyEdgeCount: 40);

        Assert.That(line, Is.EqualTo("Health: ATTENTION · edges 40 · pending 5 · blocked 2"));
    }
}
