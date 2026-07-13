namespace ProjectAegis.Data.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Runtime scenario manifest: policy id + catalog snapshot binding (doc 06 / doc 11 P0).</summary>
public sealed class ScenarioPackage
{
    public ScenarioPackage(
        string scenarioId,
        string policyId,
        string dbSnapshotId,
        string? dbRef = null,
        string? tlBranch = null,
        ulong seed = 42,
        int editVersion = 0)
    {
        ScenarioId = scenarioId;
        PolicyId = policyId;
        DbSnapshotId = dbSnapshotId;
        DbRef = dbRef;
        TlBranch = CatalogTlTier.Normalize(tlBranch);
        Seed = seed;
        EditVersion = editVersion;
    }

    public string ScenarioId { get; }

    public string PolicyId { get; }

    public string DbSnapshotId { get; }

    public string? DbRef { get; }

    /// <summary>Resolved TL branch label (TL-0…TL-5) bound at authoring/load.</summary>
    public string TlBranch { get; }

    public ulong Seed { get; }

    public int EditVersion { get; }

    public static ScenarioPackage FromDocument(string scenarioId, ScenarioDocumentDto document) =>
        FromDocument(scenarioId, document, catalog: null);

    public static ScenarioPackage FromDocument(string scenarioId, ScenarioDocumentDto document, ICatalogReader? catalog)
    {
        var meta = document.Metadata;
        var policyId = string.IsNullOrWhiteSpace(meta.PolicyId)
            ? "baltic-patrol"
            : meta.PolicyId.Trim();
        var binding = ResolveBinding(meta, catalog);
        return new ScenarioPackage(
            scenarioId,
            policyId,
            binding.DbSnapshotId,
            binding.DbRef ?? meta.DbRef,
            meta.TlBranch,
            meta.Seed,
            meta.EditVersion);
    }

    public static bool HasExplicitDbBinding(ScenarioMetadataDto metadata) =>
        !string.IsNullOrWhiteSpace(metadata.DbSnapshotId) ||
        !string.IsNullOrWhiteSpace(metadata.DbRef);

    public sealed record ScenarioDbBinding(string DbSnapshotId, string? DbRef);

    public static ScenarioDbBinding ResolveBinding(ScenarioMetadataDto metadata, ICatalogReader? catalog)
    {
        if (!string.IsNullOrWhiteSpace(metadata.DbSnapshotId))
        {
            return new ScenarioDbBinding(metadata.DbSnapshotId.Trim(), metadata.DbRef?.Trim());
        }

        if (!string.IsNullOrWhiteSpace(metadata.DbRef))
        {
            var trimmedRef = metadata.DbRef.Trim();
            if (catalog?.TryResolveDbRef(trimmedRef, out var resolved) == true)
            {
                return new ScenarioDbBinding(resolved, trimmedRef);
            }

            if (CatalogValidationDefaults.TryResolveBalticDbRef(trimmedRef, out var baltic))
            {
                return new ScenarioDbBinding(baltic, trimmedRef);
            }

            return new ScenarioDbBinding(trimmedRef, trimmedRef);
        }

        var tlBranch = ResolveTlBranch(metadata);
        if (catalog?.TryResolveSnapshotForTlBranch(tlBranch, out var snapshotId, out var dbRef) == true)
        {
            return new ScenarioDbBinding(snapshotId, dbRef);
        }

        return new ScenarioDbBinding(CatalogValidationDefaults.BalticSnapshotId, null);
    }

    public static string ResolveTlBranch(ScenarioMetadataDto metadata) =>
        CatalogTlTier.Normalize(metadata.TlBranch);

    public static string ResolveDbSnapshotId(ScenarioMetadataDto metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.DbSnapshotId))
        {
            return metadata.DbSnapshotId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(metadata.DbRef) &&
            CatalogValidationDefaults.TryResolveBalticDbRef(metadata.DbRef.Trim(), out var fromRef))
        {
            return fromRef;
        }

        return CatalogValidationDefaults.BalticSnapshotId;
    }
}