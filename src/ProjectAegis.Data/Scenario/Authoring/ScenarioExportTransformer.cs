namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Export transform: strip TeleportUnit actions with manifest log (GDD AC-11).</summary>
public static class ScenarioExportTransformer
{
    public static (ScenarioDocumentDto Exported, IReadOnlyList<ExportManifestEntry> Manifest) TransformForExport(
        ScenarioDocumentDto source)
    {
        var manifest = new List<ExportManifestEntry>();
        var events = new List<ScenarioEventDto>();

        foreach (var evt in source.Events.OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            var keptActions = new List<ScenarioEventActionDto>();
            foreach (var action in evt.Actions)
            {
                if (string.Equals(action.Type, "TeleportUnit", StringComparison.OrdinalIgnoreCase))
                {
                    manifest.Add(new ExportManifestEntry(
                        "TeleportUnitRemoved",
                        evt.Id,
                        action.UnitId ?? "",
                        $"Removed TeleportUnit from event '{evt.Id}' during export."));
                    continue;
                }

                keptActions.Add(action);
            }

            events.Add(new ScenarioEventDto
            {
                Id = evt.Id,
                Priority = evt.Priority,
                Trigger = evt.Trigger,
                Conditions = evt.Conditions,
                Actions = keptActions,
            });
        }

        var exported = CloneWithoutEvents(source, events);
        return (exported, manifest);
    }

    private static ScenarioDocumentDto CloneWithoutEvents(
        ScenarioDocumentDto source,
        IReadOnlyList<ScenarioEventDto> events) =>
        new()
        {
            Metadata = source.Metadata,
            Features = source.Features,
            Sides = source.Sides,
            Orbat = source.Orbat,
            ReferencePoints = source.ReferencePoints,
            Missions = source.Missions,
            OperationsTimeline = source.OperationsTimeline,
            Events = events,
            Variables = source.Variables,
            EditorState = source.EditorState,
        };
}

public sealed record ExportManifestEntry(
    string Transform,
    string EventId,
    string UnitId,
    string Message);
