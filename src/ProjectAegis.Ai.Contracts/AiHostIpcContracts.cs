namespace ProjectAegis.Ai.Contracts;

/// <summary>
/// Process-boundary verb identifiers accepted by the standalone <c>ProjectAegis.Ai.Host</c> executable (DRG-772 / ADR-030).
/// </summary>
public static class AiHostIpcVerbs
{
    /// <summary>Opt-in after-action explain against an exported JSONL evidence file.</summary>
    public const string AarExplain = "aar_explain";

    /// <summary>Opt-in fake/scripted authoring proposal against a scenario fixture or provider script.</summary>
    public const string MissionPlanSuggest = "mission_plan_suggest";
}

/// <summary>
/// Process-boundary request DTO for the <c>aar_explain</c> verb sent from CLI to <c>ProjectAegis.Ai.Host</c> over stdin JSON.
/// </summary>
public sealed record AiHostAarExplainRequest(
    string? EvidencePath,
    bool Enabled,
    string? ArtifactRoot,
    string? ScriptPath);

/// <summary>
/// Process-boundary request DTO for the <c>mission_plan_suggest --provider fake</c> verb sent from CLI to <c>ProjectAegis.Ai.Host</c> over stdin JSON.
/// </summary>
public sealed record AiHostMissionPlanSuggestRequest(
    string Intent,
    string? FixtureCaseId,
    string? ScriptPath,
    int? ExpectedEditVersion,
    int? CurrentEditVersion,
    IReadOnlyList<string>? AllowedUnitIds,
    IReadOnlyList<string>? AllowedTargetIds,
    IReadOnlyList<string>? SupportedMissionTypes,
    string? IdempotencyKey,
    string? DbRef,
    IReadOnlyList<string>? AdditionalFixtureRoots = null);

/// <summary>
/// Process-boundary response frame returned by <c>ProjectAegis.Ai.Host</c> on stdout JSON.
/// </summary>
public sealed record AiHostIpcResponse(
    int ExitCode,
    string PayloadJson);
