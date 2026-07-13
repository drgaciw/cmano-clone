namespace ProjectAegis.Delegation.UnityAdapter.Editor;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Headless-testable Unity edit-mode shell adapter (GDD AC-8).</summary>
public static class ScenarioEditorHostAdapter
{
    public static ScenarioEditorHostState LoadForEditMode(string scenarioPath)
    {
        ScenarioDocumentDto document;
        if (scenarioPath.EndsWith(".aegis-scenario", StringComparison.OrdinalIgnoreCase))
        {
            document = AegisScenarioPackage.Read(scenarioPath);
        }
        else
        {
            document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        }

        var editorState = document.EditorState ?? CreateDefaultEditorState(document).EditorState;
        return new ScenarioEditorHostState(document, editorState);
    }

    public static ScenarioEditorHostState CreateDefaultEditorState(ScenarioDocumentDto document)
    {
        var (lat, lon) = ResolveTheaterCentroid(document);
        return new ScenarioEditorHostState(
            document,
            new ScenarioEditorStateDto
            {
                CameraLat = lat,
                CameraLon = lon,
                CameraZoom = 1.0,
                LayersVisible = true,
            });
    }

    private static (double Lat, double Lon) ResolveTheaterCentroid(ScenarioDocumentDto document)
    {
        if (document.Orbat?.Units is { Count: > 0 } units)
        {
            return (units.Average(u => u.Lat), units.Average(u => u.Lon));
        }

        if (document.Missions.FirstOrDefault()?.PatrolZone is { Count: > 0 } zone)
        {
            return (zone.Average(w => w.Lat), zone.Average(w => w.Lon));
        }

        return (57.0, 20.0);
    }
}

public sealed record ScenarioEditorHostState(
    ScenarioDocumentDto Document,
    ScenarioEditorStateDto EditorState);
