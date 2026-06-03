namespace ProjectAegis.Data.Validation;

public sealed record ValidationFinding(
    string Code,
    ValidationSeverity Severity,
    string Message,
    string? MissionId = null,
    string? UnitId = null,
    string? TargetId = null,
    IReadOnlyDictionary<string, string>? Data = null);