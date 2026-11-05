namespace ProjectAegis.Data.Scenario.Authoring;

using System.Security.Cryptography;
using System.Text;

/// <summary>Mutable scenario document with optimistic concurrency (req 11 / ADR-008).</summary>
public sealed class ScenarioDocumentEditor
{
    private ScenarioDocumentEditor(ScenarioDocumentDto snapshot, List<ScenarioMissionDto> missions)
    {
        Metadata = snapshot.Metadata;
        Features = snapshot.Features;
        Sides = snapshot.Sides;
        Orbat = snapshot.Orbat;
        ReferencePoints = snapshot.ReferencePoints;
        OperationsTimeline = snapshot.OperationsTimeline;
        Events = snapshot.Events;
        Variables = snapshot.Variables;
        EditorState = snapshot.EditorState;
        Missions = missions;
    }

    public ScenarioMetadataDto Metadata { get; private set; }

    public ScenarioFeaturesDto? Features { get; private set; }

    public IReadOnlyList<ScenarioSideDto> Sides { get; private set; }

    public ScenarioOrbatDto? Orbat { get; private set; }

    public IReadOnlyList<ScenarioReferencePointDto> ReferencePoints { get; private set; }

    public IReadOnlyList<ScenarioOperationsTimelineEntryDto> OperationsTimeline { get; private set; }

    public IReadOnlyList<ScenarioEventDto> Events { get; private set; }

    public IReadOnlyDictionary<string, System.Text.Json.JsonElement>? Variables { get; private set; }

    public ScenarioEditorStateDto? EditorState { get; private set; }

    public List<ScenarioMissionDto> Missions { get; }

    public static ScenarioDocumentEditor Load(string path)
    {
        var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
        return new ScenarioDocumentEditor(dto, dto.Missions.ToList());
    }

    public static ScenarioDocumentEditor CreateNew(
        string? dbRef = "baltic_patrol",
        ulong seed = 42,
        string? policyId = "baltic-patrol-catalog",
        string title = "Untitled Scenario")
    {
        return new ScenarioDocumentEditor(
            new ScenarioDocumentDto
            {
                Metadata = new ScenarioMetadataDto
                {
                    Title = title,
                    DbRef = dbRef,
                    Seed = seed,
                    PolicyId = policyId,
                    EditVersion = 1,
                },
            },
            new List<ScenarioMissionDto>());
    }

    public ScenarioDocumentDto ToDto() =>
        new()
        {
            Metadata = Metadata,
            Features = Features,
            Sides = Sides,
            Orbat = Orbat,
            ReferencePoints = ReferencePoints,
            Missions = Missions,
            OperationsTimeline = OperationsTimeline,
            Events = Events,
            Variables = Variables,
            EditorState = EditorState,
        };

    public string ComputeFileHash()
    {
        var json = ScenarioDocumentJsonWriter.Serialize(ToDto());
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
        return BitConverter.ToString(bytes).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
    }

    public void RequireEditVersion(int expectedEditVersion, string? path = null)
    {
        var conflict = ScenarioEditVersionGuard.TryCheck(
            expectedEditVersion,
            Metadata.EditVersion,
            ComputeFileHash());
        if (conflict != null)
        {
            throw conflict;
        }
    }

    public void CommitMutation()
    {
        Metadata = new ScenarioMetadataDto
        {
            Title = Metadata.Title,
            Description = Metadata.Description,
            Author = Metadata.Author,
            SchemaVersion = Metadata.SchemaVersion,
            DbRef = Metadata.DbRef,
            DbSnapshotId = Metadata.DbSnapshotId,
            EditVersion = Metadata.EditVersion + 1,
            Seed = Metadata.Seed,
            PolicyId = Metadata.PolicyId,
            UnitReadiness = Metadata.UnitReadiness,
            MaxTechnologyLevel = Metadata.MaxTechnologyLevel,
            NearFutureUnits = Metadata.NearFutureUnits,
        };
    }

    public void AddPatrolMission(
        string missionId,
        IReadOnlyList<string> assignedUnitIds,
        IReadOnlyList<ScenarioWaypointDto> patrolZone)
    {
        if (Missions.Any(m => string.Equals(m.Id, missionId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Mission id '{missionId}' already exists.");
        }

        Missions.Add(new ScenarioMissionDto
        {
            Id = missionId,
            Type = "Patrol",
            AssignedUnitIds = assignedUnitIds,
            PatrolZone = patrolZone,
        });
    }

    public void AddStrikeMission(
        string missionId,
        IReadOnlyList<string> assignedUnitIds,
        IReadOnlyList<string> targetIds)
    {
        if (Missions.Any(m => string.Equals(m.Id, missionId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Mission id '{missionId}' already exists.");
        }

        Missions.Add(new ScenarioMissionDto
        {
            Id = missionId,
            Type = "Strike",
            AssignedUnitIds = assignedUnitIds,
            TargetIds = targetIds,
        });
    }

    public bool TryRemoveMission(string missionId)
    {
        var index = Missions.FindIndex(m =>
            string.Equals(m.Id, missionId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        Missions.RemoveAt(index);
        return true;
    }

    public void UpdatePatrolMission(
        string missionId,
        IReadOnlyList<string>? assignedUnitIds = null,
        IReadOnlyList<ScenarioWaypointDto>? patrolZone = null)
    {
        var mission = RequireMission(missionId, "Patrol");
        var index = Missions.IndexOf(mission);
        Missions[index] = new ScenarioMissionDto
        {
            Id = mission.Id,
            Type = mission.Type,
            AssignedUnitIds = assignedUnitIds ?? mission.AssignedUnitIds,
            TargetIds = mission.TargetIds,
            FerryDestinationBaseId = mission.FerryDestinationBaseId,
            PatrolZone = patrolZone ?? mission.PatrolZone,
        };
    }

    public void UpdateStrikeMission(
        string missionId,
        IReadOnlyList<string>? assignedUnitIds = null,
        IReadOnlyList<string>? targetIds = null)
    {
        var mission = RequireMission(missionId, "Strike");
        var index = Missions.IndexOf(mission);
        Missions[index] = new ScenarioMissionDto
        {
            Id = mission.Id,
            Type = mission.Type,
            AssignedUnitIds = assignedUnitIds ?? mission.AssignedUnitIds,
            TargetIds = targetIds ?? mission.TargetIds,
            FerryDestinationBaseId = mission.FerryDestinationBaseId,
            PatrolZone = mission.PatrolZone,
        };
    }

    public void UpdateFerryMission(
        string missionId,
        IReadOnlyList<string>? assignedUnitIds = null,
        string? ferryDestinationBaseId = null)
    {
        var mission = RequireMission(missionId, "Ferry");
        var index = Missions.IndexOf(mission);
        Missions[index] = new ScenarioMissionDto
        {
            Id = mission.Id,
            Type = mission.Type,
            AssignedUnitIds = assignedUnitIds ?? mission.AssignedUnitIds,
            TargetIds = mission.TargetIds,
            FerryDestinationBaseId = ferryDestinationBaseId ?? mission.FerryDestinationBaseId,
            PatrolZone = mission.PatrolZone,
        };
    }

    public void Save(string path) =>
        ScenarioDocumentJsonWriter.WriteToFile(ToDto(), path);

    private ScenarioMissionDto RequireMission(string missionId, string expectedType)
    {
        var mission = Missions.FirstOrDefault(m =>
            string.Equals(m.Id, missionId, StringComparison.OrdinalIgnoreCase));
        if (mission == null)
        {
            throw new InvalidOperationException($"Mission id '{missionId}' was not found.");
        }

        if (!string.Equals(mission.Type, expectedType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Mission '{missionId}' is type '{mission.Type}', expected '{expectedType}'.");
        }

        return mission;
    }
}