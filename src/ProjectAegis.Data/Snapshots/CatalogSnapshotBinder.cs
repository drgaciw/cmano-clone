namespace ProjectAegis.Data.Snapshots;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

/// <summary>Records catalog snapshot hash + release row after write-gate approve (P2-3).</summary>
public static class CatalogSnapshotBinder
{
    public sealed record BindResult(
        string ReleaseVersion,
        string SnapshotId,
        string ContentHashSha256,
        int SensorRowCount,
        string TlTier);

    public static BindResult BindAfterApprove(
        string databasePath,
        string batchId,
        ICatalogClock clock,
        string? snapshotId = null,
        string? releaseVersion = null,
        string? tlTier = null)
    {
        var resolvedSnapshotId = string.IsNullOrWhiteSpace(snapshotId)
            ? CatalogValidationDefaults.BalticSnapshotId
            : snapshotId.Trim();
        var resolvedRelease = string.IsNullOrWhiteSpace(releaseVersion)
            ? $"catalog-approve-{SanitizeReleaseToken(batchId)}"
            : releaseVersion.Trim();
        var resolvedTlTier = CatalogTlTier.Normalize(tlTier);

        using var reader = new SqliteCatalogReader(databasePath, "snapshot-bind");
        var bindings = reader.GetSortedSensorBindings();
        var contentHash = CatalogSnapshotHasher.ComputeSha256Hex(bindings);

        using (var store = new DbSnapshotStore(databasePath))
        {
            store.RecordRelease(
                resolvedRelease,
                resolvedSnapshotId,
                contentHash,
                clock.UtcTicks,
                schemaVersion: CatalogTlTier.CatalogSchemaVersion,
                notes: $"batch={batchId};contentHash={contentHash}",
                branch: resolvedTlTier);
        }

        return new BindResult(resolvedRelease, resolvedSnapshotId, contentHash, bindings.Count, resolvedTlTier);
    }

    private static string SanitizeReleaseToken(string batchId)
    {
        var token = batchId.Replace(":", "-");
        return token.Length <= 64 ? token : token[..64];
    }
}