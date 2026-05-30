namespace ProjectAegis.Sim.Scenario;

public sealed class ScenarioPolicyJsonDto
{
    public string Id { get; set; } = "";

    public string FriendlyRoe { get; set; } = "WeaponsFree";

    public string OpposingRoe { get; set; } = "WeaponsFree";

    public Dictionary<string, string>? UnitOverrides { get; set; }
}
