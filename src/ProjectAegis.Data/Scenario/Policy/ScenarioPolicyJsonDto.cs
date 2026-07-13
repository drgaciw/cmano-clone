namespace ProjectAegis.Data.Scenario.Policy;

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

    public List<ScenarioCatalogWithdrawJsonDto>? CatalogWithdraw { get; set; }

    public List<ScenarioJammerJsonDto>? Jammers { get; set; }

    public ScenarioContactLifecycleJsonDto? ContactLifecycle { get; set; }

    public ScenarioReplayJsonDto? Replay { get; set; }

    public ScenarioMissionJsonDto? Mission { get; set; }

    public ScenarioMissionPolicyJsonDto? MissionPolicy { get; set; }

    public ScenarioDelegationJsonDto? Delegation { get; set; }

    public List<ScenarioCommsJsonDto>? Comms { get; set; }

    public ScenarioLogisticsJsonDto? Logistics { get; set; }

    public ScenarioCommsDisplayJsonDto? CommsDisplay { get; set; }

    public ScenarioSpeculativeJsonDto? Speculative { get; set; }

    public Dictionary<string, ScenarioUnitReadinessJsonDto>? UnitReadiness { get; set; }

    public List<ScenarioSpoofJsonDto>? SpoofTracks { get; set; }

    public ScenarioTelemetryJsonDto? Telemetry { get; set; }

    public ScenarioDatalinkJsonDto? Datalink { get; set; }

    public ScenarioMineHazardJsonDto? MineHazard { get; set; }
}

public sealed class ScenarioMineHazardJsonDto
{
    public double ZoneMinRangeMeters { get; set; }

    public double ZoneMaxRangeMeters { get; set; }

    public double? TriggerRadiusMeters { get; set; }

    public double? HazardSeverity { get; set; }

    public List<ScenarioMinePlacementJsonDto>? Mines { get; set; }

    public List<ScenarioMineTransitJsonDto>? Transit { get; set; }
}

public sealed class ScenarioMinePlacementJsonDto
{
    public string MineId { get; set; } = "";

    public double RangeMeters { get; set; }

    public double Lethality { get; set; } = 1.0;
}

public sealed class ScenarioMineTransitJsonDto
{
    public string PlatformId { get; set; } = "u1";

    public List<double>? RangesMeters { get; set; }
}

public sealed class ScenarioDatalinkJsonDto
{
    public bool? OrganicOnly { get; set; }

    public Dictionary<string, string>? UnitSides { get; set; }

    public int? ShareLagTicks { get; set; }
}

public sealed class ScenarioTelemetryJsonDto
{
    public bool? EnableBalanceDrift { get; set; }

    public double? WinRateDriftThreshold { get; set; }

    public int? MinimumSampleRuns { get; set; }

    public double? DefaultExpectedWinRate { get; set; }

    public List<ScenarioBalanceTrialJsonDto>? BalanceTrials { get; set; }
}

public sealed class ScenarioBalanceTrialJsonDto
{
    public string EntityId { get; set; } = "";

    public string EntityKind { get; set; } = "Platform";

    public double? ExpectedWinRate { get; set; }
}

public sealed class ScenarioUnitReadinessJsonDto
{
    public bool ReadyForLaunch { get; set; } = true;
}

public sealed class ScenarioSpoofJsonDto
{
    public ulong AtTick { get; set; }

    public string ContactId { get; set; } = "";

    public string Reason { get; set; } = "spoof";
}

public sealed class ScenarioSpeculativeJsonDto
{
    public bool? BlackProjectMode { get; set; }

    public int? MaxTechnologyLevel { get; set; }
}

public sealed class ScenarioLogisticsJsonDto
{
    public double JokerSimSeconds { get; set; } = 300;

    public double BingoSimSeconds { get; set; } = 600;

    public double FuelCapacityKg { get; set; }

    public double BurnRateKgPerSecond { get; set; }

    public double JokerFuelFraction { get; set; } = 0.25;

    public double BingoFuelFraction { get; set; } = 0.10;

    public bool LogTickBurn { get; set; }
}

public sealed class ScenarioCommsDisplayJsonDto
{
    public int DegradedLagTicks { get; set; } = 2;

    public float GhostOffsetX { get; set; } = 0.06f;

    public float GhostOffsetY { get; set; } = 0.04f;

    public int DegradedOrderDelayTicks { get; set; }

    public int DegradedStaleThresholdDivisor { get; set; } = 1;
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

    public List<ScenarioMissionContactTriggerJsonDto>? Triggers { get; set; }
}

public sealed class ScenarioMissionContactTriggerJsonDto
{
    public string Id { get; set; } = "";

    public string ObserverId { get; set; } = "";

    public string TargetClass { get; set; } = "Any";

    public string Side { get; set; } = "friendly";

    public string MissionCode { get; set; } = "";

    public string Roe { get; set; } = "WeaponsFree";

    public List<string>? UnitIds { get; set; }
}

/// <summary>Mission-level doctrine override (req 13 inheritance between side and unit).</summary>
public sealed class ScenarioMissionPolicyJsonDto
{
    public string Roe { get; set; } = "WeaponsFree";

    public List<string>? UnitIds { get; set; }

    public int? MaxSalvo { get; set; }
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

public sealed class ScenarioCatalogWithdrawJsonDto
{
    public string PlatformId { get; set; } = "u1";

    public double CurrentHpPct { get; set; } = 100.0;
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

    public double EccmFactor { get; set; } = 1.0;

    public bool RequiresActiveRadar { get; set; } = true;
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

    /// <summary>WRA cap: max rounds per engagement (policy GDD).</summary>
    public int? MaxSalvo { get; set; }

    public int? WeaponTechnologyLevel { get; set; }

    public bool? WeaponRequiresBlackProject { get; set; }

    public string? DlzPersonality { get; set; }

    public string? CombatDomain { get; set; }

    public bool? MountOnline { get; set; }

    public bool? ContactIdentified { get; set; }

    /// <summary>ADR-009: enable registry validators on engage path (default false).</summary>
    public bool? CombatDomainsEnabled { get; set; }
}
