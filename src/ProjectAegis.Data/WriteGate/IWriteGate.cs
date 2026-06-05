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