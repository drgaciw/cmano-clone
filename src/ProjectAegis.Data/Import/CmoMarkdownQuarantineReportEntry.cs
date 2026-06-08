namespace ProjectAegis.Data.Import;

/// <summary>Structured quarantine row for catalog_import_markdown JSON report (S19-02).</summary>
public sealed record CmoMarkdownQuarantineReportEntry(
    string PlatformId,
    string SensorId,
    string Reason,
    string SourceFile);