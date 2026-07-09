namespace ProjectAegis.Data.Platform;

/// <summary>
/// PLE-2.3 / PLE-4.4: structured quarantine report entry for platform workbook import.
/// Mirrors <c>CmoMarkdownQuarantineReportEntry</c> shape for Excel-path unresolved FK / TRL gates.
/// Quarantined rows are never proposed to the write gate.
/// </summary>
public sealed record PlatformImportQuarantineEntry(
    string EntityKind,
    string PlatformId,
    string EntityId,
    string Reason,
    string SourceSheet,
    string Detail = "");
