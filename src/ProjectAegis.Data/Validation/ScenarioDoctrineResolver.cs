namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Doctrine inheritance resolution at validation time (GDD AC-4).</summary>
public static class ScenarioDoctrineResolver
{
    public static ResolvedMissionDoctrine ResolveMissionDoctrine(
        ScenarioDocumentDto scenario,
        ScenarioMissionDto mission)
    {
        var side = scenario.Sides.FirstOrDefault(s =>
            mission.AssignedUnitIds.Count > 0 &&
            scenario.Orbat?.Units.Any(u =>
                string.Equals(u.Id, mission.AssignedUnitIds[0], StringComparison.OrdinalIgnoreCase) &&
                string.Equals(u.SideId, s.Id, StringComparison.OrdinalIgnoreCase)) == true);

        var sideRoe = side?.DefaultRoe ?? "WeaponsFree";
        var sideEmcon = side?.DefaultEmcon ?? "Normal";

        var roe = !string.IsNullOrWhiteSpace(mission.RoeOverride) ? mission.RoeOverride! : sideRoe;
        var emcon = !string.IsNullOrWhiteSpace(mission.EmconOverride) ? mission.EmconOverride! : sideEmcon;

        return new ResolvedMissionDoctrine(mission.Id, roe, emcon, HasRoeOverride: !string.IsNullOrWhiteSpace(mission.RoeOverride));
    }
}

public sealed record ResolvedMissionDoctrine(
    string MissionId,
    string Roe,
    string Emcon,
    bool HasRoeOverride);
