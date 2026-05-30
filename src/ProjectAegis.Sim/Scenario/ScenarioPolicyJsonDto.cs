namespace ProjectAegis.Sim.Scenario;

public sealed class ScenarioPolicyJsonDto
{
    public string Id { get; set; } = "";

    public string FriendlyRoe { get; set; } = "WeaponsFree";

    public string OpposingRoe { get; set; } = "WeaponsFree";

    public string? PlayerInfoModel { get; set; }

    public string? PersonalityEditPolicy { get; set; }

    public Dictionary<string, string>? UnitOverrides { get; set; }
}
