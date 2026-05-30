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
            overrides)
        {
            Id = dto.Id,
        };
    }

    private static RoeLevel ParseRoe(string value) =>
        Enum.TryParse<RoeLevel>(value, ignoreCase: true, out var roe)
            ? roe
            : throw new InvalidDataException($"Unknown ROE value: {value}");
}
