using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

public sealed class OrdnanceStateChangeOrderLogTests
{
    [Test]
    public void Fingerprint_includes_shotgun_and_winchester_tokens()
    {
        var log = new DecisionLog();
        log.AppendOrdnanceStateChange(new OrdnanceStateChangeRecord(
            0, 1.0, 1, new TargetId("u1"), "NOMINAL", "SHOTGUN", 1));
        log.AppendOrdnanceStateChange(new OrdnanceStateChangeRecord(
            0, 2.0, 2, new TargetId("u1"), "SHOTGUN", "WINCHESTER", 0));
        var fingerprint = log.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("OrdnanceStateChange"));
        Assert.That(fingerprint, Does.Contain("SHOTGUN"));
        Assert.That(fingerprint, Does.Contain("WINCHESTER"));
        Assert.That(fingerprint, Does.Contain("u1|NOMINAL|SHOTGUN|1"));
        Assert.That(fingerprint, Does.Contain("u1|SHOTGUN|WINCHESTER|0"));
    }
}
