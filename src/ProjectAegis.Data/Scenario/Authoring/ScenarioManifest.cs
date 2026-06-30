namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;
using ProjectAegis.Data.Validation;

/// <summary>
/// Produces "Scenario manifest" for publishing/provenance track 5/5 per req 11.
/// Includes title, synopsis, assumptions, recommended DB version, changelog + semver +
/// embedded validation report + provenance for AI/imported content.
/// </summary>
public static class ManifestBuilder
{
    public sealed record ProvenanceTag(
        string Tag,
        string Source, // "user", "ai", "import", "ai-nl-scaffold"
        string? AgentId = null,
        string? Evidence = null,
        string? Hash = null);

    public sealed record ScenarioManifest(
        string Title,
        string Synopsis,
        IReadOnlyList<string> Assumptions,
        string RecommendedDbVersion,
        string Semver,
        IReadOnlyList<string> Changelog,
        ValidationReport EmbeddedValidationReport,
        IReadOnlyList<ProvenanceTag> ProvenanceTags,
        string ManifestHash);

    /// Produces "Scenario manifest" with provenance tags.
    public static ScenarioManifest Build(
        string scenarioId,
        ScenarioDocumentDto document,
        ValidationReport validationReport,
        string? title = null,
        string? synopsis = null,
        IReadOnlyList<string>? assumptions = null,
        string? recommendedDb = null,
        string semver = "0.1.0",
        IReadOnlyList<string>? changelog = null,
        IReadOnlyList<ProvenanceTag>? provenance = null)
    {
        var meta = document.Metadata;
        var dbVer = recommendedDb ?? meta.DbRef ?? meta.DbSnapshotId ?? meta.TlBranch ?? "baltic_patrol";
        var prov = provenance?.ToArray() ?? Array.Empty<ProvenanceTag>();

        // Default provenance tag for publish
        if (prov.Length == 0)
        {
            prov = new[]
            {
                new ProvenanceTag("publish", "user", "cli-publish", "embedded validation at publish")
            };
        }

        var t = title ?? $"Scenario {scenarioId}";
        var syn = synopsis ?? "Auto-generated scenario manifest from document.";
        var ass = assumptions ?? new[] { "Baltic patrol vertical slice assumptions", "TL branch compatible DB" };
        var ch = changelog ?? new[] { $"Initial publish v{semver}" };

        var payloadForHash = new
        {
            scenarioId,
            t,
            syn,
            ass,
            dbVer,
            semver,
            ch,
            validationHash = validationReport.ReportHash,
            prov = prov.Select(p => p.Tag).ToArray()
        };
        var hash = ComputeManifestHash(JsonSerializer.Serialize(payloadForHash));

        return new ScenarioManifest(
            t,
            syn,
            ass,
            dbVer,
            semver,
            ch,
            validationReport,
            prov,
            hash);
    }

    public static string Serialize(ScenarioManifest manifest)
    {
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return JsonSerializer.Serialize(manifest, options);
    }

    private static string ComputeManifestHash(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
    }
}
