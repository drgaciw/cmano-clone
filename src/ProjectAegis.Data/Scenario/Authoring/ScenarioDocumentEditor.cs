namespace ProjectAegis.Data.Scenario.Authoring;

using System.Security.Cryptography;
using System.Text;

/// <summary>Mutable scenario document with optimistic concurrency (req 11 / ADR-008).</summary>
public sealed class ScenarioDocumentEditor
{
    private ScenarioDocumentEditor(ScenarioMetadataDto metadata, List<ScenarioMissionDto> missions)
    {
        Metadata = metadata;
        Missions = missions;
    }

    public ScenarioMetadataDto Metadata { get; private set; }

    public List<ScenarioMissionDto> Missions { get; }

    public static ScenarioDocumentEditor Load(string path)
    {
        var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
        return new ScenarioDocumentEditor(
            dto.Metadata,
            dto.Missions.ToList());
    }

    public static ScenarioDocumentEditor CreateNew(
        string? dbRef = "baltic_patrol",
        ulong seed = 42,
        string? policyId = "baltic-patrol-catalog")
    {
        return new ScenarioDocumentEditor(
            new ScenarioMetadataDto
            {
                DbRef = dbRef,
                Seed = seed,
                PolicyId = policyId,
                EditVersion = 1,
            },
            new List<ScenarioMissionDto>());
    }

    public ScenarioDocumentDto ToDto() =>
        new()
        {
            Metadata = Metadata,
            Missions = Missions,
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
            DbRef = Metadata.DbRef,
            DbSnapshotId = Metadata.DbSnapshotId,
            EditVersion = Metadata.EditVersion + 1,
            Seed = Metadata.Seed,
            PolicyId = Metadata.PolicyId,
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

    public void Save(string path) =>
        ScenarioDocumentJsonWriter.WriteToFile(ToDto(), path);
}