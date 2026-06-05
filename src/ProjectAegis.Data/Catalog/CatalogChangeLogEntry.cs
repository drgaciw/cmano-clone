namespace ProjectAegis.Data.Catalog;

public sealed record CatalogChangeLogEntry(
    long ChangeId,
    string BatchId,
    string TableName,
    string EntityKey,
    string FieldName,
    string PreviousValue,
    string NewValue,
    string ActorType,
    string ActorId,
    string Rationale,
    string ApprovalState,
    long RevisedUtcTicks,
    string ReleaseVersion);