# Slice G1 — Unity requirements and evidence baseline

**Issue:** [DRG-234](https://linear.app/drgamtd-workspace/issue/DRG-234) · Parent [DRG-233](https://linear.app/drgamtd-workspace/issue/DRG-233)  
**Plan:** [2026-09-08-slice-g-unity-ux-ui.md](../plans/2026-09-08-slice-g-unity-ux-ui.md)  
**Notion contract:** [Slice G acceptance](https://app.notion.com/p/3d5f7cb4e4df812186c2db1a397b17d1)  
**Traceability owners (do not duplicate):** [DRG-187](https://linear.app/drgamtd-workspace/issue/DRG-187), [DRG-188](https://linear.app/drgamtd-workspace/issue/DRG-188)  
**Baseline date:** 2026-09-17 (orchestrator tick 0 coordinator fallback)  
**Checkout:** `c7810de` (`main`, behind `origin/main` by 11 at board write)  
**Presentation ADRs:** ADR-010 §2–3, ADR-007, ADR-001 (never Git ADR-018)

## Evidence class legend

| Class | Meaning |
|-------|---------|
| source-only | Requirement or code exists; no automated/Editor proof cited here |
| headless-tested | Proven by `dotnet test` / proxy harness with dated run |
| Editor-tested | Unity Play Mode / MCP capture with dated package |
| owner-accepted | Explicit human UX signoff recorded |
| unverified | Required evidence missing or stale; reason given |

Linear **delivery status** is separate from evidence class.

## Tooling constraints this baseline

| Tool | State | Effect on rows |
|------|-------|----------------|
| Unity MCP `:8080` | DOWN (connection timeout 2026-09-17) | No new Editor captures; reuse 2026-09-08 Slice C package only |
| GitNexus MCP | Ladybug file v42 vs MCP storage v40 | No MCP `impact`/`detect_changes`; CLI impact usable (see DRG-197 note) |
| Fresh suite this tick | **Not re-run** | Headless numbers below are **prior recorded** (Slice C package / Sep 8 dashboard), not 2026-09-17 measurements |

---

## Requirement → evidence matrix (Slice G surfaces)

### Doc 20 — Command and Control UI

| Req ID | Criterion (short) | Unity surface | Projection / command boundary | Test | Scenario | Screenshot / log | Commit / local delta | Linear owner | Evidence class | Owner acceptance |
|--------|-------------------|---------------|-------------------------------|------|----------|------------------|----------------------|--------------|----------------|------------------|
| CMD-01…03 | Layout zones; UI Toolkit; ADR-010 bind | C2 hosts / presentation | Projections in; facades out | PlayModeSmokeHarness; UA tests | DelegationSmoke / proxy | Slice C package | HEAD + prior Slice C uncommitted layout | DRG-233 family / C2 owners | headless-tested + Editor-tested (prior) | **pending** |
| CMD-07 | Selection sync map↔OOB↔detail | CombatMapView, detail hosts | Selection projection only | CombatMapViewTests (1) | Baltic smoke | historical-review / live-advice PNGs | `37e6ba6d` + layout styles | Slice C / G2 | Editor-tested (callback, not hardware mouse) | pending |
| CMD-11 / review | Intent preview / history review | CommandReviewView | History must not authorize live | CommandReviewViewTests (1); CoordinationEndToEndAcceptanceTests | pause + historical t=52.417 | historical-review.png | layout shrink fix uncommitted | DRG-185 / G2 | Editor-tested + headless-tested | pending |
| CMD-12 | A11y scale / color / keyboard | C2AccessibilitySettings + hosts | Presentation only | DRG-170 In Review | — | missing fresh 2026-09-17 | — | DRG-170 / 177 / 205 | unverified (this tick); prior Partial | pending |
| CMD-17 | Unknown-due-to-comms display | contact/detail hosts | Doc 19 degradation → UI state | — | — | — | — | G2 defect route | source-only (Open in doc 20) | unverified |
| CMD-29 | Contact detail ≠ own-unit | ContactDetailPanelHost | Distinct projections | Slice C live advice checks | stale c1 | live-advice.png | prior package | G2 | Editor-tested (partial vs full CMD-29 AC) | pending |
| Alerting / auto-pause | Interrupt + return-to-work | WatchAttentionQueue / toast hosts | Clock interrupt presentation | cited in audit B-10 | — | — | S115/116 claims | DRG-205 / G2 | unverified this tick (no Editor) | pending |
| CMD-31…39 | Allocated IDs outside doc 20 | various ops hosts | Must not invent §Alerting | — | — | — | drafts CMD-38/39 pending owner | audit D / G1 | **source-only / PROPOSED** | **not approved** |
| CMD-40…43 | Proposed NFR/approval UX | — | — | — | — | — | — | audit D | **PROPOSED only** | **not approved** |
| Positive group/BDA live | Hold→BDA→Withdraw in one live scene | group hosts | Facades only | CoordinationEndToEndAcceptanceTests (headless) | smoke scene has **no** task group | n/a live | prior Slice C limit | DRG-185 / 175 | headless-tested; **live Unity unverified** | pending |
| Live coverage geometry | Authored coverage in scenario | CoverageMapView | Immutable DTOs | CoverageMapViewTests (4) | fixture JSON only | coverage-fixture-*.png | labeled fixture | DRG-192 | Editor-tested **fixture**; live coverage **unverified** | pending |

### Doc 11 — Agentic Mission Editor

| Req ID | Criterion (short) | Unity surface | Boundary | Test | Scenario | Evidence | Commit / delta | Linear owner | Evidence class | Owner acceptance |
|--------|-------------------|---------------|----------|------|----------|----------|----------------|--------------|----------------|------------------|
| AME shell / library / live-edit | Load, navigate, map select, validate, save/export | ScenarioEditorShellHost, ScenarioLibraryPanelHost, LiveEditPanelHost, ScenarioMapAuthoringWindow | Authoring ≠ sim; editorState derived-only | UA authoring tests (exist; not re-run this tick) | — | missing Editor screenshots this tick | audit D-10 notes hosts exist | G3 / AME owners | source-only + prior headless Partial+ claims | pending |
| AME-2.4 / 6.5 | editorState never sim input; save≠export gate | CLI + hosts | Validation Engine | schema / export reject tests | — | — | shipped headless claims in doc 11 | G3 | headless-tested (doc-claimed; not remeasured) | pending |
| AME-5.5 debugger chrome | Live Unity event debugger | residual Unity chrome | order-log projection | EventDebuggerTests headless | — | Unity chrome missing | ME-W2 note | G3 | headless-tested; Editor **unverified** | pending |
| AME P0 gaps (audit D-10) | AME-2.1/2.2/5.2–5.4/7.4 | mixed | — | — | — | — | audit 2026-09-02 | G3 / audit D | unverified / product gaps TBD | pending |
| Export/publish gating | Reject bad export | publish path | human gates | fixed historically `3cf67cb4` claim | — | — | D-10 | G3 | unverified this tick | pending |

### Doc 21 — Platform Editor

| Req ID | Criterion (short) | Unity surface | Boundary | Test | Scenario | Evidence | Commit / delta | Linear owner | Evidence class | Owner acceptance |
|--------|-------------------|---------------|----------|------|----------|----------|----------------|--------------|----------------|------------------|
| PLE Excel round-trip | Export/import/diff via write gate | PlatformEditorShellHost (viewer) + Excel path | **ADR-011 Excel-primary**; ApproveBatch human | PlatformWorkbook* / Import panel tests | workbook fixtures | headless claims in doc 21 PE-W0–W4 COMPLETE | — | G3 / PE owners | headless-tested (doc); Editor browse/import UX **unverified** this tick | pending |
| PLE-3.1 no bypass | No write-gate bypass from UI | Import host | IWriteGate only | PlatformImportPanelTests patterns | — | — | — | G3 | headless-tested (pattern) | pending |
| Phase N screenshots | In-engine viewer polish | PlatformEditorShellHost | read-only viewer | — | — | **missing** | residual | G3 | unverified | pending |

---

## Audit reassessment (vs 2026-09-02 findings)

| Finding | Sep 2 claim | 2026-09-17 reassessment | Disposition |
|---------|-------------|-------------------------|------------|
| B-10 | Live Play Mode picture / CMD-31…39 outside doc | Slice C Editor package proves some live/history/stale/coverage-fixture behaviors; CMD-31…39 still **absent from doc 20** (grep 2026-09-17). Auto-pause/toast not re-captured (Unity MCP down). | Keep open; G2 must exercise alerting with Editor when `:8080` live. **PROPOSED:** append Alerting section + CMD IDs to doc 20. |
| B-15 (presentation portion) | C2 accessibility scale, Cesium, etc. without req coverage | CMD-12 Partial; a11y owned by DRG-170 In Review. No new measurement this tick. | Route to DRG-170/177/205; do not close via G. |
| D-10 | Doc 11 Unity status wrong; P0 AME gaps | Hosts cited in audit still present in plan read-list; full re-baseline of verb/MCP counts **not performed** this tick (no Editor). | G3 must reconcile claimed-missing vs tree; flag unimplemented P0s explicitly. |
| D-15 | ≥15 CMD Open though shipped; CMD-31…39 undefined; false evidence paragraphs | Doc 20 still lists many Open/Partial; CMD-31…39 still undefined in doc body. Slice C evidence supersedes some “no Editor evidence” claims for review/coverage. | **PROPOSED** doc 20 status re-baseline after G2; do not mark owner-accepted. |

---

## CMD-31…43 reconciliation

| ID band | Status in repo requirements | Notes |
|---------|----------------------------|-------|
| CMD-01…30 | Documented in doc 20 with Shipped/Partial/Open | Use as G2 checklist seeds |
| CMD-31…39 | **Not in doc 20** (confirmed grep) | Appear in audit + code comments / drafts; **PROPOSED** append only after owner-approved definitions |
| CMD-38/39 drafts | `docs/superpowers/specs/2026-08-17-cmd-38/39-*` pending owner approval | Preserve pending decision; not approved |
| CMD-40…43 | Audit-proposed NFR/approval UX | **PROPOSED only** until definitions + approval |

---

## Prior recorded gates (do not treat as 2026-09-17 re-run)

From [Slice C Play Mode acceptance](2026-09-08-slice-c-playmode-acceptance.md) / Sep 8 dashboard:

- Build 0e/0w; solution tests **3,216**; PlayModeSmoke **24/24**; ReplayGolden **6/6**; hash `17144800277401907079`
- Technical Editor smoke **PASS**; **owner acceptance pending**
- Positive live group/BDA and live authored coverage **explicitly unverified**

---

## PROPOSED requirement amendments (not approved)

1. **Doc 20:** Add §Alerting and Interruption; allocate/define CMD-31…39 from code+drafts; separate approval UX + NFR block (CMD-40…43 proposals).
2. **Doc 20 statuses:** Re-baseline Open vs Shipped using G2 evidence; never equate headless PASS with visual acceptance.
3. **Doc 11:** Correct Unity surface inventory vs residual P0 AME gaps after G3 tree walk; keep save≠export and Excel/ADR-011 boundaries.
4. **Doc 21:** Keep Excel-primary + human ApproveBatch; Phase N screenshot residual remains open until Editor captures.

Canonical amendments land only through audit D owner after G evidence — not by this file alone.

---

## G2 / G3 unlock

| Track | May start evidence gathering? | Caveats |
|-------|-------------------------------|---------|
| **G2 (DRG-235)** | **YES** for planning + headless/read of hosts | Editor acceptance needs Unity MCP or local Editor; route defects to DRG-170/177/205/185/175/192 |
| **G3 (DRG-236)** | **YES** for planning + headless/read of editor hosts | HTML review requires actual screenshots; Unity MCP down → mark visual rows unverified |
| Symbol edits | **NO** until GitNexus MCP Ladybug mismatch resolved **or** CLI impact + human-recorded results accepted | See `production/agentic/scratch/drg-197-gitnexus-recovery-2026-09-17.md` |

---

## Explicit non-claims

- This file does **not** close DRG-234 in Linear.
- Planning authorization ≠ owner visual signoff.
- Headless-only rows are never labeled owner-accepted.
- Fixture coverage ≠ live scenario coverage.
- Missing-group proof ≠ positive group/BDA live flow.

---

## 2026-10-02 reconciliation addendum (DRG-234 per-criterion contract)

The tables above are kept unchanged as the **2026-09-17 historical baseline**. This addendum responds to the [2026-10-01 G1 review](2026-10-01-g1-harness-review.md). It replaces grouped placeholders with one row per criterion, each with a concrete test, a delivery owner and an acceptance owner. Missing evidence stays marked as missing.

**Revision:** `origin/main` at `366e3f90` (2026-10-02). No suite was re-run for this addendum. Test names were confirmed to exist in the tree at that revision; results come from the cited closeouts or the latest green CI run on main.
**Acceptance owner (all rows):** drg amtd, the repository owner and only human acceptance authority. This matches `owner` in `production/planning-reconciliation-2026-09-30.json`. Owner acceptance stays **pending** on every row until the owner records it. DRG-208 is the human-only Play Mode gate.
**Delivery owner:** the Linear issue accountable for delivering the row. Agent lanes deliver; they never accept.

### Doc 20 rows (CMD-01–30 defined in doc)

| Req ID | Test (concrete) | Test layer / result | Scenario | Artifact | Delivery owner | Acceptance owner | Definition maturity | Missing evidence |
|--------|-----------------|---------------------|----------|----------|----------------|------------------|---------------------|------------------|
| CMD-01…03 | `PlayModeSmokeHarnessTests` | headless proxy; 25/25 (2026-10-01 G1 review) | DelegationSmoke proxy | Slice C package (2026-09-08) | DRG-233 | drg amtd | defined (Doc 20) | fresh Editor capture |
| CMD-07 | `CombatMapViewTests` (Unity), `UiIaSelectionSyncOracleTests` | Editor-tested (prior) + headless | Baltic smoke | historical-review / live-advice PNGs | DRG-235 (G2) | drg amtd | defined | hardware-mouse selection |
| CMD-11 | `CommandReviewViewTests` (Unity), `CoordinationEndToEndAcceptanceTests` | Editor-tested (prior) + headless | pause + historical t=52.417 | historical-review.png | DRG-185 | drg amtd | defined | layout shrink fix to be committed |
| CMD-12 | `C2AccessibilitySettingsTests` | headless; Editor unverified | — | none fresh | DRG-170 | drg amtd | defined (Partial) | Editor scale/contrast/keyboard capture |
| CMD-17 | `UnitCommsDisplayTests` (`UNKNOWN (Out of comms)`) | headless (CMD-31–37 closeout, 2026-08-01) | — | none | DRG-235 (G2) | drg amtd | defined (Open in doc) | Editor capture; Doc 20 status refresh |
| CMD-29 | `ContactDetailProjectionTests` | headless + Editor partial (Slice C) | stale c1 | live-advice.png | DRG-235 (G2) | drg amtd | defined | full CMD-29 AC in Editor |
| Alerting / auto-pause | `WatchAttentionQueueTests`, `AttentionToastApplyStateTests`, `AttentionToastHostContractTests` | headless; Editor unverified | — | none | DRG-205 | drg amtd | **undefined in Doc 20** (CMD-39 draft) | definition approval + Editor capture |
| Positive group/BDA live | `CoordinationEndToEndAcceptanceTests` | headless only | smoke scene has no task group | n/a | DRG-185 / DRG-175 | drg amtd | defined | live Unity flow |
| Live coverage geometry | `CoverageMapViewTests` (Unity) | Editor-tested **fixture** only | fixture JSON | coverage-fixture-*.png | DRG-192 | drg amtd | defined | authored live coverage |

### CMD-31–43: delivery evidence and definitions tracked separately

Doc 20 at `366e3f90` defines CMD-01–30 only. Delivered code and a missing canonical definition are recorded as separate facts. Delivered code does not count as an approved requirement.

| ID | Delivery evidence | Test | Delivery owner | Canonical definition | Acceptance owner |
|----|-------------------|------|----------------|----------------------|------------------|
| CMD-31 | `C2CommandIssuance` + `C2PlayerCommandBridge` ([CMD-31–37 closeout](../../../production/agentic/sprint-ui-maturity-cmd31-37-closeout-2026-08-01.md), lane A `c26bcad`) | `C2CommandIssuanceTests` | lane A (closeout) | **missing in Doc 20** | drg amtd, pending |
| CMD-32 | `DatalinkPictureProjection` (lane B `2206fc9`) | `DatalinkPictureProjectionTests` | lane B | **missing** | drg amtd, pending |
| CMD-33 | doctrine map overlay ([Wave 2 closeout](../../../production/agentic/sprint-ui-maturity-wave2-closeout-2026-08-01.md), `w2-map`) | `DoctrineMapOverlayProjectionTests` | Wave 2 lane M | **missing** | drg amtd, pending |
| CMD-34 | `TacticalOverlayProjection` envelope rings (lane B) | `TacticalOverlayProjectionTests` | lane B | **missing** | drg amtd, pending |
| CMD-35 | live-edit findings contract + Commit gate (Wave 2 `w2-liveedit`) | `LiveEditApplyStateTests` | Wave 2 lane L | **missing** | drg amtd, pending |
| CMD-36 | rich-bind perf bench p95 ≪ 100 ms (Wave 2 `w2-perf`) | `CombatPanelPerfBenchTests` (mapping to the closeout bench not re-verified) | Wave 2 lane P | **missing** | drg amtd, pending |
| CMD-37 | `AgentRosterProjection` + `AgentRosterPanelHost` (lane D `071acb0`) | `AgentRosterProjectionTests` | lane D | **missing** | drg amtd, pending |
| CMD-38 | partial implementation; [draft](../specs/2026-08-17-cmd-38-kinematic-map-picture-draft.md) | — | DRG-235 (G2) | **draft, pending owner authorization to append** | drg amtd, pending |
| CMD-39 | partial implementation; [draft](../specs/2026-08-17-cmd-39-attention-toast-clock-interrupt-draft.md) | attention-toast tests above | DRG-205 | **draft, pending owner authorization to append** | drg amtd, pending |
| CMD-40…43 | none | — | audit D | **audit proposals only** | drg amtd, pending |

### Doc 11 / Doc 21 test anchors (replacing "tests exist, not re-run")

| Row | Concrete tests | Delivery owner | Acceptance owner | Missing evidence |
|-----|----------------|----------------|------------------|------------------|
| AME shell / library / live-edit | `ScenarioEditorShellProjectionTests`, `ScenarioLibraryProjectionTests`, `ScenarioLibraryApplyStateTests`, `LiveEditApplyStateTests`, `ScenarioMapAuthoringHostFlowSmokeTests` | DRG-236 (G3) | drg amtd | Editor screenshots |
| AME-2.4 / 6.5 save ≠ export | `SaveVsExportGateTests` | DRG-236 (G3) | drg amtd | named package/schema/event/migration criteria refresh |
| AME-5.5 debugger | `EventDebuggerTests` | DRG-236 (G3) | drg amtd | Unity debugger chrome |
| PLE Excel round-trip (ADR-011) | `PlatformWorkbookWriteBridgeTests`, `PlatformWorkbookBinaryGoldenTests`, `PlatformWorkbookGovernanceAdversarialTests` | DRG-236 (G3) | drg amtd (human ApproveBatch) | Editor browse/import UX |
| PLE-3.1 no bypass | `PlatformImportPanelTests` | DRG-236 (G3) | drg amtd | — |
| Phase N screenshots | — | DRG-236 (G3) | drg amtd | **missing** |

This addendum does not close DRG-234, approve any requirement text or record owner acceptance.
