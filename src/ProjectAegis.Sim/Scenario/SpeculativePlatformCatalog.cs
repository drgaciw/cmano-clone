namespace ProjectAegis.Sim.Scenario;

using System.Text.Json;
using ProjectAegis.Sim.Doctrine;

/// <summary>Loads data/catalog/speculative_platforms.json for TL/black-project metadata.</summary>
public sealed class SpeculativePlatformCatalog
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Dictionary<string, SpeculativePlatformEntry> _byId;

    public SpeculativePlatformCatalog(IReadOnlyDictionary<string, SpeculativePlatformEntry> byId) =>
        _byId = new Dictionary<string, SpeculativePlatformEntry>(byId, StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string platformId, out SpeculativePlatformEntry entry) =>
        _byId.TryGetValue(platformId, out entry!);

    public static SpeculativePlatformCatalog LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<SpeculativePlatformCatalogDto>(json, Options)
            ?? throw new InvalidDataException($"Invalid speculative catalog: {path}");

        var map = new Dictionary<string, SpeculativePlatformEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var platform in dto.Platforms ?? [])
        {
            map[platform.PlatformId] = new SpeculativePlatformEntry(
                platform.PlatformId,
                platform.GameTechnologyLevel,
                platform.RequiresBlackProject,
                ParseMaturity(platform.TechnologyMaturity));
        }

        return new SpeculativePlatformCatalog(map);
    }

    private static TechnologyMaturityTag ParseMaturity(string? value) =>
        Enum.TryParse<TechnologyMaturityTag>(value, ignoreCase: true, out var tag)
            ? tag
            : TechnologyMaturityTag.Simulated;

    private sealed class SpeculativePlatformCatalogDto
    {
        public List<SpeculativePlatformJsonDto>? Platforms { get; set; }
    }

    private sealed class SpeculativePlatformJsonDto
    {
        public string PlatformId { get; set; } = "";

        public int GameTechnologyLevel { get; set; }

        public bool RequiresBlackProject { get; set; }

        public string? TechnologyMaturity { get; set; }
    }
}

public readonly record struct SpeculativePlatformEntry(
    string PlatformId,
    int GameTechnologyLevel,
    bool RequiresBlackProject,
    TechnologyMaturityTag TechnologyMaturity);