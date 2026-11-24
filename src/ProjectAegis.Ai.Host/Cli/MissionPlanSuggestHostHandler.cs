using System.Text.Json;
using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Authoring;
using ProjectAegis.Data;
using ProjectAegis.Data.Scenario.Authoring;

namespace ProjectAegis.Ai.Host.Cli;

/// <summary>
/// Out-of-process handler for <c>mission_plan_suggest --provider fake</c> (DRG-747 / DRG-772).
/// Fake/scripted clients only — yields a validated draft and op diff; never applies.
/// </summary>
public static class MissionPlanSuggestHostHandler
{
    private static readonly JsonSerializerOptions CamelOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>Executes <c>mission_plan_suggest</c> in fake-provider mode and returns the IPC response frame.</summary>
    public static AiHostIpcResponse Execute(AiHostMissionPlanSuggestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Intent))
        {
            return Error("MISSING_INTENT", "mission_plan_suggest --provider requires --intent <text>");
        }

        AuthoringProposeRequest proposeRequest;
        AuthoringModelDraft draft;
        string scriptSource;
        try
        {
            (proposeRequest, draft, scriptSource) = LoadDraftAndRequest(request);
        }
        catch (FileNotFoundException ex)
        {
            return Error("NOT_FOUND", ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            return Error("NOT_FOUND", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Error("INVALID_SCRIPT", ex.Message);
        }
        catch (JsonException ex)
        {
            return Error("INVALID_SCRIPT", ex.Message);
        }

        // Touch FakeChatClient so the path is explicitly offline / non-paid (no live keys).
        using (var fake = new FakeChatClient())
        {
            fake.Enqueue(FakeChatScript.Success);
            _ = fake.GetResponseAsync(Array.Empty<ChatMessage>()).GetAwaiter().GetResult();
        }

        var pipeline = new AuthoringProposePipeline();
        var result = pipeline.Propose(proposeRequest, draft);
        var opDiff = BuildOpDiff(draft.ProposedOperations);

        if (result.OutcomeClass == AuthoringOutcomeClass.Conflict)
        {
            var conflictMessage = result.Diagnostics.Count > 0
                ? result.Diagnostics[0].Message
                : "editVersion mismatch; refuse stale scenario revision.";
            return Error(
                result.ConflictCode ?? ScenarioEditVersionGuard.ConflictCode,
                conflictMessage,
                new
                {
                    mode = "provider",
                    provider = "fake",
                    outcomeClass = result.OutcomeClass.ToString(),
                    intent = request.Intent,
                    expectedEditVersion = proposeRequest.ExpectedEditVersion,
                    currentEditVersion = proposeRequest.CurrentEditVersion,
                    diagnostics = result.Diagnostics.Select(d => new { d.Code, d.Message }).ToArray(),
                    apply = false,
                    note = "Stale revision refused. Re-pin --expected-edit-version to the open scenario before proposing again.",
                });
        }

        var payload = new Dictionary<string, object?>
        {
            ["ok"] = result.OutcomeClass == AuthoringOutcomeClass.ValidDraft,
            ["mode"] = "provider",
            ["provider"] = "fake",
            ["intent"] = request.Intent,
            ["scriptSource"] = scriptSource,
            ["outcomeClass"] = result.OutcomeClass.ToString(),
            ["conflictCode"] = result.ConflictCode,
            ["diagnostics"] = result.Diagnostics.Select(d => new { d.Code, d.Message }).ToArray(),
            ["missions"] = result.Missions,
            ["proposedOperationsDiff"] = opDiff,
            ["draftDocument"] = result.DraftDocument,
            ["validationPassed"] = result.ValidationReport?.Passed,
            ["provenanceTags"] = result.ProvenanceTags.Select(t => new
            {
                t.Tag,
                t.Source,
                t.AgentId,
                t.Evidence,
            }).ToArray(),
            ["transparencyLabel"] = result.TransparencyLabel,
            ["submitAllowedWithoutHuman"] = result.SubmitAllowedWithoutHuman,
            ["apply"] = false,
            ["applyNote"] =
                "Proposal only. Apply via explicit mission verbs (mission_add_patrol / mission_add_strike / …) with a matching --edit-version.",
        };

        var exitCode = result.OutcomeClass == AuthoringOutcomeClass.ValidDraft ? 0 : 1;
        return new AiHostIpcResponse(exitCode, JsonSerializer.Serialize(payload, CamelOptions));
    }

    private static AiHostIpcResponse Error(string code, string message, object? extra = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["ok"] = false,
            ["code"] = code,
            ["message"] = message,
        };
        if (extra is not null)
        {
            payload["details"] = extra;
        }

        var exitCode = code == ScenarioEditVersionGuard.ConflictCode ? 3 : 1;
        return new AiHostIpcResponse(exitCode, JsonSerializer.Serialize(payload, CamelOptions));
    }

    private static string DefaultScriptIdempotencyKey(string intent) =>
        "cli-script-" + StableHash.Fnv1A32(intent).ToString("x8");

    private static (AuthoringProposeRequest Request, AuthoringModelDraft Draft, string ScriptSource)
        LoadDraftAndRequest(AiHostMissionPlanSuggestRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.FixtureCaseId))
        {
            var dir = ResolveFixtureCaseDir(req.FixtureCaseId, req.AdditionalFixtureRoots);
            var casePath = Path.Combine(dir, "case.json");
            var requestPath = Path.Combine(dir, "request.json");
            var scriptFile = Path.Combine(dir, "provider-script.json");
            if (!File.Exists(scriptFile))
            {
                throw new FileNotFoundException("provider-script.json missing for fixture: " + req.FixtureCaseId, scriptFile);
            }

            using var caseDoc = JsonDocument.Parse(File.ReadAllText(casePath));
            var open = caseDoc.RootElement.GetProperty("openScenario");
            using var requestDoc = JsonDocument.Parse(File.ReadAllText(requestPath));
            var reqRoot = requestDoc.RootElement;

            var draft = AuthoringModelDraftParser.Parse(File.ReadAllText(scriptFile));
            var proposeRequest = new AuthoringProposeRequest(
                Brief: GetString(reqRoot, "brief") ?? req.Intent,
                IdempotencyKey: req.IdempotencyKey
                                ?? GetString(reqRoot, "idempotencyKey")
                                ?? req.FixtureCaseId,
                ExpectedEditVersion: req.ExpectedEditVersion ?? GetInt(reqRoot, "expectedEditVersion"),
                CurrentEditVersion: req.CurrentEditVersion ?? GetInt(open, "currentEditVersion"),
                AllowedUnitIds: req.AllowedUnitIds is { Count: > 0 }
                    ? req.AllowedUnitIds
                    : ReadStringArray(open, "allowedUnitIds") ?? Array.Empty<string>(),
                AllowedTargetIds: req.AllowedTargetIds is { Count: > 0 }
                    ? req.AllowedTargetIds
                    : ReadStringArray(open, "allowedTargetIds") ?? Array.Empty<string>(),
                SupportedMissionTypes: req.SupportedMissionTypes is { Count: > 0 }
                    ? req.SupportedMissionTypes
                    : ReadStringArray(open, "supportedMissionTypes") ?? Array.Empty<string>(),
                DbRef: req.DbRef ?? GetString(open, "dbRef") ?? "baltic_patrol");
            return (proposeRequest, draft, "fixture:" + req.FixtureCaseId);
        }

        if (string.IsNullOrWhiteSpace(req.ScriptPath))
        {
            throw new InvalidOperationException(
                "mission_plan_suggest --provider fake requires --fixture <caseId> or --script <provider-script.json>.");
        }

        if (!File.Exists(req.ScriptPath))
        {
            throw new FileNotFoundException("Provider script not found: " + req.ScriptPath, req.ScriptPath);
        }

        if (req.ExpectedEditVersion is null || req.CurrentEditVersion is null)
        {
            throw new InvalidOperationException(
                "--script mode requires --expected-edit-version and --current-edit-version.");
        }

        var scriptDraft = AuthoringModelDraftParser.Parse(File.ReadAllText(req.ScriptPath));
        var pinRequest = new AuthoringProposeRequest(
            Brief: req.Intent,
            IdempotencyKey: req.IdempotencyKey ?? DefaultScriptIdempotencyKey(req.Intent),
            ExpectedEditVersion: req.ExpectedEditVersion.Value,
            CurrentEditVersion: req.CurrentEditVersion.Value,
            AllowedUnitIds: req.AllowedUnitIds ?? Array.Empty<string>(),
            AllowedTargetIds: req.AllowedTargetIds ?? Array.Empty<string>(),
            SupportedMissionTypes: req.SupportedMissionTypes ?? Array.Empty<string>(),
            DbRef: req.DbRef ?? "baltic_patrol");
        return (pinRequest, scriptDraft, "script:" + req.ScriptPath);
    }

    private static object[] BuildOpDiff(IReadOnlyList<AuthoringProposedOperation> ops)
    {
        var list = new List<object>(ops.Count);
        foreach (var op in ops)
        {
            list.Add(new
            {
                action = "propose",
                op = op.Op,
                missionId = op.MissionId,
                assignedUnitIds = op.AssignedUnitIds,
                targetIds = op.TargetIds,
                waypoints = op.Waypoints?.Select(w => new { w.Lat, w.Lon }).ToArray(),
                role = op.Role,
                applyVerb = MapApplyVerb(op.Op),
            });
        }

        return list.ToArray();
    }

    private static string MapApplyVerb(string op) => op switch
    {
        "AddPatrolMission" => "mission_add_patrol",
        "AddStrikeMission" => "mission_add_strike",
        "AddFerryMission" => "mission_add_ferry",
        "AddSupportMission" => "mission_add_support",
        _ => "mission_update_patrol",
    };

    private static string ResolveFixtureCaseDir(string caseId, IReadOnlyList<string>? additionalRoots)
    {
        foreach (var root in EnumerateFixtureRoots(additionalRoots))
        {
            var dir = Path.Combine(root, caseId);
            if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "provider-script.json")))
            {
                return dir;
            }
        }

        throw new DirectoryNotFoundException(
            "Authoring fixture case not found: " + caseId
            + " (expected under tests/ai-fixtures/authoring or test output ai-fixtures/authoring).");
    }

    private static IEnumerable<string> EnumerateFixtureRoots(IReadOnlyList<string>? additionalRoots)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();
        void Add(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && seen.Add(path))
            {
                candidates.Add(path);
            }
        }

        if (additionalRoots is not null)
        {
            foreach (var root in additionalRoots)
            {
                Add(root);
            }
        }

        Add(Path.Combine(AppContext.BaseDirectory, "ai-fixtures", "authoring"));

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            Add(Path.Combine(dir, "tests", "ai-fixtures", "authoring"));
            Add(Path.Combine(dir, "ai-fixtures", "authoring"));
            var parent = Path.GetDirectoryName(dir);
            if (parent is null || parent == dir)
            {
                break;
            }

            dir = parent;
        }

        dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 10; i++)
        {
            Add(Path.Combine(dir, "tests", "ai-fixtures", "authoring"));
            var parent = Path.GetDirectoryName(dir);
            if (parent is null || parent == dir)
            {
                break;
            }

            dir = parent;
        }

        return candidates;
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
}
