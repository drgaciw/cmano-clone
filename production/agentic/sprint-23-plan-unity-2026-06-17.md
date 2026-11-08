# Sprint 23 — Unity / Presentation Plan

**Date:** 2026-06-17  
**Domain:** Unity presentation (Req 13, Req 20, ADR-007, ADR-010)  
**Predecessor:** Sprint 22 SHIPPED — `DoctrineInheritancePanelHost` headless proxy green; Cesium Phase B spike **PROCEED**; Unity Editor visual gates deferred  
**Author:** Sprint 23 Unity/Presentation Planning agent  
**Out of scope:** Data/platform stories, `sprint-status.yaml`, kickoff yaml (program agent owns)

---

## Sprint 23 Unity goal

Close Sprint 22 presentation carryover by wiring **Doctrine Inheritance Panel** into `DelegationSmoke`, refreshing **PlayMode/C2 regression gates**, and advancing **Cesium globe production polish** (ADR-007 Phase B) without touching `DelegationBridge.cs`.

---

## Context summary (carryover)

| Item | Sprint 22 / retro state | Sprint 23 action |
|------|-------------------------|------------------|
| `DoctrineInheritancePanelHost.cs` | Host implemented; binds via `DelegationBridgeHost.RefreshDoctrineInheritance()` + `TrySetDoctrineOverride()` (ADR-010 seam) | **S23-U01** — UXML/USS + scene wiring + Editor sign-off |
| Doctrine UI assets | **Missing** — no `Assets/UI/DoctrineInheritance/` UXML/USS; host expects `doctrine-root`, `roe-dropdown`, `apply-override-button` | **S23-U01** |
| `DelegationSmokeSceneBuilder` | Wires TopBar, LeftDrawer, MapPlaceholder, RightUnit, MessageLog — **no doctrine host** | **S23-U01** |
| Headless doctrine tests | `DoctrineOverrideCommandTests` (4) + projection/binder (12) + PlayMode doctrine row → **17 PASS** | Regression gate only; no new headless work required |
| Cesium Phase B checklist | **PROCEED** verdict; depth/occlusion item **unchecked**; evidence screenshots are placeholders (`cesium-s20-local-editor-evidence.md`) | **S23-U02** |
| `CesiumGlobeBridge.GetCurrentPositions()` | Baltic demo geo hardcoded; `MapSymbolEntry` has no `App6Sidc` yet | **S23-U02** polish; **S23-U04** spike |
| C2 manual sign-off | 13/13 PASS @ `7401fac` (pre-doctrine panel) | **S23-U03** batch refresh; **S23-U05** full regression |
| Retro action #6 | Unity Editor doctrine panel visual — closes Req 13 UI gate | **S23-U01** |
| Retro action #7 | Full `ProjectAegis.sln` test gate post-rebase | Unity runs scoped PlayMode filter; full sln owned by data/devops |

### Architecture constraints (all stories)

| Rule | Enforcement |
|------|-------------|
| **ZERO touch `DelegationBridge.cs`** | Doctrine writes route `DoctrineInheritancePanelHost` → `DelegationBridgeHost.TrySetDoctrineOverride` → `DoctrineOverrideCommand.TryApply` per ADR-010 |
| Presentation-only map/globe | No sim mutation from clicks; `C2PresentationController` selection path only (ADR-007) |
| Ion token | Cesium ion secret in Editor Inspector only — never committed |
| GitNexus before symbol edit | `gitnexus impact <symbol> --direction upstream --repo cmano-clone` on `DelegationBridgeHost`, `CesiumGlobeBridge`, scene builder |

---

## Must-have stories (critical path)

### S23-U01 — Doctrine Inheritance Panel: assets + DelegationSmoke wiring + Editor sign-off

| Field | Value |
|-------|-------|
| **Estimate** | 2 days |
| **Owner** | team-unity / c-sharp-engineer |
| **Req trace** | Req 13 (P0 side/unit doctrine UI), Req 20 §4.1 (unit context doctrine), ADR-010 §2–3 |
| **Dependencies** | Sprint 22 doctrine headless stack merged/rebased (`a95e06f` host conflict resolved); retro pre-work: commit + `git pull --rebase origin main` |
| **GitNexus impact-check** | `DoctrineInheritancePanelHost`, `DelegationBridgeHost`, `DelegationSmokeSceneBuilder` — expect LOW; **CRITICAL zero-touch** on `DelegationBridge` |

**Problem:** `DoctrineInheritancePanelHost` exists and headless tests pass, but there are no UI Toolkit assets and the panel is not in `DelegationSmoke.unity`. Sprint 22 sign-off condition C4 deferred Editor visual gate.

**Approach:**

1. Create `Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uxml` + `.uss` matching host element names (`doctrine-root`, `unit-id-label`, `roe-label`, `salvo-label`, `source-label`, `override-label`, `roe-dropdown`, `apply-override-button`).
2. Extend `DelegationSmokeSceneBuilder` to add `DoctrineInheritancePanelHost` (same `CreatePanelHost<T>` pattern as other C2 panels).
3. Wire `bridgeHost` reference; panel reads projection only; override dispatches via `TrySetDoctrineOverride` (no direct orchestrator access from UI).
4. Local Editor PlayMode: select friendly unit → panel shows ROE/SALVO/SOURCE lines; change ROE dropdown → Apply → labels refresh; verify `CanOverride` disables button when hostile selected.
5. Capture evidence per `sprint-18-c2-signoff-runbook` pattern.

**Acceptance criteria:**

- [ ] UXML/USS exist and compile; all seven named elements resolve (`_wired == true` in host).
- [ ] `DelegationSmokeSceneBuilder` creates doctrine host; rebuilt scene opens without missing-reference errors.
- [ ] PlayMode: friendly unit selected → panel shows non-placeholder ROE/SALVO/SOURCE; hostile/contact selection shows override unavailable.
- [ ] Apply override updates panel lines within one refresh cycle; no console errors.
- [ ] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`.
- [ ] Headless regression unchanged: `dotnet test ... --filter "Doctrine|PlayModeSmoke"` → ≥13/13 PASS.

**PlayMode / manual evidence paths:**

- Automated: `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~Doctrine|FullyQualifiedName~PlayModeSmoke" -v minimal`
- Editor scene: `unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity` (rebuild via **Project Aegis → Build DelegationSmoke Scene (classify QA)**)
- Manual evidence (new): `production/qa/doctrine-inheritance-s23-editor-evidence.md` — screenshots + console excerpt + tester/date
- Runbook pattern: `production/qa/sprint-18-c2-signoff-runbook-2026-06-04.md`

**Manual QA session:** **1 × ~2 h** (Editor PlayMode doctrine override walk)

---

### S23-U02 — Cesium globe production polish (S21 carryover)

| Field | Value |
|-------|-------|
| **Estimate** | 1.5 days |
| **Owner** | team-unity |
| **Req trace** | Req 20 §globe/map P0, ADR-007 Phase B |
| **Dependencies** | Cesium package pin resolved in local Editor (`docs/engineering/cesium-unity-package-pin.md`); ion token available locally |
| **GitNexus impact-check** | `CesiumGlobeBridge`, `CesiumGlobeHost`, `MapPlaceholderPanelHost` — LOW presentation-only |

**Problem:** Phase B spike **PROCEED** but checklist item **depth/occlusion** remains open; `production/qa/cesium-s20-local-editor-evidence.md` uses PASS-assumption placeholders without attached screenshots; `GetCurrentPositions()` still returns fixed Baltic demo coordinates rather than deriving count/affiliation from live `LastMapSymbols`.

**Approach:**

1. Improve `GetCurrentPositions()` to iterate `mapHost` binder symbols (count + hostile flag) while retaining deterministic Baltic geo mapping until sim publishes lat/lon.
2. Replace primitive spheres with scaled billboards or labeled markers; document depth/occlusion behavior in checklist.
3. Attach real screenshots + Profiler capture to evidence file (fill placeholders in `cesium-s20-local-editor-evidence.md` or add `cesium-s23-production-polish-evidence.md`).
4. Mark checklist rendering + depth items in `docs/engineering/cesium-phase-b-spike-checklist.md`.
5. Keep `useGlobeMap = false` default on `DelegationSmoke`; polish validated in `CesiumSpike.unity` per `CESIUM-SPIKE-SETUP.md`.

**Acceptance criteria:**

- [ ] `CesiumSpike.unity` PlayMode: globe loads, ≥1 friendly + ≥1 hostile anchored, no console errors.
- [ ] Selection click on globe entity syncs OOB highlight (same `C2PresentationController` path as Toolkit map).
- [ ] Depth/occlusion note recorded (PASS or documented failure + mitigation).
- [ ] FPS note captured (~60 FPS empty scene baseline per S20 AC).
- [ ] Headless gates unchanged: `PlayModeSmokeHarnessTests` ≥8/8 PASS; `MapPanelBinderTests` green.
- [ ] **ZERO edits** to `DelegationBridge.cs`.

**PlayMode / manual evidence paths:**

- Scene setup: `unity/ProjectAegis/Assets/Scenes/CESIUM-SPIKE-SETUP.md`
- Checklist: `docs/engineering/cesium-phase-b-spike-checklist.md`
- Evidence: `production/qa/cesium-s20-local-editor-evidence.md` (append) or `production/qa/cesium-s23-production-polish-evidence.md`
- ADR: `docs/architecture/adr-007-c2-map-presentation.md`

**Manual QA session:** **1 × ~1.5 h** (CesiumSpike PlayMode + profiler + selection)

---

### S23-U03 — Post-S22 PlayMode smoke + C2 batch regression gate

| Field | Value |
|-------|-------|
| **Estimate** | 1 day |
| **Owner** | team-unity / qa-tester |
| **Req trace** | Req 20, PI-006 headless proxy + Editor batch gate |
| **Dependencies** | S23-U01 scene rebuild (doctrine host must not break batch runner); Unity 6000.3.14f1 installed locally |
| **GitNexus impact-check** | `C2PlayModeSignoffBatchRunner`, `DelegationSmokeSceneBuilder` — LOW |

**Problem:** Sprint 22 added doctrine PlayMode row and potentially changed `DelegationSmoke` composition. Last full batch sign-off @ `7401fac` predates doctrine panel. Retro recommends full test gate post-rebase.

**Approach:**

1. Run headless pre-check: `PlayModeSmokeHarnessTests` full filter + doctrine row.
2. Run batch PlayMode: `pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario comms` and `-Scenario classify`.
3. Confirm check 1 (no console errors) still PASS with doctrine host present.
4. Publish log + summary to `production/qa/sprint-23-c2-playmode-regression-2026-06-17.md`.

**Acceptance criteria:**

- [ ] `dotnet test ... --filter PlayModeSmokeHarnessTests` → all rows PASS (baseline 8/8 minimum; 9/9 if doctrine row counted separately).
- [ ] `Invoke-C2PlayModeSignoffBatch.ps1` comms + classify → exit 0; `unity-c2-playmode-signoff.log` clean.
- [ ] Rebuilt `DelegationSmoke.unity` is the scene under test (not stale binary).
- [ ] **ZERO edits** to `DelegationBridge.cs`.

**PlayMode / manual evidence paths:**

- Headless: `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests -v minimal`
- Batch: `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1`
- Checklist reference: `production/qa/c2-manual-signoff-2026-06-02.md`
- Setup: `unity/ProjectAegis/PLAYMODE-SMOKE.md`
- Evidence (new): `production/qa/sprint-23-c2-playmode-regression-2026-06-17.md`

**Manual QA session:** **1 × ~1 h** (batch runner + log review; human only if batch fails)

---

## Should-have stories

### S23-U04 — APP-6 symbology Phase C spike (ADR-007)

| Field | Value |
|-------|-------|
| **Estimate** | 2 days |
| **Owner** | team-unity + c-sharp-engineer (projection DTO) |
| **Req trace** | Req 20 §4.3 (NATO/APP-6 P0), ADR-007 Phase C, Req 01 accessibility (shape-coded symbology) |
| **Dependencies** | **Headless (soft):** optional `MapSymbolEntry.App6Sidc` field + `MapPanelBinder` test — can land in `ProjectAegis.Delegation` without `DelegationBridge` touch; spike may use hardcoded SIDC table in Unity-only atlas |
| **GitNexus impact-check** | `MapSymbolEntry`, `MapPanelBinder`, `MapPlaceholderPanelHost`, `CesiumGlobeBridge` |

**Approach:**

1. Spike data-driven SIDC → sprite/atlas lookup (1 friendly air + 1 hostile surface minimum).
2. Render APP-6 glyph on Toolkit map placeholder **or** Cesium billboard (pick one surface for spike).
3. Document Phase C scope vs MVP in `design/ux/c2-map-placeholder.md` §Symbology.
4. Headless test: binder emits stable SIDC string per affiliation/domain stub.

**Acceptance criteria:**

- [ ] Two symbol types render with distinct APP-6 shapes (not color-alone).
- [ ] Spike ADR-007 Phase C note appended to checklist or new `docs/engineering/app6-symbology-phase-c-spike.md`.
- [ ] Existing `MapPanelBinderTests` + PlayMode smoke green (extend-only).
- [ ] **ZERO edits** to `DelegationBridge.cs`.

**PlayMode / manual evidence paths:**

- Headless: `src/ProjectAegis.Delegation.Tests/Projection/MapPanelBinderTests.cs` (extend)
- Editor: `DelegationSmoke.unity` map zone screenshot in `production/qa/app6-s23-spike-evidence.md`

**Manual QA session:** **0.5 × ~1 h** (visual compare ■/◆ vs APP-6 icons)

---

### S23-U05 — C2 manual sign-off refresh (doctrine + comms/classify)

| Field | Value |
|-------|-------|
| **Estimate** | 1 day |
| **Owner** | qa-tester / team-unity |
| **Req trace** | Req 20, S19-01 carryover runbook |
| **Dependencies** | S23-U01 complete (doctrine panel in scene); S23-U03 batch gate green |

**Approach:**

1. Re-run checks 1–13 from `c2-manual-signoff-2026-06-02.md` on post-S22 build.
2. Add **check 14 (new):** Doctrine panel override round-trip on friendly unit in Editor PlayMode.
3. Update checklist header SHA + verdict row.

**Acceptance criteria:**

- [ ] Checks 1–13 remain PASS (batch or headless proxy where applicable).
- [ ] Check 14 doctrine PASS with Editor evidence link.
- [ ] Verdict recorded in checklist file.

**PlayMode / manual evidence paths:**

- `production/qa/c2-manual-signoff-2026-06-02.md` (update)
- `production/qa/sprint-18-c2-signoff-runbook-2026-06-04.md`
- `production/qa/doctrine-inheritance-s23-editor-evidence.md`

**Manual QA session:** **1 × ~2 h** (full C2 walk + doctrine check 14)

---

### S23-U06 — `useGlobeMap` integration variant in DelegationSmoke (behind flag)

| Field | Value |
|-------|-------|
| **Estimate** | 1 day |
| **Owner** | team-unity |
| **Req trace** | ADR-007 Phase B integration |
| **Dependencies** | S23-U02 polish complete; Cesium define + ion token in Editor |

**Approach:** Optional second scene build menu item or `useGlobeMap=true` builder path that swaps `MapPlaceholderPanelHost` for `CesiumGlobeHost` + bridge while preserving default false for CI/headless safety.

**Acceptance criteria:**

- [ ] Default `DelegationSmoke` build unchanged (`useGlobeMap=false`).
- [ ] Flagged variant loads globe without breaking TopBar/LeftDrawer/MessageLog hosts.
- [ ] Rollback: delete variant scene; `dotnet test` green.
- [ ] **ZERO edits** to `DelegationBridge.cs`.

**Manual QA session:** **0.5 × ~1 h**

---

## Nice-to-have

### S23-U07 — Doctrine panel keyboard / motion-prefs parity

| Field | Value |
|-------|-------|
| **Estimate** | 0.5 day |
| **Owner** | team-unity |
| **Req trace** | Req 01 accessibility, C2 interaction patterns (mirror `OsintStagingPanelHost`) |

**Approach:** Tab order for ROE dropdown + Apply; `MotionPreferences` dim/hover on doctrine panel root; no new commands.

**Acceptance criteria:**

- [ ] Keyboard: Tab reaches dropdown and Apply; Enter triggers Apply when enabled.
- [ ] No regression on PlayMode smoke.
- [ ] **ZERO edits** to `DelegationBridge.cs`.

---

## Quality gates (sprint-wide)

| Gate | Command / path | Pass threshold |
|------|----------------|----------------|
| PlayMode smoke (headless) | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests` | All rows PASS (≥8/8) |
| Doctrine scoped | `--filter "FullyQualifiedName~Doctrine\|FullyQualifiedName~PlayModeSmoke"` | ≥13/13 PASS |
| Unity plugin guard | `tools/Test-UnityPluginAssemblies.ps1` | PASS before Editor work |
| DelegationBridge zero-touch | `git diff -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` | Empty diff on all Unity PRs |
| C2 batch (Editor) | `pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario comms` + `-Scenario classify` | Exit 0, no game console errors |
| Unity Editor checklist | `unity/ProjectAegis/PLAYMODE-SMOKE.md` | Scene stack complete |
| Cesium checklist | `docs/engineering/cesium-phase-b-spike-checklist.md` | Depth/occlusion + evidence rows closed |
| GitNexus detect | `npx gitnexus detect_changes --repo cmano-clone` | Run before commit state |

---

## Dependencies on headless / platform work

| Dependency | Owner | Blocks | Notes |
|------------|-------|--------|-------|
| Sprint 22 rebase + commit (`retro-sprint-22` #1–2) | implementer | **S23-U01** start | Doctrine host may conflict with `a95e06f` |
| Doctrine headless stack (S22-05) | done | — | `DoctrineOverrideCommand`, projection, binder already green |
| `MapSymbolEntry.App6Sidc` (optional) | team-data / delegation | **S23-U04** only | Spike can proceed Unity-only with stub SIDC map |
| Sim-published lat/lon on wire | simulation (future) | — | S23-U02 uses deterministic Baltic geo mapping; not blocking |
| Full `ProjectAegis.sln` test gate | team-data / devops (`S23-D08`) | — | Unity runs scoped filters; full sln not Unity sprint gate |

---

## Capacity & manual QA estimate

| Story tier | Stories | Dev estimate | Manual QA sessions |
|------------|---------|--------------|-------------------|
| Must-have | U01–U03 | **4.5 days** | **3 sessions (~4.5 h)** |
| Should-have | U04–U06 | **4 days** | **2 sessions (~3.5 h)** |
| Nice-to-have | U07 | **0.5 day** | — |
| **Total (must-only)** | 3 | **4.5 days** | **3 sessions** |
| **Total (must + should)** | 6 | **8.5 days** | **5 sessions (~8 h)** |

**Recommendation:** Schedule **3 must-have manual QA sessions** in sprint; treat U05 as should-have session #4 if capacity allows.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Doctrine UXML element name drift vs host constants | Copy names verbatim from `DoctrineInheritancePanelHost.cs` constants |
| Cesium package fails to resolve in CI/agent env | Editor-only validation; headless gates remain authoritative for merge |
| `DelegationSmoke.unity` binary merge conflicts | Prefer scene rebuild via `DelegationSmokeSceneBuilder` over hand-editing YAML |
| APP-6 spike scope creep into full atlas | Time-box to 2 symbols + spike doc; defer LOD clustering |

---

## References

- Retro: `production/retrospectives/retro-sprint-22-2026-06-17.md` (action items #6–7)
- Sprint 22 sign-off C4: `production/agentic/sprint-22-pr-description-2026-06-17.md`
- Doctrine host: `unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs`
- ADR-010 command seam: `docs/architecture/adr-010-headless-first-command-driven-ui.md`
- ADR-007 globe phases: `docs/architecture/adr-007-c2-map-presentation.md`
- Cesium evidence: `production/qa/cesium-s20-local-editor-evidence.md`
- Parallel data plan: `production/agentic/sprint-23-plan-data-2026-06-17.md`

*Generated by Sprint 23 Unity/Presentation Planning agent — 2026-06-17.*