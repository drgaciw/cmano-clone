using System.Text.Json;
using ProjectAegis.Ai.Host.Authoring;

namespace ProjectAegis.Ai.Host.Tests.Authoring;

/// <summary>Loads DRG-763 authoring fixtures from the test output ai-fixtures/authoring tree.</summary>
internal static class AuthoringFixtureLoader
{
    public static string FixtureRoot
    {
        get
        {
            var root = Path.Combine(AppContext.BaseDirectory, "ai-fixtures", "authoring");
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException($"Authoring fixtures not found at '{root}'.");
            }

            return root;
        }
    }

    public static (AuthoringProposeRequest Request, AuthoringModelDraft Draft, AuthoringExpected Expected) Load(
        string caseId)
    {
        var dir = Path.Combine(FixtureRoot, caseId);

        using var caseDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "case.json")));
        var caseRoot = caseDoc.RootElement;
        var open = caseRoot.GetProperty("openScenario");

        using var requestDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "request.json")));
        var reqRoot = requestDoc.RootElement;

        using var expectedDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "expected.json")));
        var expected = ReadExpected(expectedDoc.RootElement);

        var draft = AuthoringModelDraftParser.Parse(
            File.ReadAllText(Path.Combine(dir, "provider-script.json")));

        IReadOnlyDictionary<string, IReadOnlyList<string>>? caps = null;
        if (open.TryGetProperty("unitCapabilities", out var capsEl)
            && capsEl.ValueKind == JsonValueKind.Object)
        {
            var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in capsEl.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                map[prop.Name] = prop.Value.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? string.Empty)
                    .ToArray();
            }

            caps = map;
        }

        IReadOnlyList<string>? priorKeys = null;
        if (open.TryGetProperty("priorAcceptedIdempotencyKeys", out var priorEl)
            && priorEl.ValueKind == JsonValueKind.Array)
        {
            priorKeys = priorEl.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString() ?? string.Empty)
                .ToArray();
        }

        var request = new AuthoringProposeRequest(
            Brief: GetString(reqRoot, "brief") ?? string.Empty,
            IdempotencyKey: GetString(reqRoot, "idempotencyKey") ?? caseId,
            ExpectedEditVersion: GetInt(reqRoot, "expectedEditVersion"),
            CurrentEditVersion: GetInt(open, "currentEditVersion"),
            AllowedUnitIds: ReadStringArray(open, "allowedUnitIds") ?? Array.Empty<string>(),
            AllowedTargetIds: ReadStringArray(open, "allowedTargetIds") ?? Array.Empty<string>(),
            SupportedMissionTypes: ReadStringArray(open, "supportedMissionTypes") ?? Array.Empty<string>(),
            DbRef: GetString(open, "dbRef") ?? "baltic_patrol",
            UnitCapabilities: caps,
            PriorAcceptedIdempotencyKeys: priorKeys,
            IsRetry: reqRoot.TryGetProperty("isRetry", out var retryEl)
                     && retryEl.ValueKind == JsonValueKind.True);

        return (request, draft, expected);
    }

    private static AuthoringExpected ReadExpected(JsonElement root)
    {
        var diagnostics = new List<AuthoringExpectedDiagnostic>();
        if (root.TryGetProperty("diagnostics", out var diagEl)
            && diagEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in diagEl.EnumerateArray())
            {
                diagnostics.Add(new AuthoringExpectedDiagnostic(
                    GetString(d, "code") ?? string.Empty,
                    GetString(d, "messageContains")));
            }
        }

        int? missionDelta = null;
        if (root.TryGetProperty("missionCountDelta", out var deltaEl)
            && deltaEl.ValueKind == JsonValueKind.Number)
        {
            missionDelta = deltaEl.GetInt32();
        }

        return new AuthoringExpected(
            OutcomeClass: GetString(root, "outcomeClass") ?? string.Empty,
            Missions: ReadStringArray(root, "missions"),
            Diagnostics: diagnostics,
            ConflictCode: GetString(root, "conflictCode"),
            ProvenanceSource: GetString(root, "provenanceSource"),
            SubmitAllowedWithoutHuman: root.TryGetProperty("submitAllowedWithoutHuman", out var suh)
                                       && suh.ValueKind == JsonValueKind.True,
            MustNotContain: ReadStringArray(root, "mustNotContain"),
            MissionCountDelta: missionDelta);
    }

    private static IReadOnlyList<string>? ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return el.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? string.Empty)
            .ToArray();
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static int GetInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetInt32()
            : 0;

    internal sealed record AuthoringExpected(
        string OutcomeClass,
        IReadOnlyList<string>? Missions,
        IReadOnlyList<AuthoringExpectedDiagnostic> Diagnostics,
        string? ConflictCode,
        string? ProvenanceSource,
        bool SubmitAllowedWithoutHuman,
        IReadOnlyList<string>? MustNotContain,
        int? MissionCountDelta);

    internal sealed record AuthoringExpectedDiagnostic(string Code, string? MessageContains);
}
