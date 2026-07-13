namespace ProjectAegis.Sim.Scenario;

/// <summary>ADR-009 bounded mine transit hazard zone + seeded placement (S32-08).</summary>
public sealed class ScenarioMineHazardSettings
{
    public ScenarioMineHazardSettings(
        double zoneMinRangeMeters,
        double zoneMaxRangeMeters,
        double triggerRadiusMeters,
        double hazardSeverity,
        IReadOnlyList<ScenarioMinePlacement> mines,
        IReadOnlyList<ScenarioMineTransitSchedule> transit)
    {
        ZoneMinRangeMeters = zoneMinRangeMeters;
        ZoneMaxRangeMeters = zoneMaxRangeMeters;
        TriggerRadiusMeters = triggerRadiusMeters;
        HazardSeverity = Math.Clamp(hazardSeverity, 0.0, 1.0);
        Mines = mines ?? Array.Empty<ScenarioMinePlacement>();
        Transit = transit ?? Array.Empty<ScenarioMineTransitSchedule>();
    }

    public double ZoneMinRangeMeters { get; }

    public double ZoneMaxRangeMeters { get; }

    public double TriggerRadiusMeters { get; }

    public double HazardSeverity { get; }

    public IReadOnlyList<ScenarioMinePlacement> Mines { get; }

    public IReadOnlyList<ScenarioMineTransitSchedule> Transit { get; }

    public bool HasTransit => Transit.Count > 0 && Mines.Count > 0;

    public bool IsRangeInsideZone(double rangeMeters) =>
        rangeMeters >= ZoneMinRangeMeters && rangeMeters <= ZoneMaxRangeMeters;
}

public sealed record ScenarioMinePlacement(
    string MineId,
    double RangeMeters,
    double Lethality);

public sealed record ScenarioMineTransitSchedule(
    string PlatformId,
    IReadOnlyList<double> RangesMeters);