namespace ProjectAegis.Data.Scenario;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Loads <see cref="ScenarioPackage"/> from canonical scenario JSON documents.</summary>
public static class ScenarioPackageLoader
{
    public static ScenarioPackage LoadFromFile(string scenarioPath)
    {
        var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        var scenarioId = Path.GetFileNameWithoutExtension(scenarioPath);
        return ScenarioPackage.FromDocument(scenarioId, document);
    }

    public static bool TryLoadFromFile(string scenarioPath, out ScenarioPackage? package)
    {
        package = null;
        if (!File.Exists(scenarioPath))
        {
            return false;
        }

        package = LoadFromFile(scenarioPath);
        return true;
    }
}