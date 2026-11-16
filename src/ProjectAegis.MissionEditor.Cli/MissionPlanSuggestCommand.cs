namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using Microsoft.Extensions.AI;
using Ai.Host;
using Ai.Host.Authoring;
using Data.Scenario.Authoring;

/// <summary>
/// Headless NL plan stub — suggests patrol/strike/ferry/support ops from keywords (req 11).
/// DRG-747: opt-in <c>--provider fake</c> runs <see cref="AuthoringProposePipeline"/> against a
/// scripted draft (FakeChatClient / provider-script only). Apply remains a separate mission verb.
/// </summary>
public static class MissionPlanSuggestCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly JsonSerializerOptions CamelOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>Keyword-mode suggest (default). Byte-stable contract for existing tests.</summary>
    public static int Run(string intent, TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(intent))
        {
            Console.Error.WriteLine("mission_plan_suggest requires --intent");
            return 2;
        }

        var normalized = intent.ToLowerInvariant();
        var suggestions = new List<object>();
        if (normalized.Contains("patrol", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "mission_add_patrol",
                id = "patrol-1",
                units = new[] { "u1" },
                waypoints = new[] { "57,20", "57.1,20.1", "57.2,20.2" },
            });
        }

        if (normalized.Contains("strike", StringComparison.Ordinal) || normalized.Contains("attack", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "mission_add_strike",
                id = "strike-1",
                units = new[] { "u1" },
                targets = new[] { "hostile-1" },
            });
        }

        if (normalized.Contains("ferry", StringComparison.Ordinal) ||
            normalized.Contains("redeploy", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "mission_add_ferry",
                id = "ferry-1",
                units = new[] { "u1" },
                destination = "base-1",
                template = "tpl-ferry-empty",
            });
            suggestions.Add(new
            {
                tool = "mission_add_from_template",
                template = "tpl-ferry-empty",
                id = "ferry-from-tpl",
                note = "Empty ferry mission template then assign units/destination",
            });
        }

        // "ew" must not match substrings like "redeploy" — use token-ish checks.
        var hasAew = ContainsToken(normalized, "aew");
        var hasEw = ContainsToken(normalized, "ew");
        var hasTanker = normalized.Contains("tanker", StringComparison.Ordinal);
        var hasSupport = normalized.Contains("support", StringComparison.Ordinal);
        if (hasSupport || hasTanker || hasAew || hasEw)
        {
            var role = hasAew ? "AEW" : hasEw ? "EW" : "Tanker";
            suggestions.Add(new
            {
                tool = "mission_add_support",
                id = "support-1",
                units = new[] { "u1" },
                role,
                template = "tpl-support-tanker",
            });
            suggestions.Add(new
            {
                tool = "mission_add_from_template",
                template = "tpl-support-tanker",
                id = "support-from-tpl",
                note = "Support template then assign units/station",
            });
        }

        if (normalized.Contains("baltic", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_create",
                policyId = "baltic-patrol",
                seed = 42,
                note = "Baltic vertical-slice policy preset",
            });
        }

        if (normalized.Contains("roe", StringComparison.Ordinal) || normalized.Contains("doctrine", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_validate",
                policyId = "baltic-patrol-mission-roe",
                note = "Validate mission ROE inheritance fixture after edits",
            });
        }

        if (normalized.Contains("comms", StringComparison.Ordinal) || normalized.Contains("ew", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_comms_status",
                policyId = normalized.Contains("comms", StringComparison.Ordinal)
                    ? "baltic-patrol-comms"
                    : "baltic-patrol",
                note = "Inspect comms display + timeline before simulate",
            });
        }

        if (normalized.Contains("cyber", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_cyber_status",
                policyId = "baltic-patrol-comms",
                note = "Cyber abort catalog + comms delay coupling",
            });
        }

        if (normalized.Contains("cca", StringComparison.Ordinal) ||
            normalized.Contains("hypersonic", StringComparison.Ordinal) ||
            normalized.Contains("near-future", StringComparison.Ordinal) ||
            normalized.Contains("near future", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_near_future_spawn",
                note = "Validate gated CCA/hypersonic spawns from scenario metadata",
            });
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add(new
            {
                tool = "scenario_validate",
                note = "No mission keyword matched; validate scenario before adding missions.",
            });
        }

        var payload = new { ok = true, intent, suggestions };
        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }

    /// <summary>
    /// Opt-in provider mode (fake/scripted only). Yields a validated draft + op diff; never applies.
    /// </summary>
    public static int RunProviderFake(
        string intent,
        string? fixtureCaseId,
        string? scriptPath,
        int? expectedEditVersion,
        int? currentEditVersion,
        IReadOnlyList<string>? allowedUnitIds,
        IReadOnlyList<string>? allowedTargetIds,
        IReadOnlyList<string>? supportedMissionTypes,
        string? idempotencyKey,
        string? dbRef,
        TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(intent))
        {
            return McpToolResult.WriteError(output, "MISSING_INTENT", "mission_plan_suggest --provider requires --intent <text>");
        }

        AuthoringProposeRequest request;
        AuthoringModelDraft draft;
        string scriptSource;
        try
        {
            (request, draft, scriptSource) = LoadDraftAndRequest(
                intent,
                fixtureCaseId,
                scriptPath,
                expectedEditVersion,
                currentEditVersion,
                allowedUnitIds,
                allowedTargetIds,
                supportedMissionTypes,
                idempotencyKey,
                dbRef);
        }
        catch (FileNotFoundException ex)
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return McpToolResult.WriteError(output, "INVALID_SCRIPT", ex.Message);
        }
        catch (JsonException ex)
        {
            return McpToolResult.WriteError(output, "INVALID_SCRIPT", ex.Message);
        }

        // Touch FakeChatClient so the path is explicitly offline / non-paid (no live keys).
        using (var fake = new FakeChatClient())
        {
            fake.Enqueue(FakeChatScript.Success);
            _ = fake.GetResponseAsync(Array.Empty<ChatMessage>()).GetAwaiter().GetResult();
        }

        var pipeline = new AuthoringProposePipeline();
        var result = pipeline.Propose(request, draft);
        var opDiff = BuildOpDiff(draft.ProposedOperations);

        if (result.OutcomeClass == AuthoringOutcomeClass.Conflict)
        {
            var conflictMessage = result.Diagnostics.Count > 0
                ? result.Diagnostics[0].Message
                : "editVersion mismatch; refuse stale scenario revision.";
            return McpToolResult.WriteError(
                output,
                result.ConflictCode ?? ScenarioEditVersionGuard.ConflictCode,
                conflictMessage,
                new
                {
                    mode = "provider",
                    provider = "fake",
                    outcomeClass = result.OutcomeClass.ToString(),
                    intent,
                    expectedEditVersion = request.ExpectedEditVersion,
                    currentEditVersion = request.CurrentEditVersion,
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
            ["intent"] = intent,
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

        output.WriteLine(JsonSerializer.Serialize(payload, CamelOptions));
        return result.OutcomeClass == AuthoringOutcomeClass.ValidDraft ? 0 : 1;
    }

    private static (AuthoringProposeRequest Request, AuthoringModelDraft Draft, string ScriptSource)
        LoadDraftAndRequest(
            string intent,
            string? fixtureCaseId,
            string? scriptPath,
            int? expectedEditVersion,
            int? currentEditVersion,
            IReadOnlyList<string>? allowedUnitIds,
            IReadOnlyList<string>? allowedTargetIds,
            IReadOnlyList<string>? supportedMissionTypes,
            string? idempotencyKey,
            string? dbRef)
    {
        if (!string.IsNullOrWhiteSpace(fixtureCaseId))
        {
            var dir = ResolveFixtureCaseDir(fixtureCaseId);
            var casePath = Path.Combine(dir, "case.json");
            var requestPath = Path.Combine(dir, "request.json");
            var scriptFile = Path.Combine(dir, "provider-script.json");
            if (!File.Exists(scriptFile))
            {
                throw new FileNotFoundException("provider-script.json missing for fixture: " + fixtureCaseId, scriptFile);
            }

            using var caseDoc = JsonDocument.Parse(File.ReadAllText(casePath));
            var open = caseDoc.RootElement.GetProperty("openScenario");
            using var requestDoc = JsonDocument.Parse(File.ReadAllText(requestPath));
            var reqRoot = requestDoc.RootElement;

            var draft = AuthoringModelDraftParser.Parse(File.ReadAllText(scriptFile));
            var request = new AuthoringProposeRequest(
                Brief: GetString(reqRoot, "brief") ?? intent,
                IdempotencyKey: idempotencyKey
                                ?? GetString(reqRoot, "idempotencyKey")
                                ?? fixtureCaseId,
                ExpectedEditVersion: expectedEditVersion ?? GetInt(reqRoot, "expectedEditVersion"),
                CurrentEditVersion: currentEditVersion ?? GetInt(open, "currentEditVersion"),
                AllowedUnitIds: allowedUnitIds is { Count: > 0 }
                    ? allowedUnitIds
                    : ReadStringArray(open, "allowedUnitIds") ?? Array.Empty<string>(),
                AllowedTargetIds: allowedTargetIds is { Count: > 0 }
                    ? allowedTargetIds
                    : ReadStringArray(open, "allowedTargetIds") ?? Array.Empty<string>(),
                SupportedMissionTypes: supportedMissionTypes is { Count: > 0 }
                    ? supportedMissionTypes
                    : ReadStringArray(open, "supportedMissionTypes") ?? Array.Empty<string>(),
                DbRef: dbRef ?? GetString(open, "dbRef") ?? "baltic_patrol");
            return (request, draft, "fixture:" + fixtureCaseId);
        }

        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            throw new InvalidOperationException(
                "mission_plan_suggest --provider fake requires --fixture <caseId> or --script <provider-script.json>.");
        }

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Provider script not found: " + scriptPath, scriptPath);
        }

        if (expectedEditVersion is null || currentEditVersion is null)
        {
            throw new InvalidOperationException(
                "--script mode requires --expected-edit-version and --current-edit-version.");
        }

        var scriptDraft = AuthoringModelDraftParser.Parse(File.ReadAllText(scriptPath));
        var pinRequest = new AuthoringProposeRequest(
            Brief: intent,
            IdempotencyKey: idempotencyKey ?? ("cli-script-" + intent.GetHashCode(StringComparison.Ordinal).ToString("x8")),
            ExpectedEditVersion: expectedEditVersion.Value,
            CurrentEditVersion: currentEditVersion.Value,
            AllowedUnitIds: allowedUnitIds ?? Array.Empty<string>(),
            AllowedTargetIds: allowedTargetIds ?? Array.Empty<string>(),
            SupportedMissionTypes: supportedMissionTypes ?? Array.Empty<string>(),
            DbRef: dbRef ?? "baltic_patrol");
        return (pinRequest, scriptDraft, "script:" + scriptPath);
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

    private static string ResolveFixtureCaseDir(string caseId)
    {
        foreach (var root in EnumerateFixtureRoots())
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

    private static IEnumerable<string> EnumerateFixtureRoots()
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

    /// <summary>True when <paramref name="token"/> appears as a whole token (space/punct delimited).</summary>
    private static bool ContainsToken(string haystack, string token)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(token))
        {
            return false;
        }

        if (haystack == token)
        {
            return true;
        }

        var start = 0;
        while (start <= haystack.Length - token.Length)
        {
            var idx = haystack.IndexOf(token, start, StringComparison.Ordinal);
            if (idx < 0)
            {
                return false;
            }

            var leftOk = idx == 0 || !char.IsLetterOrDigit(haystack[idx - 1]);
            var rightIdx = idx + token.Length;
            var rightOk = rightIdx >= haystack.Length || !char.IsLetterOrDigit(haystack[rightIdx]);
            if (leftOk && rightOk)
            {
                return true;
            }

            start = idx + 1;
        }

        return false;
    }
}
