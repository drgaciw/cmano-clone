using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>Propose-lane result. Never implies Submit; human authority required.</summary>
public sealed record AuthoringProposeResult(
    AuthoringOutcomeClass OutcomeClass,
    IReadOnlyList<AuthoringDiagnostic> Diagnostics,
    ScenarioDocumentDto? DraftDocument,
    IReadOnlyList<string> Missions,
    ManifestBuilder.ProvenanceTag[] ProvenanceTags,
    string? TransparencyLabel,
    string? ConflictCode,
    ValidationReport? ValidationReport,
    bool SubmitAllowedWithoutHuman,
    AuthoringBaselineMetrics Metrics);
