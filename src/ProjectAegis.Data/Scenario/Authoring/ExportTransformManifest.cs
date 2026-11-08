namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>AC-11 export-transform manifest entries detached from transform class for stable blast-radius analysis.</summary>
public sealed record ExportTransformRemovalEntry(
    string EventId,
    int ActionIndex,
    string ActionType,
    string Message);

public sealed record ExportTransformResult(
    ScenarioDocumentDto ExportedDocument,
    IReadOnlyList<ExportTransformRemovalEntry> ManifestEntries);