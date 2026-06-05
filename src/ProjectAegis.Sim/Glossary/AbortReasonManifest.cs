namespace ProjectAegis.Sim.Glossary;

using System.Reflection;
using System.Text.Json;

/// <summary>Loads req-12 abort reason manifest and maps enum members to stable log codes.</summary>
public sealed class AbortReasonManifest
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Dictionary<string, Dictionary<string, string>> _familyMembers = new(StringComparer.Ordinal);

    private readonly HashSet<string> _logisticsCodes = new(StringComparer.Ordinal);

    private readonly Dictionary<string, HashSet<string>> _stringCodeFamilies = new(StringComparer.Ordinal);

    public int Version { get; private init; }

    public static AbortReasonManifest LoadFromEmbeddedOrFile(string? manifestPath = null)
    {
        manifestPath ??= ResolveDefaultManifestPath();
        var json = File.ReadAllText(manifestPath);
        return Parse(json);
    }

    public static AbortReasonManifest Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<AbortReasonManifestDto>(json, Options)
            ?? throw new InvalidDataException("Invalid abort_reason_manifest.json");

        var manifest = new AbortReasonManifest { Version = dto.Version };
        foreach (var family in dto.Families ?? [])
        {
            if (family.Entries is { Count: > 0 })
            {
                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var entry in family.Entries)
                {
                    map[entry.Member] = entry.LogCode;
                }

                manifest._familyMembers[family.Name] = map;
            }

            if (family.StringCodes != null)
            {
                var codes = new HashSet<string>(family.StringCodes, StringComparer.Ordinal);
                manifest._stringCodeFamilies[family.Name] = codes;
                if (string.Equals(family.Name, "Logistics", StringComparison.Ordinal))
                {
                    foreach (var code in codes)
                    {
                        manifest._logisticsCodes.Add(code);
                    }
                }
            }
        }

        return manifest;
    }

    public bool TryGetLogCode(string family, string enumMember, out string logCode)
    {
        if (_familyMembers.TryGetValue(family, out var map) && map.TryGetValue(enumMember, out logCode!))
        {
            return true;
        }

        logCode = "";
        return false;
    }

    public string GetLogCode<TEnum>(string family, TEnum value) where TEnum : struct, Enum
    {
        if (!TryGetLogCode(family, value.ToString(), out var logCode))
        {
            throw new KeyNotFoundException($"No manifest entry for {family}.{value}");
        }

        return logCode;
    }

    public IReadOnlyCollection<string> LogisticsCodes => _logisticsCodes;

    public IReadOnlyCollection<string> GetStringCodes(string family) =>
        _stringCodeFamilies.TryGetValue(family, out var codes)
            ? codes
            : Array.Empty<string>();

    public IReadOnlyDictionary<string, string> GetFamilyMembers(string family) =>
        _familyMembers.TryGetValue(family, out var map)
            ? map
            : new Dictionary<string, string>();

    private static string ResolveDefaultManifestPath()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        for (var i = 0; i < 8 && dir != null; i++, dir = Path.GetDirectoryName(dir))
        {
            var candidate = Path.Combine(dir, "data", "glossary", "abort_reason_manifest.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var cwd = Directory.GetCurrentDirectory();
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(cwd, "data", "glossary", "abort_reason_manifest.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            cwd = Path.GetDirectoryName(cwd) ?? cwd;
        }

        throw new FileNotFoundException("abort_reason_manifest.json not found");
    }

    private sealed class AbortReasonManifestDto
    {
        public int Version { get; set; }

        public List<AbortReasonFamilyDto>? Families { get; set; }
    }

    private sealed class AbortReasonFamilyDto
    {
        public string Name { get; set; } = "";

        public string? Enum { get; set; }

        public List<AbortReasonEntryDto>? Entries { get; set; }

        public List<string>? StringCodes { get; set; }
    }

    private sealed class AbortReasonEntryDto
    {
        public string LogCode { get; set; } = "";

        public string Member { get; set; } = "";
    }
}