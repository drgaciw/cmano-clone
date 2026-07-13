# Scope-Expansion Decision — Track B Release Enablement Gate

**Date:** 2026-06-20  
**Status:** **APPROVED**  
**Sign-off date:** 2026-06-20  
**Sign-off authority:** User (creative-director approval via chat — "Approve Track B default")  
**Gate position:** After S41 Polish-exit; before S42 agent dispatch  
**Authority:** [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §4, §5, §9  
**Template source:** [`scope-expansion-decision-template-2026-06-20.md`](scope-expansion-decision-template-2026-06-20.md)

> **Scope gate APPROVED.** S42–S48 may proceed per program guide once Track A exit criteria are met.  
> **Track A prerequisite:** S40 and S41 must still complete in-boundary before S42 dispatch.

---

## Decision record

| Field | Value |
|-------|-------|
| Decision date | 2026-06-20 (approved) |
| Decision maker(s) | User / creative-director |
| S41 closeout reference | **[PENDING S41]** — `production/qa/smoke-sprint-41-closeout-*.md` not yet authored; S41 status `planned` per `production/sprint-status.yaml` |
| Polish-exit report | **[PENDING S41]** — S41-05 target: `production/qa/evidence/README-polish-exit-*.md` |
| Gap analysis artifact | **Inline below (interim)** — formal S41-07 doc pending; grounded in tracker + roadmap + polish boundary handoff table |
| Evidence baseline @ proposal | S39 closeout: **1215/1215** tests, ReplayGolden **6/6**, C2 proxy **18/18**, Baltic hash **`17144800277401907079`** — [`production/qa/smoke-sprint-39-closeout-2026-06-20.md`](../qa/smoke-sprint-39-closeout-2026-06-20.md) |
| Release target definition | **Baltic vertical slice v1.0** — shippable C2 + Platform Editor + Baltic replay path; not full Req 01–21 corpus MVP-done (per `AGENTS.md` learned preferences + gate-check S38 CONCERNS) |

---

## Interim Track B gap analysis (S41-07 substitute)

**Sources:** [`Game-Requirements/implementation-tracker-2026-06-04.md`](../../Game-Requirements/implementation-tracker-2026-06-04.md) (all Req 01–21 **Partial**), [`production/polish-scope-boundary-2026-06-19.md`](../polish-scope-boundary-2026-06-19.md) §Explicitly Out of Scope + §Sprint 35 Handoff Candidates, [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §1 release blockers, [`design/art/art-bible.md`](../../design/art/art-bible.md) lean header.

| Release blocker (roadmap §1) | Current state | Track B epic | Proposed resolution |
|---|---|---|---|
| Post-MVP tracker rows Partial | 21/21 Partial | **B1** | Commit **13 rows** to MVP-done for Baltic v1.0 (S42+S43); **8 rows** remain Partial post-release |
| Art bible lean (C2+Editor only) | §1–4, §6, §8–9 substantive; §5/§7 explicit defer stubs | **B2** | APPROVE full 9-section + asset specs; §5/§7 document *intentional N/A for map-first v1* |
| Launch artifacts absent | No release checklist, store, i18n | **B5** | APPROVE S46 scope (checklist + store drafts + i18n pipeline spec) |
| Structural debt | Decision 60%, Telemetry 67%, Osint 68% (GitNexus @ HEAD) | **B3** | APPROVE S44 refactor per S41 ADR **[PENDING S41]** |
| Perf beyond P0/P1 | P1 carryovers in perf baseline | **B4** | APPROVE bounded DOTS/ECS + Runtime hot-path in S45 |
| Polish carryovers | Catalog/Import surfacing, perf P1 | Track A | S40–S41 complete before S42 |

**GitNexus structural-debt signal (roadmap §2):** `Decision` (60%), `Telemetry` (67%), `Import` (67%, slipped), `Osint` (68%), `WriteGate` (75%, slipped). B3 addresses Decision/Telemetry/Osint; Import cohesion monitored but not primary B3 scope.

---

## 1. Boundary — lift/replace polish-scope-boundary

### Recommendation: **Replace** (do not keep Polish boundary for Track B)

| Option | Selected | Rationale |
|--------|----------|-----------|
| Keep Polish boundary; defer Track B | [ ] | Contradicts 10-sprint program intent; S42–S48 plans already assume post-gate boundary |
| **Replace with new scope doc** | **[x] Recommended** | Polish boundary explicitly forbids tracker MVP completion (`polish-scope-boundary-2026-06-19.md` §Explicitly Out of Scope: "Full Req 01–21 MVP completion") |
| Lift with amendments only | [ ] | Insufficient — cut-line rules (§Cut Line Rules) conflict with B1 content work |

### Proposed new scope document

**Path:** `production/release-enablement-scope-boundary-2026-06-20.md`  
**Supersedes for Track B:** `production/polish-scope-boundary-2026-06-19.md` (archived as Track A authority only)

**Outline:**

1. **Purpose & authority** — Baltic v1.0 release enablement; cites this decision record + roadmap §5  
2. **In scope (B1–B6)** — committed tracker rows (tables below); art bible completion; B3 refactor modules; B4 perf targets; B5 launch artifacts; B6 gate-check  
3. **Standing invariants** — Baltic hash, ReplayGolden 6/6, C2 proxy floor, DelegationBridge ADR, CatalogWriteGate policy (see Gate matrix)  
4. **Explicitly out of scope (post-v1.0)** — deferred tracker rows; globe/Cesium production; full corpora in CI; multiplayer; global campaigns  
5. **Cut-line rules** — every Track B story cites new boundary + committed row ID; `impact()` mandatory on Catalog/Platform symbols  
6. **Sprint mapping** — S42–S48 per roadmap §9  
7. **Related artifacts** — sprint plans, readiness checklist, program execution guide  

**Publish timing:** Coordinator drafts on APPROVE; must exist before S42-01 dispatch.

---

## 2. Tracker rows — B1 committed scope (Partial → MVP-done)

**Principle:** Baltic v1.0 closes player-facing gaps on the proven production→C2→Platform path. Agentic/multi-theater/speculative rows stay Partial unless they intersect Baltic replay evidence.

### S42 wave 1 — Catalog / Platform / Scenario / UX foundation

| Req | Title | Committed next-stack scope | Evidence / boundary link |
|-----|-------|---------------------------|--------------------------|
| **02** | Core Gameplay Loop | Complete Phase 1–2 UX; explicit **Begin Execution** flow sign-off (extends S30-07 planning chrome) | `C2TopBarBeginExecutionTests`; boundary In Scope §C2 planning chrome |
| **06** | Database Intelligence | **Dependency-graph platform→link edges** in Editor UI (`CatalogDependencyGraphIndex` → link FK graph); catalog provenance/quarantine surfacing in read-models (extends S40 Horizon 2) | Handoff #8; S33-02 index exists; projection-side only |
| **12** | Terms Glossary | **UI tooltips** for abort/sensor/cyber families in C2 + Platform panels | Tracker next: "UI tooltips" |
| **13** | Doctrine ROE EMCON WRA | **Doctrine inheritance panel** sign-off complete (ADR-010); presentation-only | S29-07 partial; boundary In Scope |
| **16** | Logistics & Magazines | **Live magazine counts from catalog** in Platform Editor (loadout surfacing) | Handoff #2; ties Req 21 |
| **21** | Platform Editor | **Loadout/magazine Unity surfacing**; advisory live Editor PNG refresh for Phases C–H; bounded datalink latency workbook bridge (catalog-derived lag already in S34) | Handoff #2; Phases C–H complete per tracker |

**S42 scenario/data (no separate row):** Baltic policy JSON maintenance only; **no production hash change** without golden ADR.

### S43 wave 2 — Engage / C2 features / combat remainder

| Req | Title | Committed next-stack scope | Evidence / boundary link |
|-----|-------|---------------------------|--------------------------|
| **03** | Simulation Modes | **Mode UI on C2 top bar** (RTwP / executing indicators; bounded) | Tracker next; C2 top bar In Scope |
| **04** | Agent Delegation | **C2 delegation badges**; trust emit-only in order log (presentation via projection; **DelegationBridge ADR if bridge touched**) | Handoff #7; boundary was Out of Scope in Polish |
| **14** | Engagement & Fire Control | **Swarm coordinator sectors** (bounded Baltic fixture); **DLZ Phase 2** (bounded) | Replay-gated Engage paths |
| **15** | Sensor Detection & EW | **ECCM Phase 2 bounded** (extends S32-05 jam fixture); catalog onboard ECCM flags (read-model + staging) | S32-05 precedent |
| **17** | Replay AAR & Order Log | **Scrub UI stub** + order-log AAR export hooks (headless-first) | CI replay suite exists |
| **18** | Combat Domains | **Mine-laying/clearing missions** (bounded fixture); **BDA lifecycle extension** beyond S32-09 hook | ADR-009 Phase 6 extension |
| **19** | Cyber & Comms | **JADC2 node damage** (bounded schema + fixture); ECCM Phase 2 tie-in with Req 15 | Research gap P1 deferred items |

### Explicitly **NOT** committed in B1 (remain Partial post-v1.0)

| Req | Reason to defer | Revisit |
|-----|-----------------|---------|
| **01** | Multi-thousand-entity perf gate → **B4/S45** scoped benchmark, not content | S45 |
| **05** | S21+ MCP + Data P1 — OSINT panel exists; MCP integration is post-Baltic epic | Post-release |
| **07** | Agentic Infrastructure — scenario gen + experiment workers | Post-release |
| **08** | Agentic Architecture — DOTS sim API export overlaps **B4** selectively | S45 (hot path only) |
| **09** | Near-Future — full DOTS spawn, MASS tier | Post-release |
| **10** | Speculative Systems — orbital DEW, escalation ladder | Post-release |
| **11** | Agentic Mission Editor — full NL planner | Post-release |
| **20** | C2 UI — **globe map / Cesium production**, `HYPERSONIC_ALERT` UI | Handoff #1; post-v1.0 epic |
| **06** (subset) | Full corpora in CI (7208/4844/4403); runtime TL fork selection | Post-release data epic |

**B1 exit criterion:** 13 committed rows updated to **MVP-done** (or **Partial+** with documented Baltic AC tests) at S43 closeout; tracker updated per row.

---

## 3. Art bible budget (B2)

### Current state ([`design/art/art-bible.md`](../../design/art/art-bible.md))

| Section | Status |
|---------|--------|
| §1 Visual Identity | **Complete** (lean, AD-approved S38) |
| §2 Mood & Atmosphere | **Complete** |
| §3 Color Palette | **Complete** |
| §4 Typography & Iconography | **Complete** |
| §5 Character Design | **Stub** — "Deferred post-Baltic slice" |
| §6 UI Visual Language | **Complete** (C2 + Platform) |
| §7 VFX & Particle Style | **Stub** — "Deferred post-Baltic slice" |
| §8 Asset Standards | **Complete** (evidence protocol, USS, atlases) |
| §9 Style Prohibitions | **Complete** |

Gate-check S38 blocker #2: lean bible APPROVED WITH CONDITIONS; full sign-off deferred.

### Recommendation: **Partial APPROVE** (full 9-section structure; §5/§7 remain intentional N/A)

| Option | Selected | Rationale |
|--------|----------|-----------|
| Yes — full creative expansion | [ ] | §5/§7 have no Baltic v1 assets; expanding them would scope-creep art production |
| **Partial — sections per sprint** | **[x] Recommended** | S42: refine §1–4 + token consolidation; S43: §5/§7 *policy docs* (N/A rationale) + §8 asset spec expansion + §9 review |
| Defer entire B2 | [ ] | Blocks B5 store pages and gate-check #2 |

**Budget:** **8 agent-days** (art-director + team-ui)

| Sprint | Days | Deliverable |
|--------|------|-------------|
| S42 | 3 | §1–4 refinement; `AegisTokens.uss` recommendation implemented or documented; gate matrix cross-ref |
| S43 | 5 | §5/§7 formal N/A for v1; §8 full asset spec sheets (APP-6 atlas, Platform evidence naming); §9 sign-off; AD-ART-BIBLE full verdict |

---

## 4. Structural debt (B3) — S44

### Recommendation: **Yes — APPROVE** S44 refactor scope

| Option | Selected |
|--------|----------|
| **Yes** | **[x] Recommended** |
| Defer | [ ] |

**Scope (per [`sprint-44-structural-debt-refactor.md`](../sprints/sprint-44-structural-debt-refactor.md)):**

- **Decision module** refactor — target cohesion ≥70% (from 60%)
- **Telemetry module** refactor — target cohesion ≥72% (from 67%); **zero shared files** with Decision track
- **Osint audit** — read/fix minimal surface (68% cohesion)
- **Mandatory:** GitNexus `rename`/`impact`; ReplayGolden **6/6 after every merge**; determinism-engineer on replay gate track

**ADR reference:** **[PENDING S41]** — S41-03 target: `docs/adr/` or `production/adr/` structural-debt ADR. **S44 dispatch blocked until ADR exists** even after scope approval.

**Interim cohesion targets (pending ADR refinement):**

| Module | Pre | Target post-S44 |
|--------|-----|-----------------|
| Decision | 60% | ≥70% |
| Telemetry | 67% | ≥72% |
| Osint | 68% | ≥72% or audit-only with defer list |

---

## 5. Performance scale-out (B4) — S45

### Recommendation: **Yes — APPROVE with determinism-engineer pairing**

| Option | Selected |
|--------|----------|
| **Yes with determinism-engineer pairing** | **[x] Recommended** |
| Defer | [ ] |

**In scope (per [`sprint-45-performance-scale-out.md`](../sprints/sprint-45-performance-scale-out.md) + polish boundary lift):**

- **Req 01 subset:** Multi-thousand-entity **headless benchmark gate** (not full MVP row close)
- **Runtime/Sensors hot path** — profiled optimization on existing `ProjectAegis.Sim` path
- **DOTS/ECS sensor hot path** — **bounded pilot** on isolated fixture only (was Explicitly Out of Scope in Polish §Perf budgets)
- **Engage scale** — hot-tick budgets per perf-profile appendix
- **Out of scope:** Hypersonic DOTS spawn (Req 09); production hash change; DelegationBridge changes

**Prerequisite:** B1 locked at S43 closeout.

---

## 6. Launch artifacts (B5) + release cadence (B6)

### B5 — Recommendation: **Yes — APPROVE**

| Option | Selected |
|--------|----------|
| **Yes** | **[x] Recommended** |
| Defer | [ ] |

**S46 deliverables** ([`sprint-46-launch-artifacts.md`](../sprints/sprint-46-launch-artifacts.md)):

| Artifact | Path (proposed) | Owner |
|----------|-----------------|-------|
| Release checklist | `production/release/release-checklist-v1.md` | release-manager |
| Store page drafts | `production/release/store/` | community-manager + team-ui |
| Localization pipeline spec | `production/release/i18n-pipeline-spec.md` | localization-lead |
| Launch evidence index | `production/qa/evidence/README-release-evidence-*.md` | team-qa |

**Budget:** **8 effective agent-days** (S46 plan); cloud-heavy; no code-heavy features.

**Prerequisite:** B1 + B2 complete (S43 closeout).

### B6 — Target release cadence (S47–S48)

| Sprint | Calendar | Purpose | Human gate |
|--------|----------|---------|------------|
| **S47** | ~5–7 days | Full test sweep; gate-check **draft**; Buildkite preflight; Go/No-Go checklist | Coordinator + buildkite-ci-lead |
| **S48** | ~3–5 days | `/gate-check` Polish→Release; stage advance; program retro | **User + technical-director verdict mandatory** |

**Target:** ~**2 calendar weeks** from S46 closeout to release gate verdict (S47+S48), assuming S47 Go.

**Release definition:** Baltic vertical slice v1.0 shippable — not all 21 tracker rows MVP-done.

---

## 7. Standing invariants & post-Polish gate matrix

### Carry forward unchanged

| Invariant | Policy | Change mechanism |
|-----------|--------|------------------|
| Baltic hash `17144800277401907079` | **Immutable** for v1.0 | Golden ADR + Producer + Technical Director sign-off only |
| ReplayGolden | **6/6** every sprint | Fixture additions isolated unless production pin ADR |
| DelegationBridge | **ZERO touch** default | Explicit ADR revokes S34 control manifest; required if Req 04 badges touch bridge |
| CatalogWriteGate | **Extend-only** default | Scope ADR may revoke for specific B1 data rows |

### Post-Polish gate matrix (Track B floor)

| Gate | Floor @ S42 start | Track B policy |
|------|-------------------|----------------|
| **Headless tests** | **≥1215** (`smoke-sprint-39-closeout-2026-06-20.md`; S40/S41 may raise) | Monotonic growth; never regress below post-S41 closeout baseline |
| **ReplayGolden** | 6/6 | 6/6 mandatory; S44+ after every B3 merge |
| **C2 proxy** | **18/18** baseline checks | **18/18+**; expand matrix when S43 lands Req 03/04 UI (add checks 19–20 for mode UI + delegation badges) |
| **C2 proxy filters** | PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu\|PlatformImport\|Doctrine\|C2TopBar\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog\|Graph* | Append `DelegationBadge\|SimulationMode` filters when features land |
| **DelegationBridge** | ZERO diff | ADR required before any edit |
| **CatalogWriteGate** | Extend-only | Extend-only unless B1 row explicitly requires new gate verb + ADR |
| **GitNexus** | `impact()` before edit; `detect_changes()` before commit | HIGH/CRITICAL warnings block merge without TD review |
| **Boundary cite** | `polish-scope-boundary-2026-06-19.md` (Track A) | **`release-enablement-scope-boundary-2026-06-20.md`** (Track B) |

**Expanded gate matrix doc:** S42-01 produces `production/qa/gate-matrix-track-b-2026-06-20.md` (or dated at S42 start).

---

## Verdict

| Option | Selected | Notes |
|--------|----------|-------|
| **APPROVE Track B** — proceed S42→S48 per program guide | **[x] Approved** | User sign-off 2026-06-20 via "Approve Track B default" |
| **CONDITIONAL APPROVE** — constraints listed | [ ] | Superseded by full approval with constraints retained below |
| **DEFER Track B** — remain in Polish / replan | [ ] | Rejected |

### Verdict: **APPROVED Track B**

**Constraints (must hold):**

1. **S40 + S41 complete** before S42 dispatch (Track A exit + S41 ADR + polish-exit pack).
2. **New boundary doc published** before S42-01.
3. **B1 limited to 13 rows** listed above — no scope creep into Req 05/07/09/10/11/20 without new decision.
4. **B2 partial** — §5/§7 are policy N/A, not character/VFX production.
5. **B4 DOTS** — isolated-fixture pilot only until determinism-engineer sign-off.
6. **DelegationBridge / CatalogWriteGate** — ADR before any deviation from extend-only / zero-touch.

---

## Recommended default (one-reply approval)

Reply **"Approve Track B default"** to accept:

- Replace polish boundary with `production/release-enablement-scope-boundary-2026-06-20.md`
- B1: **13 tracker rows** committed (S42: Req 02, 06, 12, 13, 16, 21; S43: Req 03, 04, 14, 15, 17, 18, 19)
- B2: **8 agent-days** partial full-bible (§5/§7 N/A policy)
- B3/B4/B5/B6: **Approved** as specified above
- Gate matrix: **1215+** tests, **18/18+** proxy (expand on Req 03/04), standing invariants unchanged
- Dispatch S42 only after **S41 closeout PASS**

---

## Sign-off

| Role | Name | Date | Signature / ack |
|------|------|------|-----------------|
| User / creative-director | User | 2026-06-20 | **Approve Track B default** (chat) |
| Coordinator / producer | _pending_ | | |
| technical-director (optional) | _pending_ | | |

---

## Post-decision actions (coordinator — execute on APPROVE)

- [x] Copy completed record to `production/gate-checks/scope-expansion-decision-2026-06-20.md`
- [x] Publish `production/release-enablement-scope-boundary-2026-06-20.md`
- [x] Update `production/sprint-status.yaml` — `ten_sprint_program.scope_gate` → `approved`
- [x] Update `docs/reports/future-sprint-roadpmap.md` §9 dispatch status
- [ ] Run `/qa-plan sprint 42` before S42-03+ waves
- [ ] Bootstrap worktrees per `production/agentic/s39-s48-worktree-manifest.md`
- [ ] Reconcile S41 outputs when available (ADR, polish-exit pack, formal gap analysis)

---

## Evidence index

| Artifact | Path |
|----------|------|
| Scope template | `production/gate-checks/scope-expansion-decision-template-2026-06-20.md` |
| Roadmap §4–§5, §9 | `docs/reports/future-sprint-roadpmap.md` |
| Polish boundary | `production/polish-scope-boundary-2026-06-19.md` |
| Implementation tracker | `Game-Requirements/implementation-tracker-2026-06-04.md` |
| S39 retro + gates | `production/retrospectives/retro-sprint-39-2026-06-20.md` |
| S39 smoke | `production/qa/smoke-sprint-39-closeout-2026-06-20.md` |
| S41 plan | `production/sprints/sprint-41-polish-hardening-release-preflight.md` |
| Program guide | `production/agentic/s39-s48-program-execution-guide.md` |
| Track B readiness | `production/agentic/sprint-42-48-readiness-checklist.md` |
| Art bible (lean) | `design/art/art-bible.md` |
| Gate-check S38 | `production/gate-checks/s38-polish-continuation-2026-06-20.md` |
| Sprint plans S42–S48 | `production/sprints/sprint-42-*.md` … `sprint-48-*.md` |

---

*Approved 2026-06-20. S41 artifacts marked [PENDING S41] must be reconciled before S42 dispatch.*
