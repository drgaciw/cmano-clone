using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessContactTests
{
    [Test]
    public void Baltic_patrol_run_includes_contact_change_in_fingerprint()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2);
        var b = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2);
        Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint));
        Assert.That(a.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(a.Fingerprint, Does.Contain("|Unknown|Detected"));
    }

    [Test]
    public void Detection_plus_contacts_emits_scripted_appearAtTick_contact_change()
    {
        const string policyId = "baltic-patrol-scripted-contact";
        var result = BalticReplayHarness.Run(42, policyId, ticks: 3);

        Assert.That(result.Fingerprint, Does.Contain("|c1|hostile-1|"));
        Assert.That(
            result.Fingerprint,
            Does.Contain("|2|u1|c-scripted-1|hostile-reinforce-1|Unknown|Detected"));
    }
}