namespace ProjectAegis.Data.Validation;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ValidationReportJsonDto
{
    public bool Passed { get; init; }

    public bool CanExport { get; init; }

    public string ReportHash { get; init; } = "";

    public IReadOnlyList<ValidationFindingJsonDto> Findings { get; init; } = Array.Empty<ValidationFindingJsonDto>();

    public IReadOnlyList<DoctrineResolutionJsonDto> DoctrineResolution { get; init; } =
        Array.Empty<DoctrineResolutionJsonDto>();

    public static ValidationReportJsonDto FromReport(ValidationReport report, ValidationConfig config) =>
        new()
        {
            Passed = report.Passed,
            CanExport = report.CanExport(config),
            ReportHash = report.ReportHash,
            Findings = report.Findings.Select(ValidationFindingJsonDto.FromFinding).ToArray(),
            DoctrineResolution = report.Findings
                .Where(f => string.Equals(f.Code, "DOCTRINE_RESOLVED", StringComparison.Ordinal))
                .OrderBy(f => f.MissionId ?? "", StringComparer.Ordinal)
                .Select(DoctrineResolutionJsonDto.FromFinding)
                .ToArray(),
        };

    public static string Serialize(ValidationReport report, ValidationConfig config)
    {
        var dto = FromReport(report, config);
        return JsonSerializer.Serialize(dto, SerializerOptions);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };
}

public sealed class ValidationFindingJsonDto
{
    public string Code { get; init; } = "";

    public string Severity { get; init; } = "";

    public string Message { get; init; } = "";

    public string? MissionId { get; init; }

    public string? UnitId { get; init; }

    public string? TargetId { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }

    public static ValidationFindingJsonDto FromFinding(ValidationFinding finding) =>
        new()
        {
            Code = finding.Code,
            Severity = finding.Severity.ToString(),
            Message = finding.Message,
            MissionId = finding.MissionId,
            UnitId = finding.UnitId,
            TargetId = finding.TargetId,
            Data = finding.Data,
        };
}

public sealed class DoctrineResolutionJsonDto
{
    public string MissionId { get; init; } = "";

    public string ResolvedRoe { get; init; } = "";

    public static DoctrineResolutionJsonDto FromFinding(ValidationFinding finding)
    {
        var data = finding.Data;
        return new DoctrineResolutionJsonDto
        {
            MissionId = data != null && data.TryGetValue("missionId", out var missionId)
                ? missionId
                : finding.MissionId ?? "",
            ResolvedRoe = data != null && data.TryGetValue("resolvedRoe", out var resolvedRoe)
                ? resolvedRoe
                : "",
        };
    }
}