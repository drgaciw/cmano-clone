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
        ulong seed = 42,
        int editVersion = 0)
    {
        ScenarioId = scenarioId;
        PolicyId = policyId;
        DbSnapshotId = dbSnapshotId;
        DbRef = dbRef;
        Seed = seed;
        EditVersion = editVersion;
    }

    public string ScenarioId { get; }

    public string PolicyId { get; }

    public string DbSnapshotId { get; }

    public string? DbRef { get; }

    public ulong Seed { get; }

    public int EditVersion { get; }

    public static ScenarioPackage FromDocument(string scenarioId, ScenarioDocumentDto document)
    {
        var meta = document.Metadata;
        var policyId = string.IsNullOrWhiteSpace(meta.PolicyId)
            ? "baltic-patrol"
            : meta.PolicyId.Trim();
        var dbSnapshotId = ResolveDbSnapshotId(meta);
        return new ScenarioPackage(
            scenarioId,
            policyId,
            dbSnapshotId,
            meta.DbRef,
            meta.Seed,
            meta.EditVersion);
    }

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