# Platform-editor host bridges

> **Scope.** The three thin **host facades** in `ProjectAegis.Delegation.UnityAdapter/Bridge/` that
> expose the ADR-011 platform-editor round-trip to a Unity / CLI host without any direct SQLite or
> write-gate access: `PlatformCatalogExportBridge` (Phase C — read-only export/diff),
> `PlatformDesignAssistantBridge` (DRG-73 — draft/propose), and `PlatformWorkbookWriteBridge`
> (Phase D — propose → approve/reject). These are the **write/authoring** counterpart to the
> read-side C2 presentation facades. Each is a `static` wrapper that delegates to a `ProjectAegis.Data`
> service — this page documents the *host seam* (entry points, the read-only-vs-write split, actor
> attribution, deterministic clock, the no-gate-bypass contract); the underlying **round-trip
> mechanics** live in [`platform-workbook-roundtrip.md`](platform-workbook-roundtrip.md) and the
> **assistant** in [`platform-design-assistant.md`](platform-design-assistant.md).
>
> Design: [ADR-011 (platform-editor Excel round-trip)](../architecture/adr-011-platform-editor-excel-roundtrip.md).
> The bridge project has **no `UnityEngine` reference**, so all of this runs under headless
> `dotnet test`.

---

## Where it lives

All in `src/ProjectAegis.Delegation.UnityAdapter/Bridge/`:

| Bridge | Phase | Delegates to | Touches the DB how |
|--------|-------|--------------|--------------------|
| `PlatformCatalogExportBridge` | C | `PlatformCatalogExportResolver` + `PlatformWorkbookExporter` | **Read-only** — export a workbook / round-trip diff. Never proposes or writes. |
| `PlatformDesignAssistantBridge` | DRG-73 | `PlatformDesignAssistant` | Draft (read) or propose (through the write gate). |
| `PlatformWorkbookWriteBridge` | D | `PlatformWorkbookWriteService` | Propose → approve/reject through the `CatalogWriteGate`. Never writes SQLite directly. |

> **Invariant — no direct SQLite, no gate bypass.** Every write path funnels through
> `PlatformWorkbookWriteService` / `PlatformDesignAssistant` → `CatalogWriteGate`. The bridges never
> open a `SqliteConnection`, run `INSERT`, or reference `CatalogWriteGate` in their own signatures.
> This is enforced by reflection + source-token tests (see [Tests](#tests)) — the same extend-only
> discipline as [`catalog-write-gate.md`](catalog-write-gate.md).

---

## `PlatformCatalogExportBridge` — read-only export + diff (Phase C)

| Method | Result |
|--------|--------|
| `ExportFromDatabase(db, snapshotId, clockTicks=0, exporter?)` | Resolve the snapshot (`PlatformCatalogExportResolver.TryResolve`, throws if it does not resolve) + `CatalogExportManifest.Resolve`, then export a `PlatformWorkbook`. |
| `ExportBalticWorkbook(db, clockTicks=0)` | Convenience for `CatalogValidationDefaults.BalticSnapshotId`. |
| `ExportToFile` / `ExportBalticToFile(…, outPath, ioFlag?)` | Export then write to disk via `PlatformWorkbookIoSelection.Resolve` (canonical text vs `.xlsx`). |
| `DiffUneditedRoundTrip(db, snapshotId, clockTicks=0)` / `DiffBaltic…` | Export and `PlatformWorkbookDiff.Compare(workbook, workbook)` — the **empty-diff-on-unedited** golden. |

Arguments are validated (`db`/`snapshotId`/`outPath` required); everything is read-only.

## `PlatformDesignAssistantBridge` — draft / propose (DRG-73)

| Method | Result |
|--------|--------|
| `Draft(catalog, brief)` / `DraftFromDatabase(db, brief, layerVersion?)` | Peer-relative `PlatformDesignProposal` — pure read, no write. |
| `Propose(db, brief, clockTicks=0, actorType="agent", actorId="platform-design-assistant", rationale?)` | Stage a proposal through the write gate; returns `PlatformDesignProposeResult`. |

The bridge holds one shared `PlatformDesignAssistant` instance and opens a short-lived
`SqliteCatalogReader` (read-only reader, not a writer) for the DB overloads.

## `PlatformWorkbookWriteBridge` — propose → approve/reject (Phase D)

| Method | Result |
|--------|--------|
| `ExportBalticWorkbook(db, clockTicks=0)` | Export via the write service (the edit source for a round-trip). |
| `ProposeWorkbook(db, workbook, actorType, actorId, clockTicks=0, rationale="")` | Stage an edited workbook → `PlatformWorkbookWriteResult` (`Proposed`, `BatchIds`, `Import` plan, optional balance-drift advisory). An **unedited** workbook yields `Proposed == false` + empty `BatchIds`. |
| `ProposeWorkbookFromFile(db, path, actorType, actorId, clockTicks=0, rationale="", ioFlag?)` | Same, reading the workbook from disk (`PlatformWorkbookIoSelection.Resolve`). |
| `ApproveBatches(db, batchIds, actorType, actorId, clockTicks=0)` | Commit staged batches → `PlatformWorkbookWriteDecisionResult` (`AllCommitted`). |
| `RejectBatches(db, batchIds, actorType, actorId, clockTicks=0, rationale="")` | Reject staged batches; live rows stay unchanged (`AllCommitted == false`). |

A typical host flow: `ExportBalticWorkbook` → edit cells → `ProposeWorkbook` → `ApproveBatches`
(or `RejectBatches`). The E2E test edits a sensor `BasePd`, proposes as `actorType:"unity"`,
approves as `actorType:"human"`, and reads the new value back.

---

## Cross-cutting host concerns

- **Deterministic clock.** Every write/export takes a `clockTicks` long that becomes a `FixedCatalogClock` — provenance/audit timestamps are host-supplied, never wall-clock, so exports and proposals are reproducible.
- **Actor attribution.** Propose/approve take `actorType` / `actorId` (e.g. `"agent"`/`"unity"` proposes, `"human"` approves) which flow into the write-gate audit trail — human approval stays the gate for production DBs.
- **IO selection.** File paths route through `PlatformWorkbookIoSelection` (canonical text default; `.xlsx` via the [`ProjectAegis.Data.Excel`](../../src/ProjectAegis.Data.Excel/README.md) adapter).
- **Snapshot convenience.** `CatalogValidationDefaults.BalticSnapshotId` backs the `*Baltic*` helpers used across the tests.

---

## See also

| Doc | For |
|-----|-----|
| [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) | The `PlatformWorkbookExporter` / `Diff` / `Validator` / `Importer` / `WriteService` round-trip these bridges wrap. |
| [platform-design-assistant.md](platform-design-assistant.md) | The `PlatformDesignAssistant` draft/propose engine behind `PlatformDesignAssistantBridge`. |
| [catalog-write-gate.md](catalog-write-gate.md) | The extend-only `CatalogWriteGate` propose→approve every write ultimately funnels through. |
| [`ProjectAegis.Data.Excel` README](../../src/ProjectAegis.Data.Excel/README.md) | The `.xlsx` IO adapter behind `PlatformWorkbookIoSelection`. |
| [`UnityAdapter` README](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) | The adapter project + its no-`UnityEngine` contract. |

## Tests

`src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/`:

| Test | Pins |
|------|------|
| `PlatformCatalogExportBridgeTests.Bridge_export_trigger_produces_workbook_artifact_for_baltic_fixture` | Export produces a `Platforms`-bearing workbook + on-disk artifact. |
| `PlatformCatalogExportBridgeTests.Bridge_unedited_round_trip_diff_is_empty_golden` | Unedited export→diff is empty. |
| `PlatformCatalogExportBridgeTests.Bridge_type_has_no_write_gate_or_direct_sqlite_patterns` | Read-only: no `SqliteConnection` / `CatalogWriteGate` / `Propose` tokens. |
| `PlatformWorkbookWriteBridgeTests.Bridge_E2E_export_edit_propose_approve_readback_baltic_fixture` | Full export→edit→propose→approve→read-back. |
| `PlatformWorkbookWriteBridgeTests.Bridge_reject_batch_leaves_live_sensor_unchanged` | Reject leaves live rows untouched. |
| `PlatformWorkbookWriteBridgeTests.Bridge_unedited_round_trip_produces_empty_diff_golden` | Unedited propose is a no-op (`Proposed == false`). |
| `PlatformWorkbookWriteBridgeTests.Bridge_type_has_no_direct_sqlite_or_gate_bypass_patterns` | No direct SQLite / gate-bypass tokens. |
