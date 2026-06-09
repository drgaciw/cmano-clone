namespace ProjectAegis.Data.Platform;

/// <summary>
/// Req-21 / ADR-011: port that isolates Excel (.xlsx) serialization from the engine-free Data assembly.
/// The production adapter (ClosedXML, deferred) implements this; <see cref="CanonicalTextWorkbookIo"/> is a
/// dependency-free reference implementation used by golden tests and as the contract spec for the xlsx adapter.
/// Implementations MUST round-trip: <c>Read(Write(wb)) == wb</c> for any exporter-produced workbook.
/// </summary>
public interface IPlatformWorkbookIo
{
    void Write(PlatformWorkbook workbook, string path);

    PlatformWorkbook Read(string path);
}
