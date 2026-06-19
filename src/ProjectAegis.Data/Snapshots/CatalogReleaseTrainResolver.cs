namespace ProjectAegis.Data.Snapshots;

using ProjectAegis.Data.Catalog;

/// <summary>
/// S31-03: deterministic snapshotId+dbRef resolution from tlBranch using
/// <c>catalog_snapshot.branch</c> and <c>db_release</c> metadata.
/// </summary>
public static class CatalogReleaseTrainResolver
{
    /// <summary>
    /// Resolve snapshotId and dbRef from branch-matched snapshot candidates and sorted release rows.
    /// Prefers the snapshot referenced by the latest matching <c>db_release</c> row
    /// (<c>release_version ASC</c>, take last match); otherwise first candidate by <c>snapshot_id ASC</c>.
    /// </summary>
    /// <param name="matchingSnapshotIds">Snapshot ids with matching branch, ordered by snapshot_id ASC.</param>
    /// <param name="releases">Release rows ordered by release_version ASC.</param>
    public static bool TryResolveFromCandidates(
        IReadOnlyList<string> matchingSnapshotIds,
        IReadOnlyList<DbReleaseRecord> releases,
        out string snapshotId,
        out string dbRef)
    {
        snapshotId = "";
        dbRef = "";

        if (matchingSnapshotIds.Count == 0)
        {
            return false;
        }

        var candidateSet = new HashSet<string>(matchingSnapshotIds, StringComparer.Ordinal);
        string? preferred = null;
        foreach (var release in releases)
        {
            if (candidateSet.Contains(release.SnapshotId))
            {
                preferred = release.SnapshotId;
            }
        }

        snapshotId = preferred ?? matchingSnapshotIds[0];
        dbRef = ToDbRef(snapshotId);
        return true;
    }

    /// <summary>Map snapshot id to scenario dbRef (Baltic alias when applicable).</summary>
    public static string ToDbRef(string snapshotId) =>
        string.Equals(snapshotId, CatalogValidationDefaults.BalticSnapshotId, StringComparison.Ordinal)
            ? CatalogValidationDefaults.BalticSnapshotId
            : snapshotId;
}