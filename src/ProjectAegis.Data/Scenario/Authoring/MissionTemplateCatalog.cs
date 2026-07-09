namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Built-in mission template descriptor for the Mission Board wizard (AME-3.4).</summary>
public sealed class MissionTemplateSpec
{
    /// <summary>Stable template id (e.g. <c>tpl-patrol-empty</c>).</summary>
    public string TemplateId { get; init; } = "";

    /// <summary>Mission type produced: Patrol|Strike|Ferry|Support.</summary>
    public string Type { get; init; } = "";

    /// <summary>Human-readable template name.</summary>
    public string DisplayName { get; init; } = "";
}

/// <summary>Built-in mission templates for Mission Board wizard (AME-3.4).</summary>
public static class MissionTemplateCatalog
{
    private static readonly ScenarioWaypointDto[] BalticBox =
    [
        new() { Lat = 57.0, Lon = 20.0 },
        new() { Lat = 57.1, Lon = 20.1 },
        new() { Lat = 57.2, Lon = 20.0 },
    ];

    /// <summary>All built-in templates (fixed ids for tests and CLI).</summary>
    public static IReadOnlyList<MissionTemplateSpec> All { get; } =
    [
        new() { TemplateId = "tpl-patrol-empty", Type = "Patrol", DisplayName = "Empty Patrol" },
        new() { TemplateId = "tpl-strike-empty", Type = "Strike", DisplayName = "Empty Strike" },
        new() { TemplateId = "tpl-ferry-empty", Type = "Ferry", DisplayName = "Empty Ferry" },
        new() { TemplateId = "tpl-support-tanker", Type = "Support", DisplayName = "Tanker Support" },
    ];

    /// <summary>Materializes a template into a mission DTO under <paramref name="newMissionId"/>.</summary>
    public static ScenarioMissionDto Materialize(string templateId, string newMissionId)
    {
        if (string.IsNullOrWhiteSpace(newMissionId))
        {
            throw new InvalidOperationException("Mission id is required.");
        }

        return templateId switch
        {
            "tpl-patrol-empty" => new ScenarioMissionDto
            {
                Id = newMissionId,
                Type = "Patrol",
                AssignedUnitIds = Array.Empty<string>(),
                PatrolZone = BalticBox.ToArray(),
            },
            "tpl-strike-empty" => new ScenarioMissionDto
            {
                Id = newMissionId,
                Type = "Strike",
                AssignedUnitIds = Array.Empty<string>(),
                TargetIds = Array.Empty<string>(),
            },
            "tpl-ferry-empty" => new ScenarioMissionDto
            {
                Id = newMissionId,
                Type = "Ferry",
                AssignedUnitIds = Array.Empty<string>(),
                FerryDestinationBaseId = "base-1",
            },
            "tpl-support-tanker" => new ScenarioMissionDto
            {
                Id = newMissionId,
                Type = "Support",
                SupportRole = "Tanker",
                AssignedUnitIds = Array.Empty<string>(),
                PatrolZone = BalticBox.ToArray(),
                StationGeometry = BalticBox.ToArray(),
            },
            _ => throw new InvalidOperationException($"Unknown template id '{templateId}'."),
        };
    }
}
