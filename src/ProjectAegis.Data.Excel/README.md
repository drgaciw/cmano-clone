# ProjectAegis.Data.Excel

The **production `.xlsx` adapter** for the platform editor (Req-21 / ADR-011). It supplies a
real Microsoft-Excel workbook implementation of `ProjectAegis.Data`'s `IPlatformWorkbookIo` port,
backed by [ClosedXML](https://github.com/ClosedXML/ClosedXML). This is a thin **edge assembly** —
it exists purely so the ClosedXML dependency never enters the deterministic, engine-free
`ProjectAegis.Data` core.

> **Architecture rule (ADR-006 boundary).** The workbook export/diff logic lives in
> `ProjectAegis.Data` and is spreadsheet-library-free and unit-testable. Only the final
> serialize-to-bytes step is isolated here. `ProjectAegis.Data` never references ClosedXML;
> the adapter is injected at the CLI/test edge via a factory. Targets **`net8.0` only** — it is
> not consumed by the `netstandard2.1` sim path.

---

## Why a separate assembly

`ProjectAegis.Data` targets `net8.0;netstandard2.1` and must stay free of heavy, non-deterministic
dependencies (ADR-006). ClosedXML pulls in the OpenXML SDK and is `net8.0`-only, so it is quarantined
here. The core defines the port and a dependency-free reference implementation
(`CanonicalTextWorkbookIo`); this project provides the binary `.xlsx` counterpart that must satisfy
the **same round-trip contract**.

```
ProjectAegis.Data (engine-free core)          ProjectAegis.Data.Excel (this project, net8.0)
├── Platform/IPlatformWorkbookIo   ◄───────────  implements
├── Platform/CanonicalTextWorkbookIo (fallback)  ClosedXmlPlatformWorkbookIo
├── Platform/PlatformWorkbook{,Sheet} (model)    PlatformWorkbookIoFactories.ClosedXml()
└── Platform/PlatformWorkbookIoSelection ─────►  (selects adapter at the edge)
```

---

## Subsystem map

| File | Purpose | Key type |
|------|---------|----------|
| `ClosedXmlPlatformWorkbookIo.cs` | `IPlatformWorkbookIo` backed by ClosedXML; one worksheet per `PlatformWorkbookSheet`, cells written as text | `ClosedXmlPlatformWorkbookIo` |
| `PlatformWorkbookIoFactories.cs` | Edge factory injected into `PlatformWorkbookIoSelection.Resolve(...)` | `PlatformWorkbookIoFactories.ClosedXml()` |
| `PlatformEmconEnums.cs` | Allowed EMCON enum values surfaced as Excel in-cell dropdown validation (migration 008) | `PlatformEmconEnums` |

---

## Round-trip contract

Every `IPlatformWorkbookIo` implementation MUST satisfy `Read(Write(wb)) == wb` for any
exporter-produced `PlatformWorkbook`. The binary adapter earns this by writing **all cells as text**
and pinning each column's number format to `"@"`, so numeric-looking values (e.g. `"57"`) are not
coerced to numbers on load and survive byte-for-byte. Layout on write:

- Row 1 = the sheet header (bold).
- Rows 2..N = data rows, in the deterministic order produced by `PlatformWorkbookExporter`.
- On read, the used range is scanned back into a `PlatformWorkbook` with the same header/rows.

Empty sheets round-trip to an empty header + empty rows (no exception).

---

## EMCON list validation (Phase B, S24-11)

On the `Emcon` sheet, the adapter attaches Excel data-validation dropdowns to the `Condition` and
`Posture` columns so an editor can only pick migration-008 enum values:

| Column | Allowed values (`platform_emcon`) |
|--------|-----------------------------------|
| `Condition` | `silent`, `restricted`, `free` |
| `Posture` | `off`, `standby`, `active` |

Validation is applied to data rows down to a fixed floor (`EnumValidationLastRow = 1000`) so freshly
added rows still get the dropdown, and only to the `Emcon` sheet — other sheets (`Mobility`,
`Signatures`, …) are untouched. This is UX-only metadata: it does not alter cell values and so does
not affect the round-trip hash.

---

## Usage

The adapter is never constructed directly by callers; it is passed as a factory to the core's
selection helper, which chooses between canonical text and binary `.xlsx`:

```csharp
using ProjectAegis.Data.Platform;   // core: exporter, selection, model
using ProjectAegis.Data.Excel;      // this assembly: the factory

// Resolve the adapter from an explicit flag, PLATFORM_WORKBOOK_IO env var, or path extension.
// The ClosedXML factory is only invoked when binary .xlsx is selected.
IPlatformWorkbookIo io = PlatformWorkbookIoSelection.Resolve(
    path: "platform-export.xlsx",
    ioFlag: null,                              // or "closedxml" / "canonical"
    closedXmlFactory: PlatformWorkbookIoFactories.ClosedXml);

var exporter = new PlatformWorkbookExporter();
exporter.WriteToFile(workbook, "platform-export.xlsx", io);
```

Selection precedence (`PlatformWorkbookIoSelection`): explicit `--io` flag → `PLATFORM_WORKBOOK_IO`
env var → file extension (`.txt`/`.platform.txt` ⇒ canonical, `.xlsx` ⇒ ClosedXML) → binary default.
Requesting ClosedXML without supplying the factory throws `InvalidOperationException` — headless
callers with no Excel adapter must pass `--io canonical`.

### From the CLI

The `ProjectAegis.MissionEditor.Cli` verbs wire this factory automatically:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_export_xlsx --out platform.xlsx [--db catalog.db] [--snapshot <id>] \
  [--tl-tier TL-0..TL-5] [--io closedxml|canonical]

dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_import_xlsx --db catalog.db [--in platform.xlsx] [--io closedxml|canonical]

dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_diff_xlsx [--db catalog.db] [--base <path>] [--edited <path>] [--io closedxml|canonical]
```

Import stages rows through the write gate (no auto-commit); follow with `catalog_write_approve`.
See [`docs/engineering/mission-editor-cli.md`](../../docs/engineering/mission-editor-cli.md).

---

## Constraints & deferred work

- **Worksheet-name safety.** Names are truncated to Excel's 31-char cap and the forbidden characters
  `: \ / ? * [ ]` are replaced with `_`. Current schema names are short and clean; this only guards
  against a future extended schema producing an invalid workbook.
- **Sheet / primary-key column protection** (PLE-1.2, doc 21 OQ5) is **deferred** — the workbook does
  not lock structural cells yet.
- **`net8.0` only** — do not reference this project from the sim/`netstandard2.1` path. Add it at the
  CLI/test edge only.

---

## Build & test

```bash
dotnet build src/ProjectAegis.Data.Excel/ProjectAegis.Data.Excel.csproj
dotnet test  src/ProjectAegis.Data.Excel.Tests/ProjectAegis.Data.Excel.Tests.csproj -v minimal
```

`ProjectAegis.Data.Excel.Tests` (~5 tests, part of the ≥1232-test solution baseline) proves the
contract:

- `PlatformWorkbookBinaryGoldenTests` — binary round-trip preserves the workbook hash golden, an
  edited EMCON posture round-trips with a deterministic diff, and an unedited round-trip yields an
  empty diff.
- `ClosedXmlValidationMetadataTests` — the `Emcon` sheet carries `Condition`/`Posture` dropdown
  validation, and `Mobility`/`Signatures` sheets do not.

Golden hashes are shared with the core's canonical adapter — regenerate them intentionally, never
silently.

## See also

| Topic | Doc |
|-------|-----|
| Data / catalog core (owns the exporter, diff, model, port) | [`../ProjectAegis.Data/README.md`](../ProjectAegis.Data/README.md) |
| Core round-trip & governance pipeline (`Platform/`) | [`docs/engineering/platform-workbook-roundtrip.md`](../../docs/engineering/platform-workbook-roundtrip.md) |
| Workbook round-trip CLI (export / import / diff) | [`docs/engineering/mission-editor-cli.md`](../../docs/engineering/mission-editor-cli.md) |
| Data-layer boundary decision | [`adr-006-data-layer-boundary.md`](../../docs/architecture/adr-006-data-layer-boundary.md) |
| Determinism rules (ordering, hashing) | [`docs/engineering/determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md) |
