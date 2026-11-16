using System.Text.Json;
using ProjectAegis.Ai.Host.Authoring;

namespace ProjectAegis.Ai.Host.Tests.Authoring;

/// <summary>Loads DRG-763 authoring fixtures from the test output ai-fixtures/authoring tree.</summary>
internal static class AuthoringFixtureLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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

    public static (AuthoringProposeRequest Request, AuthoringModelDraft Draft, ExpectedDto Expected) Load(string caseId)
    {
        var dir = Path.Combine(FixtureRoot, caseId);
        var caseJson = JsonSerializer.Deserialize<CaseDto>(
            File.ReadAllText(Path.Combine(dir, "case.json")), Options)
            ?? throw new InvalidOperationException("case.json null");
        var requestJson = JsonSerializer.Deserialize<RequestDto>(
            File.ReadAllText(Path.Combine(dir, "request.json")), Options)
            ?? throw new InvalidOperationException("request.json null");
        var expected = JsonSerializer.Deserialize<ExpectedDto>(
            File.ReadAllText(Path.Combine(dir, "expected.json")), Options)
            ?? throw new InvalidOperationException("expected.json null");

        var draft = AuthoringModelDraftParser.Parse(
            File.ReadAllText(Path.Combine(dir, "provider-script.json")));

        IReadOnlyDictionary<string, IReadOnlyList<string>>? caps = null;
        if (caseJson.OpenScenario?.UnitCapabilities is not null)
        {
            caps = caseJson.OpenScenario.UnitCapabilities
                .ToDictionary(
                    kv => kv.Key,
                    kv => (IReadOnlyList<string>)kv.Value,
                    StringComparer.OrdinalIgnoreCase);
        }

        IReadOnlyList<string> allowedUnits = caseJson.OpenScenario?.AllowedUnitIds is { Count: > 0 } u
            ? u
            : Array.Empty<string>();
        IReadOnlyList<string> allowedTargets = caseJson.OpenScenario?.AllowedTargetIds is { Count: > 0 } t
            ? t
            : Array.Empty<string>();
        IReadOnlyList<string> missionTypes = caseJson.OpenScenario?.SupportedMissionTypes is { Count: > 0 } m
            ? m
            : Array.Empty<string>();

        var request = new AuthoringProposeRequest(
            Brief: requestJson.Brief ?? string.Empty,
            IdempotencyKey: requestJson.IdempotencyKey ?? caseId,
            ExpectedEditVersion: requestJson.ExpectedEditVersion,
            CurrentEditVersion: caseJson.OpenScenario?.CurrentEditVersion ?? 0,
            AllowedUnitIds: allowedUnits,
            AllowedTargetIds: allowedTargets,
            SupportedMissionTypes: missionTypes,
            DbRef: caseJson.OpenScenario?.DbRef ?? "baltic_patrol",
            UnitCapabilities: caps,
            PriorAcceptedIdempotencyKeys: caseJson.OpenScenario?.PriorAcceptedIdempotencyKeys,
            IsRetry: requestJson.IsRetry);

        return (request, draft, expected);
    }

    internal sealed class ExpectedDto
    {
        public string? OutcomeClass { get; set; }

        public List<string>? Missions { get; set; }

        public List<DiagDto>? Diagnostics { get; set; }

        public string? ConflictCode { get; set; }

        public string? ProvenanceSource { get; set; }

        public bool SubmitAllowedWithoutHuman { get; set; }

        public List<string>? MustNotContain { get; set; }

        public int? MissionCountDelta { get; set; }
    }

    internal sealed class DiagDto
    {
        public string? Code { get; set; }

        public string? MessageContains { get; set; }
    }

    private sealed class CaseDto
    {
        public OpenScenarioDto? OpenScenario { get; set; }
    }

    private sealed class OpenScenarioDto
    {
        public string? DbRef { get; set; }

        public List<string>? AllowedUnitIds { get; set; }

        public List<string>? AllowedTargetIds { get; set; }

        public List<string>? SupportedMissionTypes { get; set; }

        public int CurrentEditVersion { get; set; }

        public Dictionary<string, List<string>>? UnitCapabilities { get; set; }

        public List<string>? PriorAcceptedIdempotencyKeys { get; set; }
    }

    private sealed class RequestDto
    {
        public string? Brief { get; set; }

        public int ExpectedEditVersion { get; set; }

        public string? IdempotencyKey { get; set; }

        public bool IsRetry { get; set; }
    }
}
