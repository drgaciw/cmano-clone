namespace ProjectAegis.Data.Scenario.Authoring;

using System.Security.Cryptography;
using System.Text;

/// <summary>Mutable scenario document with optimistic concurrency (req 11 / ADR-008).</summary>
public sealed class ScenarioDocumentEditor
{
    private ScenarioDocumentEditor(ScenarioDocumentDto document)
    {
        Document = document;
    }

    public ScenarioMetadataDto Metadata => Document.Metadata;

    public List<ScenarioMissionDto> Missions { get; } = new();

    private ScenarioDocumentDto Document { get; set; }

    public static ScenarioDocumentEditor Load(string path)
    {
        var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
        var editor = new ScenarioDocumentEditor(dto);
        editor.Missions.AddRange(dto.Missions);
        return editor;
    }

    public static ScenarioDocumentEditor CreateNew(
        string? dbRef = "baltic_patrol",
        ulong seed = 42,
        string? policyId = "baltic-patrol-catalog")
    {
        return new ScenarioDocumentEditor(new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                Title = "Untitled Scenario",
                DbRef = dbRef,
                Seed = seed,
                PolicyId = policyId,
                EditVersion = 1,
                SchemaVersion = 1,
            },
            EditorState = null,
        });
    }

    public ScenarioDocumentDto ToDto() =>
        new()
        {
            Metadata = Document.Metadata,
            Features = Document.Features,
            Sides = Document.Sides,
            Orbat = Document.Orbat,
            ReferencePoints = Document.ReferencePoints,
            Missions = Missions.ToArray(),
            OperationsTimeline = Document.OperationsTimeline,
            Events = Document.Events,
            Variables = Document.Variables,
            EditorState = Document.EditorState,
        };

    public string ComputeFileHash()
    {
        var json = ScenarioStableJsonWriter.Serialize(ToDto());
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
        var meta = Document.Metadata;
        Document = CloneDocument(Document, new ScenarioMetadataDto
        {
            Title = meta.Title,
            Description = meta.Description,
            Author = meta.Author,
            SchemaVersion = meta.SchemaVersion,
            DbRef = meta.DbRef,
            DbSnapshotId = meta.DbSnapshotId,
            EditVersion = meta.EditVersion + 1,
            Seed = meta.Seed,
            PolicyId = meta.PolicyId,
            UnitReadiness = meta.UnitReadiness,
            MaxTechnologyLevel = meta.MaxTechnologyLevel,
            NearFutureUnits = meta.NearFutureUnits,
        });
    }

    public void SetReferencePoint(ScenarioReferencePointDto point)
    {
        var list = Document.ReferencePoints.ToList();
        var index = list.FindIndex(p => string.Equals(p.Id, point.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            list[index] = point;
        }
        else
        {
            list.Add(point);
        }

        Document = CloneDocument(Document, referencePoints: list.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray());
    }

    public void AddEvent(ScenarioEventDto evt)
    {
        if (Document.Events.Any(e => string.Equals(e.Id, evt.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Event id '{evt.Id}' already exists.");
        }

        var list = Document.Events.ToList();
        list.Add(evt);
        Document = CloneDocument(Document, events: list.OrderBy(e => e.Id, StringComparer.Ordinal).ToArray());
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

    public void AddSupportMission(
        string missionId,
        IReadOnlyList<string> assignedUnitIds,
        string supportRole,
        IReadOnlyList<ScenarioWaypointDto> stationGeometry)
    {
        if (Missions.Any(m => string.Equals(m.Id, missionId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Mission id '{missionId}' already exists.");
        }

        Missions.Add(new ScenarioMissionDto
        {
            Id = missionId,
            Type = "Support",
            AssignedUnitIds = assignedUnitIds,
            SupportRole = supportRole,
            StationGeometry = stationGeometry,
        });
    }

    public void AddFerryMission(
        string missionId,
        IReadOnlyList<string> assignedUnitIds,
        string ferryDestinationBaseId)
    {
        if (Missions.Any(m => string.Equals(m.Id, missionId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Mission id '{missionId}' already exists.");
        }

        Missions.Add(new ScenarioMissionDto
        {
            Id = missionId,
            Type = "Ferry",
            AssignedUnitIds = assignedUnitIds,
            FerryDestinationBaseId = ferryDestinationBaseId,
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
            SupportRole = mission.SupportRole,
            PatrolZone = patrolZone ?? mission.PatrolZone,
            StationGeometry = mission.StationGeometry,
            RoeOverride = mission.RoeOverride,
            EmconOverride = mission.EmconOverride,
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
            SupportRole = mission.SupportRole,
            PatrolZone = mission.PatrolZone,
            StationGeometry = mission.StationGeometry,
            RoeOverride = mission.RoeOverride,
            EmconOverride = mission.EmconOverride,
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
            SupportRole = mission.SupportRole,
            PatrolZone = mission.PatrolZone,
            StationGeometry = mission.StationGeometry,
            RoeOverride = mission.RoeOverride,
            EmconOverride = mission.EmconOverride,
        };
    }

    public void Save(string path, bool stable = true)
    {
        if (stable)
        {
            ScenarioStableJsonWriter.WriteToFile(ToDto(), path);
        }
        else
        {
            ScenarioDocumentJsonWriter.WriteToFile(ToDto(), path);
        }
    }

    public void SavePackage(string packagePath, bool stable = true)
    {
        var dto = ToDto();
        if (stable)
        {
            AegisScenarioPackage.Write(packagePath, dto);
        }
        else
        {
            AegisScenarioPackage.Write(packagePath, dto);
        }
    }

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

    private static ScenarioDocumentDto CloneDocument(
        ScenarioDocumentDto source,
        ScenarioMetadataDto? metadata = null,
        IReadOnlyList<ScenarioReferencePointDto>? referencePoints = null,
        IReadOnlyList<ScenarioEventDto>? events = null) =>
        new()
        {
            Metadata = metadata ?? source.Metadata,
            Features = source.Features,
            Sides = source.Sides,
            Orbat = source.Orbat,
            ReferencePoints = referencePoints ?? source.ReferencePoints,
            Missions = source.Missions,
            OperationsTimeline = source.OperationsTimeline,
            Events = events ?? source.Events,
            Variables = source.Variables,
            EditorState = source.EditorState,
        };
}
