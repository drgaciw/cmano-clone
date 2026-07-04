namespace ProjectAegis.Data.Validation;

/// <summary>Tuning knobs from GDD §7 / ADR-008. Extended S84 for ADR-016 event-graph caps (soft warnings for complexity/density; hard cap per-event conditions). See: roadmap-execute-plan-07042026.md, scenario-editor-scope-boundary-2026-07-04.md, qa-plan-scenario-editor-2026-07-01.md #16, design/gdd/agentic-mission-editor.md §4.3, ADR-016.</summary>
public sealed record ValidationConfig(
    double IngressEgressPadNm = 50,
    double FuelFraction = 0.85,
    ValidationSeverity ExportBlockSeverityFloor = ValidationSeverity.Error,
    // ADR-016 S84-03: soft thresholds (warnings never block export); hard cap is the only blocker
    int ComplexityWarnThreshold = 400,
    int DensityWarnThreshold = 20,
    int CrossRefWeight = 2,
    int MaxConditionsPerEvent = 32);