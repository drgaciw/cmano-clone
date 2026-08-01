using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class UnitCommsDisplayTests
{
    [Test]
    public void FormatStatus_destroyed_wins_over_comms()
    {
        Assert.That(
            UnitCommsDisplay.FormatStatus(isAlive: false, CommsState.Nominal),
            Is.EqualTo(UnitCommsDisplay.Destroyed));
        Assert.That(
            UnitCommsDisplay.FormatStatus(isAlive: false, CommsState.Denied),
            Is.EqualTo(UnitCommsDisplay.Destroyed));
    }

    [Test]
    public void FormatStatus_denied_is_named_unknown_out_of_comms()
    {
        Assert.That(
            UnitCommsDisplay.FormatStatus(isAlive: true, CommsState.Denied),
            Is.EqualTo(UnitCommsDisplay.UnknownOutOfComms));
        Assert.That(
            UnitCommsDisplay.FormatStatus(isAlive: true, CommsState.Denied),
            Does.Contain("Out of comms"));
    }

    [Test]
    public void FormatStatus_degraded_keeps_operational_with_comms_note()
    {
        Assert.That(
            UnitCommsDisplay.FormatStatus(isAlive: true, CommsState.Degraded),
            Is.EqualTo(UnitCommsDisplay.OperationalCommsDegraded));
    }

    [Test]
    public void FormatStatus_nominal_is_operational()
    {
        Assert.That(
            UnitCommsDisplay.FormatStatus(isAlive: true, CommsState.Nominal),
            Is.EqualTo(UnitCommsDisplay.Operational));
    }
}
