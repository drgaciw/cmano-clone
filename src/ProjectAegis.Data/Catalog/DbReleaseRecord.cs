namespace ProjectAegis.Data.Catalog;

public sealed record DbReleaseRecord(
    string ReleaseVersion,
    string SnapshotId,
    string SchemaVersion,
    long CreatedUtcTicks,
    string Notes);