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

    public List<ScenarioCatalogDetectionJsonDto>? CatalogDetection { get; set; }

    public List<ScenarioJammerJsonDto>? Jammers { get; set; }

    public ScenarioContactLifecycleJsonDto? ContactLifecycle { get; set; }

    public ScenarioReplayJsonDto? Replay { get; set; }

    public ScenarioMissionJsonDto? Mission { get; set; }

    public ScenarioDelegationJsonDto? Delegation { get; set; }

    public List<ScenarioCommsJsonDto>? Comms { get; set; }

    public ScenarioLogisticsJsonDto? Logistics { get; set; }

    public ScenarioCommsDisplayJsonDto? CommsDisplay { get; set; }
}

public sealed class ScenarioLogisticsJsonDto
{
    public double JokerSimSeconds { get; set; } = 300;

    public double BingoSimSeconds { get; set; } = 600;
}

public sealed class ScenarioCommsDisplayJsonDto
{
    public int DegradedLagTicks { get; set; } = 2;

    public float GhostOffsetX { get; set; } = 0.06f;

    public float GhostOffsetY { get; set; } = 0.04f;
}

public sealed class ScenarioCommsJsonDto
{
    public ulong AtTick { get; set; }

    public string NewState { get; set; } = "Nominal";

    public string NodeId { get; set; } = "c2-net";

    public string Reason { get; set; } = "";
}

public sealed class ScenarioDelegationJsonDto
{
    public bool UsePatrolCandidates { get; set; }
}

public sealed class ScenarioReplayJsonDto
{
    public int CheckpointIntervalTicks { get; set; } = 300;
}

public sealed class ScenarioMissionJsonDto
{
    public List<string> FireOrder { get; set; } = [];

    public List<ScenarioMissionEventJsonDto> Events { get; set; } = [];
}

public sealed class ScenarioMissionEventJsonDto
{
    public string Id { get; set; } = "";

    public ulong FireAtTick { get; set; }

    public string Kind { get; set; } = "MissionTransition";

    public string Code { get; set; } = "";
}

public sealed class ScenarioContactLifecycleJsonDto
{
    public int StaleThresholdTicks { get; set; } = 30;

    /// <summary>Ticks since first detection before Classified (0 = disabled).</summary>
    public int ClassifyAfterTicks { get; set; }

    /// <summary>Ticks since first detection before Identified (0 = disabled).</summary>
    public int IdentifyAfterTicks { get; set; }
}

public sealed class ScenarioJammerJsonDto
{
    public string TargetId { get; set; } = "hostile-1";

    public double JamStrength { get; set; }

    public ulong ActiveFromTick { get; set; }

    public string? ObserverId { get; set; }
}

public sealed class ScenarioCatalogDetectionJsonDto
{
    public string ObserverId { get; set; } = "u1";

    public string SensorId { get; set; } = "radar-1";

    public string TargetId { get; set; } = "hostile-1";

    public string ContactId { get; set; } = "c1";

    public double EnvMask { get; set; } = 1.0;

    public double JamStrength { get; set; }
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

    public double PkBase { get; set; } = 0.85;

    public double PkIntercept { get; set; } = 0.0;

    public double PkKill { get; set; } = 1.0;

    public int SalvoSize { get; set; } = 1;
}
