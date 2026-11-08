# Future Sprint Roadmap — Project Aegis (cmano-clone)
> **Parallel-Agentic Edition — Post–S72 E7 Prep (S73+ Baltic v3)**

> **Status:** Living document. Authored **2026-06-25** (revision `.01`); supersedes planning intent in [`future-sprint-roadpmap-062526.md`](future-sprint-roadpmap-062526.md) (2026-06-25 S69–S72 E7 program, now archived).
> **Edition:** Optimized for serial sprints S73–S80 (E9 lead) with parallel tracks inside each sprint; stage **Release** throughout; code + content allowed (unlike E7 docs-only); GitNexus mandatory; verification-before on all claims.
> **Stable alias:** [`future-sprint-roadpmap.md`](future-sprint-roadpmap.md) → this file.
> **Execute plan:** [`roadmap-execute-plan-062526.01.md`](roadmap-execute-plan-062526.01.md)
> **Design spec:** [`docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md`](../superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md)
> **Prior execute plan (archived program):** [`roadmap-execute-plan-062526.md`](roadmap-execute-plan-062526.md) (S69–S72 COMPLETE).
> **GitNexus @ doc authoring (2026-06-25):** MCP `list_repos` canonical `/home/username01/projects/active/cmano-clone/cmano-clone` — **20,193** symbols / **37,859** edges / **2,487** files (pre S73-03). Post S73-03 re-index (CLI analyze --force + MCP): **20,322** symbols / **38,055** edges / **2,491** files @ HEAD `b2c9411` fresh (no staleness). `impact` upstream summaryOnly: **CatalogWriteGate 178**, **PatrolCandidateEngagePolicy 97**, **DelegationBridge 127**, **BalticReplayHarness 52** (exact §5 match). `detect_changes` unstaged low (docs only). S73-03 COMPLETE. Re-index @ S73-03.
> **Verification @ doc authoring:** build 0e/0w; test **1232/0f** (Sim 279, Cli 43, Del 247, Excel 5, UA 252, Data 406); ReplayGolden **6/6**; C2 proxy **18/18**; hash **`17144800277401907079`** preserved; ZERO `DelegationBridge` source edits.
> **Stage:** **Release** (`production/stage.txt`) — S72 commercial launch prep complete (human ack 2026-06-25); **no stage advance** at S80 unless explicit new decision.
> **Closed milestones:** S39–S48 Release enablement; S49–S56 internal engineering; **S57–S64 Baltic v2**; **S65–S68 release train**; **S69–S72 E7 commercial launch prep**.
> **Active program:** **S73–S80 Baltic v3 content expansion (E9 lead)** — 8-sprint train mirroring S57–S64 pattern.

> This roadmap is **direction, not a commitment**. Per `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md`,
> each sprint is still planned via `/sprint-plan` with user approval. Filename retains the
> `roadpmap` spelling for link stability.

---

## 0. Parallel execution model (S73+ program)

Every sprint is a **serial program** (S73 → … → S80) with **parallel dispatch** inside each sprint — multiple agent tracks run concurrently in isolated git worktrees, merging at sprint-close. Model proven S39–S72; see [`future-sprint-roadpmap-062226.md`](future-sprint-roadpmap-062226.md) §0 and [`future-sprint-roadpmap-062426.md`](future-sprint-roadpmap-062426.md) §0 for full protocol reference.

### 0.1 Agent environments

| Env | Capacity | Suited for | Not suited for |
|-----|----------|------------|----------------|
| **Local** | ≤6 concurrent | Closeout/merge, playtest capture, gate verification, human sign-off | Mass CI-only hygiene |
| **Cloud Agent** | ≤5 concurrent | Scenario/content code, catalog ingest, docs, replay goldens | Unity Editor PNG capture |
| **Combined** | 4–6 effective tracks | — | — |

**Routing:** `production/agentic/local-cloud-agent-routing.md` — cloud handles code/tests/content; local owns closeout + coordinator merge + human playtest gates.

### 0.2 Worktree strategy

```
.worktrees/stack/sprint{N}/{track-slug}/
```

| Convention | Example | Purpose |
|------------|---------|---------|
| Stack prefix | `stack/sprint73/baltic-v3-boundary` | Graphite stack grouping |
| Track slug | `scenario-wave`, `theater-oob`, `replay-goldens`, `closeout` | Unique per sprint |
| Closeout track | `stack/sprint{N}/closeout` | Merge coordinator (always local) |

### 0.3 Dispatch patterns (S73+ emphasis)

| Pattern | When | Example |
|---------|------|---------|
| **Fan-out** | Independent tracks | S73: boundary ∥ playtest-prep ∥ re-index |
| **Pipeline** | Goldens depend on scenarios | S74: scenarios → replay goldens (mid-sprint) |
| **Shadow** | Cloud builds content, local captures playtest | S79: automated batch (cloud) + human template (local) |
| **Gate** | Human + automated loop | S80 → Baltic v3 content gate |

### 0.4 Merge gate protocol (every sprint close)

1. All tracks `gt submit` their stacks.
2. Closeout track runs `gt restack` on trunk `main`.
3. Verify: `dotnet build ProjectAegis.sln && dotnet test ProjectAegis.sln -v minimal`.
4. Hard gates pass (determinism, replay, proxy, test floor) → merge.
5. GitNexus re-index after merge.

**Graphite:** `gt sync`, `gt restack`, `gt submit --stack --no-interactive` — see [`docs/engineering/graphite-github-substitute-plan.md`](../engineering/graphite-github-substitute-plan.md).

### 0.5 Shared-resource coordination (S73+)

| Resource | Access pattern | Coordination rule |
|----------|---------------|-------------------|
| `PatrolCandidateEngagePolicy` | S73 E1 if new AAR topics | CRITICAL — single owner; golden ADR if production hash touched |
| `CatalogWriteGate` | S77 catalog track | CRITICAL — extend-only; one owner per sprint |
| `DelegationBridge` | Any | **ZERO touch** — ADR required before any edit |
| `BalticReplayHarness` | S74–S76 goldens | CRITICAL — isolated fixtures first; read/test in gate |
| Production hash `17144800277401907079` | Read-mostly | Immutable unless golden ADR + replay 6/6 |
| `baltic-v2-*` corpus | Read / extend | v3 uses `baltic-v3-*` prefix until S80 promotion decision |
| Test baseline (`≥1232`) | Monotonic | Post–S72 floor; no track may regress |

### 0.6 Pre-flight checklist (per track)

- [ ] GitNexus `impact()` on every symbol in file-ownership matrix
- [ ] Report risk level (CRITICAL/HIGH → user ack before editing)
- [ ] Confirm worktree isolation (`git worktree list`)
- [ ] Cite `production/baltic-v3-scope-boundary-2026-06-25.md` + row/epic ID (publish @ S73-01)
- [ ] Verify test baseline passes before any change

---

## 1. Where we are (post–S72 E7 prep gate)

| Dimension | State | Evidence |
|---|---|---|
| Stage | **Release** — RC1 + internal + Baltic v2 + release train + E7 prep complete | `production/stage.txt`, [`s72-commercial-launch-prep-gate-2026-06-25.md`](../../production/gate-checks/s72-commercial-launch-prep-gate-2026-06-25.md) |
| Closed milestone (Baltic v2) | **S57–S64 — CLOSED** | [`s57-s64-program-closeout-2026-06-22.md`](../../production/qa/s57-s64-program-closeout-2026-06-22.md) |
| Closed milestone (release train) | **S65–S68 E10 — CLOSED** | [`s68-release-train-gate-2026-06-25.md`](../../production/gate-checks/s68-release-train-gate-2026-06-25.md) |
| Closed milestone (E7 prep) | **S69–S72 — CLOSED** | [`s72-commercial-launch-prep-gate-2026-06-25.md`](../../production/gate-checks/s72-commercial-launch-prep-gate-2026-06-25.md) + human ack 2026-06-25 |
| Last sprint | **S72 complete** — commercial launch prep gate | S72 gate + ack 2026-06-25 |
| Next sprint | **S73 planned** — Baltic v3 foundations | §10 |
| Test baseline | **1232/1232** headless, **ReplayGolden 6/6**, **C2 proxy 18/18** | Verified 2026-06-25 |
| Baltic v2 content | **10** `baltic-v2-*` policies; **9** v2 replay goldens | `data/scenarios/`, `tests/regression/` |
| E7 prep artifacts | Checklist v3, store drafts, i18n spec, launch pack | `production/release/` (see §2) |
| Tracker | **21/21 MVP-done or Partial+** (closed at S56) | v3 adds content depth, not row re-count |

**What S69–S72 delivered (closed):**

- **E7:** `commercial-launch-scope-boundary-2026-06-25.md`; gate matrix; store drafts (`production/release/store/`); `community-templates.md`; `release-checklist-v3.md`; i18n pipeline spec + string inventory + extraction plan; launch doc pack (`production/release/launch/`); l10n QA plan; S72 gate + human ack **"commercial launch prep complete"**.
- Stage remained **Release** (prep ≠ Launch).
- GitNexus re-index through program (final ~20,174 nodes pre closeout per sprint-status).

---

## 2. Completed program archive

### Track D — E7 Commercial Launch Prep (S69–S72) — COMPLETE (2026-06-25)

| Sprint | Epic(s) | Primary outcome | Closeout |
|--------|---------|-----------------|----------|
| **S69** | E7 | Commercial boundary + gate matrix + GitNexus re-index | PASS 2026-06-25 |
| **S70** | E7 | Store drafts + community templates + checklist v3 skeleton | PASS 2026-06-25 |
| **S71** | E7 | i18n spec + launch doc pack + l10n QA plan | PASS 2026-06-25 |
| **S72** | Gate | Full verification + human ack (prep complete) | PASS + ack 2026-06-25 |

**Archive:** [`commercial-launch-scope-boundary-2026-06-25.md`](../../production/commercial-launch-scope-boundary-2026-06-25.md) (superseded for S73+), [`roadmap-execute-plan-062526.md`](roadmap-execute-plan-062526.md), smoke closeouts `production/qa/smoke-sprint-{69..72}-closeout-2026-06-25.md`.

### Prior programs — see archived roadmaps

| Program | Roadmap snapshot |
|---------|------------------|
| S65–S68 release train | [`future-sprint-roadpmap-062426.md`](future-sprint-roadpmap-062426.md) |
| S57–S64 Baltic v2 | [`future-sprint-roadpmap-062226.md`](future-sprint-roadpmap-062226.md) |
| S49–S56 internal eng | [`future-sprint-roadpmap-062126.md`](future-sprint-roadpmap-062126.md) |
| S39–S48 Release enablement | [`future-sprint-roadpmap-062026.md`](future-sprint-roadpmap-062026.md) |

---

## 3. S73–S80 committed scope — Baltic v3 content expansion

User decision **2026-06-25:** next train optimizes for **Baltic v3 / E9 content expansion** — new scenario family, theater slices, mission narrative, catalog content, C2 UX, structured playtest loop v3, and S80 content gate. **8-sprint train (S73–S80)** mirroring S57–S64. Stage remains **Release** until explicit S80 decision.

| Theme | Req touchpoints | Sprint(s) | ∥ Tracks | Primary deliverable |
|-------|-----------------|-----------|----------|---------------------|
| **V3 foundations + playtest prep** | 01, 17, E1 | S73 | 3–4 | `baltic-v3-scope-boundary-2026-06-25.md`; playtest manifest v3; GitNexus re-index |
| **Scenario content wave 2** | 02, 03, 13 | S74 | 3 | New `baltic-v3-*` patrol/mission variants; isolated replay goldens |
| **Theater package expansion** | 01, 06, 18 | S75 | 2 | Extended OOB v3; optional regional slice spike |
| **Mission events & narrative** | 02, 03, 17 | S76 | 2 | Contact-window / narrative arc policies (extend v2 patterns) |
| **Catalog & platform content** | 06, 16, 21 | S77 | 2 | Unit/loadout slices for v3 scenarios; Excel round-trip |
| **C2 scenario UX v3** | 20, 12 | S78 | 2 | Scenario picker v3 bands; difficulty/tooltip pass for v3 manifest |
| **Playtest loop v3** | 01, 17, E1 | S79 | 2 | Automated batch + human session template; fun-hypothesis refresh |
| **Baltic v3 gate** | (cross) | S80 | 1–2 | Content sign-off + playtest PASS; promotion decision for v3 corpus |

**Program exit criterion (S73–S80):** Baltic v3 **content-complete** for expanded scenario/theater family + **playtest sign-off** (automated + human sessions per difficulty band) + standing invariants — **not** E7 store submission (prep done at S72) and **not** automatic Launch stage advance.

**Still out of scope (unless new scope decision):** E7 store submission / paid marketing, production i18n translation execution, multiplayer, global campaigns, `DelegationBridge` edits, production hash change without ADR, full Req Partial→MVP-done sweep.

---

## 4. Epic buckets (S73+ program map)

```
 S72 CLOSED ──► S73 ──► S74 ──► S75 ──► S76 ──► S77 ──► S78 ──► S79 ──► S80 gate

 Parallel tracks per sprint (example):
 ┌────────────────────────────────────────────────────────────────────────────┐
 │ S73 Found.   S74 Scenarios  S75 Theater   S76 Mission  S77 Catalog S78 C2  │
 │ boundary     wave 2         OOB v3        events       ingest    UX v3     │
 │ ∥ playtest   ∥ goldens      ∥ hash fam    ∥ briefings  ∥ Excel   ∥ bands   │
 │ ∥ re-index                                                              S79 playtest ∥ S80 GATE │
 └────────────────────────────────────────────────────────────────────────────┘

 E9 Baltic v3 ★   E1 (S73/S79)   E5 (S77)   E4 (S78)   E7 (hold — prep done)
```

### E9 — Baltic v3 content & theater expansion ★ **LEAD** (S74–S80)

| Theme | Sprint | Tracks | Notes |
|-------|--------|--------|-------|
| Scenario family v3 | S74 | 2–3 | `baltic-v3-*` prefix; isolated goldens before promotion |
| Theater package | S75 | 2 | Extend v2 OOB patterns; optional second regional slice @ S80 |
| Mission narrative | S76 | 2 | Extend `baltic-v2-*-narrative-*` patterns |
| Content ingest | S77 | 2 | Catalog slices; extend-only `CatalogWriteGate` |
| C2 presentation | S78 | 2 | Picker + bands for v3 manifest |

### E1 — Gameplay quality (S73, S79)

New AAR/playtest topics from post–S72 corpus only — no repeat of S57 Topic 1 unless regression found.

### E7 — Commercial launch — **ON HOLD**

Prep complete at S72; submission remains a **future decision** outside S73–S80 unless scope amended.

### E10 — Release ops — **MAINTENANCE**

Release checklist v3 + v2 corpus remain authoritative; no release-train code churn unless CI drift found.

---

## 5. GitNexus pre-flight map (S73+ hot symbols)

| Symbol / area | Risk | Touched by | Constraint |
|---------------|------|------------|------------|
| `DelegationBridge` | **CRITICAL** | Any | ZERO touch |
| `CatalogWriteGate` | **CRITICAL** | S77 catalog | Extend-only; single owner |
| `PatrolCandidateEngagePolicy` | **CRITICAL** | S73 E1 if needed | Single owner; golden ADR if behavior change |
| `BalticReplayHarness` | **CRITICAL** | S74–S76 | Isolated fixtures; no production hash change w/o ADR |
| `UnifiedReleaseTrainManifest` | MED | S80 promotion | Extend-only if v3 corpus recorded |
| `ScenarioPackage` / policy JSON | MED | S74–S76 | Backward-compatible schema |
| `C2TopBarPanelHost` / scenario picker | MED | S78 UX | Additive UI only |

**Verified @ authoring:** CatalogWriteGate **178**, Patrol **97**, Bridge **127**, Baltic **52** — exact match to prior boundaries.

---

## 6. Prioritization decisions (locked 2026-06-25)

| # | Question | Decision |
|---|----------|----------|
| 1 | S69–S72 program status | **CLOSED** — human ack 2026-06-25 |
| 2 | Next train focus | **S73–S80 Baltic v3 / E9 content expansion** |
| 3 | Sprint count | **8 sprints** (mirror S57–S64) |
| 4 | Stage advance | **Stay at Release** through S80 unless explicit gate decision |
| 5 | Lead epic | **E9 Baltic v3 content** (S74–S80); S73 foundations prerequisite |
| 6 | Filename | **`future-sprint-roadpmap-062526.01.md`** snapshot; update stable alias on publish |
| 7 | E7 commercial | **On hold** — prep complete; submission out of S73–S80 |
| 8 | v2 corpus | **Frozen** as shippable baseline; v3 uses isolated prefix until S80 |

### Scope boundary (publish @ S73-01)

**`production/baltic-v3-scope-boundary-2026-06-25.md`** — draft at S73 planning:

- Supersedes [`commercial-launch-scope-boundary-2026-06-25.md`](../../production/commercial-launch-scope-boundary-2026-06-25.md) for S73+ only (archived, not deleted).
- Cites §3 committed themes + E9 lead + Baltic v3 exit at S80.
- Carries standing invariants from §7 unchanged unless ADR.

---

## 7. Standing invariants (carry forward)

Every S73+ sprint **fails** if any invariant regresses:

1. **Determinism:** Production Baltic hash `17144800277401907079` unless golden-updated with ADR (v3 scenarios use isolated hashes until S80 promotion).
2. **ReplayGolden 6/6** and **C2 proxy 18/18+** every sprint.
3. **CatalogWriteGate extend-only**; **ZERO DelegationBridge** unless ADR.
4. **Test baseline never regresses** (floor **1232** post–S72; monotonic).
5. **GitNexus discipline:** `impact()` before symbol edits; `detect_changes()` before commit.
6. **Scope citation:** every story cites `baltic-v3-scope-boundary-2026-06-25.md` + epic/theme ID.
7. **Parallel safety:** no two tracks edit the same CRITICAL symbol in the same sprint.

---

## 8. Risk register (S73+ program)

| Risk | Like. | Impact | Mitigation |
|------|-------|--------|------------|
| v3 scenarios break production hash | Med | Critical | Isolated `baltic-v3-*` + goldens; ADR before promotion |
| Content sprawl without playtest | High | High | S79 playtest loop; S80 gate requires human session |
| Catalog contention (S77) | Med | Med | Single owner; stagger from S74 scenario track |
| GitNexus index drift | Med | Med | Re-index @ S73 open + after each merge |
| Scope creep into E7 submission | Med | High | Boundary out-of-scope list; cite on every story |
| v2/v3 policy fork drift | Med | High | Promote v3 to production only at S80 with full golden ADR |

---

## 9. Decisions log & planning artifacts

**Resolved 2026-06-25** — see §6.

**Planning artifacts (to create @ S73):**

| Artifact | Path | Status |
|----------|------|--------|
| Baltic v3 scope boundary | `production/baltic-v3-scope-boundary-2026-06-25.md` | **Draft @ S73-01** |
| Execute plan | [`roadmap-execute-plan-062526.01.md`](roadmap-execute-plan-062526.01.md) | **Published** |
| Design spec | [`docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md`](../superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md) | **Published** |
| Sprint 73 plan | `production/sprints/sprint-73-baltic-v3-foundations.md` | **Draft @ dispatch** |
| Sprint 73 kickoff | `production/agentic/sprint-73-parallel-kickoff-2026-06-25.md` | **Draft @ dispatch** |

**Next execution steps:** Publish boundary → finalize sprint plan → `/qa-plan sprint` → `/dev-story dispatch S73-01`.

---

## 10. S73–S80 per-sprint parallel decomposition

> **Lead:** E9 from S74 (S73 is foundations). **Exit:** S80 Baltic v3 content gate.
> **Model:** §0. Each sprint: 2–4 parallel tracks with isolated worktrees.

---

### S73 — E9/E1: Baltic v3 foundations + playtest prep

| Est. | ~5–7 days | Dispatch | **READY TO PLAN** |
|------|-----------|----------|-------------------|

**Parallel tracks (4):**

| Track | Stories | Owner | Env | Depends on | Feeds |
|-------|---------|-------|-----|------------|-------|
| Scope boundary | S73-01 | producer | **Local** | — | All tracks |
| Playtest manifest v3 | S73-02 | qa-lead | Cloud | — | S79 loop |
| GitNexus re-index | S73-03 | devops-engineer | Cloud | — | — |
| Closeout | S73-04 | devops-engineer | **Local** | All | — |

**Scope:** Publish baltic-v3 boundary; draft v3 playtest manifest (extends v2 index); re-index GitNexus @ HEAD; optional E1 spike only if new AAR topic identified.

**Hard gates:** Test ≥1232, ReplayGolden 6/6, C2 proxy ≥18, production Baltic hash immutable, ZERO bridge.

---

### S74 — E9: Scenario content wave 2

| Est. | ~8–10 days | Dispatch | After S73 |
|------|------------|----------|-----------|

**Parallel tracks (3):**

| Track | Stories | Owner | Env |
|-------|---------|-------|-----|
| Scenario policies v3 | S74-01, S74-02 | producer | Cloud |
| Replay goldens (isolated) | S74-03 | c-sharp-test-engineer | Cloud |
| Closeout | S74-04 | devops-engineer | **Local** |

**Scope:** New `baltic-v3-*` policies + isolated replay goldens; do not mutate v2 production goldens without ADR.

---

### S75 — E9: Theater package expansion v3

| Est. | ~8–10 days | Dispatch | After S74 |

**Tracks:** theater OOB ∥ regional slice spike (optional) ∥ closeout.

---

### S76 — E9: Mission events & narrative arcs v3

| Est. | ~8–10 days | Dispatch | After S75 |

**Tracks:** mission-event policies ∥ narrative arc fixtures ∥ closeout.

---

### S77 — E9+E5: Catalog & platform content

| Est. | ~8–10 days | Dispatch | After S76 |

**Tracks:** catalog slices ∥ platform Excel round-trip ∥ closeout. **Single owner** for `CatalogWriteGate` if touched.

---

### S78 — E4+E9: C2 scenario UX v3

| Est. | ~6–8 days | Dispatch | After S77 |

**Tracks:** scenario picker v3 ∥ difficulty bands/tooltips ∥ closeout. Additive UI only.

---

### S79 — E1+E9: Playtest loop v3

| Est. | ~8–10 days | Dispatch | After S78 |

**Tracks:** automated playtest batch (cloud) ∥ human session template (local) ∥ closeout.

---

### S80 — Baltic v3 content gate

| Est. | ~5–7 days | Dispatch | After S79 |

**Parallel tracks (2):**

| Track | Stories | Owner | Env |
|-------|---------|-------|-----|
| Gate verification | S80-01 | devops-engineer | **Local** |
| Human sign-off + promotion decision | S80-02 | producer | **Local** |

**Exit criteria (S80):**

- [ ] S73–S79 closeouts PASS
- [ ] v3 scenario manifest + goldens indexed
- [ ] Playtest sign-off (automated + human per band)
- [ ] Test baseline ≥1232; ReplayGolden 6/6; C2 proxy ≥18
- [ ] Production Baltic hash unchanged OR golden ADR documented
- [ ] Gate document: `production/gate-checks/s80-baltic-v3-content-gate-2026-06-*.md`
- [ ] Human ack on Baltic v3 content-complete
- [ ] Optional: promote selected `baltic-v3-*` to production corpus (explicit decision only)
- [ ] Stage: default **stay Release** unless separate Launch decision

**Total program (S73–S80):** ~52–68 calendar days with 2–4 parallel tracks (estimate only).

---

## 11. Related artifacts

| Artifact | Path |
|----------|------|
| Stable alias (update on publish) | [`future-sprint-roadpmap.md`](future-sprint-roadpmap.md) |
| Prior snapshot (S69–S72) | [`future-sprint-roadpmap-062526.md`](future-sprint-roadpmap-062526.md) |
| Prior execute plan (S69–S72) | [`roadmap-execute-plan-062526.md`](roadmap-execute-plan-062526.md) |
| Execute plan | [`roadmap-execute-plan-062526.01.md`](roadmap-execute-plan-062526.01.md) |
| Design spec | [`docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md`](../superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md) |
| S72 gate | [`production/gate-checks/s72-commercial-launch-prep-gate-2026-06-25.md`](../../production/gate-checks/s72-commercial-launch-prep-gate-2026-06-25.md) |
| E7 prep boundary (archived) | [`production/commercial-launch-scope-boundary-2026-06-25.md`](../../production/commercial-launch-scope-boundary-2026-06-25.md) |
| Baltic v2 boundary (reference) | [`production/baltic-v2-scope-boundary-2026-06-22.md`](../../production/baltic-v2-scope-boundary-2026-06-22.md) |
| v2 playtest index | [`production/qa/evidence/baltic-v2-playtest-index.md`](../../production/qa/evidence/baltic-v2-playtest-index.md) |
| Release checklist v3 | [`production/release/release-checklist-v3.md`](../../production/release/release-checklist-v3.md) |
| v2 scenario manifest | [`production/playtests/baltic-v2-scenario-manifest.yaml`](../../production/playtests/baltic-v2-scenario-manifest.yaml) |

---

## 12. Dependency & coordination matrix

### Cross-sprint dependency chain

```
S73 ──► S74 ──► S75 ──► S76 ──► S77 ──► S78 ──► S79 ──► S80

Boundary ─► Scenarios ─► Theater ─► Mission ─► Catalog ─► C2 ─► Playtest ─► GATE
 re-index     goldens      OOB        events     ingest    UX     loop
```

### What CAN run in parallel (within sprint)

| Sprint | Parallel pair | Reasoning |
|--------|---------------|-----------|
| S73 | Boundary ∥ playtest prep ∥ re-index | Independent; boundary day 1 |
| S74 | Scenarios ∥ goldens (goldens trail mid-sprint) | Goldens need scenario shapes |
| S77 | Catalog ∥ Excel (stagger if same symbols) | Single CatalogWriteGate owner |
| S79 | Automated ∥ human prep | Human template uses automated manifest |
| S80 | Verification ∥ sign-off prep | Serial within sprint; human gate last |

### What MUST be serial (cross-sprint)

| Dependency | Reason |
|------------|--------|
| S73 boundary → S74 content | v3 prefix + manifest locked in boundary |
| S74 scenarios → S75 theater | OOB references scenario IDs |
| S78 C2 UX → S79 playtest | Picker bands needed for human sessions |
| All → S80 gate | Gate verifies cumulative v3 corpus |

---

*Grounded in: S69–S72 E7 prep COMPLETE (human ack 2026-06-25); user prioritization 2026-06-25 (E9 Baltic v3, S73–S80); GitNexus fresh 20322/38055 post S73-03 re-index COMPLETE; test baseline 1232 verified 2026-06-25. Update via `/sprint-plan` for S73+. Each sprint dispatched via `production/agentic/sprint-{N}-parallel-kickoff-*.md`. Cite `production/baltic-v3-scope-boundary-2026-06-25.md` on all artifacts after S73-01.*
