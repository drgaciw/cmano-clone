using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class CommsStateProjectionTests
{
    [Test]
    public void Project_reads_latest_comms_change()
    {
        var log = new DecisionLog();
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 1, 1, "net", CommsState.Nominal, CommsState.Degraded, "jam"));
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 3, 3, "net", CommsState.Degraded, CommsState.Denied, "down"));

        var snapshot = CommsStateProjection.Project(log);
        Assert.That(snapshot.State, Is.EqualTo(CommsState.Denied));
        Assert.That(snapshot.TopBarLabel, Is.EqualTo("COMMS: DENIED"));
        Assert.That(CommsStateProjection.BlocksNewEngagement(snapshot.State), Is.True);
    }
}