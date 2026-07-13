namespace ProjectAegis.Data.Snapshots;

using ProjectAegis.Data.Catalog;

/// <summary>S32-07 / S65-03 (Baltic v2 corpus hardening): deterministic read-only diff between two <c>ReleaseVersion</c> values.
/// Supports v2 corpus release versions and domain hashes in diff rows (e.g. baltic-v2-* named drops).
/// Extend-only Catalog; cite production/release-train-scope-boundary-2026-06-24.md + roadmap-062426.md §5/§7.
/// GitNexus: Compare reported CRITICAL47 indirect - additive doc only.
/// </summary>
public enum UnifiedReleaseTrainDiffKind
{
    Added,
    Changed,
    Removed,
}

public sealed record UnifiedReleaseTrainDiffRow(
    UnifiedReleaseTrainDiffKind Kind,
    string Domain,
    string FromReleaseVersion,
    string ToReleaseVersion,
    string FromSnapshotId,
    string ToSnapshotId,
    string FromContentHashSha256,
    string ToContentHashSha256)
{
    public string ToCanonicalLine() =>
        string.Join(
            '\t',
            Kind.ToString(),
            Domain,
            FromReleaseVersion,
            ToReleaseVersion,
            FromSnapshotId,
            ToSnapshotId,
            FromContentHashSha256,
            ToContentHashSha256);
}

public sealed record UnifiedReleaseTrainDiffReport(
    string FromReleaseVersion,
    string ToReleaseVersion,
    IReadOnlyList<UnifiedReleaseTrainDiffRow> Rows)
{
    public bool IsEmpty => Rows.Count == 0;

    public IReadOnlyList<string> ToSortedCanonicalLines() =>
        Rows
            .OrderBy(r => r.Kind)
            .ThenBy(r => r.Domain, StringComparer.Ordinal)
            .ThenBy(r => r.FromReleaseVersion, StringComparer.Ordinal)
            .ThenBy(r => r.ToReleaseVersion, StringComparer.Ordinal)
            .Select(r => r.ToCanonicalLine())
            .ToArray();
}

public static class UnifiedReleaseTrainDiffComparer
{
    public static UnifiedReleaseTrainDiffReport Compare(DbSnapshotStore store, string fromReleaseVersion, string toReleaseVersion)
    {
        if (string.IsNullOrWhiteSpace(fromReleaseVersion))
        {
            throw new ArgumentException("From release version required.", nameof(fromReleaseVersion));
        }

        if (string.IsNullOrWhiteSpace(toReleaseVersion))
        {
            throw new ArgumentException("To release version required.", nameof(toReleaseVersion));
        }

        var from = fromReleaseVersion.Trim();
        var to = toReleaseVersion.Trim();
        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            return new UnifiedReleaseTrainDiffReport(from, to, []);
        }

        var fromIndex = BuildDomainIndex(store, from);
        var toIndex = BuildDomainIndex(store, to);
        var domains = fromIndex.Keys
            .Concat(toIndex.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(d => d, StringComparer.Ordinal)
            .ToArray();

        var rows = new List<UnifiedReleaseTrainDiffRow>();
        foreach (var domain in domains)
        {
            var hasFrom = fromIndex.TryGetValue(domain, out var fromDrop);
            var hasTo = toIndex.TryGetValue(domain, out var toDrop);
            if (hasFrom && !hasTo)
            {
                rows.Add(new UnifiedReleaseTrainDiffRow(
                    UnifiedReleaseTrainDiffKind.Removed,
                    domain,
                    fromDrop!.ReleaseVersion,
                    "",
                    fromDrop.SnapshotId,
                    "",
                    fromDrop.ContentHashSha256,
                    ""));
                continue;
            }

            if (!hasFrom && hasTo)
            {
                rows.Add(new UnifiedReleaseTrainDiffRow(
                    UnifiedReleaseTrainDiffKind.Added,
                    domain,
                    "",
                    toDrop!.ReleaseVersion,
                    "",
                    toDrop.SnapshotId,
                    "",
                    toDrop.ContentHashSha256));
                continue;
            }

            if (hasFrom && hasTo && !DropsSemanticallyEqual(fromDrop!, toDrop!))
            {
                rows.Add(new UnifiedReleaseTrainDiffRow(
                    UnifiedReleaseTrainDiffKind.Changed,
                    domain,
                    fromDrop!.ReleaseVersion,
                    toDrop!.ReleaseVersion,
                    fromDrop.SnapshotId,
                    toDrop.SnapshotId,
                    fromDrop.ContentHashSha256,
                    toDrop.ContentHashSha256));
            }
        }

        return new UnifiedReleaseTrainDiffReport(from, to, rows);
    }

    private static Dictionary<string, UnifiedReleaseTrainDomainDrop> BuildDomainIndex(
        DbSnapshotStore store,
        string releaseVersion)
    {
        if (store.TryGetUnifiedManifest(releaseVersion, out var manifest))
        {
            return manifest.DomainDrops.ToDictionary(d => d.Domain, d => d, StringComparer.Ordinal);
        }

        var release = ResolveRelease(store, releaseVersion);
        if (!CatalogReleaseTrainDomains.TryParseFromReleaseVersion(releaseVersion, out var domain))
        {
            domain = releaseVersion;
        }

        var contentHash = ResolveDomainContentHash(store, release);
        return new Dictionary<string, UnifiedReleaseTrainDomainDrop>(StringComparer.Ordinal)
        {
            [domain] = new UnifiedReleaseTrainDomainDrop(
                domain,
                release.ReleaseVersion,
                release.SnapshotId,
                contentHash),
        };
    }

    private static DbReleaseRecord ResolveRelease(DbSnapshotStore store, string releaseVersion)
    {
        foreach (var release in store.GetSortedReleases())
        {
            if (string.Equals(release.ReleaseVersion, releaseVersion, StringComparison.Ordinal))
            {
                return release;
            }
        }

        throw new ArgumentException($"Release version '{releaseVersion}' not found in db_release.", nameof(releaseVersion));
    }

    private static string ResolveDomainContentHash(DbSnapshotStore store, DbReleaseRecord release)
    {
        if (UnifiedReleaseTrainManifest.TryParseContentHashFromNotes(release.Notes, out var fromNotes))
        {
            return fromNotes;
        }

        _ = store.TryGetContentHash(release.SnapshotId, out var fromSnapshot);
        return fromSnapshot;
    }

    private static bool DropsSemanticallyEqual(
        UnifiedReleaseTrainDomainDrop fromDrop,
        UnifiedReleaseTrainDomainDrop toDrop) =>
        string.Equals(fromDrop.SnapshotId, toDrop.SnapshotId, StringComparison.Ordinal) &&
        string.Equals(fromDrop.ContentHashSha256, toDrop.ContentHashSha256, StringComparison.Ordinal);
}