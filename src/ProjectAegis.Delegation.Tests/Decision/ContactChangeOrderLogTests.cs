using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class ContactChangeOrderLogTests
{
    [Test]
    public void Contact_change_row_appears_in_fingerprint_with_stable_fields()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0,
            1.0,
            1,
            "u1",
            "c1",
            "hostile-1",
            "Unknown",
            "Detected"));

        var entries = log.ChronologicalEntries();
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Kind, Is.EqualTo(OrderLogEntryKind.ContactChange));

        var fingerprint = log.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("ContactChange|"));
        Assert.That(fingerprint, Does.Contain("|u1|c1|hostile-1|Unknown|Detected"));
    }

    [Test]
    public void Two_identical_contact_appends_yield_identical_fingerprints()
    {
        var a = FingerprintForContactAppend();
        var b = FingerprintForContactAppend();
        Assert.That(a, Is.EqualTo(b));
    }

    private static string FingerprintForContactAppend()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0,
            2.0,
            2,
            "obs-1",
            "contact-a",
            "tgt-9",
            "Unknown",
            "Detected"));
        return log.ComputeFingerprint();
    }
}