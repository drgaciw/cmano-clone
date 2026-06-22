# Future Sprint Roadmap — Project Aegis (cmano-clone)
> **Parallel-Agentic Edition**

> **Status:** Living document. Authored **2026-06-21**; prioritization **locked 2026-06-21** (user decisions §6).
> **Edition:** Optimized for parallel agentic development — see §0 for dispatch model, §10 for per-sprint track decomposition, §12 for dependency matrix.
> Supersedes planning intent in [`future-sprint-roadpmap-062026.md`](future-sprint-roadpmap-062026.md) (2026-06-20 pre-S40 snapshot).
> **Stable alias:** [`future-sprint-roadpmap.md`](future-sprint-roadpmap.md) → this file.
> GitNexus index @ S48 close: **18,025 nodes / 35,395 edges** (re-indexed 2026-06-20).
> GitNexus review 2026-06-21: MCP context shows **18,053 symbols / 35,427 edges / 300 flows** and is **1 commit behind HEAD**; use local artifacts as source of truth until re-indexed.
> **Stage:** Release (`production/stage.txt`). **Review mode:** lean (`production/review-mode.txt`).
> **Closed milestone:** Baltic v1.0 RC1 + vertical slice / Spirit 1 alias — **COMPLETE** (see §1).
> **Active program:** **S49+** — internal engineering train; lead epic **E2**; all v1.0-deferred tracker rows committed (§3).
>
> This roadmap is **direction, not a commitment**. Per `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md`,
> each sprint is still planned via `/sprint-plan` with user approval. Filename retains the
> `roadpmap` spelling for link stability.

---

## 0. Parallel execution model (S49+ program)

Every sprint is a **parallel dispatch** — multiple agent tracks run concurrently in isolated git worktrees, merging at sprint-close. This section defines the model; §10 applies it per sprint.

### 0.1 Agent environments

| Env | Capacity | Suited for | Not suited for |
|-----|----------|------------|----------------|
| **Local** (Cursor) | ≤6 concurrent | Editor evidence, Catalog-cluster lead, closeout/merge, determinism review | Mass CI runs, pure-code hygiene |
| **Cloud Agent** | ≤5 concurrent | Code/tests/hygiene, perf replay, MCP/CLI, docs, ADR | Unity Editor PNG capture |
| **Combined** | 4–6 effective tracks | — | — |

**Routing:** `production/agentic/local-cloud-agent-routing.md` — cloud handles code/test; local owns Editor evidence + coordinator merge.

### 0.2 Worktree strategy

```
.worktrees/stack/sprint{N}/{track-slug}/
```

Each track gets its own worktree. Tracks share **nothing** at runtime — coordination only at merge gates.

| Convention | Example | Purpose |
|------------|---------|---------|
| Stack prefix | `stack/sprint49/mcp-production` | Graphite stack grouping |
| Track slug | `mcp-production`, `osint-production`, `agentic-infra` | Unique per sprint |
| Closeout track | `stack/sprint{N}/closeout` | Merge coordinator (always local) |

### 0.3 Dispatch patterns

| Pattern | When | Example |
|---------|------|---------|
| **Fan-out** | Independent rows in same sprint | S49: MCP ∥ OSINT ∥ Infra |
| **Pipeline** | Serial dependency across sprints | S49 infra → S50 scenario workers |
| **Split-merge** | One row, parallel sub-tracks | S52: benchmark track ∥ DOTS expand track → merge at gate |
| **Single-owner** | HIGH-risk shared symbol cluster | Catalog cluster — one local agent, no cloud split |
| **Shadow** | Cloud does code, local does evidence | S55: cloud builds Cesium path, local captures Editor PNG |

### 0.4 Merge gate protocol (every sprint close)

```
┌──────────┐   ┌──────────┐   ┌──────────┐
│ Track A   │   │ Track B   │   │ Track C   │
│ (cloud)   │   │ (cloud)   │   │ (local)   │
└─────┬─────┘   └─────┬─────┘   └─────┬─────┘
      │ gt submit       │ gt submit     │ gt submit
      ▼                 ▼               ▼
┌─────────────────────────────────────────────┐
│           Sprint merge gate                  │
│  closeout track: gt restack → verify → merge │
│  verify: dotnet build + test + invariants     │
│  gate: all hard-gates pass → merge to main    │
└─────────────────────────────────────────────┘
```

1. All tracks `gt submit` their stacks.
2. Closeout track (local coordinator) runs `gt restack` on trunk `main`.
3. Verify: `dotnet build ProjectAegis.sln && dotnet test ProjectAegis.sln -v minimal`.
4. All hard-gates pass (determinism, replay, proxy, test floor) → merge.
5. GitNexus re-index after merge.

### 0.5 Shared-resource coordination

| Resource | Access pattern | Coordination rule |
|----------|---------------|-------------------|
| `CatalogWriteGate` | Extend-only | One owner per sprint; `impact()` CRITICAL before any edit |
| `DelegationBridge` | ZERO touch | ADR required before any edit; projection-first |
| Baltic determinism hash | Immutable | ADR + golden-update if required; all tracks verify |
| Test baseline (`≥1227`) | Monotonic | Any track adding tests raises floor; no track may regress |
| `OsintCatalogMapper` | HIGH blast-radius | Coordinate via GitNexus pre-flight; single modifier per sprint |
| `BalticBatchRunner` | HIGH if sim-binding | If touched by agentic infra, no other track touches sim path |

### 0.6 Pre-flight checklist (per track, before first edit)

- [ ] GitNexus `impact()` on every symbol in file-ownership matrix
- [ ] Report risk level (CRITICAL/HIGH → get user ack before editing)
- [ ] Confirm worktree isolation (`git worktree list`)
- [ ] Cite `post-release-scope-boundary-2026-06-21.md` + row/epic ID in first commit
- [ ] Verify test baseline passes before any change (`dotnet test`)

---

## 1. Where we are (post–Release gate)

| Dimension | State | Evidence |
|---|---|---|
| Stage | **Release** — Baltic v1.0 RC1 cut | `production/stage.txt`, [`s48-release-gate-2026-06-20.md`](../../production/gate-checks/s48-release-gate-2026-06-20.md) |
| Closed milestone | **v1.0 / vertical slice (Spirit 1) — CLOSED** | S48 gate; [`vertical-slice-mvp.md`](../../production/milestones/vertical-slice-mvp.md); no further Spirit 1 gap remediation |
| Last sprint (v1.0 program) | **S48 complete** | S48 gate + human ack 2026-06-20 |
| Next sprint | **S49 planned** — E2 Agentic kickoff (Req 05 + 07 foundations) | §10 |
| Test baseline | **1227/1227** headless, **ReplayGolden 6/6**, **C2 proxy 18/18** | S48 gate verification |
| Determinism | Baltic hash **`17144800277401907079`** immutable; ZERO DelegationBridge default | S48 + standing invariants §7 |
| RC1 intent | **Internal engineering milestone** — not a commercial launch train | User decision 2026-06-21; E7 out of S49+ scope |
| Parallel readiness | **4-track pattern proven** (S39–S48); S49 dispatched with 4 parallel tracks | [`sprint-49-parallel-kickoff-2026-06-21.md`](../../production/agentic/sprint-49-parallel-kickoff-2026-06-21.md) |

**What v1.0 delivered (closed):**
- Shippable **C2 + Platform Editor + Baltic replay path** (13 B1 tracker rows).
- Art bible 9-section structure (art-bible §5 and §7 intentionally N/A for map-first v1).
- B3–B6 complete (refactor, perf pilot, launch stubs, Release gate).

**Carried forward (not v1.0 blockers):**
- Playtest AAR (After Action Report) items ([`game-players-report-0620206.md`](../../game-players-report-0620206.md) — note: date-stamp in filename may be an anomaly) — scheduled in **S56** sweep, not S49 lead.
- Live Editor PNG evidence — advisory; refresh rides **E4/S55** where applicable.
- B5 store/i18n stubs remain stubs until a future commercial train (**E7**, explicitly out of S49+).

---

## 2. Completed program — S39–S48 (archive)

The 2026-06-20 roadmap executed through Release. Summary:

### Track A — Deeper Polish (S39–S41)

| Sprint | Goal | Closeout | Baseline @ close |
|--------|------|----------|------------------|
| **S39** | C2/Platform polish, hygiene, perf P1, evidence, replay | PASS 2026-06-20 | 1215 tests, 6/6, 18/18 |
| **S40** | Catalog/Import surfacing, perf P1 burn-down | PASS 2026-06-20 | 1226 tests |
| **S41** | Polish hardening, determinism audit, polish-exit pack, scope gate | PASS + user ack 2026-06-20 | 1226 tests |

### Track B — Release Enablement (S42–S48)

| Sprint | Epic(s) | Primary outcome | Closeout |
|--------|---------|-----------------|----------|
| **S42** | B1 W1 + B2 start | 6 tracker rows; art §1–4 | PASS |
| **S43** | B1 W2 + B2 complete | 7 tracker rows; art bible complete | PASS |
| **S44** | B3 | Decision/Telemetry/Osint refactor | PASS |
| **S45** | B4 | Runtime/Sensors/Engage perf; DOTS pilot | PASS |
| **S46** | B5 | Release checklist + launch stubs | PASS |
| **S47** | B6 prep | Release dry-run | PASS |
| **S48** | B6 | Release gate; stage→Release; RC1 | PASS |

**v1.0 B1 rows (13):** S42 — 02, 06, 12, 13, 16, 21; S43 — 03, 04, 14, 15, 17, 18, 19.

**Archive:** [`s39-s48-program-execution-guide.md`](../../production/agentic/s39-s48-program-execution-guide.md), [`s48-release-gate-2026-06-20.md`](../../production/gate-checks/s48-release-gate-2026-06-20.md).

---

## 3. S49+ committed scope — all v1.0-deferred tracker rows

User decision **2026-06-21:** the next internal engineering milestone commits **every row** deferred at v1.0 closeout. These moved from backlog to **in-scope** for S49–S56 under the published scope boundary (§6).

| Req | Title | Epic | Sprint | ∥ Tracks | Blocks | Blocked by | Committed scope |
|-----|-------|------|--------|----------|--------|------------|-----------------|
| **05** | Dynamic Speculative Systems Agent | E2 ★ | S49 | 2 (MCP ∥ OSINT) | 07, 11 | — | MCP + OSINT production path; connector hardening; staging panel beyond lean proxy |
| **07** | Agentic Infrastructure | E2 ★ | S49–S50 | 1 (S49) → 2 (S50) | 11 | 05 (MCP tools) | Scenario gen workers; experiment schema (Monte Carlo gap); harness integration |
| **11** | Agentic Mission Editor | E2 ★ | S50 | 1 (depends on 05, 07) | — | 05, 07 | NL planner UX beyond CLI; Unity edit-mode path |
| **06** (subset) | Database Intelligence | E5 | S51 | 2 (corpora ∥ TL fork) | 01 (benchmark data) | — | Full corpora in CI; runtime TL fork selection beyond export metadata |
| **01** | Project Overview | E6 | S52 | 2 (benchmark ∥ sim-API) | 08 | 06 (corpora) | Multi-thousand-entity headless benchmark as MVP-done gate |
| **08** | Agentic Architecture | E6 | S52 | ∥ with 01 (separate code) | — | 07 (harness) | Stable sim API export; expand S45 DOTS pilot |
| **09** | Near-Future Technologies | E3 | S53 | 2 (DOTS ∥ MASS) | 10 | 08 (DOTS expand) | Full DOTS spawn; MASS tier beyond harness `NF_SPAWN` |
| **10** | Speculative Systems | E3 | S54 | 2 (orbital ∥ escalation) | — | 09 (DOTS spawn) | Orbital DEW runtime; escalation ladder; `KESSLER_RISK_METER` |
| **20** | Command & Control UI | E4 | S55 | 2 (Cesium ∥ hypersonic) ∥ Editor evidence | — | — | Cesium/globe production; `HYPERSONIC_ALERT` UI; live Editor PNG refresh |

**Program exit criterion (S49+):** All **21** implementation-tracker rows at **MVP-done** (or documented **Partial+** — feature-gated subset sufficient to pass Baltic AC tests in isolation) + internal engineering gate at **S56** — not commercial launch (E7).

**Still out of scope (unless new scope decision):** multiplayer, global campaigns, E7 commercial launch ops, E8 art-bible §5/§7 production (optional parallel).

---

## 4. Epic buckets (S49+ program map)

```
 v1.0 CLOSED ──► S49 (E2 lead) ──► S50 ──► S51 ──► S52 ──► S53 ──► S54 ──► S55 ──► S56 gate

 Parallel tracks per sprint:
 ┌────────────────────────────────────────────────────────────────────────────┐
 │ S49          S50          S51          S52          S53    S54    S55      │
 │                                                                             │
 │ MCP ───►     Scenario ─►  Corpora ─►   Benchmark    DOTS   Orbit  Cesium   │
 │  │            workers      │             │           spawn  DEW    │        │
 │  ├─ OSINT ─►  NL Editor    ├─ TL fork    ├─ Sim API   MASS   Escal  ├─ Hyp  │
 │  │                         │             │           tier   ladder  │  UI    │
 │  └─ Infra ──►              │             │                   │       │        │
 │                             │             │                   │       └─ Evid │
 └────────────────────────────────────────────────────────────────────────────┘

 E2 Agentic ★          E5 Data    E6 Perf/arch    E3 Speculative   E4 Map/C2

 E1 Playtest sweep ──► S56 (closeout)
 E7 Commercial    ──► DEFERRED
 E8 Art §5/§7     ──► OPTIONAL parallel (not tracker rows)
```

### E2 — Agentic platform ★ **LEAD** (Req 05, 07, 11)

| Theme | Sprint | Tracks | Notes |
|-------|--------|--------|-------|
| MCP + OSINT | S49 | 2 ∥ (MCP, OSINT) + infra | MCP tools + CLI; OSINT digest stub + Catalog integration |
| Infrastructure | S49–S50 | 1 → 2 | Scenario gen workers; Monte Carlo schema; harness integration |
| Mission Editor | S50 | 1 (serial: needs 05+07) | NL planner; Unity edit-mode UX |

**Dispatch:** S49 is READY — kickoff + plan published (§9, §10).

### E5 — Data scale & CI (Req 06 subset) — **S51**

| Theme | Tracks | Notes |
|-------|--------|-------|
| Full corpora CI | ∥ Track A | Corpora ingest pipeline; CI strategy |
| TL runtime fork | ∥ Track B | Fork selection logic beyond export metadata |
| Import cohesion | (sequel) | Monitoring; rides corpora track |

### E6 — Performance & sim architecture (Req 01, 08) — **S52**

| Theme | Tracks | Notes |
|-------|--------|-------|
| Multi-k benchmark | ∥ Track A | Headless entity stress; MVP-done gate |
| Sim API export | ∥ Track B | Stable API surface; DOTS sensor hot-path |
| DOTS determinism | Both tracks verify | determinism-engineer sign-off on both |

### E3 — Speculative & near-future (Req 09, 10) — **S53–S54**

Isolated fixtures first; no production hash change without golden ADR.

### E4 — Map & C2 production (Req 20) — **S55**

| Theme | Tracks | Notes |
|-------|--------|-------|
| Cesium globe | ∥ Track A (Cloud) | Production path; integration |
| Hypersonic UI | ∥ Track B (Cloud) | `HYPERSONIC_ALERT`; C2 integration |
| Editor evidence | ∥ Track C (Local) | PNG corpus refresh; Editor-only |

### E1 — Gameplay quality — **S56** (closeout sweep)

Playtest AAR remediation; proxy filter expansion; internal milestone gate (21/21 exit). Not S49 lead per user priority.

### E7 — Commercial launch — **OUT OF S49+ TRAIN**

RC1 is internal engineering only. Store/i18n production remains future optional work.

### E8 — Art production — **OPTIONAL**

Art-bible §5/§7 beyond N/A policy if map/speculative assets require it; not a tracker-row gate.

---

## 5. GitNexus pre-flight map (S49+ hot symbols)

Run before any track touches these symbols. Shared across all sprints.

| Symbol / area | Risk | Touched by | Constraint |
|---------------|------|------------|------------|
| `CatalogWriteGate` | **CRITICAL** | S49 OSINT, S51 corpora | Extend-only; one owner per sprint |
| `DelegationBridge` | **CRITICAL** | Any sim-touching track | ZERO touch; ADR required |
| `OsintCatalogMapper` | HIGH | S49 OSINT | Single modifier; `impact()` before |
| `OsintDigestRunner` | HIGH | S49 OSINT, MCP | Coordinate across MCP/OSINT tracks |
| `BalticBatchRunner` | HIGH | S49 infra, S52 sim-API | No concurrent sim-binding changes |
| `ScenarioPackage` | MED | S49–S50 | Schema evolution must be backward-compatible |
| `Program` (CLI entry) | LOW–MED | S49 MCP, S50 NL editor | Additive changes only |
| `SensorHotPath` | MED | S52 DOTS expand | determinism-engineer review |
| `KesslerRiskMeter` | — | S54 speculative | New symbol; no blast radius |

---

## 6. Prioritization decisions (locked 2026-06-21)

| # | Question | Decision |
|---|----------|----------|
| 1 | Next train naming | **Continue S49+** numbered sprints |
| 2 | Lead epic | **E2 Agentic platform** (S49 dispatch) |
| 3 | RC1 intent | **Internal engineering milestone** — no E7 commercial train |
| 4 | Tracker commitment | **All v1.0-deferred rows** (§3 table — 9 row groups) |
| 5 | Vertical slice / Spirit 1 | **v1.0 milestone CLOSED** — no ongoing gap-remediation program |
| 6 | Filename alias | **`future-sprint-roadpmap.md`** → this file |
| 7 | Parallel model | **4-track fan-out** per sprint; local + cloud combined ≤6 effective tracks |

### Scope boundary (published — S49 dispatch unlocked)

**[`production/post-release-scope-boundary-2026-06-21.md`](../../production/post-release-scope-boundary-2026-06-21.md)** — published 2026-06-21:
- Supersedes v1.0 defer list in [`release-enablement-scope-boundary-2026-06-20.md`](../../production/release-enablement-scope-boundary-2026-06-20.md) for S49+ only.
- Cites §3 committed rows + E2 lead + internal-milestone exit at S56.
- Carries standing invariants from §7 unchanged unless ADR.

---

## 7. Standing invariants (carry forward from Release)

Every S49+ sprint **fails** if any of these invariants regress:

1. **Determinism:** Baltic hash `17144800277401907079` unless golden-updated with ADR.
2. **ReplayGolden 6/6** and **C2 proxy 18/18+** every sprint.
3. **CatalogWriteGate extend-only**; **ZERO DelegationBridge** unless ADR.
4. **Test baseline never regresses** (floor **1227** post-S48; monotonic).
5. **GitNexus discipline:** `impact()` before symbol edits; `detect_changes()` before commit; `rename` not find-replace.
6. **Scope citation:** every story cites `post-release-scope-boundary-2026-06-21.md` + row/epic ID.
7. **Parallel safety:** no two tracks edit the same symbol in the same sprint without pre-coordinated merge order.

---

## 8. Risk register (S49+ program)

| Risk | Like. | Impact | Mitigation |
|------|-------|--------|------------|
| E2 lead starves E3/E4/E5/E6 tracks | Med | High | §10 allocates parallel capacity to non-E2 epics starting S51 |
| All-rows commitment exceeds capacity | High | High | S49–S56 is direction; `/sprint-plan` may split S56 or add sprints |
| Agentic/speculative breaks determinism | Med | Critical | determinism-engineer reviews every sim-touching merge |
| Boundary citations drift | Med | High | S49-01 gate matrix + QA plan enforce boundary citations |
| DelegationBridge pressure (Req 04 follow-ons) | Med | High | ADR before any bridge edit; projection-first default |
| GitNexus index drift | Med | Med | Re-index at sprint open + after major merges |
| **Parallel merge conflict** (same file, 2 tracks) | Med | High | File-ownership matrix per sprint (§10); single-owner per Catalog cluster |
| **Catalog cluster contention** (OSINT + corpora) | Med | High | S49 OSINT owns cluster; S51 corpora extends (staggered sprints) |
| **Determinism hash drift** from concurrent sim edits | Low | Critical | §0.5 shared-resource rules; no concurrent sim-binding changes |

---

## 9. Decisions log (supersedes open questions)

All prioritization questions from the 2026-06-21 draft are **resolved** — see §6.

**Planning artifacts (2026-06-21):**

| Artifact | Path | Status |
|----------|------|--------|
| Post-release scope boundary | [`production/post-release-scope-boundary-2026-06-21.md`](../../production/post-release-scope-boundary-2026-06-21.md) | **Published** |
| Sprint 49 plan | [`production/sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md`](../../production/sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md) | **Published** |
| Sprint 49 kickoff | [`production/agentic/sprint-49-parallel-kickoff-2026-06-21.md`](../../production/agentic/sprint-49-parallel-kickoff-2026-06-21.md) | **Published** |

**Next execution steps:** `/qa-plan sprint` → `/dev-story dispatch S49-01`.

---

## 10. S49–S56 per-sprint parallel decomposition

> **Lead:** E2 from S49. **Exit:** S56 internal gate when 21/21 tracker rows meet MVP-done criteria.
> **Model:** §0. Each sprint decomposes into 2–4 parallel tracks with isolated worktrees.
> **S49 plan:** [`sprint-49-agentic-kickoff-mcp-osint-infra.md`](../../production/sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md)

---

### S49 — E2: MCP/OSINT production + agentic infra foundations

| Est. | ~10–12 days | Dispatch | **READY** |
|------|-------------|----------|-----------|

**Parallel tracks (4):**

| Track | Stories | Owner | Env | Stack prefix | Blocks |
|-------|---------|-------|-----|--------------|--------|
| Baseline + QA | S49-01, S49-02 | devops + qa | Cloud | `stack/sprint49/baseline-qa` | All feature tracks |
| MCP production | S49-03 | c-sharp-engineer | Cloud | `stack/sprint49/mcp-production` | OSINT (MCP tools), Infra (CLI verbs) |
| OSINT production | S49-04, S49-07 | team-data | **Local** | `stack/sprint49/osint-production` | — |
| Agentic infra | S49-05 | c-sharp-engineer + sim | Cloud | `stack/sprint49/agentic-infra` | S50 scenario workers |
| Closeout | S49-06, S49-08 | devops-engineer | **Local** | `stack/sprint49/closeout` | — |

**Dependency graph:**
```
Baseline+QA ──► MCP ──► OSINT (needs MCP tools)
                   │
                   └──► Infra (needs CLI verbs)
```

**File ownership:**
| Files | Track | Risk |
|-------|-------|------|
| `src/ProjectAegis.MissionEditor.Cli/**` | MCP, Infra | MED — additive |
| `tools/mission-editor/mcp-tools.json` | MCP | LOW |
| `src/ProjectAegis.Data/Osint/**` | OSINT | HIGH |
| `src/ProjectAegis.Data/Import/OsintCatalogMapper*` | OSINT | HIGH — single owner |
| `src/ProjectAegis.Delegation/**` (BalticBatchRunner) | Infra | HIGH — no concurrent sim-binding |
| `production/qa/gate-matrix-post-release-*.md` | Baseline+QA | LOW |

**Hard gates:** Test ≥1227, ReplayGolden 6/6, C2 proxy ≥18, Baltic hash immutable, DelegationBridge ZERO.

---

### S50 — E2: Scenario gen workers + Mission Editor NL planner

| Est. | ~10–12 days | Dispatch | After S49 |
|------|-------------|----------|-----------|

**Parallel tracks (3):**

| Track | Stories | Owner | Env | Stack prefix | Depends on |
|-------|---------|-------|-----|--------------|------------|
| Scenario gen workers | S50-01, S50-02 | c-sharp-engineer + sim | Cloud | `stack/sprint50/scenario-workers` | S49 infra (batch schema) |
| Monte Carlo schema | S50-03 | team-simulation | Cloud | `stack/sprint50/monte-carlo` | S49 infra (experiment schema) |
| NL Mission Editor | S50-04, S50-05 | unity-engineer | **Local** | `stack/sprint50/nl-editor` | S49 MCP (tools) + S49 infra (validate) |
| Closeout | S50-06 | devops-engineer | **Local** | `stack/sprint50/closeout` | All |

**Dependency graph:**
```
S49 infra ──► Scenario workers (∥ Monte Carlo schema)
S49 MCP   ──► NL Editor (needs MCP tools + CLI validate)
                  │
                  └── (all three run in parallel)
```

**Coordination:** Monte Carlo schema must not break `ScenarioPackage` backward compatibility. NL Editor runs local for Unity edit-mode verification.

---

### S51 — E5: Full corpora CI + TL runtime fork selection

| Est. | ~10–14 days | Dispatch | After S50 |
|------|-------------|----------|-----------|

**Parallel tracks (3):**

| Track | Stories | Owner | Env | Stack prefix | Depends on |
|-------|---------|-------|-----|--------------|------------|
| Corpora CI pipeline | S51-01, S51-02 | team-data | Cloud | `stack/sprint51/corpora-ci` | — |
| TL runtime fork | S51-03, S51-04 | c-sharp-engineer | Cloud | `stack/sprint51/tl-fork` | — |
| Import cohesion | S51-05 | team-data | Cloud | `stack/sprint51/import-cohesion` | Corpora CI |
| Closeout | S51-06 | devops-engineer | **Local** | `stack/sprint51/closeout` | All |

**Dependency graph:**
```
Corpora CI ──► Import cohesion
TL fork     ∥  (independent)
```

**Coordination:** Corpora track extends `OsintCatalogMapper` (built in S49). TL fork must not regress existing export metadata path. Both tracks are independent — full parallel.

---

### S52 — E6: Multi-k entity gate + sim API / DOTS expand

| Est. | ~10–14 days | Dispatch | After S51 |
|------|-------------|----------|-----------|

**Parallel tracks (3):**

| Track | Stories | Owner | Env | Stack prefix | Depends on |
|-------|---------|-------|-----|--------------|------------|
| Multi-k benchmark | S52-01, S52-02 | team-simulation | Cloud | `stack/sprint52/benchmark` | S51 corpora (test data) |
| Sim API export | S52-03, S52-04 | c-sharp-engineer | Cloud | `stack/sprint52/sim-api` | S49–S50 harness |
| DOTS expand | S52-05, S52-06 | unity-engineer | Cloud | `stack/sprint52/dots-expand` | S45 DOTS pilot |
| Closeout | S52-07 | devops-engineer | **Local** | `stack/sprint52/closeout` | All |

**Dependency graph:**
```
S51 corpora ──► Benchmark ∥ Sim-API ∥ DOTS expand
                (all three fully parallel)
```

**Coordination:** All three tracks stress-test the simulation. determinism-engineer reviews all merges. Baltic hash must remain `17144800277401907079`. DOTS track must not regress existing non-DOTS sensor path.

---

### S53 — E3: Near-future full DOTS spawn + MASS tier

| Est. | ~10–14 days | Dispatch | After S52 |
|------|-------------|----------|-----------|

**Parallel tracks (2):**

| Track | Stories | Owner | Env | Stack prefix | Depends on |
|-------|---------|-------|-----|--------------|------------|
| DOTS spawn | S53-01, S53-02 | unity-engineer | Cloud | `stack/sprint53/dots-spawn` | S52 DOTS expand |
| MASS tier | S53-03, S53-04 | team-simulation | Cloud | `stack/sprint53/mass-tier` | S52 benchmark |
| Closeout | S53-05 | devops-engineer | **Local** | `stack/sprint53/closeout` | All |

**Dependency graph:**
```
S52 DOTS expand ──► DOTS spawn ∥ MASS tier
```

**Coordination:** Isolated fixtures first; no production hash change without golden ADR. Both tracks are independent (separate code paths — DOTS in ECS layer, MASS in simulation tier).

---

### S54 — E3: Speculative systems (orbital DEW, escalation)

| Est. | ~10–14 days | Dispatch | After S53 |
|------|-------------|----------|-----------|

**Parallel tracks (2):**

| Track | Stories | Owner | Env | Stack prefix | Depends on |
|-------|---------|-------|-----|--------------|------------|
| Orbital DEW runtime | S54-01, S54-02 | team-simulation | Cloud | `stack/sprint54/orbital-dew` | S53 DOTS spawn (ECS infra) |
| Escalation ladder | S54-03, S54-04 | team-simulation | Cloud | `stack/sprint54/escalation` | — |
| Closeout | S54-05 | devops-engineer | **Local** | `stack/sprint54/closeout` | All |

**Dependency graph:**
```
S53 DOTS spawn ──► Orbital DEW ∥ Escalation ladder
```

**Coordination:** `KESSLER_RISK_METER` is a new symbol — no blast radius. Both tracks add speculative systems scoped behind feature flags; no production path regression.

---

### S55 — E4: Cesium/globe production + hypersonic C2 UI

| Est. | ~10–12 days | Dispatch | After S54 |
|------|-------------|----------|-----------|

**Parallel tracks (3):**

| Track | Stories | Owner | Env | Stack prefix | Depends on |
|-------|---------|-------|-----|--------------|------------|
| Cesium globe | S55-01, S55-02 | unity-engineer | Cloud | `stack/sprint55/cesium` | — |
| Hypersonic UI | S55-03, S55-04 | c-sharp-engineer | Cloud | `stack/sprint55/hypersonic` | — |
| Editor PNG evidence | S55-05 | unity-engineer | **Local** | `stack/sprint55/editor-evidence` | Cesium globe (code) |
| Closeout | S55-06 | devops-engineer | **Local** | `stack/sprint55/closeout` | All |

**Dependency graph:**
```
Cesium globe ∥ Hypersonic UI ∥ Editor evidence (shadow: code done → screenshot)
```

**Coordination:** Cloud builds Cesium production path + hypersonic alert UI. Local captures Editor PNG evidence after cloud merges (shadow pattern). `HYPERSONIC_ALERT` is new UI — no blast radius.

---

### S56 — E1 + exit gate: Playtest AAR sweep + 21/21 milestone

| Est. | ~5–7 days | Dispatch | After S55 |
|------|-----------|----------|-----------|

**Parallel tracks (2):**

| Track | Stories | Owner | Env | Stack prefix | Depends on |
|-------|---------|-------|-----|--------------|------------|
| AAR remediation | S56-01, S56-02 | team-gameplay | Cloud | `stack/sprint56/aar-sweep` | — |
| Proxy filter expand | S56-03 | c-sharp-engineer | Cloud | `stack/sprint56/proxy-filter` | — |
| Internal gate | S56-04 | devops-engineer | **Local** | `stack/sprint56/gate` | All 21 rows |

**Exit criteria:**
- [ ] All 21 tracker rows at MVP-done or documented Partial+
- [ ] Test baseline ≥ prior sprint (monotonic)
- [ ] ReplayGolden 6/6, C2 proxy ≥18
- [ ] Baltic hash `17144800277401907079` (or golden ADR)
- [ ] DelegationBridge ZERO
- [ ] Gate document: `production/gate-checks/s56-internal-engineering-gate-*.md`
- [ ] Human ack on 21/21 status

---

### Program infrastructure (reuse S39–S48 patterns)

| Artifact | Path (proposed @ S49 plan) |
|----------|----------------------------|
| Scope boundary | [`production/post-release-scope-boundary-2026-06-21.md`](../../production/post-release-scope-boundary-2026-06-21.md) |
| S49 kickoff | [`production/agentic/sprint-49-parallel-kickoff-2026-06-21.md`](../../production/agentic/sprint-49-parallel-kickoff-2026-06-21.md) |
| Worktree manifest | `production/agentic/s49-s56-worktree-manifest.md` (TBD @ S49-01) |
| Local/cloud routing | `production/agentic/local-cloud-agent-routing.md` (existing) |
| Program guide | `production/agentic/s49-s56-program-execution-guide.md` |

**Total program (S49–S56):** ~75–95 calendar days with 2–4 parallel tracks inside each sprint (estimate only).

---

## 11. Related artifacts

| Artifact | Path |
|----------|------|
| Stable alias (this doc) | [`future-sprint-roadpmap.md`](future-sprint-roadpmap.md) |
| Prior snapshot (pre-S40) | [`future-sprint-roadpmap-062026.md`](future-sprint-roadpmap-062026.md) |
| v1.0 release boundary (archived) | [`production/release-enablement-scope-boundary-2026-06-20.md`](../../production/release-enablement-scope-boundary-2026-06-20.md) |
| S48 gate / RC1 close | [`production/gate-checks/s48-release-gate-2026-06-20.md`](../../production/gate-checks/s48-release-gate-2026-06-20.md) |
| Vertical slice milestone (closed) | [`production/milestones/vertical-slice-mvp.md`](../../production/milestones/vertical-slice-mvp.md) |
| Implementation tracker | [`Game-Requirements/implementation-tracker-2026-06-04.md`](../../Game-Requirements/implementation-tracker-2026-06-04.md) |
| Playtest AAR (S56 input) | [`game-players-report-0620206.md`](../../game-players-report-0620206.md) |
| Post-release boundary | [`production/post-release-scope-boundary-2026-06-21.md`](../../production/post-release-scope-boundary-2026-06-21.md) |
| Sprint 49 plan | [`production/sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md`](../../production/sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md) |
| Sprint 49 parallel kickoff | [`production/agentic/sprint-49-parallel-kickoff-2026-06-21.md`](../../production/agentic/sprint-49-parallel-kickoff-2026-06-21.md) |

---

## 12. Dependency & coordination matrix

### Cross-sprint dependency chain

```
S49 ──────► S50 ──────► S51 ──────► S52 ──────► S53 ──────► S54 ──────► S55 ──────► S56

MCP ───► NL Editor
OSINT ──► (free)
Infra ──► Scenario ─► (free)              Sim-API
          MonteCarlo                      │
                    Corpora ─► Benchmark ─┤
                    TL fork   DOTS exp ───► DOTS sp ─► Orb DEW
                                           MASS ────► Escal
                                                          Cesium ─► Evid
                                                          Hyp UI
                                                                     AAR sweep
                                                                     Proxy fil
                                                                     GATE
```

### What CAN run in parallel (cross-sprint)

| Pair | Sprints | Reasoning |
|------|---------|-----------|
| MCP ∥ OSINT ∥ Infra ∥ Baseline+QA | S49 | Four independent tracks; MCP tools feed OSINT/Infra mid-sprint |
| Scenario workers ∥ Monte Carlo schema | S50 | Both depend on S49 infra; independent sub-domains |
| Corpora CI ∥ TL fork | S51 | Separate code paths; no shared symbols |
| Benchmark ∥ Sim-API ∥ DOTS expand | S52 | Separate code; all depend on earlier sprints but not each other |
| DOTS spawn ∥ MASS tier | S53 | ECS layer vs simulation tier — no overlap |
| Orbital DEW ∥ Escalation ladder | S54 | Both speculative; feature-flagged; no shared runtime |
| Cesium ∥ Hypersonic UI ∥ Editor evidence | S55 | Fully independent (shadow pattern for evidence) |
| AAR sweep ∥ Proxy filter | S56 | Playtest data vs C2 proxy code |

### What MUST be serial (cross-sprint)

| Dependency | Reason |
|------------|--------|
| S49 infra → S50 scenario/Monte Carlo | Schema definition must precede worker implementation |
| S49 MCP → S50 NL Editor | NL Editor consumes MCP tools |
| S50 harness → S52 sim-API | Sim API exports what the harness validates |
| S51 corpora → S52 benchmark | Benchmark needs corpora test data |
| S52 DOTS expand → S53 DOTS spawn | Incremental DOTS capability |
| S53 DOTS spawn → S54 orbital DEW | Orbital DEW uses DOTS ECS infrastructure |
| All 21 rows → S56 gate | Gate verifies cumulative result |

### Shared-resource contention map

| Resource | S49 | S50 | S51 | S52 | S53 | S54 | S55 | S56 |
|----------|-----|-----|-----|-----|-----|-----|-----|-----|
| `CatalogWriteGate` | OSINT | — | Corpora | — | — | — | — | — |
| `OsintCatalogMapper` | OSINT | — | Corpora | — | — | — | — | — |
| `BalticBatchRunner` | Infra | — | — | Sim-API | — | — | — | — |
| `DelegationBridge` | Z | Z | Z | Z | Z | Z | Z | Z |
| Baltic hash | R/O | R/O | R/O | R/O | R/O | R/O | R/O | R/O |

**Staggered safely:** OSINT (S49) → Corpora (S51) — two sprints apart, no concurrent edits to `CatalogWriteGate` or `OsintCatalogMapper`.
**Z = ZERO touch.** R/O = read-only verification only.

---

*Grounded in: S48 release gate; user prioritization 2026-06-21; post-release-scope-boundary; S49 sprint plan; parallel kickoff pattern (S39–S48 proven). Update via `/sprint-plan` for S50+. Each sprint dispatched via `production/agentic/sprint-{N}-parallel-kickoff-*.md`.*
