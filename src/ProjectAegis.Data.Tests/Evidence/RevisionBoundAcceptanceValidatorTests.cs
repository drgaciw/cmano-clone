using NUnit.Framework;
using ProjectAegis.Data.Evidence;

namespace ProjectAegis.Data.Tests.Evidence;

[TestFixture]
public sealed class RevisionBoundAcceptanceValidatorTests
{
    [Test]
    public void Pending_record_with_complete_ver07_3_provenance_is_schema_valid_and_not_owner_accepted()
    {
        var record = ValidPendingRecord();
        var result = AcceptanceEvidenceValidator.Validate(record);

        Assert.That(result.IsSchemaValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.IsOwnerAcceptedForCurrentRevision, Is.False);
        Assert.That(result.HistoricalRecordPreserved, Is.True);
        Assert.That(record.Counts.DeclaredFloor, Is.Not.EqualTo(record.Counts.ObservedPassed));
    }

    [Test]
    public void Missing_required_ver07_3_provenance_fails_closed()
    {
        var missingCommit = ValidPendingRecord() with
        {
            Revision = ValidPendingRecord().Revision with { CommitSha = "" },
        };
        var missingArtifactHash = ValidPendingRecord() with { ArtifactSha256 = "   " };
        var missingPolicies = ValidPendingRecord() with
        {
            Inputs = ValidPendingRecord().Inputs with { Policies = Array.Empty<HashedInputFile>() },
        };

        Assert.That(AcceptanceEvidenceValidator.Validate(missingCommit).IsSchemaValid, Is.False);
        Assert.That(AcceptanceEvidenceValidator.Validate(missingArtifactHash).IsSchemaValid, Is.False);
        Assert.That(AcceptanceEvidenceValidator.Validate(missingPolicies).IsSchemaValid, Is.False);
    }

    [Test]
    public void Ver07_4_ci_green_never_auto_accepts_and_forged_owner_fields_fail_closed()
    {
        var forgedPending = ValidPendingRecord() with
        {
            OwnerAcceptance = new OwnerAcceptanceDecision(
                OwnerAcceptanceStatus.Pending,
                Owner: "auto-ci-bot",
                DecisionDateUtc: "2026-11-23T15:00:00Z",
                DecisionReference: "ci-123",
                AcceptedRevision: ValidPendingRecord().Revision,
                AcceptedInputs: ValidPendingRecord().Inputs),
        };

        var pendingResult = AcceptanceEvidenceValidator.Validate(forgedPending);
        Assert.That(pendingResult.IsSchemaValid, Is.False);
        Assert.That(pendingResult.IsOwnerAcceptedForCurrentRevision, Is.False);

        var acceptedWithoutOwner = ValidPendingRecord() with
        {
            OwnerAcceptance = new OwnerAcceptanceDecision(
                OwnerAcceptanceStatus.Accepted,
                Owner: null,
                DecisionDateUtc: "2026-11-23T15:00:00Z",
                DecisionReference: "DRG-244",
                AcceptedRevision: ValidPendingRecord().Revision,
                AcceptedInputs: ValidPendingRecord().Inputs),
        };

        var missingOwnerResult = AcceptanceEvidenceValidator.Validate(acceptedWithoutOwner);
        Assert.That(missingOwnerResult.IsSchemaValid, Is.False);
        Assert.That(missingOwnerResult.IsOwnerAcceptedForCurrentRevision, Is.False);

        var acceptedWithoutInputs = ValidAcceptedRecord() with
        {
            OwnerAcceptance = ValidAcceptedRecord().OwnerAcceptance with { AcceptedInputs = null },
        };
        var missingInputsResult = AcceptanceEvidenceValidator.Validate(acceptedWithoutInputs);
        Assert.That(missingInputsResult.IsSchemaValid, Is.False);
        Assert.That(missingInputsResult.IsOwnerAcceptedForCurrentRevision, Is.False);
        Assert.That(missingInputsResult.Errors, Has.Some.Contains("AcceptedInputs"));

        var validAccepted = ValidAcceptedRecord();
        var acceptedResult = AcceptanceEvidenceValidator.Validate(
            validAccepted,
            validAccepted.Revision,
            validAccepted.Inputs);
        Assert.That(acceptedResult.IsSchemaValid, Is.True);
        Assert.That(acceptedResult.IsOwnerAcceptedForCurrentRevision, Is.True);
        Assert.That(acceptedResult.RequiresReassessment, Is.False);
    }

    [Test]
    public void Ver07_5_revision_delta_or_input_drift_invalidates_current_acceptance_while_preserving_history()
    {
        var accepted = ValidAcceptedRecord();
        var originalRevision = accepted.Revision;

        var driftedCommit = accepted.Revision with { CommitSha = "deadbeef" };
        var commitResult = AcceptanceEvidenceValidator.Validate(accepted, driftedCommit, accepted.Inputs);
        Assert.That(commitResult.IsOwnerAcceptedForCurrentRevision, Is.False);
        Assert.That(commitResult.RequiresReassessment, Is.True);
        Assert.That(commitResult.HistoricalRecordPreserved, Is.True);
        Assert.That(accepted.Revision, Is.EqualTo(originalRevision));

        var driftedDirtyTree = accepted.Revision with { LocalDeltaIdentity = "dirty-sha256:9999" };
        var dirtyResult = AcceptanceEvidenceValidator.Validate(accepted, driftedDirtyTree, accepted.Inputs);
        Assert.That(dirtyResult.IsOwnerAcceptedForCurrentRevision, Is.False);
        Assert.That(dirtyResult.RequiresReassessment, Is.True);

        var driftedCatalog = accepted.Inputs with { CatalogSha256 = "catalog-changed" };
        var catalogResult = AcceptanceEvidenceValidator.Validate(accepted, accepted.Revision, driftedCatalog);
        Assert.That(catalogResult.IsOwnerAcceptedForCurrentRevision, Is.False);
        Assert.That(catalogResult.RequiresReassessment, Is.True);
    }

    [Test]
    public void Ver07_6_layer_substitution_and_floor_aliasing_fail_closed()
    {
        var headlessForPlayMode = ValidPendingRecord() with
        {
            Layer = AcceptanceEvidenceLayer.Headless,
            RequiredLayer = AcceptanceEvidenceLayer.EditorPlayMode,
        };
        var fixtureForOwnerWalk = ValidPendingRecord() with
        {
            Layer = AcceptanceEvidenceLayer.Fixture,
            RequiredLayer = AcceptanceEvidenceLayer.OwnerWalk,
        };

        var r1 = AcceptanceEvidenceValidator.Validate(headlessForPlayMode);
        var r2 = AcceptanceEvidenceValidator.Validate(fixtureForOwnerWalk);

        Assert.That(r1.IsSchemaValid, Is.False);
        Assert.That(r1.Errors.Any(e => e.Contains("Layer", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(r2.IsSchemaValid, Is.False);
        Assert.That(r2.Errors.Any(e => e.Contains("Layer", StringComparison.OrdinalIgnoreCase)), Is.True);

        var failedBelowFloor = ValidPendingRecord() with
        {
            Counts = new FloorVsObservedCounts(DeclaredFloor: 4987, ObservedPassed: 4900, ObservedFailed: 0, ObservedSkipped: 0),
        };
        var r3 = AcceptanceEvidenceValidator.Validate(failedBelowFloor);
        Assert.That(r3.IsSchemaValid, Is.False);
    }

    private static RevisionBoundAcceptanceRecord ValidPendingRecord() =>
        new(
            CriterionId: "VER-07.3",
            RequirementId: "DRG-201",
            Revision: new RevisionIdentity("4b5b4ca1", "net11.0-debug", "clean", Array.Empty<string>()),
            Inputs: new InputDigestManifest(
                ScenarioId: "baltic-patrol",
                ScenarioSha256: "scen-sha",
                Seed: 42,
                CatalogId: "baltic_patrol.db",
                CatalogSha256: "cat-sha",
                Policies: [new HashedInputFile("data/scenarios/baltic-patrol.policy.json", "pol-sha")]),
            Layer: AcceptanceEvidenceLayer.Headless,
            RequiredLayer: AcceptanceEvidenceLayer.Headless,
            ExecutionCommandOrSteps: "dotnet test ProjectAegis.sln -v minimal --no-build",
            Result: "PASS",
            ArtifactLocation: "production/qa/evidence/sprint-6/s6-c8-red-evidence.md",
            ArtifactSha256: "art-sha",
            CollectedAtUtc: "2026-11-23T15:20:00Z",
            Counts: new FloorVsObservedCounts(DeclaredFloor: 4987, ObservedPassed: 4999, ObservedFailed: 0, ObservedSkipped: 0),
            OwnerAcceptance: new OwnerAcceptanceDecision(
                OwnerAcceptanceStatus.Pending,
                Owner: null,
                DecisionDateUtc: null,
                DecisionReference: null,
                AcceptedRevision: null,
                AcceptedInputs: null));

    private static RevisionBoundAcceptanceRecord ValidAcceptedRecord()
    {
        var baseRec = ValidPendingRecord() with
        {
            Layer = AcceptanceEvidenceLayer.OwnerWalk,
            RequiredLayer = AcceptanceEvidenceLayer.OwnerWalk,
        };
        return baseRec with
        {
            OwnerAcceptance = new OwnerAcceptanceDecision(
                OwnerAcceptanceStatus.Accepted,
                Owner: "owner-operator",
                DecisionDateUtc: "2026-11-23T16:00:00Z",
                DecisionReference: "DRG-244-SIGNOFF",
                AcceptedRevision: baseRec.Revision,
                AcceptedInputs: baseRec.Inputs),
        };
    }
}
