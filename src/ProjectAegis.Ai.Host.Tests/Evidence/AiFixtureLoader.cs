using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

/// <summary>Loads DRG-732 ai-fixtures into a stub order log + side annotations.</summary>
internal static class AiFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static string ResolveFixturesRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "ai-fixtures");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "manifest.json")))
            {
                return candidate;
            }

            var repoCandidate = Path.Combine(dir.FullName, "tests", "ai-fixtures");
            if (Directory.Exists(repoCandidate) && File.Exists(Path.Combine(repoCandidate, "manifest.json")))
            {
                return repoCandidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate tests/ai-fixtures (or copied ai-fixtures) from " + AppContext.BaseDirectory);
    }

    public static IReadOnlyList<string> ListCaseIds(string fixturesRoot)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixturesRoot, "manifest.json")));
        return doc.RootElement.GetProperty("cases").EnumerateArray()
            .Select(e => e.GetString()!)
            .ToArray();
    }

    public static LoadedAiFixture LoadCase(string fixturesRoot, string caseId)
    {
        var caseDir = Path.Combine(fixturesRoot, caseId);
        var caseJson = JsonSerializer.Deserialize<CaseJson>(
            File.ReadAllText(Path.Combine(caseDir, "case.json")),
            JsonOptions) ?? throw new InvalidOperationException("case.json missing");
        var orderLog = JsonSerializer.Deserialize<OrderLogJson>(
            File.ReadAllText(Path.Combine(caseDir, "order-log.json")),
            JsonOptions) ?? throw new InvalidOperationException("order-log.json missing");
        var expectedIds = JsonSerializer.Deserialize<ExpectedIdsJson>(
            File.ReadAllText(Path.Combine(caseDir, "expected-evidence-ids.json")),
            JsonOptions) ?? throw new InvalidOperationException("expected-evidence-ids.json missing");

        IReadOnlyDictionary<string, string>? facts = null;
        var factsPath = Path.Combine(caseDir, "expected-facts.json");
        if (File.Exists(factsPath))
        {
            using var factsDoc = JsonDocument.Parse(File.ReadAllText(factsPath));
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            FlattenFacts(factsDoc.RootElement, prefix: null, map);
            facts = map;
        }

        var entries = new List<OrderLogEntry>(orderLog.Entries.Count);
        var annotations = new Dictionary<ulong, EvidenceSourceAnnotation>();
        foreach (var raw in orderLog.Entries.OrderBy(e => e.SequenceId))
        {
            if (!Enum.TryParse(raw.Kind, ignoreCase: true, out OrderLogEntryKind kind))
            {
                throw new InvalidOperationException($"Unknown kind '{raw.Kind}' in {caseId}");
            }

            var side = ParseSide(raw.Side);
            var hidden = (raw.HiddenFrom ?? Array.Empty<string>())
                .Select(ParseSide)
                .ToArray();
            annotations[raw.SequenceId] = new EvidenceSourceAnnotation(side, hidden);

            var bag = new Dictionary<string, string>(StringComparer.Ordinal);
            if (raw.ExtensionData is not null)
            {
                foreach (var prop in raw.ExtensionData)
                {
                    bag[prop.Key] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False =>
                            prop.Value.GetRawText(),
                        _ => prop.Value.GetRawText(),
                    };
                }
            }

            if (!string.IsNullOrEmpty(raw.UnitId))
            {
                bag["unitId"] = raw.UnitId;
            }

            if (!string.IsNullOrEmpty(raw.ShooterUnitId))
            {
                bag["shooterUnitId"] = raw.ShooterUnitId;
            }

            if (!string.IsNullOrEmpty(raw.TargetId))
            {
                bag["targetId"] = raw.TargetId;
            }

            if (!string.IsNullOrEmpty(raw.VictimUnitId))
            {
                bag["victimUnitId"] = raw.VictimUnitId;
            }

            if (!string.IsNullOrEmpty(raw.Summary))
            {
                bag["summary"] = raw.Summary;
            }

            bag["side"] = side.ToString();
            entries.Add(new OrderLogEntry(raw.SequenceId, kind, raw.SimTime, bag));
        }

        var log = new StubReadOnlyOrderLog(entries, orderLog.FingerprintPrefix);
        var viewingSide = ParseSide(caseJson.DefaultViewingSide);
        return new LoadedAiFixture(
            caseJson.Id,
            orderLog.FingerprintPrefix,
            viewingSide,
            caseJson.DebriefRevealsAll,
            log,
            annotations,
            expectedIds.Ids,
            expectedIds.BlueVisibleIds,
            facts);
    }

    private static void FlattenFacts(JsonElement element, string? prefix, Dictionary<string, string> map)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var next = prefix is null ? prop.Name : prefix + "." + prop.Name;
                    FlattenFacts(prop.Value, next, map);
                }

                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenFacts(item, (prefix ?? "item") + "[" + i + "]", map);
                    i++;
                }

                break;
            case JsonValueKind.String:
                map[prefix ?? "value"] = element.GetString() ?? string.Empty;
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                map[prefix ?? "value"] = element.GetRawText();
                break;
            case JsonValueKind.Null:
                break;
        }
    }

    private static ViewingSide ParseSide(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ViewingSide.Unknown;
        }

        return Enum.Parse<ViewingSide>(raw, ignoreCase: true);
    }

    private sealed class CaseJson
    {
        public string Id { get; set; } = string.Empty;
        public string DefaultViewingSide { get; set; } = "Blue";
        public bool DebriefRevealsAll { get; set; }
    }

    private sealed class OrderLogJson
    {
        public string FingerprintPrefix { get; set; } = string.Empty;
        public List<OrderEntryJson> Entries { get; set; } = new();
    }

    private sealed class OrderEntryJson
    {
        public ulong SequenceId { get; set; }
        public string Kind { get; set; } = string.Empty;
        public double SimTime { get; set; }
        public string? Side { get; set; }
        public string[]? HiddenFrom { get; set; }
        public string? UnitId { get; set; }
        public string? ShooterUnitId { get; set; }
        public string? VictimUnitId { get; set; }
        public string? TargetId { get; set; }
        public string? Summary { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    private sealed class ExpectedIdsJson
    {
        public string[] Ids { get; set; } = Array.Empty<string>();
        public string[]? BlueVisibleIds { get; set; }
    }
}

internal sealed record LoadedAiFixture(
    string CaseId,
    string FingerprintPrefix,
    ViewingSide DefaultViewingSide,
    bool DebriefRevealsAll,
    StubReadOnlyOrderLog Log,
    IReadOnlyDictionary<ulong, EvidenceSourceAnnotation> Annotations,
    IReadOnlyList<string> ExpectedIds,
    IReadOnlyList<string>? BlueVisibleIds,
    IReadOnlyDictionary<string, string>? Facts);
