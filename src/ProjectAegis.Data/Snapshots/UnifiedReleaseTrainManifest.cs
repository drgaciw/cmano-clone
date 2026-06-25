namespace ProjectAegis.Data.Snapshots;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectAegis.Data.Catalog;

/// <summary>
/// S32-02 / S65-03 (Baltic v2 corpus hardening): curator drop manifest consolidating S31 per-domain <c>releaseVersion</c> rows
/// into one deterministic export manifest for scenario load binding.
/// Baltic v2 support via v2-named domain drops (e.g. nightly-*-baltic-v2-*) and unified releases.
/// Extend-only per release-train-scope-boundary-2026-06-24.md + roadmap-062426.md §5/§7.
/// GitNexus impact pre-edit: LOW.
/// </summary>
public sealed record UnifiedReleaseTrainDomainDrop(
    string Domain,
    string ReleaseVersion,
    string SnapshotId,
    string ContentHashSha256);

public sealed record UnifiedReleaseTrainManifest(
    string ReleaseVersion,
    string SnapshotId,
    string TlTier,
    string ContentHashSha256,
    string SchemaVersion,
    IReadOnlyList<UnifiedReleaseTrainDomainDrop> DomainDrops)
{
    public const string ManifestSchemaVersion = "1";
    public const string NotesPrefix = "unified-manifest:";

    public static UnifiedReleaseTrainManifest Consolidate(
        DbSnapshotStore store,
        string unifiedReleaseVersion,
        string snapshotId,
        string tlTier,
        IReadOnlyList<string> domainReleaseVersions)
    {
        if (string.IsNullOrWhiteSpace(unifiedReleaseVersion))
        {
            throw new ArgumentException("Unified release version required.", nameof(unifiedReleaseVersion));
        }

        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            throw new ArgumentException("Snapshot id required.", nameof(snapshotId));
        }

        if (domainReleaseVersions.Count == 0)
        {
            throw new ArgumentException("At least one domain release version required.", nameof(domainReleaseVersions));
        }

        var releaseIndex = store.GetSortedReleases()
            .ToDictionary(r => r.ReleaseVersion, r => r, StringComparer.Ordinal);

        var drops = new List<UnifiedReleaseTrainDomainDrop>();
        foreach (var releaseVersion in domainReleaseVersions
                     .Select(v => v.Trim())
                     .Where(v => !string.IsNullOrWhiteSpace(v))
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(v => v, StringComparer.Ordinal))
        {
            if (!CatalogReleaseTrainDomains.TryParseFromReleaseVersion(releaseVersion, out var domain))
            {
                throw new ArgumentException(
                    $"Release version '{releaseVersion}' is not a recognized per-domain nightly drop.",
                    nameof(domainReleaseVersions));
            }

            if (!releaseIndex.TryGetValue(releaseVersion, out var release))
            {
                throw new ArgumentException(
                    $"Release version '{releaseVersion}' not found in db_release.",
                    nameof(domainReleaseVersions));
            }

            if (!string.Equals(release.SnapshotId, snapshotId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Domain release '{releaseVersion}' snapshot '{release.SnapshotId}' does not match unified snapshot '{snapshotId}'.",
                    nameof(domainReleaseVersions));
            }

            var contentHash = ResolveDomainContentHash(store, release);
            drops.Add(new UnifiedReleaseTrainDomainDrop(
                domain,
                releaseVersion,
                release.SnapshotId,
                contentHash));
        }

        var sortedDrops = drops
            .OrderBy(d => d.Domain, StringComparer.Ordinal)
            .ThenBy(d => d.ReleaseVersion, StringComparer.Ordinal)
            .ToArray();

        var manifestHash = ComputeManifestHash(sortedDrops);
        return new UnifiedReleaseTrainManifest(
            unifiedReleaseVersion.Trim(),
            snapshotId.Trim(),
            CatalogTlTier.Normalize(tlTier),
            manifestHash,
            ManifestSchemaVersion,
            sortedDrops);
    }

    public static string ComputeManifestHash(IReadOnlyList<UnifiedReleaseTrainDomainDrop> domainDrops)
    {
        var sorted = domainDrops
            .OrderBy(d => d.Domain, StringComparer.Ordinal)
            .ThenBy(d => d.ReleaseVersion, StringComparer.Ordinal)
            .ToArray();

        var sb = new StringBuilder(sorted.Length * 96);
        foreach (var drop in sorted)
        {
            sb.Append(drop.Domain);
            sb.Append('\t');
            sb.Append(drop.ReleaseVersion);
            sb.Append('\t');
            sb.Append(drop.SnapshotId);
            sb.Append('\t');
            sb.Append(drop.ContentHashSha256);
            sb.Append('\n');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var sha = SHA256.Create();
        return ToHexLower(sha.ComputeHash(bytes));
    }

    public string ToNotesJson()
    {
        var payload = new NotesPayload(
            SchemaVersion,
            TlTier,
            DomainDrops
                .OrderBy(d => d.Domain, StringComparer.Ordinal)
                .Select(d => new NotesDomainDrop(d.Domain, d.ReleaseVersion, d.SnapshotId, d.ContentHashSha256))
                .ToArray());
        return NotesPrefix + JsonSerializer.Serialize(payload);
    }

    public static bool TryParseFromNotes(string? notes, string releaseVersion, out UnifiedReleaseTrainManifest manifest)
    {
        manifest = null!;
        if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith(NotesPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var json = notes[NotesPrefix.Length..];
            var payload = JsonSerializer.Deserialize<NotesPayload>(json);
            if (payload == null || payload.DomainDrops.Length == 0)
            {
                return false;
            }

            var drops = payload.DomainDrops
                .Select(d => new UnifiedReleaseTrainDomainDrop(
                    d.Domain,
                    d.ReleaseVersion,
                    d.SnapshotId,
                    d.ContentHashSha256))
                .OrderBy(d => d.Domain, StringComparer.Ordinal)
                .ToArray();

            manifest = new UnifiedReleaseTrainManifest(
                releaseVersion,
                drops[0].SnapshotId,
                CatalogTlTier.Normalize(payload.TlTier),
                ComputeManifestHash(drops),
                string.IsNullOrWhiteSpace(payload.SchemaVersion)
                    ? ManifestSchemaVersion
                    : payload.SchemaVersion,
                drops);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ResolveDomainContentHash(DbSnapshotStore store, DbReleaseRecord release)
    {
        if (TryParseContentHashFromNotes(release.Notes, out var fromNotes))
        {
            return fromNotes;
        }

        _ = store.TryGetContentHash(release.SnapshotId, out var fromSnapshot);
        return fromSnapshot;
    }

    internal static bool TryParseContentHashFromNotes(string? notes, out string contentHash)
    {
        contentHash = "";
        if (string.IsNullOrWhiteSpace(notes))
        {
            return false;
        }

        const string prefix = "contentHash=";
        foreach (var segment in notes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = segment.Trim();
            if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
            {
                contentHash = trimmed[prefix.Length..];
                return !string.IsNullOrEmpty(contentHash);
            }
        }

        return false;
    }

    public CatalogExportManifest ToExportManifest() =>
        new(
            ReleaseVersion,
            TlTier,
            CatalogTlTier.CatalogSchemaVersion,
            ContentHashSha256,
            CatalogTlTier.ExportManifestSchemaVersion);

    private static string ToHexLower(byte[] hash)
    {
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var b = hash[i];
            chars[i * 2] = GetHexNibble(b >> 4);
            chars[i * 2 + 1] = GetHexNibble(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) =>
        (char)(value < 10 ? '0' + value : 'a' + (value - 10));

    private sealed record NotesPayload(
        string SchemaVersion,
        string TlTier,
        NotesDomainDrop[] DomainDrops);

    private sealed record NotesDomainDrop(
        string Domain,
        string ReleaseVersion,
        string SnapshotId,
        string ContentHashSha256);
}