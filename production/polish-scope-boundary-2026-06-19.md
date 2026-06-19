# Polish Scope Boundary — Phase 1

**Date:** 2026-06-19  
**Authority:** Resolves gate-check blocker #3 (`production/gate-checks/production-to-polish-2026-06-19.md`)  
**Baseline:** `main` @ `d3db76d` — Sprint 34 COMPLETE (1193/1193 PASS; ReplayGolden 6/6; C2 18/18 PASS WITH NOTES)  
**Tracker:** [implementation-tracker-2026-06-04.md](../Game-Requirements/implementation-tracker-2026-06-04.md) — all Req 01–21 rows remain **Partial** MVP  
**Sprint 34 cut line:** [sprint-34-linkcatalog-datalink-latency-phase-h.md](sprints/sprint-34-linkcatalog-datalink-latency-phase-h.md) — Explicitly Out of Scope section

## Purpose

Polish Phase 1 hardens the **Baltic vertical slice + C2 + Platform Editor** player-facing path already proven in Production. It does **not** advance tracker rows to MVP-complete or expand into deferred Sprint 35+ epics. Any polish story without a traceable link to this document is out of scope until scope is formally revised.

---

## In Scope (Polish Phase 1)

### Baltic vertical slice (production + isolated fixtures)

- **Production pin:** `data/scenarios/baltic-patrol.policy.json` — world hash `17144800277401907079` must remain unchanged unless an isolated fixture or explicit gate-approved hash bump is documented.
- **ReplayGolden 6/6 suite:** engage, classify, stale, readiness, spoof, replay — maintenance, regression fixes, and golden refresh only when tied to in-scope polish (no new scenario families).
- **Isolated Baltic fixtures** (polish/tune allowed; must not alter production pin):
  - `baltic-patrol-datalink-catalog-latency` (S34-07)
  - `baltic-patrol-datalink-comms` (S33-07)
  - `baltic-patrol-combat-domains` and bounded hot-tick variants (S29–S32)
  - `baltic-patrol-jammed`, `baltic-patrol-classify`, `baltic-patrol-comms`, `baltic-patrol-mission-roe`, `baltic-patrol-readiness`, `baltic-patrol-spoof`
- **Harness path:** `BalticReplayHarness` + `SimulationSession` headless smoke — proxy evidence acceptable per lean QA (PI-006).

### C2 UI path (checks 1–18)

- **Headless proxy (primary gate):** `Invoke-ManualQaHeadlessGate.ps1` + `c2-automated-proxy-2026-06-02.md` — maintain **18/18** checks via `ProjectAegis.Delegation.UnityAdapter.Tests` filters (`PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu|PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog`).
- **Manual sign-off checklist:** `production/qa/c2-manual-signoff-2026-06-02.md` — checks 1–18 per [sprint-34-c2-signoff-2026-06-19.md](qa/sprint-34-c2-signoff-2026-06-19.md).
- **Editor when available (advisory, non-blocking):** live Unity re-capture of `*-s30..s34-*.png` protocol placeholders; `Invoke-C2PlayModeSignoffBatch.ps1` for import + begin-execution scenarios on Windows/macOS host.
- **UX polish within C2 path only:** `design/ux/c2-command-post.md`, map placeholder panel, selection/OOB/attack menu/comms bar feel — no globe/Cesium production work.
- **Planning chrome:** Begin Execution top bar (`C2TopBarBeginExecutionTests`) and doctrine inheritance panel (`DoctrineOverrideCommandTests`) — presentation and clarity only.

### Platform Editor Phases C–H

- **Phase C — catalog viewer:** `PlatformCatalogViewerHost` list/detail; `PlatformCatalogViewerTests`.
- **Phase D — workbook write:** export/diff hook; `PlatformWorkbookPhaseDWriteTests`.
- **Phase E — import UI:** `PlatformImportPanelHost` + staging projection; propose→acknowledge→approve gate; `PlatformImportPanelTests`.
- **Phase F — damage surfacing:** damage columns + `MaxHp` staging diff; S32 evidence path.
- **Phase G — comms surfacing:** comms columns + `COMMS` staging diff; link `DisplayName` resolution; `PlatformCommsTests`.
- **Phase H — link catalog surfacing:** link list + `LINK` staging diff; `PlatformLinkCatalogTests`; workbook round-trip via `PlatformWorkbookRoundTripTests` (data spine, no new sim behavior in Unity).
- **Presentation evidence:** replace or validate `production/qa/evidence/*-s30..s34-*.png` placeholders within Phases C–H screens only.

### Determinism / ReplayGolden maintenance

- **`/replay-verify` mandatory** on any sim/delegation merge touching `BalticReplayHarness`, `DatalinkSidePictureMerger`, `DatalinkShareLagResolver`, or order-log projection.
- **ReplayGolden 6/6** must pass on every polish PR; isolated fixture pins updated only in matching `tests/regression/replay-golden-*.txt` files (not production 6/6 set).
- **ZERO touch** on `DelegationBridge.cs` unless a future ADR explicitly revokes S34 control manifest.
- **Extend-only** on `CatalogWriteGate` for any data polish touching catalog staging.

### Performance budgets (perf-profile reference path)

- **Authoritative budget doc (create if missing):** `production/perf/perf-profile-polish-baseline-2026-06-19.md` — produced by `/perf-profile` before optimization stories land.
- **P0/P1 items only** from perf-profile govern Polish optimization work (e.g. headless harness tick budget, C2 panel bind latency, catalog read at scenario load) — P2+ deferred to Sprint 35+ unless blocking a P0 player path.
- **No DOTS/ECS hot-path migration** under Polish Phase 1; measure and budget the existing `ProjectAegis.Sim` + Unity adapter path.

### Polish-phase gate artifacts (first-sprint prerequisites)

- **Playtest corpus:** `production/playtests/` — ≥3 structured sessions (NPE, mid-game systems, difficulty signal) per gate-check blocker #1.
- **Fun validation:** document Player Fantasy status against `design/gdd/game-concept.md` (validated / revised / hypothesis-only).

---

## Explicitly Out of Scope (defer Sprint 35+)

### Globe map / Cesium production

- Production globe map, `CesiumGlobeBridge` runtime polish, `CesiumGlobeHost` player-facing integration beyond spike evidence.
- `design/ux/c2-map-placeholder.md` remains placeholder — no Cesium production deployment.

### Full Req 01–21 MVP completion

- Closing any tracker **Partial** row to **MVP-done** (multi-thousand-entity perf gate, full NL planner, orbital DEW runtime, UNREP, swarm coordinator, Monte Carlo schema, etc.).
- New requirement phases (e.g. Req 07 phases 3–5, Req 15 ECCM workbook round-trip beyond catalog-derived lag, Req 18 mine-laying/clearing, full facility combat).

### Agentic delegation badges UX

- C2 delegation badges, trust emit-only order-log UX (Req 04), agent personality on engage.
- OSINT staging panel live-proxy polish beyond existing S20 slice (Req 05 MCP/S21+ work).

### Near-future / speculative runtime polish

- `NearFutureArchetypeRuntime`, hypersonic DOTS spawn, MASS tier runtime (Req 09).
- `HYPERSONIC_ALERT` UI state (Req 20 — deferred until hypersonic track in sim).
- Speculative escalation ladder, orbital DEW, `KESSLER_RISK_METER` (Req 10/18).

### Data / sim infrastructure (Sprint 34+ deferrals)

- **Full corpora in `dotnet test` CI** (7208 sensor, 4844 ship, 4403 weapon — off-CI by design).
- **TL Phase 5** — physical SQLite forks / `TlBranchDatabaseResolver` / runtime TL fork selection beyond export metadata.
- **Full ECCM Phase 2** — catalog onboard ECCM flags, bounded S32-05 jam fixture expansion, JADC2 node damage (Req 19).
- **DOTS ECS** sensor hot path and sim API export (Req 08).
- **CMO mission/scenario import** runtime (vs. existing workbook/catalog import).
- **Dependency-graph platform→link edges** (plan-only / S35 stretch).
- **Loadout/magazine Unity surfacing** (Req 16/21 — deferred S35 per S34 out-of-scope).

### Product-scale features

- **Multiplayer** networking, session sync, or authoritative server work.
- **Global campaigns**, multi-theater persistence, or scenario libraries beyond Baltic slice fixtures.

### Studio template / doc-only tracker gaps

- Migrating test evidence to `tests/unit/` + `tests/integration/` studio layout (audit hygiene — not Polish feature work).
- Art bible, full accessibility tier, interaction-pattern library at `design/ux/interaction-patterns.md` — track as parallel UX foundation, not scope-expanding gameplay.

---

## Cut Line Rules

1. **Traceability required:** Every Polish story must cite (a) a path in **In Scope** above, or (b) a **P0/P1** line item in `production/perf/perf-profile-polish-baseline-2026-06-19.md`. Stories citing only tracker "Next stack task" columns without an in-scope path are **rejected**.
2. **Tracker Partial ≠ Polish backlog:** Rows marked **Partial** in [implementation-tracker-2026-06-04.md](../Game-Requirements/implementation-tracker-2026-06-04.md) may be **documented** (notes, handoff stubs, ADR drafts) but **no polish implementation stories** unless the row's next task intersects In Scope (e.g. Req 20 globe map → out; Req 20 C2 check polish → in).
3. **No scope creep via fixtures:** New isolated fixtures are allowed only when they exercise an in-scope subsystem already on the production→C2 path; they must not become alternate MVP vertical slices.
4. **Sim vs. Unity boundary:** Unity polish is presentation, bind latency, and staging UX — sim behavior changes stay in `ProjectAegis.Sim` with `/replay-verify`; Phase H precedent (S34-06 schema-only Unity) holds.
5. **Hash discipline:** Production Baltic hash `17144800277401907079` is immutable for Polish unless Producer + Technical Director sign a documented golden refresh with root-cause.
6. **Gate before stage advance:** `production/stage.txt` moves to **Polish** only after playtest corpus + fun validation + this document acknowledged; re-run `/gate-check` for PASS or accepted CONCERNS.

---

## Sprint 35 Handoff Candidates

Deferred items explicitly named in Sprint 34 out-of-scope and tracker next-stack columns. **Do not schedule in Polish Phase 1.**

| # | Handoff item | Source | Tracker / notes |
|---|--------------|--------|-----------------|
| 1 | **Globe map + Cesium production** | S34 out-of-scope; Req 20 next | Replace map placeholder; production `CesiumGlobeBridge` |
| 2 | **Loadout/magazine Unity surfacing** | S34 out-of-scope; Req 16/21 | Live magazine counts from catalog in Editor |
| 3 | **TL Phase 5 — physical SQLite forks** | S34 out-of-scope; Req 06 next | `TlBranchDatabaseResolver`; runtime branch selection |
| 4 | **Full corpora in CI** | S34 out-of-scope; Req 06 | 7208 sensor / 4844 ship / 4403 weapon in `dotnet test` |
| 5 | **Full ECCM Phase 2 + JADC2 node damage** | S34 out-of-scope; Req 15/18/19 | Beyond `baltic-patrol-jammed` bounded slice |
| 6 | **`HYPERSONIC_ALERT` UI + hypersonic DOTS spawn** | S34 out-of-scope; Req 09/20 | `HypersonicEngageGate` player-facing state |
| 7 | **C2 delegation badges + trust UX** | Tracker Req 04 next | `TrustSignalEmitter` → order-log badges |
| 8 | **Dependency-graph platform→link edges** | S34 out-of-scope (S35 stretch) | `CatalogDependencyGraphIndex` → link FK graph in UI |

---

## Related artifacts

| Artifact | Path |
|----------|------|
| Production → Polish gate | [production-to-polish-2026-06-19.md](gate-checks/production-to-polish-2026-06-19.md) |
| C2 sign-off (18 checks) | [c2-manual-signoff-2026-06-02.md](qa/c2-manual-signoff-2026-06-02.md) |
| Sprint 34 out-of-scope | [sprint-34-linkcatalog-datalink-latency-phase-h.md](sprints/sprint-34-linkcatalog-datalink-latency-phase-h.md) |
| Perf budgets (to author) | `production/perf/perf-profile-polish-baseline-2026-06-19.md` |
| ReplayGolden catalog | [tests/regression/README.md](../tests/regression/README.md) |
| Platform Editor ADR | [ADR-011](../docs/architecture/adr-011-platform-editor-excel-roundtrip.md) |

*Generated to close Production → Polish gate blocker #3 (2026-06-19).*