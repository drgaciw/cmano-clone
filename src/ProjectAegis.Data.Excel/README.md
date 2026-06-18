# ProjectAegis.Data.Excel — ClosedXML `.xlsx` adapter

The production `.xlsx` serialization adapter for the platform editor. Implements
requirement **[21-Platform-Editor](../../Game-Requirements/requirements/21-Platform-Editor.md)**
under **[ADR-011](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)**.

This project contains a single class,
[`ClosedXmlPlatformWorkbookIo`](ClosedXmlPlatformWorkbookIo.cs), which implements
[`IPlatformWorkbookIo`](../ProjectAegis.Data/Platform/IPlatformWorkbookIo.cs) using
[ClosedXML](https://github.com/ClosedXML/ClosedXML) (MIT). It reads and writes real
Excel workbooks; all of the deterministic export/diff/validate/import logic lives in
[`ProjectAegis.Data/Platform`](../ProjectAegis.Data/Platform/README.md).

## Why a separate project

ClosedXML is deliberately kept **out of `ProjectAegis.Data`** so the engine-free,
deterministic core never takes a spreadsheet dependency (ADR-006). This adapter:

- targets **`net8.0` only** — it is an edge/CLI adapter, not consumed by the
  `netstandard2.1` sim path;
- references `ProjectAegis.Data` for the `Platform` model and the
  `IPlatformWorkbookIo` port;
- is the **only** place ClosedXML appears in the solution.

After adding/restoring this project run `dotnet restore` so the ClosedXML NuGet
package is available.

## Round-trip contract

The adapter must satisfy the same contract proven by the in-core reference
implementation [`CanonicalTextWorkbookIo`](../ProjectAegis.Data/Platform/CanonicalTextWorkbookIo.cs):

```
Read(Write(workbook)) == workbook
```

To make numeric-looking ids round-trip **byte-for-byte** rather than being coerced
to numbers, `Write`:

- writes one worksheet per [`PlatformWorkbookSheet`](../ProjectAegis.Data/Platform/PlatformWorkbook.cs)
  (header row bold, in order);
- pins every column's number format to text (`"@"`), so a cell like `"57"` stays the
  string `"57"` on re-read instead of becoming the number `57`;
- writes all cells as their string values exactly as the exporter produced them.

`Read` walks each worksheet's used range, treating row 1 as the header and the
remaining rows as data, reconstructing the `PlatformWorkbook`. An empty worksheet
yields a sheet with an empty header and no rows.

### Worksheet-name guard

Excel caps worksheet names at 31 characters and forbids `: \ / ? * [ ]`.
`SafeSheetName` replaces forbidden characters with `_` and truncates to 31 chars.
The current Phase A sheet names (`Platforms`, `Sensors`, `Mounts`, `Loadouts`,
`Magazines`, `Comms`, `_Meta`) are already safe; the guard protects an extended
schema from producing an invalid workbook.

## Usage

```csharp
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;

IPlatformWorkbookIo io = new ClosedXmlPlatformWorkbookIo();

// Export: build the workbook in-core, then serialize to .xlsx.
PlatformWorkbook workbook = exporter.Export(catalogData, snapshotId, clock);
io.Write(workbook, "platforms.xlsx");

// Import: read the edited .xlsx back into the engine-free model, then diff/stage.
PlatformWorkbook edited = io.Read("platforms.xlsx");
PlatformImportPlan plan = importer.Plan(edited);
```

In practice the mission-editor CLI wires this up for you via
`platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx` — see the
[CLI README](../ProjectAegis.MissionEditor.Cli/README.md#platform-excel-round-trip-adr-011).

## Constraints & pitfalls

- **Text format is load-bearing.** Removing the `"@"` column format reintroduces
  numeric coercion and breaks the byte-for-byte round-trip (the most likely
  regression here).
- **The `_Meta` sheet must survive the round-trip.** The importer reads
  `_Meta.SourceSnapshotId` to know which snapshot to diff against; dropping or
  editing it makes a safe import impossible.
- **No deterministic-core tests.** This project is intentionally not unit-tested in
  the deterministic core (it carries the ClosedXML dependency). Round-trip fidelity
  is proven in-core against `CanonicalTextWorkbookIo`; add an integration test here
  once the package is restored locally.
- **Adapter only — no business logic.** Ordering, hashing, diffing, validation, and
  write-gate staging all belong in `ProjectAegis.Data/Platform`. Keep this class a
  thin serialization shim.

## See also

- [Platform round-trip pipeline](../ProjectAegis.Data/Platform/README.md) — the in-core exporter/diff/validator/importer
- [`IPlatformWorkbookIo`](../ProjectAegis.Data/Platform/IPlatformWorkbookIo.cs) — the port this implements
- [ADR-011 — platform editor Excel round-trip](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)
- [ADR-006 — data-layer boundary](../../docs/architecture/adr-006-data-layer-boundary.md)
- [Mission-editor CLI README](../ProjectAegis.MissionEditor.Cli/README.md#platform-excel-round-trip-adr-011)
