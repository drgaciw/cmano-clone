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

    public List<ScenarioContactJsonDto>? Contacts { get; set; }

    public ScenarioEmconJsonDto? Emcon { get; set; }

    public List<ScenarioDetectionJsonDto>? Detection { get; set; }

    public List<ScenarioJammerJsonDto>? Jammers { get; set; }

    public ScenarioContactLifecycleJsonDto? ContactLifecycle { get; set; }
}

public sealed class ScenarioContactLifecycleJsonDto
{
    public int StaleThresholdTicks { get; set; } = 30;
}

public sealed class ScenarioJammerJsonDto
{
    public string TargetId { get; set; } = "hostile-1";

    public double JamStrength { get; set; }

    public ulong ActiveFromTick { get; set; }

    public string? ObserverId { get; set; }
}

public sealed class ScenarioDetectionJsonDto
{
    public string ObserverId { get; set; } = "u1";

    public string SensorId { get; set; } = "radar-1";

    public string TargetId { get; set; } = "hostile-1";

    public string ContactId { get; set; } = "c1";

    public double BasePd { get; set; } = 1.0;

    public double EnvMask { get; set; } = 1.0;

    public double JamStrength { get; set; }
}

public sealed class ScenarioEmconJsonDto
{
    public Dictionary<string, ScenarioUnitEmconJsonDto>? Units { get; set; }
}

public sealed class ScenarioUnitEmconJsonDto
{
    public string Radar { get; set; } = "Active";
}

public sealed class ScenarioContactJsonDto
{
    public string ObserverId { get; set; } = "u1";

    public string TargetId { get; set; } = "hostile-1";

    public string ContactId { get; set; } = "c1";

    public ulong AppearAtTick { get; set; }

    public bool HasFireControlTrack { get; set; } = true;
}

public sealed class ScenarioEngageJsonDto
{
    public double RangeMeters { get; set; } = 50_000;

    public double EnvelopeMinMeters { get; set; } = 1_000;

    public double EnvelopeMaxMeters { get; set; } = 100_000;

    public int DefaultMagazineRounds { get; set; } = 2;

    public bool HasFireControlTrack { get; set; } = true;
}
