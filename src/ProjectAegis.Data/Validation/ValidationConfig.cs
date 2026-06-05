namespace ProjectAegis.Data.Validation;

/// <summary>Tuning knobs from GDD §7 / ADR-008.</summary>
public sealed record ValidationConfig(
    double IngressEgressPadNm = 50,
    double FuelFraction = 0.85,
    ValidationSeverity ExportBlockSeverityFloor = ValidationSeverity.Error);