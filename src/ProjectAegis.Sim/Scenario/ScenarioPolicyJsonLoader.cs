namespace ProjectAegis.Sim.Scenario;

using System.Text.Json;
using ProjectAegis.Sim.Policy;

public static class ScenarioPolicyJsonLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ScenarioPolicyProfile LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(json, Options)
            ?? throw new InvalidDataException($"Invalid scenario policy JSON: {path}");
        return ToProfile(dto);
    }

    public static IReadOnlyDictionary<string, ScenarioPolicyProfile> LoadDirectory(string directoryPath)
    {
        var map = new Dictionary<string, ScenarioPolicyProfile>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(directoryPath))
        {
            return map;
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.policy.json"))
        {
            var profile = LoadFromFile(file);
            map[profile.Id] = profile;
        }

        return map;
    }

    public static ScenarioPolicyProfile ToProfile(ScenarioPolicyJsonDto dto)
    {
        var overrides = new Dictionary<string, EffectivePolicy>(StringComparer.OrdinalIgnoreCase);
        if (dto.UnitOverrides != null)
        {
            foreach (var pair in dto.UnitOverrides)
            {
                overrides[pair.Key] = new EffectivePolicy(ParseRoe(pair.Value));
            }
        }

        return new ScenarioPolicyProfile(
            new EffectivePolicy(ParseRoe(dto.FriendlyRoe)),
            new EffectivePolicy(ParseRoe(dto.OpposingRoe)),
            overrides,
            ParsePlayerInfoModel(dto.PlayerInfoModel),
            ParsePersonalityEditPolicy(dto.PersonalityEditPolicy),
            ParseEngageDefaults(dto.Engage),
            dto.AllowDualSideControl ?? false,
            ParseContactSeeds(dto.Contacts),
            ParseUnitRadarEmcon(dto.Emcon),
            ParseDetectionTrials(dto.Detection),
            ParseCatalogDetectionTargets(dto.CatalogDetection),
            ParseJammers(dto.Jammers),
            ParseContactLifecycle(dto.ContactLifecycle),
            ParseReplaySettings(dto.Replay),
            ParseMissionTimeline(dto.Mission),
            ParseDelegationSettings(dto.Delegation))
        {
            Id = dto.Id,
        };
    }

    private static ScenarioDelegationSettings ParseDelegationSettings(ScenarioDelegationJsonDto? delegation) =>
        delegation == null
            ? ScenarioDelegationSettings.Default
            : new ScenarioDelegationSettings(delegation.UsePatrolCandidates);

    private static ScenarioReplaySettings ParseReplaySettings(ScenarioReplayJsonDto? replay) =>
        replay == null
            ? ScenarioReplaySettings.Default
            : new ScenarioReplaySettings(Math.Max(1, replay.CheckpointIntervalTicks));

    private static ScenarioMissionTimeline? ParseMissionTimeline(ScenarioMissionJsonDto? mission)
    {
        if (mission?.Events == null || mission.Events.Count == 0)
        {
            return null;
        }

        var fireOrder = mission.FireOrder ?? [];
        var events = mission.Events
            .Select(e => new ScenarioMissionEvent(
                e.Id,
                e.FireAtTick,
                e.Kind,
                e.Code))
            .ToArray();
        return new ScenarioMissionTimeline(fireOrder, events);
    }

    private static ScenarioContactLifecycle ParseContactLifecycle(ScenarioContactLifecycleJsonDto? lifecycle) =>
        lifecycle == null
            ? ScenarioContactLifecycle.Default
            : new ScenarioContactLifecycle(Math.Max(1, lifecycle.StaleThresholdTicks));

    private static IReadOnlyList<ScenarioJammer> ParseJammers(List<ScenarioJammerJsonDto>? jammers)
    {
        if (jammers == null || jammers.Count == 0)
        {
            return Array.Empty<ScenarioJammer>();
        }

        return jammers
            .Select(j => new ScenarioJammer(j.TargetId, j.JamStrength, j.ActiveFromTick, j.ObserverId))
            .ToArray();
    }

    private static IReadOnlyList<ScenarioCatalogDetectionTarget> ParseCatalogDetectionTargets(
        List<ScenarioCatalogDetectionJsonDto>? catalogDetection)
    {
        if (catalogDetection == null || catalogDetection.Count == 0)
        {
            return Array.Empty<ScenarioCatalogDetectionTarget>();
        }

        return catalogDetection
            .Select(d => new ScenarioCatalogDetectionTarget(
                d.ObserverId,
                d.SensorId,
                d.TargetId,
                d.ContactId,
                d.EnvMask,
                d.JamStrength))
            .ToArray();
    }

    private static IReadOnlyList<ScenarioDetectionTrial> ParseDetectionTrials(
        List<ScenarioDetectionJsonDto>? detection)
    {
        if (detection == null || detection.Count == 0)
        {
            return Array.Empty<ScenarioDetectionTrial>();
        }

        return detection
            .Select(d => new ScenarioDetectionTrial(
                d.ObserverId,
                d.SensorId,
                d.TargetId,
                d.ContactId,
                d.BasePd,
                d.EnvMask,
                d.JamStrength))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, EmconState> ParseUnitRadarEmcon(ScenarioEmconJsonDto? emcon)
    {
        if (emcon?.Units == null || emcon.Units.Count == 0)
        {
            return new Dictionary<string, EmconState>();
        }

        var map = new Dictionary<string, EmconState>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in emcon.Units)
        {
            map[pair.Key] = ParseEmconState(pair.Value.Radar);
        }

        return map;
    }

    private static EmconState ParseEmconState(string value) =>
        Enum.TryParse<EmconState>(value, ignoreCase: true, out var state)
            ? state
            : throw new InvalidDataException($"Unknown EMCON radar value: {value}");

    private static IReadOnlyList<ScenarioContactSeed> ParseContactSeeds(List<ScenarioContactJsonDto>? contacts)
    {
        if (contacts == null || contacts.Count == 0)
        {
            return Array.Empty<ScenarioContactSeed>();
        }

        return contacts
            .Select(c => new ScenarioContactSeed(
                c.ObserverId,
                c.TargetId,
                c.ContactId,
                c.AppearAtTick,
                c.HasFireControlTrack))
            .ToArray();
    }

    private static ScenarioEngageDefaults? ParseEngageDefaults(ScenarioEngageJsonDto? engage) =>
        engage == null
            ? null
            : new ScenarioEngageDefaults(
                engage.RangeMeters,
                engage.EnvelopeMinMeters,
                engage.EnvelopeMaxMeters,
                engage.DefaultMagazineRounds,
                engage.HasFireControlTrack,
                engage.PkBase,
                engage.PkIntercept,
                engage.PkKill,
                engage.SalvoSize);

    private static RoeLevel ParseRoe(string value) =>
        Enum.TryParse<RoeLevel>(value, ignoreCase: true, out var roe)
            ? roe
            : throw new InvalidDataException($"Unknown ROE value: {value}");

    private static PlayerInfoModel ParsePlayerInfoModel(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? PlayerInfoModel.FullTransparency
            : Enum.TryParse<PlayerInfoModel>(value, ignoreCase: true, out var model)
                ? model
                : throw new InvalidDataException($"Unknown playerInfoModel value: {value}");

    private static PersonalityEditPolicy ParsePersonalityEditPolicy(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? PersonalityEditPolicy.Anytime
            : Enum.TryParse<PersonalityEditPolicy>(value, ignoreCase: true, out var policy)
                ? policy
                : throw new InvalidDataException($"Unknown personalityEditPolicy value: {value}");
}
