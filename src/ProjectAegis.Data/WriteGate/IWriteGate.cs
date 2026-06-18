namespace ProjectAegis.Data.WriteGate;

using ProjectAegis.Data.Catalog;

/// <summary>ADR-006 / req-06: staged catalog writes (propose → approve → commit).</summary>
public interface IWriteGate
{
    string ProposeSensorBatch(
        IReadOnlyList<CatalogSensorBinding> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    string ProposeMountBatch(
        IReadOnlyList<CatalogMount> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    string ProposeLoadoutBatch(
        IReadOnlyList<CatalogLoadout> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    string ProposeMagazineBatch(
        IReadOnlyList<CatalogMagazineEntry> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    string ProposeCommsBatch(
        IReadOnlyList<CatalogCommsBinding> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    /// <summary>S22-04: stage platform metadata parsed from CMO markdown platform sections.</summary>
    string ProposePlatformBatch(
        IReadOnlyList<CatalogPlatformBinding> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    /// <summary>S22-04: stage weapon catalog rows parsed from CMO weapon markdown sections.</summary>
    string ProposeWeaponBatch(
        IReadOnlyList<CatalogWeaponRecord> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    /// <summary>S24-03: stage platform mobility rows from Phase B workbook edits.</summary>
    string ProposeMobilityBatch(
        IReadOnlyList<CatalogMobility> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    /// <summary>S24-03: stage platform signature rows from Phase B workbook edits.</summary>
    string ProposeSignatureBatch(
        IReadOnlyList<CatalogSignature> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    /// <summary>S24-03: stage platform EMCON rows from Phase B workbook edits.</summary>
    string ProposeEmconBatch(
        IReadOnlyList<CatalogEmcon> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    /// <summary>S25-04: stage platform damage rows from Phase B workbook edits.</summary>
    string ProposePlatformDamageBatch(
        IReadOnlyList<CatalogPlatformDamage> proposed,
        string actorType,
        string actorId,
        string rationale = "");

    WriteGateDecision ApproveBatch(string batchId, string actorType, string actorId);

    WriteGateDecision RejectBatch(string batchId, string actorType, string actorId, string rationale = "");

    IReadOnlyList<CatalogStagingBatchSummary> ListPendingBatches();
}

public sealed record WriteGateDecision(
    bool Committed,
    string BatchId,
    IReadOnlyList<string> Errors);

public sealed record CatalogStagingBatchSummary(
    string BatchId,
    string ActorType,
    string ActorId,
    int RecordCount,
    string ApprovalState,
    long ProposedUtcTicks);