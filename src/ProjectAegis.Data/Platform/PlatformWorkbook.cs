namespace ProjectAegis.Data.Platform;

/// <summary>
/// Req-21: an engine-free, deterministic in-memory model of a platform-editor workbook.
/// One <see cref="PlatformWorkbookSheet"/> per entity domain. Serialized to .xlsx by an
/// <see cref="IPlatformWorkbookIo"/> adapter; kept free of any spreadsheet library so the
/// exporter/diff stay pure and unit-testable (ADR-006 boundary).
/// </summary>
public sealed record PlatformWorkbook(IReadOnlyList<PlatformWorkbookSheet> Sheets)
{
    public PlatformWorkbookSheet? FindSheet(string name) =>
        Sheets.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.Ordinal));
}

/// <summary>A single sheet: an ordered header plus deterministically ordered rows of cell strings.</summary>
public sealed record PlatformWorkbookSheet(
    string Name,
    IReadOnlyList<string> Header,
    IReadOnlyList<IReadOnlyList<string>> Rows);
