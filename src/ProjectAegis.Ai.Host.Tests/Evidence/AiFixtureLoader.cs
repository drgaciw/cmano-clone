using System.Text.Json;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

/// <summary>Loads DRG-732 ai-fixtures into a stub order log + side annotations.</summary>
internal static class AiFixtureLoader
{
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

        using var caseDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(caseDir, "case.json")));
        var caseRoot = caseDoc.RootElement;
        var caseIdentity = caseRoot.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("case.json missing id");
        var defaultViewingSide = ParseSide(GetStringOrNull(caseRoot, "defaultViewingSide") ?? "Blue");
        var debriefRevealsAll = caseRoot.TryGetProperty("debriefRevealsAll", out var debriefEl)
            && debriefEl.ValueKind == JsonValueKind.True;

        using var orderDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(caseDir, "order-log.json")));
        var orderRoot = orderDoc.RootElement;
        var fingerprintPrefix = orderRoot.GetProperty("fingerprintPrefix").GetString()
            ?? throw new InvalidOperationException("order-log.json missing fingerprintPrefix");

        using var expectedDoc = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(caseDir, "expected-evidence-ids.json")));
        var expectedRoot = expectedDoc.RootElement;
        var expectedIds = expectedRoot.GetProperty("ids").EnumerateArray()
            .Select(e => e.GetString()!)
            .ToArray();
        string[]? blueVisibleIds = null;
        if (expectedRoot.TryGetProperty("blueVisibleIds", out var blueEl)
            && blueEl.ValueKind == JsonValueKind.Array)
        {
            blueVisibleIds = blueEl.EnumerateArray().Select(e => e.GetString()!).ToArray();
        }

        IReadOnlyDictionary<string, string>? facts = null;
        var factsPath = Path.Combine(caseDir, "expected-facts.json");
        if (File.Exists(factsPath))
        {
            using var factsDoc = JsonDocument.Parse(File.ReadAllText(factsPath));
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            FlattenFacts(factsDoc.RootElement, prefix: null, map);
            facts = map;
        }

        var entries = new List<OrderLogEntry>();
        var annotations = new Dictionary<ulong, EvidenceSourceAnnotation>();
        foreach (var raw in orderRoot.GetProperty("entries").EnumerateArray()
                     .OrderBy(e => e.GetProperty("sequenceId").GetUInt64()))
        {
            var sequenceId = raw.GetProperty("sequenceId").GetUInt64();
            var kindRaw = raw.GetProperty("kind").GetString()
                ?? throw new InvalidOperationException($"Missing kind in {caseId}");
            if (!Enum.TryParse(kindRaw, ignoreCase: true, out OrderLogEntryKind kind))
            {
                throw new InvalidOperationException($"Unknown kind '{kindRaw}' in {caseId}");
            }

            var simTime = raw.GetProperty("simTime").GetDouble();
            var side = ParseSide(GetStringOrNull(raw, "side"));
            var hidden = Array.Empty<ViewingSide>();
            if (raw.TryGetProperty("hiddenFrom", out var hiddenEl)
                && hiddenEl.ValueKind == JsonValueKind.Array)
            {
                hidden = hiddenEl.EnumerateArray()
                    .Select(e => ParseSide(e.GetString()))
                    .ToArray();
            }

            annotations[sequenceId] = new EvidenceSourceAnnotation(side, hidden);

            var bag = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var prop in raw.EnumerateObject())
            {
                if (IsReservedEntryProperty(prop.Name))
                {
                    continue;
                }

                bag[prop.Name] = JsonElementToString(prop.Value);
            }

            bag["side"] = side.ToString();
            entries.Add(new OrderLogEntry(sequenceId, kind, simTime, bag));
        }

        var log = new StubReadOnlyOrderLog(entries, fingerprintPrefix);
        return new LoadedAiFixture(
            caseIdentity,
            fingerprintPrefix,
            defaultViewingSide,
            debriefRevealsAll,
            log,
            annotations,
            expectedIds,
            blueVisibleIds,
            facts);
    }

    private static bool IsReservedEntryProperty(string name) =>
        name is "sequenceId" or "kind" or "simTime" or "side" or "hiddenFrom";

    private static string JsonElementToString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.GetRawText();

    private static string? GetStringOrNull(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

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
