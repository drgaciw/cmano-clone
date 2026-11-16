using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>
/// Fake-first Propose-lane authoring (DRG-740). Keyword stubs in
/// <see cref="AiAuthoringServices"/> remain the fallback outside this host.
/// </summary>
public sealed class AuthoringProposePipeline : IAuthoringProposePipeline
{
    private readonly IScenarioValidationEngine _validationEngine;
    private readonly ICatalogReader _catalog;
    private readonly ValidationConfig _validationConfig;
    private readonly AuthoringIdempotencyStore _idempotency;

    /// <summary>Creates a pipeline with Baltic fixture catalog defaults.</summary>
    public AuthoringProposePipeline(
        AuthoringIdempotencyStore? idempotency = null,
        IScenarioValidationEngine? validationEngine = null,
        ICatalogReader? catalog = null,
        ValidationConfig? validationConfig = null)
    {
        _idempotency = idempotency ?? new AuthoringIdempotencyStore();
        _validationEngine = validationEngine ?? new ScenarioValidationEngine();
        _catalog = catalog ?? InMemoryCatalogReader.BalticPatrolFixture();
        _validationConfig = validationConfig ?? new ValidationConfig();
    }

    /// <inheritdoc />
    public AuthoringProposeResult Propose(AuthoringProposeRequest request, AuthoringModelDraft draft)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(draft);

        if (request.PriorAcceptedIdempotencyKeys is { Count: > 0 })
        {
            _idempotency.Seed(request.PriorAcceptedIdempotencyKeys);
        }

        if (_idempotency.Contains(request.IdempotencyKey))
        {
            return Fail(
                AuthoringOutcomeClass.IdempotentNoOp,
                [
                    new AuthoringDiagnostic(
                        "DUPLICATE_IDEMPOTENCY_KEY",
                        $"Proposal with idempotency key '{request.IdempotencyKey}' was already accepted."),
                ],
                conflictCode: null);
        }

        var conflict = ScenarioEditVersionGuard.TryCheck(
            request.ExpectedEditVersion,
            request.CurrentEditVersion);
        if (conflict is not null)
        {
            return Fail(
                AuthoringOutcomeClass.Conflict,
                [
                    new AuthoringDiagnostic(
                        ScenarioEditVersionGuard.ConflictCode,
                        conflict.Message),
                ],
                conflictCode: ScenarioEditVersionGuard.ConflictCode);
        }

        if (string.Equals(draft.ScriptedStatus, "ClarificationNeeded", StringComparison.OrdinalIgnoreCase))
        {
            var msg = string.IsNullOrWhiteSpace(draft.ClarificationPrompt)
                ? "Ambiguous request; specify mission type and unit id."
                : draft.ClarificationPrompt;
            return Fail(
                AuthoringOutcomeClass.Diagnostics,
                [new AuthoringDiagnostic("AMBIGUOUS_REQUEST", msg)],
                conflictCode: null);
        }

        var diagnostics = new List<AuthoringDiagnostic>();
        foreach (var op in draft.ProposedOperations)
        {
            CollectIdAndRoleDiagnostics(request, op, diagnostics);
        }

        if (diagnostics.Count > 0)
        {
            return Fail(AuthoringOutcomeClass.Diagnostics, diagnostics, conflictCode: null);
        }

        if (draft.ProposedOperations.Count == 0)
        {
            return Fail(
                AuthoringOutcomeClass.Diagnostics,
                [new AuthoringDiagnostic("AMBIGUOUS_REQUEST", "No proposed operations; clarify mission type.")],
                conflictCode: null);
        }

        var editor = ScenarioDocumentEditor.CreateNew(request.DbRef, seed: 42, title: "ai-propose");
        var missions = new List<string>();
        try
        {
            foreach (var op in draft.ProposedOperations)
            {
                ApplyOp(editor, op);
                missions.Add(op.MissionId);
            }
        }
        catch (InvalidOperationException ex)
        {
            return Fail(
                AuthoringOutcomeClass.Diagnostics,
                [new AuthoringDiagnostic("APPLY_FAILED", ex.Message)],
                conflictCode: null);
        }

        var draftDoc = editor.ToDto();
        if (draft.ForbiddenInOutput is { Count: > 0 })
        {
            var blob = System.Text.Json.JsonSerializer.Serialize(draftDoc);
            foreach (var forbidden in draft.ForbiddenInOutput)
            {
                if (blob.Contains(forbidden, StringComparison.Ordinal))
                {
                    diagnostics.Add(new AuthoringDiagnostic(
                        "UNTRUSTED_INSTRUCTION_LEAK",
                        $"Draft output must not contain untrusted instruction fragment '{forbidden}'."));
                }
            }

            if (diagnostics.Count > 0)
            {
                return Fail(AuthoringOutcomeClass.Diagnostics, diagnostics, conflictCode: null);
            }
        }

        var report = _validationEngine.Validate(draftDoc, _catalog, _validationConfig);
        var blocking = report.Findings
            .Where(f => f.Severity == ValidationSeverity.Error)
            .Select(f => new AuthoringDiagnostic(f.Code, f.Message))
            .ToList();
        if (blocking.Count > 0)
        {
            return Fail(AuthoringOutcomeClass.Diagnostics, blocking, conflictCode: null, report);
        }

        _idempotency.Accept(request.IdempotencyKey);

        var provenance = new[]
        {
            new ManifestBuilder.ProvenanceTag(
                Tag: "ai-propose",
                Source: "ai",
                AgentId: "authoring-propose-v1",
                Evidence: Truncate(request.Brief, 80)),
        };

        return new AuthoringProposeResult(
            OutcomeClass: AuthoringOutcomeClass.ValidDraft,
            Diagnostics: Array.Empty<AuthoringDiagnostic>(),
            DraftDocument: draftDoc,
            Missions: missions,
            ProvenanceTags: provenance,
            TransparencyLabel: AuthoringTransparencyLabel.ForProposeLane(),
            ConflictCode: null,
            ValidationReport: report,
            SubmitAllowedWithoutHuman: false,
            Metrics: new AuthoringBaselineMetrics(1, 0, 0, 0));
    }

    private static void CollectIdAndRoleDiagnostics(
        AuthoringProposeRequest request,
        AuthoringProposedOperation op,
        List<AuthoringDiagnostic> diagnostics)
    {
        foreach (var unitId in op.AssignedUnitIds)
        {
            if (!ContainsOrdinal(request.AllowedUnitIds, unitId))
            {
                diagnostics.Add(new AuthoringDiagnostic(
                    "UNKNOWN_UNIT_ID",
                    $"Unit id '{unitId}' is not in the open scenario allowed set."));
            }
        }

        if (op.TargetIds is not null)
        {
            foreach (var targetId in op.TargetIds)
            {
                if (!ContainsOrdinal(request.AllowedTargetIds, targetId))
                {
                    diagnostics.Add(new AuthoringDiagnostic(
                        "UNKNOWN_TARGET_ID",
                        $"Target id '{targetId}' is not in the open scenario allowed set."));
                }
            }
        }

        var missionType = InferMissionType(op.Op);
        if (missionType is not null
            && request.SupportedMissionTypes.Count > 0
            && !ContainsOrdinal(request.SupportedMissionTypes, missionType))
        {
            diagnostics.Add(new AuthoringDiagnostic(
                "UNSUPPORTED_ROLE",
                $"Mission type '{missionType}' (role '{op.Role ?? "n/a"}') is not supported for this open scenario."));
            return;
        }

        if (request.UnitCapabilities is not null
            && op.AssignedUnitIds.Count > 0
            && missionType is not null)
        {
            foreach (var unitId in op.AssignedUnitIds)
            {
                if (request.UnitCapabilities.TryGetValue(unitId, out var caps)
                    && !ContainsOrdinal(caps, missionType)
                    && (op.Role is null || !ContainsOrdinal(caps, op.Role)))
                {
                    diagnostics.Add(new AuthoringDiagnostic(
                        "UNSUPPORTED_ROLE",
                        $"Role '{op.Role ?? missionType}' is not supported for unit '{unitId}'."));
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(op.Role)
                 && string.Equals(missionType, "Support", StringComparison.OrdinalIgnoreCase)
                 && request.SupportedMissionTypes.Count > 0
                 && !ContainsOrdinal(request.SupportedMissionTypes, "Support"))
        {
            diagnostics.Add(new AuthoringDiagnostic(
                "UNSUPPORTED_ROLE",
                $"Role '{op.Role}' is not supported."));
        }
    }

    private static void ApplyOp(ScenarioDocumentEditor editor, AuthoringProposedOperation op)
    {
        var waypoints = (op.Waypoints ?? Array.Empty<AuthoringWaypoint>())
            .Select(w => new ScenarioWaypointDto { Lat = w.Lat, Lon = w.Lon })
            .ToArray();

        switch (op.Op)
        {
            case "AddPatrolMission":
                editor.AddPatrolMission(op.MissionId, op.AssignedUnitIds, waypoints);
                break;
            case "AddStrikeMission":
                editor.AddStrikeMission(
                    op.MissionId,
                    op.AssignedUnitIds,
                    op.TargetIds ?? Array.Empty<string>());
                break;
            case "AddSupportMission":
                editor.AddSupportMission(
                    op.MissionId,
                    op.AssignedUnitIds,
                    op.Role ?? "Tanker",
                    waypoints.Length >= 3
                        ? waypoints
                        : new[]
                        {
                            new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                            new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                        });
                break;
            default:
                throw new InvalidOperationException($"Unsupported proposed op '{op.Op}'.");
        }
    }

    private static string? InferMissionType(string op) => op switch
    {
        "AddPatrolMission" => "Patrol",
        "AddStrikeMission" => "Strike",
        "AddSupportMission" => "Support",
        "AddFerryMission" => "Ferry",
        _ => null,
    };

    private static bool ContainsOrdinal(IReadOnlyList<string> items, string value) =>
        items.Any(i => string.Equals(i, value, StringComparison.OrdinalIgnoreCase));

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";

    private static AuthoringProposeResult Fail(
        AuthoringOutcomeClass outcome,
        IReadOnlyList<AuthoringDiagnostic> diagnostics,
        string? conflictCode,
        ValidationReport? report = null)
    {
        var metrics = outcome switch
        {
            AuthoringOutcomeClass.Conflict => new AuthoringBaselineMetrics(0, 0, 1, 0),
            AuthoringOutcomeClass.IdempotentNoOp => new AuthoringBaselineMetrics(0, 0, 0, 1),
            _ => new AuthoringBaselineMetrics(0, diagnostics.Count, 0, 0),
        };

        return new AuthoringProposeResult(
            OutcomeClass: outcome,
            Diagnostics: diagnostics,
            DraftDocument: null,
            Missions: Array.Empty<string>(),
            ProvenanceTags: Array.Empty<ManifestBuilder.ProvenanceTag>(),
            TransparencyLabel: null,
            ConflictCode: conflictCode,
            ValidationReport: report,
            SubmitAllowedWithoutHuman: false,
            Metrics: metrics);
    }
}
