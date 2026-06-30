namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using System.IO;

/// <summary>
/// scenario_publish --path : produces and prints "Scenario manifest" with all required fields + provenance tags.
/// </summary>
public static class ScenarioPublishCommand
{
    public static int Run(string scenarioPath, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "FILE_NOT_FOUND", scenarioPath);
        }

        var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        var catalog = ScenarioValidateCommand.ResolveCatalogPublic(document); // reuse resolve, low risk
        var config = new ValidationConfig();
        var (_, report) = ScenarioValidationExportGate.EvaluateExport(document, catalog, config);

        var id = Path.GetFileNameWithoutExtension(scenarioPath);
        var manifest = ManifestBuilder.Build(
            id,
            document,
            report,
            title: $"Published {id}",
            synopsis: $"Scaffolded/published scenario from {scenarioPath}",
            assumptions: new[] { "DB version pinned at publish", "Validation embedded", "AI or imported provenance tracked" },
            recommendedDb: document.Metadata.DbRef ?? document.Metadata.DbSnapshotId,
            semver: "1.0.0",
            changelog: new[] { "Initial publish via scenario_publish", "Embedded validation report", "Provenance tags added" },
            provenance: new[]
            {
                new ManifestBuilder.ProvenanceTag("publish", "user", "scenario-publish-cli", $"source:{scenarioPath}"),
                new ManifestBuilder.ProvenanceTag("validation-embed", "system", null, report.ReportHash)
            });

        output.WriteLine(ManifestBuilder.Serialize(manifest));
        return 0;
    }
}
