namespace ProjectAegis.Sim.Scenario;

public sealed class ScenarioPolicyJsonDto
{
    public string Id { get; set; } = "";

    public string FriendlyRoe { get; set; } = "WeaponsFree";

    public string OpposingRoe { get; set; } = "WeaponsFree";

    public string? PlayerInfoModel { get; set; }

    public string? PersonalityEditPolicy { get; set; }

    public bool? AllowDualSideControl { get; set; }

    public Dictionary<string, string>? UnitOverrides { get; set; }

    public ScenarioEngageJsonDto? Engage { get; set; }
}

public sealed class ScenarioEngageJsonDto
{
    public double RangeMeters { get; set; } = 50_000;

    public double EnvelopeMinMeters { get; set; } = 1_000;

    public double EnvelopeMaxMeters { get; set; } = 100_000;

    public int DefaultMagazineRounds { get; set; } = 2;

    public bool HasFireControlTrack { get; set; } = true;
}
