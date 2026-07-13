# Platform Editor Requirements Completion Plan (Req 21 / ADR-011)

> **For agentic workers:** REQUIRED SUB-SKILLS: `dispatching-parallel-agents`, `subagent-driven-development` (or `executing-plans`), `using-git-worktrees`, `test-driven-development` for code tracks.  
> **Governing docs:** `Game-Requirements/requirements/21-Platform-Editor.md`, `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`, `Game-Requirements/requirements/06-Database-Intelligence.md`  
> **Scope decision:** Close **residual PLE ACs + honesty** so req 21 is program-complete as **headless Excel write-gate editor (Partial+ / MVP)** — **not** WYSIWYG Unity editor, live Office.js, or full Editor Mode presentation pack.

**Goal:** Finish remaining Platform Editor requirements: full enum data-validation matrix (PLE-1.2), quarantine report path (PLE-2.3), post-import release golden (PLE-3.5), TL/archetype import gate (PLE-4.4), sim-visible export provenance assert (PLE-5.3), OQ5 sheet/PK protection, doc 21 honesty + design status, program gate — without rewriting CatalogWriteGate or breaking Baltic/sim invariants.

**Architecture:** Excel is the workbench; `ProjectAegis.Data` + `IWriteGate` remain system of record. `IPlatformWorkbookIo` / ClosedXML stay the I/O port. Completions are **additive** (validation lists, importer gates, goldens, tests, docs). Unity viewer/import chrome already Partial+; presentation PNG residual stays optional Phase N.

**Tech Stack:** .NET 8, ProjectAegis.Data / Data.Excel / MissionEditor.Cli, ClosedXML, xUnit, Graphite, GitNexus.

---

## Baseline (truth as of 2026-07-09 @ main)

| Area | Status |
|------|--------|
| Tracker row 21 | MVP-done / Partial+ (S56) |
| ADR-011 | Accepted |
| Phases A–B catalog sheets + export/import/diff/write | **Shipped** |
| Phases C–H Unity viewer/import/write bridge | **Partial+** (proxy tests) |
| CLI/MCP `platform_*_xlsx` | **Shipped** |
| Empty-diff / write-gate / determinism goldens | **Shipped** |
| Doc 21 Design Status | **Draft** (stale vs implementation) |

### Unchecked / Partial ACs (completion targets)

| AC | Gap | Wave |
|----|-----|------|
| **PLE-1.2** | Emcon list validation only; full enum-column matrix incomplete | **PE-W1** |
| **PLE-2.3** | Orphan reject exists; universal quarantine report-entry path incomplete | **PE-W2** |
| **PLE-3.5** | Gate batches exist; post-import `db_release` / snapshot golden not cited | **PE-W2** |
| **PLE-4.4** | TrlLevel columns export; import gate vs `CatalogArchetypeGate` not fully asserted | **PE-W2** |
| **PLE-5.3** | Sim-visible export gate is doc-06; Excel-only assert missing | **PE-W2** |
| **OQ5** | Sheet/PK/`_Meta` protection deferred | **PE-W1** |
| **Phase N** | Live Editor screenshots | **PE-W3 optional / honesty residual** |
| Design status Draft | Stale | **PE-W0** |

### Explicit out of this program

| Item | Why |
|------|-----|
| In-engine WYSIWYG platform editor | Doc 21 / ADR-011 out of v1 |
| Live Excel/Office.js add-in | Rejected in ADR-011 |
| TL branch databases UI | Doc 06 single main + snapshots |
| Rewriting `CatalogWriteGate` write paths | **Extend-only** |
| Reopening Baltic replay hash / DelegationBridge | Standing invariants |
| Full DB3000 taxonomy | Excessive scope |

---

## Standing invariants (every wave)

| Invariant | Rule |
|-----------|------|
| Test floor | ≥1550 monotonic (current suite) |
| ReplayGolden | 6/6 |
| Hash | `17144800277401907079` |
| PlayModeSmoke | ≥18 |
| `CatalogWriteGate` | **Extend-only** — Excel path is consumer only |
| `DelegationBridge.cs` | ZERO production hotpath |
| Baltic corpora | Frozen |
| Stage | Release |

**Pre-edit hubs (GitNexus impact required):**

```bash
node .gitnexus/run.cjs impact CatalogWriteGate --direction upstream --summary-only
node .gitnexus/run.cjs impact PlatformWorkbookImporter --direction upstream --summary-only
node .gitnexus/run.cjs impact ClosedXmlPlatformWorkbookIo --direction upstream --summary-only
node .gitnexus/run.cjs impact PlatformWorkbookExporter --direction upstream --summary-only
```

Warn user before edits if CRITICAL.

---

## Program packaging

| Artifact | Path |
|----------|------|
| This plan | `docs/superpowers/plans/2026-07-09-platform-editor-completion-plan.md` |
| Epic | `production/epics/platform-editor-completion/EPIC.md` (create PE-W0) |
| Scope boundary | `production/platform-editor-completion-scope-boundary-2026-07-09.md` |
| Stories | `story-pe-001` … `story-pe-004` under epic dir |
| Gate evidence | `production/qa/platform-editor-completion-gate-YYYY-MM-DD.md` |

| Wave | Story | Goal | Est. | Parallel? |
|------|-------|------|------|-----------|
| **PE-W0** | PE-001 | Hygiene: epic, boundary, doc 21 status Draft→implementation-aligned narrative (no AC flip without evidence) | 0.5–1 d | Docs only |
| **PE-W1** | PE-002 | **Excel UX completeness:** enum data-validation matrix (PLE-1.2) + OQ5 `_Meta`/PK protection | 3–5 d | Excel I/O ∥ tests |
| **PE-W2** | PE-003 | **Governance residuals:** quarantine report (PLE-2.3), post-import release golden (PLE-3.5), TL/archetype import gate (PLE-4.4), sim-visible provenance assert (PLE-5.3) | 4–7 d | Importer ∥ gate consume ∥ docs |
| **PE-W3** | PE-004 | Optional Unity presentation residual **or** honesty residual (Phase N screenshots deferred) | 1–3 d | Optional |
| **PE-W4** | PE-005 | Program gate + human ack **“Platform editor requirements complete”** | 1 d | Serial |

**Exit for “complete”:** PE-W0 + PE-W1 + PE-W2 + PE-W4 **required**. PE-W3 may honesty-defer Live Editor screenshots as Phase N without blocking close.

---

## PE-W0 — Integration hygiene (serial, docs-first)

**Goal:** One coherent status story before code.

### Tracks

| Track | Deliverable |
|-------|-------------|
| **W0-a** | Create epic + scope boundary + story shells |
| **W0-b** | Doc 21: Design Status → **Revised — implementation-aligned**; footer Implementation grade; no false AC checks |
| **W0-c** | Tracker/roadmap one-line pointer to this program (if active indexes need it) |

**Acceptance**

1. Epic + boundary on disk  
2. Doc 21 no longer claims pure “Draft” without shipping narrative  
3. Residual ACs listed as PE-W1/W2 targets  

**Out:** Code changes.

---

## PE-W1 — Excel validation matrix + protection (parallel)

**Goal:** Close PLE-1.2 and OQ5 as much as ClosedXML allows without breaking empty-diff goldens.

### Architecture notes

- Enum lists must be **data-driven** (shared constants already in `PlatformEmconEnums` pattern) — add `PlatformWorkbookEnumCatalog` with lists for Domain, MountType, LinkType, Role, ReviewState, ValueTier, Condition, Posture, WeaponType, etc. as present on sheets.  
- Apply list data-validation on export write only; importer already validates — do not double-block empty-diff.  
- Protect `_Meta` sheet and PK columns (PlatformId, SensorId, …) as read-only / locked where ClosedXML supports it; document limits.

### Parallel tracks

| Track | Domain | Owner files | Deliverable |
|-------|--------|-------------|-------------|
| **W1-a** | Enum catalog | `ProjectAegis.Data.Excel/PlatformWorkbookEnumCatalog.cs` (+ tests) | Canonical allowed values per column |
| **W1-b** | ClosedXML apply | `ClosedXmlPlatformWorkbookIo.Apply*Validation` | List validation on all known enum columns; expand beyond Emcon |
| **W1-c** | Protection | Same IO class | `_Meta` protected; PK columns locked (best-effort) |
| **W1-d** | Tests | `ClosedXmlValidationMetadataTests`, new `PlatformWorkbookEnumValidationTests` | Assert validation rules present after export; round-trip empty-diff still holds |
| **W1-e** | Docs | Doc 21 PLE-1.2 checkbox + OQ5 | Flip only with evidence |

**Serial:** W1-a before W1-b. W1-b ∥ W1-c after A. W1-d after B/C.

**Acceptance**

1. Export of Baltic fixture applies data-validation on ≥N enum columns (enumerate in test).  
2. Emcon Condition/Posture still valid; new columns covered.  
3. `_Meta` sheet not freely editable (or documented ClosedXML limitation).  
4. Unedited binary/canonical empty-diff tests still green.  
5. PLE-1.2 checked with evidence paths.

**Risk:** Over-strict validation lists that reject legitimate catalog values → build lists from export sample + known enums.

**GitNexus:** impact `ClosedXmlPlatformWorkbookIo` before edit.

---

## PE-W2 — Governance residuals (parallel)

**Goal:** Close PLE-2.3, 3.5, 4.4, 5.3 with **tests first**, reusing doc 06 machinery.

### Parallel tracks

| Track | AC | Deliverable | File ownership |
|-------|-----|-------------|----------------|
| **W2-a** | PLE-2.3 | Ensure orphan/FK failure produces quarantine-style report entry (or extend `PlatformImportPlan` with `QuarantineEntries`); test asserts never committed | `PlatformWorkbookImporter`, validator, quarantine APIs — **extend-only** gate |
| **W2-b** | PLE-3.5 | After successful `ApproveBatch` from Excel path, assert new snapshot/release row via existing `DbSnapshotStore` / `DbReleaseRecord` (or explicit hook in `PlatformWorkbookWriteService` if missing) + golden test | Write service + snapshot store |
| **W2-c** | PLE-4.4 | Import path rejects or quarantines black-project / below-TRL rows when scenario TL / archetype gate would exclude; unit tests with fixtures | Importer + `CatalogArchetypeGate` **consume only** |
| **W2-d** | PLE-5.3 | Test that non-`approved` edited provenance is not present in sim-visible export API used by catalog TL export filter (reuse `CatalogTlExportFilter` or equivalent) | Tests in Data.Tests; production hook only if gap confirmed |
| **W2-e** | Docs | Flip PLE checkboxes with evidence; Implementation Mapping residual cleared | Doc 21 only |

**Conflict rules**

| Hub | Rule |
|-----|------|
| `CatalogWriteGate` | **No** signature/path rewrites; only call existing `Propose*Batch` / `ApproveBatch` |
| `PlatformWorkbookImporter` | Single owner per wave if multiple tracks touch it — prefer W2-a owner; W2-c/d PR after or coordinate |
| `PlatformWorkbookWriteService` | W2-b primary |

**Acceptance**

1. Each of PLE-2.3, 3.5, 4.4, 5.3 has a green co-located test cited in doc 21.  
2. No silent commit of orphan/black-project rows.  
3. Full Data.Tests + Excel.Tests green; solution floor holds.  

**Honesty fallback:** If PLE-5.3 is purely doc-06 ownership, document **AC owned by DBI** and check with cross-ref only — do not invent a second export pipeline.

---

## PE-W3 — Presentation residual (optional / honesty)

| Option | When |
|--------|------|
| **A — Honesty defer** | Keep Phase N “Live Editor screenshots” as residual; program closes without PNG pack |
| **B — Minimal evidence** | Add headless proxy checklist + existing UA Platform tests as AC evidence only |

**Recommendation:** **A** — matches mission-editor close pattern; presentation is not a headless product AC.

---

## PE-W4 — Program gate + human ack (serial)

1. Full verification:

```bash
dotnet build ProjectAegis.sln          # 0 errors
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Excel.Tests/...
dotnet test ... --filter PlayModeSmokeHarnessTests
dotnet test ... --filter ReplayGolden
# hash + ZERO CatalogWriteGate hotpath rewrites + ZERO DelegationBridge
```

2. Scope boundary + epic Complete.  
3. Tracker row 21 note: program-complete Partial+ headless Excel path; residuals Phase N if any.  
4. Human ack phrase: **`Platform editor requirements complete`**.  
5. GitNexus reindex after land.

---

## Parallel agent dispatch model

```
Coordinator
  PE-W0 docs (serial)
  PE-W1:
    worktree pe-w1-enums     → W1-a
    worktree pe-w1-closedxml → W1-b+c (same owner preferred)
    worktree pe-w1-tests     → W1-d (after B)
  PE-W2:
    worktree pe-w2-quarantine → W2-a (Importer owner)
    worktree pe-w2-release    → W2-b
    worktree pe-w2-trl        → W2-c (coordinate Importer)
    worktree pe-w2-provenance → W2-d (tests-first)
  PE-W4 serial gate
```

**Agent prompt requirements (each track)**

1. Worktree path + branch  
2. Exact files owned  
3. TDD: failing test → implement → green  
4. Invariants + GitNexus impact on hubs  
5. Report DONE + SHA + test filters  

**Do not parallel-edit:** `CatalogWriteGate.cs` write paths; `DelegationBridge.cs`.

---

## Verification (every wave exit)

```bash
dotnet build ProjectAegis.sln
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Platform
dotnet test src/ProjectAegis.Data.Excel.Tests/ProjectAegis.Data.Excel.Tests.csproj
dotnet test ProjectAegis.sln -v minimal   # floor ≥ prior
# ReplayGolden 6/6, PlayModeSmoke ≥18, hash grep, gate-extend-only audit
```

---

## Risk register

| Risk | Mitigation |
|------|------------|
| Enum list incompleteness breaks real imports | Generate lists from exported Baltic fixture + existing enum types |
| PLE-3.5 requires new release pipeline | Prefer assert existing snapshot/release after ApproveBatch; extend-only |
| PLE-5.3 scope creep into sim export | Test existing TL export filter; cross-ref doc 06 if already owned |
| CatalogWriteGate CRITICAL blast | Never alter write paths; only new Propose overloads if absolutely required (prefer none) |
| ClosedXML protection incomplete | Document OQ5 best-effort; still ship enum validation |

---

## Success definition

Platform editor requirements **complete** means:

1. Doc 21 ACs **PLE-1.* … PLE-6.*** are **[x]** or explicitly residual with Phase N ownership (only screenshots allowed residual).  
2. Headless Excel round-trip + write-gate governance remain green.  
3. Enum validation + governance goldens landed.  
4. Design Status honest; epic closed with human ack.  
5. Invariants held.

---

## Recommended execution order

1. Approve this plan → land PE-W0 packaging on `main`  
2. PE-W1 (enum validation + protection) with parallel worktrees  
3. PE-W2 governance residuals (coordinate Importer owner)  
4. PE-W3 honesty residual for screenshots  
5. PE-W4 gate + **“Platform editor requirements complete”**  

---

## Next after plan approval

1. Create `production/epics/platform-editor-completion/` + scope boundary  
2. Spawn PE-W1 parallel agents  
3. Optional: writing-plans bite-sized TDD for PE-W1 only before code  
