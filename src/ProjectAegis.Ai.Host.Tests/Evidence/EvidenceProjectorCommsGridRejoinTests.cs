using NUnit.Framework;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

[TestFixture]
public sealed class EvidenceProjectorCommsGridRejoinTests
{
    [Test]
    public void Project_maps_CommsGridRejoin_to_EvidenceEntryKind_and_extracts_attributes()
    {
        var log = new DecisionLog();
        log.AppendCommsGridRejoin(new CommsGridRejoinRecord(
            SequenceId: 0,
            SimTime: 20.0,
            SimTick: 20,
            UnitId: "u1",
            OffGridSinceTick: 10,
            RejoinTick: 20,
            InformativeMerges: new[]
            {
                new CommsGridRejoinMergeDelta("target-1", "dl-target-1", "Detected", "Identified"),
            },
            SuppressedCount: 2));

        var request = new EvidenceProjectionRequest(
            BuildIdentity: "test-build",
            ScenarioId: "rejoin-smoke",
            CatalogRevision: "rev-1",
            ViewingSide: ViewingSide.Blue,
            DebriefMode: false,
            FingerprintPrefix: "rj");

        var projector = new EvidenceProjector();
        var resultA = projector.Project(log, request);
        var resultB = projector.Project(log, request);

        Assert.That(resultA.Document.Entries, Has.Count.EqualTo(1));
        var entry = resultA.Document.Entries[0];
        Assert.That(entry.Kind, Is.EqualTo(EvidenceEntryKind.CommsGridRejoin));
        Assert.That(entry.UnitId, Is.EqualTo("u1"));
        Assert.That(entry.Attributes?["unitId"], Is.EqualTo("u1"));
        Assert.That(entry.Attributes?["offGridSinceTick"], Is.EqualTo("10"));
        Assert.That(entry.Attributes?["rejoinTick"], Is.EqualTo("20"));
        Assert.That(entry.Attributes?["informativeMergeCount"], Is.EqualTo("1"));
        Assert.That(entry.Attributes?["suppressedCount"], Is.EqualTo("2"));
        Assert.That(resultA.Document.Header.OrderLogFingerprint, Is.EqualTo(resultB.Document.Header.OrderLogFingerprint));
    }
}
