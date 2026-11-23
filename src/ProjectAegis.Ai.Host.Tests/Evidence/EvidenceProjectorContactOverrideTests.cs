using NUnit.Framework;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

[TestFixture]
public sealed class EvidenceProjectorContactOverrideTests
{
    [Test]
    public void Project_handles_ContactOverride_record_without_throwing()
    {
        var log = new DecisionLog();
        log.AppendContactOverride(new ContactOverrideRecord(
            SequenceId: 0,
            SimTime: 4.0,
            SimTick: 4,
            ContactId: "c1",
            Action: ContactOverrideAction.Reclassify,
            PreviousValue: "Hostile",
            NewValue: "Neutral"));

        var request = new EvidenceProjectionRequest(
            BuildIdentity: "test-build",
            ScenarioId: "override-smoke",
            CatalogRevision: "rev-1",
            ViewingSide: ViewingSide.Blue,
            DebriefMode: false,
            FingerprintPrefix: "ov");

        var result = new EvidenceProjector().Project(log, request);

        Assert.That(result.Document.Entries, Has.Count.EqualTo(1));
        var entry = result.Document.Entries[0];
        Assert.That(entry.Kind, Is.EqualTo(EvidenceEntryKind.ContactOverride));
        Assert.That(entry.Summary, Does.Contain("Reclassify").And.Contain("Neutral"));
        Assert.That(entry.Attributes?["contactId"], Is.EqualTo("c1"));
        Assert.That(entry.Attributes?["newValue"], Is.EqualTo("Neutral"));
    }
}
