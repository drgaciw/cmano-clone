# Future Sprint Roadmap — Project Aegis (cmano-clone)
> **Parallel-Agentic Edition — Post–Baltic v2 (S64)**

> **Status:** Living document. Authored **2026-06-24**; supersedes planning intent in [`future-sprint-roadpmap-062226.md`](future-sprint-roadpmap-062226.md) (2026-06-22 S57–S64 program, now archived).
> **Edition:** Optimized for parallel agentic development — see §0 for dispatch model, §10 for per-sprint track decomposition, §12 for dependency matrix.
> **Stable alias:** [`future-sprint-roadpmap.md`](future-sprint-roadpmap.md) → this file.
> **Execute plan:** [`roadmap-execute-plan-062426.md`](roadmap-execute-plan-062426.md)
> **GitNexus @ doc authoring (2026-06-24) + S67 final re-index + S68 gate (2026-06-25):** 19,792 symbols / 37,427 edges / 300 flows / 2,455 files (CLI node .gitnexus/run.cjs analyze post S67 changes @ HEAD `28c582d`). MCP verified: list 19792/37427/2455; detect 24/0 low (doc-only gate/status); impacts §5 exact CRITICAL (Patrol 97, Bridge 127, Catalog 178, Baltic 52). Cite production/release-train-scope-boundary-2026-06-24.md §S67/S68 + S65 reindex. Re-index after changes per AGENTS.md. S66/S67/S68 CLOSEOUT/SIGNOFF PREP COMPLETE (all gates PASS; S68 ready for human ack "i provide the ack"). **ALL S65-S68 COMPLETE (gates PASS 0e/1232/0f/6/6/18/18/hash 17144800277401907079/ZERO; GitNexus 19792/37427/2455; human ack ready). ALL TRACKS COMPLETE. Cite production/release-train-scope-boundary-2026-06-24.md on all artifacts. FINAL POLISH.**
> **Verification @ doc authoring:** build 0e/0w; test **1229/0f** (Sim 279, Cli 43, Del 247, Excel 5, UA 252, Data 403); ReplayGolden **6/6**; C2 proxy **18/18**; hash **`17144800277401907079`** preserved; ZERO `DelegationBridge` default.
> **Stage:** **Release** (`production/stage.txt`) — RC1 + internal engineering + Baltic v2 content complete; **no stage advance** to Launch until explicit S68 gate decision.
> **Closed milestones:** S39–S48 Release enablement; S49–S56 internal engineering; **S57–S64 Baltic v2 content expansion** (human ack 2026-06-22; see §2).
> **Active program:** **S65–S68 release train** — packaging, evidence, CI/ops readiness, optional Launch gate (E10 lead).

> This roadmap is **direction, not a commitment**. Per `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md`,
> each sprint is still planned via `/sprint-plan` with user approval. Filename retains the
> `roadpmap` spelling for link stability.

---

## 0. Parallel execution model (S65+ program)

Every sprint is a **parallel dispatch** — multiple agent tracks run concurrently in isolated git worktrees, merging at sprint-close. Model unchanged from S49+; see [`future-sprint-roadpmap-062126.md`](future-sprint-roadpmap-062126.md) §0 for full protocol reference.

### 0.1 Agent environments

| Env | Capacity | Suited for | Not suited for |
|-----|----------|------------|----------------|
| **Local** (Cursor) | ≤6 concurrent | Editor evidence, closeout/merge, playtest capture, scope boundary publish | Mass CI runs, pure-code hygiene |
| **Cloud Agent** | ≤5 concurrent | Code/tests/hygiene, manifest/docs, CI config | Unity Editor PNG capture |
| **Combined** | 4–6 effective tracks | — | — |

**Routing:** `production/agentic/local-cloud-agent-routing.md` — cloud handles code/test/docs; local owns closeout + coordinator merge.

### 0.2 Worktree strategy

```
.worktrees/stack/sprint{N}/{track-slug}/
```

| Convention | Example | Purpose |
|------------|---------|---------|
| Stack prefix | `stack/sprint65/release-boundary` | Graphite stack grouping |
| Track slug | `release-boundary`, `gate-matrix`, `closeout` | Unique per sprint |
| Closeout track | `stack/sprint{N}/closeout` | Merge coordinator (always local) |

### 0.3 Dispatch patterns (S65+ emphasis)

| Pattern | When | Example |
|---------|------|---------|
| **Fan-out** | Independent docs + code rows | S65: boundary ∥ gate matrix ∥ manifest hardening |
| **Pipeline** | Evidence depends on manifest | S66: manifest → evidence bundle → checklist v2 |
| **Shadow** | Cloud builds, local captures evidence | S66: cloud packaging; local playtest corpus review |
| **Gate** | Human + automated loop | S68 → optional Launch stage decision |

### 0.4 Merge gate protocol (every sprint close)

1. All tracks `gt submit` their stacks.
2. Closeout track runs `gt restack` on trunk `main`.
3. Verify: `dotnet build ProjectAegis.sln && dotnet test ProjectAegis.sln -v minimal`.
4. Hard gates pass (determinism, replay, proxy, test floor) → merge.
5. GitNexus re-index after merge.

**Graphite:** `gt sync`, `gt restack`, `gt submit --stack --no-interactive` — see [`docs/engineering/graphite-github-substitute-plan.md`](../engineering/graphite-github-substitute-plan.md).

### 0.5 Shared-resource coordination (S65+)

| Resource | Access pattern | Coordination rule |
|----------|---------------|-------------------|
| `UnifiedReleaseTrainManifest` | S65 manifest track | MED — extend-only; single owner |
| `CatalogWriteGate` | S65–S66 if catalog touched | CRITICAL — extend-only; one owner per sprint |
| `DelegationBridge` | Any | **ZERO touch** — ADR required before any edit |
| `baltic-patrol.policy.json` (production) | Read-mostly | Hash change requires golden ADR + replay 6/6 |
| Test baseline (`≥1229`) | Monotonic | Post–S64 floor; no track may regress |
| `production/release/` | Append/update per sprint | S66 owns checklist v2; cite prior v1 |

### 0.6 Pre-flight checklist (per track)

- [ ] GitNexus `impact()` on every symbol in file-ownership matrix
- [ ] Report risk level (CRITICAL/HIGH → user ack before editing)
- [ ] Confirm worktree isolation (`git worktree list`)
- [ ] Cite `production/release-train-scope-boundary-2026-06-24.md` + row/epic ID
- [ ] Verify test baseline passes before any change

---

## 1. Where we are (post–S64 Baltic v2 gate)

| Dimension | State | Evidence |
|---|---|---|
| Stage | **Release** — RC1 + internal engineering + Baltic v2 content complete | `production/stage.txt`, [`s48-release-gate-2026-06-20.md`](../../production/gate-checks/s48-release-gate-2026-06-20.md) |
| Closed milestone (v1.0 RC1) | **Baltic v1.0 vertical slice — CLOSED** | S48 gate 2026-06-20 |
| Closed milestone (internal eng) | **S49–S56 program — CLOSED** | [`s56-internal-engineering-gate-2026-06-21.md`](../../production/gate-checks/s56-internal-engineering-gate-2026-06-21.md) |
| Closed milestone (Baltic v2) | **S57–S64 program — CLOSED** | [`s57-s64-program-closeout-2026-06-22.md`](../../production/qa/s57-s64-program-closeout-2026-06-22.md) + human ack |
| Last sprint | **S64 complete** — Baltic v2 content gate | S64 gate + human ack 2026-06-22 |
| Next sprint | **S65 planned** — release train foundation | §10 |
| Test baseline | **1229/1229** headless, **ReplayGolden 6/6**, **C2 proxy 18/18** | Verified 2026-06-24 |
| Determinism | Baltic hash **`17144800277401907079`** immutable on production path; ZERO DelegationBridge default | S64 + standing invariants §7 |
| Baltic v2 content | **10** `baltic-v2-*` scenario policies; **9** v2 replay goldens | `data/scenarios/`, `tests/regression/` |
| Tracker | **21/21 MVP-done or Partial+** (closed at S56) | [`implementation-tracker-2026-06-04.md`](../../Game-Requirements/implementation-tracker-2026-06-04.md) |
| Parallel readiness | **4-track pattern proven** (S39–S64) | Closeout smokes in `production/qa/` |

**What S57–S64 delivered (closed):**
- **E1:** AAR Topic 1 code fix (`PatrolCandidateEngagePolicy`); playtest loop foundations.
- **E9:** Expanded scenario family, theater OOB, mission events, catalog slices, C2 scenario UX.
- **E5/E4 support:** Catalog ingest + scenario picker for v2 manifest.
- **Gate:** Baltic v2 content-complete sign-off + human playtest evidence.

---

## 2. Completed program archive

### Track A — Deeper Polish + Release Enablement (S39–S48)

See [`future-sprint-roadpmap-062126.md`](future-sprint-roadpmap-062126.md) §2. Summary: S39–S41 Polish exit; S42–S48 Release enablement → RC1.

### Track B — Internal Engineering (S49–S56)

| Sprint | Epic(s) | Primary outcome | Closeout |
|--------|---------|-----------------|----------|
| **S49** | E2 | MCP/OSINT production + agentic infra foundations | PASS 2026-06-21 |
| **S50** | E2 | Scenario gen workers + NL Mission Editor | PASS 2026-06-21 |
| **S51** | E5 | Full corpora CI + TL runtime fork | PASS 2026-06-21 |
| **S52** | E6 | Multi-k benchmark + sim API + DOTS expand | PASS 2026-06-21 |
| **S53** | E3 | Full DOTS spawn + MASS tier | PASS 2026-06-21 |
| **S54** | E3 | Orbital DEW + escalation ladder | PASS 2026-06-21 |
| **S55** | E4 | Cesium/globe + hypersonic C2 UI | PASS 2026-06-21 |
| **S56** | E1 + gate | AAR sweep + **21/21 internal gate** | PASS + human ack 2026-06-22 |

**Archive:** [`post-release-scope-boundary-2026-06-21.md`](../../production/post-release-scope-boundary-2026-06-21.md), [`s56-internal-engineering-gate-2026-06-21.md`](../../production/gate-checks/s56-internal-engineering-gate-2026-06-21.md).

### Track C — Baltic v2 Content Expansion (S57–S64)

| Sprint | Epic(s) | Primary outcome | Closeout |
|--------|---------|-----------------|----------|
| **S57** | E1 | AAR policy fix + replay goldens + playtest prep | PASS 2026-06-22 |
| **S58** | E9 | Scenario content wave 1 (patrol/mission variants) | PASS (program closeout) |
| **S59** | E9 | Theater package expansion + OOB | PASS (program closeout) |
| **S60** | E9 | Mission events & narrative arcs | PASS (program closeout) |
| **S61** | E9+E5 | Catalog & platform content for v2 | PASS (program closeout) |
| **S62** | E4+E9 | C2 scenario picker + difficulty bands | PASS (program closeout) |
| **S63** | E1 | Playtest loop (automated + human template) | PASS (program closeout) |
| **S64** | Gate | Baltic v2 content gate + human ack | PASS 2026-06-22 |

**Archive:** [`baltic-v2-scope-boundary-2026-06-22.md`](../../production/baltic-v2-scope-boundary-2026-06-22.md) (superseded for S65+), [`s57-s64-program-closeout-2026-06-22.md`](../../production/qa/s57-s64-program-closeout-2026-06-22.md).

---

## 3. S65–S68 committed scope — Release train

User decision **2026-06-24:** next train optimizes for **release train readiness** — unified manifest hardening, Baltic v2 evidence packaging, CI/ops alignment, optional Launch gate. Stage remains **Release** until S68 human decision.

| Theme | Sprint(s) | ∥ Tracks | Primary deliverable |
|-------|-----------|----------|---------------------|
| **Scope + gate foundation** | S65 | 4 | `release-train-scope-boundary-2026-06-24.md`; gate matrix refresh; GitNexus re-index |
| **Manifest hardening** | S65 | 2 | `UnifiedReleaseTrainManifest` + `CatalogReleaseDiffCommand` docs/tests |
| **Evidence packaging** | S66 | 3 | Baltic v2 content manifest; playtest corpus index; `release-checklist-v2.md` |
| **CI / ops readiness** | S67 | 3 | Buildkite preflight; regression baseline lock; branch-protection checklist |
| **Launch gate** | S68 | 2 | Full verification + human ack; optional **Launch** stage advance |

**Program exit criterion (S65–S68):** Release-train **ops-complete** for shippable Baltic v2 — manifest, evidence bundle, CI green, checklist v2 signed — **not** commercial store submission (E7).

**Still out of scope (unless new scope decision):** E7 commercial launch / store submission, new scenario content (E9), multiplayer, `DelegationBridge` edits, production hash change without ADR.

---

## 4. Epic buckets (S65+ program map)

```
 S64 CLOSED ──► S65 (E10) ──► S66 ──► S67 ──► S68 gate

 Parallel tracks per sprint:
 ┌────────────────────────────────────────────────────────────────────────────┐
 │ S65              S66              S67              S68                      │
 │                                                                             │
 │ Boundary ──►     Evidence ──►     CI/Buildkite ──► Gate + optional Launch  │
 │  ∥ gate matrix    ∥ checklist v2  ∥ baseline lock   ∥ human ack            │
 │  ∥ manifest       ∥ playtest idx   ∥ branch prot                            │
 │  ∥ re-index                                                                 │
 └────────────────────────────────────────────────────────────────────────────┘

 E10 Release ops ★   E1/E9 (hold)   E7 Commercial ──► DEFERRED
```

### E10 — Release operations ★ **LEAD** (S65–S68)

| Theme | Sprint | Tracks | Notes |
|-------|--------|--------|-------|
| Scope boundary | S65 | 1 | Supersedes baltic-v2 boundary for S65+ only |
| Unified release manifest | S65 | 1 | `UnifiedReleaseTrainManifest`, `UnifiedReleaseTrainDiffReport` |
| Evidence packaging | S66 | 2 | v2 scenarios, goldens, playtest corpus |
| CI / Buildkite | S67 | 2 | `.buildkite/`, regression lock |
| Launch gate | S68 | 1 | Optional stage advance to Launch |

### E7 — Commercial launch — **OUT OF S65–S68 TRAIN**

Store submission, i18n production, marketing assets remain deferred unless explicit new decision.

### E9 — Baltic content — **ON HOLD**

Content expansion complete at S64. Reactivate only via new scope decision + boundary supersede.

---

## 5. GitNexus pre-flight map (S65+ hot symbols)

| Symbol / area | Risk | Touched by | Constraint |
|---------------|------|------------|------------|
| `DelegationBridge` | **CRITICAL** | Any | ZERO touch |
| `CatalogWriteGate` | **CRITICAL** | S65–S66 if catalog | Extend-only; single owner |
| `PatrolCandidateEngagePolicy` | **CRITICAL** | Avoid unless bugfix | No release-train edits expected |
| `BalticReplayHarness` | **CRITICAL** | S66 evidence only | Read/test; no behavior change |
| `UnifiedReleaseTrainManifest` | MED | S65 manifest | Extend-only; tests in `UnifiedReleaseTrainManifestTests` |
| `DbSnapshotStore` | MED | S65–S66 | Snapshot binding; cite ADR-006 |
| `CatalogReleaseDiffCommand` | LOW | S65 CLI docs | CLI-only; `MissionEditor.Cli` |

---

## 6. Prioritization decisions (locked 2026-06-24)

| # | Question | Decision |
|---|----------|----------|
| 1 | S57–S64 program status | **CLOSED** — human ack 2026-06-22 |
| 2 | Next train focus | **S65–S68 release train** — manifest, evidence, CI, optional Launch gate |
| 3 | Sprint naming | **Continue S65+** numbered sprints with parallel tracks |
| 4 | Stage advance | **Stay at Release** until S68 explicit gate (optional Launch) |
| 5 | Lead epic | **E10 Release ops** (S65–S68) |
| 6 | Filename alias | **`future-sprint-roadpmap.md`** → this file |
| 7 | E7 commercial | **Out of S65–S68 train** |
| 8 | E9 content | **On hold** — S64 content-complete |

### Scope boundary (publish @ S65-01)

**`production/release-train-scope-boundary-2026-06-24.md`** — draft at S65 planning:
- Supersedes [`baltic-v2-scope-boundary-2026-06-22.md`](../../production/baltic-v2-scope-boundary-2026-06-22.md) for S65+ only (archived, not deleted).
- Cites §3 committed themes + E10 lead + release train exit at S68.
- Carries standing invariants from §7 unchanged unless ADR.

---

## 7. Standing invariants (carry forward)

Every S65+ sprint **fails** if any invariant regresses:

1. **Determinism:** Production Baltic hash `17144800277401907079` unless golden-updated with ADR.
2. **ReplayGolden 6/6** and **C2 proxy 18/18+** every sprint.
3. **CatalogWriteGate extend-only**; **ZERO DelegationBridge** unless ADR.
4. **Test baseline never regresses** (floor **1229** post–S64; monotonic).
5. **GitNexus discipline:** `impact()` before symbol edits; `detect_changes()` before commit.
6. **Scope citation:** every story cites `release-train-scope-boundary-2026-06-24.md` + epic/theme ID.
7. **Parallel safety:** no two tracks edit the same CRITICAL symbol in the same sprint.

---

## 8. Risk register (S65+ program)

| Risk | Like. | Impact | Mitigation |
|------|-------|--------|------------|
| Release checklist v1 stale vs v2 content | High | Med | S66 dedicated checklist v2 from Baltic v2 manifest |
| Unified manifest drift from on-disk goldens | Med | High | S65 diff CLI verification + `CatalogReleaseDiffCommand` tests |
| GitNexus index drift | Med | Med | Re-index at S65 open + after each merge |
| CI/Buildkite misalignment with local gates | Med | High | S67 preflight against same commands as §7 |
| Premature Launch stage advance | Low | High | S68 requires explicit human ack; default stay Release |
| Scope creep into E7 commercial | Med | High | Boundary §3 out-of-scope list; cite on every story |

---

## 9. Decisions log

**Resolved 2026-06-24** — see §6.

**Planning artifacts (to create @ S65):**

| Artifact | Path | Status |
|----------|------|--------|
| Release train scope boundary | `production/release-train-scope-boundary-2026-06-24.md` | **Draft @ S65-01** |
| Execute plan | [`roadmap-execute-plan-062426.md`](roadmap-execute-plan-062426.md) | **Published** |
| Sprint 65 plan | `production/sprints/sprint-65-release-train-foundation.md` | **Draft** (final after boundary published) |
| Sprint 65 kickoff | `production/agentic/sprint-65-parallel-kickoff-2026-06-24.md` | **Draft @ dispatch** |

**Next execution steps:** Publish boundary → finalize sprint plan → `/qa-plan sprint` → `/dev-story dispatch S65-01`.

---

## 10. S65–S68 per-sprint parallel decomposition

> **Lead:** E10 from S65. **Exit:** S68 release train gate (+ optional Launch).
> **Model:** §0. Each sprint: 2–4 parallel tracks with isolated worktrees.
> **Detail:** [`roadmap-execute-plan-062426.md`](roadmap-execute-plan-062426.md) §4.

---

### S65 — E10: Release train foundation

| Est. | ~5–7 days | Dispatch | **READY TO PLAN** |
|------|-----------|----------|-------------------|

**Parallel tracks (4):**

| Track | Stories | Owner | Env | Depends on | Feeds |
|-------|---------|-------|-----|------------|-------|
| Scope boundary | S65-01 | producer | **Local** | — | All tracks |
| Gate matrix refresh | S65-02 | qa-lead | Cloud | — | S66 checklist |
| Manifest hardening | S65-03, S65-04 | team-data | Cloud | — | S66 packaging |
| GitNexus re-index | S65-05 | devops-engineer | Cloud | — | — |
| Closeout | S65-06 | devops-engineer | **Local** | All | — |

**Scope:** Publish release-train boundary; refresh post-S64 gate matrix; harden `UnifiedReleaseTrainManifest` + diff CLI; re-index GitNexus @ HEAD.

**Hard gates:** Test ≥1229, ReplayGolden 6/6, C2 proxy ≥18, production Baltic hash immutable, ZERO bridge.

---

### S66 — E10: Evidence packaging + checklist v2

| Est. | ~8–10 days | Dispatch | After S65 |
|------|------------|----------|-----------|

**Parallel tracks (3):**

| Track | Stories | Owner | Env | Depends on |
|-------|---------|-------|-----|------------|
| Baltic v2 content manifest | S66-01, S66-02 | producer | Cloud | S65 manifest |
| Playtest corpus index | S66-03 | qa-lead | **Local** | — |
| `release-checklist-v2.md` | S66-04 | qa-lead | Cloud | S65 gate matrix |
| Closeout | S66-05 | devops-engineer | **Local** | All |

**Scope:** Package 10 v2 policies + 9 goldens + playtest evidence; supersede [`release-checklist-v1.md`](../../production/release/release-checklist-v1.md).

**Hard gates:** Test ≥ prior sprint; ReplayGolden 6/6; C2 proxy ≥18; evidence index in `production/qa/evidence/`.

---

### S67 — E10: CI / ops readiness — **COMPLETE (2026-06-25)** (all tracks PASS per sprint-67-buildkite-baseline-protection.md + verif; final reindex 19792/37427/2455; gates 0e/1232/0f/6/6/18/18/hash/ZERO; cite production/release-train-scope-boundary-2026-06-24.md)

| Est. | ~5–7 days | Dispatch | After S66 |
|------|-----------|----------|-----------|

**Parallel tracks (3) — ALL COMPLETE:**

| Track | Status | Owner | Env | Deliverables |
|-------|--------|-------|-----|--------------|
| Buildkite preflight | COMPLETE | buildkite-ci-lead | Cloud | .buildkite/preflight-s67.yml + pipeline + tools scripts (GitNexus + verif step; §7 parity) |
| Regression baseline lock | COMPLETE | c-sharp-devops-engineer | Cloud/Local | tests/regression/README.md S67-02 pinned (1232/0f, 6/6, 18/18, hash, ZERO); GitNexus context |
| Branch-protection checklist | COMPLETE | devops-engineer | Cloud | ci-and-branch-protection.md (2026-06-25); tools/apply-branch; .github audit |
| Closeout | COMPLETE | devops-engineer | **Local** | sprint-67-buildkite-baseline-protection.md; cross-ref smoke-66; status update |

**Scope delivered:** Align `.buildkite/` with §7 gate commands; locked baseline (1232); update ci-and-branch-protection.md. Cite release-train-scope-boundary-2026-06-24.md .

**Hard gates:** All PASS (build 0e/0w; test 1232/0f; replay 6/6; C2 18/18; hash 17144800277401907079; ZERO DelegationBridge).

**GitNexus + reindex:** Final S67 re-index (post changes): 19792 nodes / 37427 edges / 2455 files @ HEAD 28c582d (2026-06-25T13:34Z). MCP list/detect/impact: detect 27/0 low; impacts CRITICAL §5 exact (CatalogWriteGate 178, Patrol 97, Bridge 127, Baltic 52). See sprint-status + smoke-sprint-66-closeout.md. Re-index after changes per AGENTS.md + boundary.

**Verification-before:** RUN+READ all gates + GitNexus (search first) before/after edits. S67 COMPLETE. Ready S68. Cites: boundary + roadmap §0/3/5/7/10.

---

### S68 — Release train gate (+ optional Launch) — **COMPLETE (2026-06-25; verification + sign-off prep; HUMAN ACK READY)** (S68 VERIFICATION COMPLETE gates PASS per s68-release-train-gate-2026-06-25.md + GitNexus pre 19792/37427/2455 low detect; S65-S67 all PASS; S68 human ack ready "i provide the ack"; stage remains Release; cite production/release-train-scope-boundary-2026-06-24.md S68 section exactly + S67/S66/S65; ALL TRACKS COMPLETE; **ALL S65-S68 COMPLETE (gates PASS 0e/1232/0f/6/6/18/18/hash/ZERO; index 19792/37427/2455; human ack ready)**). Cite production/release-train-scope-boundary-2026-06-24.md. FINAL POLISH.

| Est. | ~5–7 days | Dispatch | After S67 (COMPLETE) |
|------|-----------|----------|----------------------|

**Parallel tracks (2):**

| Track | Stories | Owner | Env | Depends on |
|-------|---------|-------|-----|------------|
| Gate verification | S68-01 | devops-engineer | **Local** | S65–S67 |
| Human sign-off + stage decision | S68-02 | producer | **Local** | All |

**Exit criteria (S68 COMPLETE 2026-06-25):**
- [x] S65–S67 closeouts PASS
- [x] `release-checklist-v2.md` complete
- [x] Evidence bundle indexed (`production/qa/evidence/`)
- [x] Test baseline ≥1229; ReplayGolden 6/6; C2 proxy ≥18
- [x] Production Baltic hash unchanged OR golden ADR documented
- [x] Gate document: `production/gate-checks/s68-release-train-gate-2026-06-25.md`
- [x] Human ack ready on release train ops-complete ("i provide the ack" template; pending user)
- [ ] Optional: **Launch** stage advance (explicit human decision only)
**S68 gate verification PASS; GitNexus pre/post + verification-before; all S65-S68 tracks COMPLETE per release-train-scope-boundary-2026-06-24.md. ALL S65-S68 COMPLETE (index 19792/37427/2455; gates 0e/1232/0f 6/6 18/18 hash/ZERO). Cite boundary.**

---

### Program infrastructure

| Artifact | Path |
|----------|------|
| Scope boundary | `production/release-train-scope-boundary-2026-06-24.md` (draft @ S65-01) |
| Execute plan | [`roadmap-execute-plan-062426.md`](roadmap-execute-plan-062426.md) |
| Local/cloud routing | `production/agentic/local-cloud-agent-routing.md` (existing) |
| Prior stub | `production/sprints/sprint-65-stub-release-train-or-next.md` (superseded by this program) |

**Total program (S65–S68):** ~26–34 calendar days with 2–4 parallel tracks (estimate only).

---

## 11. Related artifacts

| Artifact | Path |
|----------|------|
| Stable alias (this doc) | [`future-sprint-roadpmap.md`](future-sprint-roadpmap.md) |
| Execute plan | [`roadmap-execute-plan-062426.md`](roadmap-execute-plan-062426.md) |
| Prior snapshot (S57–S64) | [`future-sprint-roadpmap-062226.md`](future-sprint-roadpmap-062226.md) |
| Baltic v2 closeout | [`production/qa/s57-s64-program-closeout-2026-06-22.md`](../../production/qa/s57-s64-program-closeout-2026-06-22.md) |
| Baltic v2 boundary (archived) | [`production/baltic-v2-scope-boundary-2026-06-22.md`](../../production/baltic-v2-scope-boundary-2026-06-22.md) |
| Release checklist v1 | [`production/release/release-checklist-v1.md`](../../production/release/release-checklist-v1.md) |
| S40–S48 execute plan (template) | [`s40-s48-local-cloud-agent-execution-plan-2026-06-20.md`](s40-s48-local-cloud-agent-execution-plan-2026-06-20.md) |
| Implementation tracker | [`Game-Requirements/implementation-tracker-2026-06-04.md`](../../Game-Requirements/implementation-tracker-2026-06-04.md) |

---

## 12. Dependency & coordination matrix

### Cross-sprint dependency chain

```
S65 ──────► S66 ──────► S67 ──────► S68

Boundary ─► Evidence ─► CI/Ops ──► GATE (+ optional Launch)
 manifest    checklist   baseline
 re-index    playtest
```

### What CAN run in parallel (within sprint)

| Sprint | Parallel pair | Reasoning |
|--------|---------------|-----------|
| S65 | Boundary ∥ gate matrix ∥ manifest ∥ re-index | Independent artifacts; boundary should land day 1 |
| S66 | Manifest ∥ playtest index ∥ checklist (checklist trails manifest mid-sprint) | Checklist cites manifest |
| S67 | Buildkite ∥ baseline lock ∥ branch protection | Independent ops docs |
| S68 | Verification ∥ sign-off prep | Serial within sprint; human gate last |

### What MUST be serial (cross-sprint)

| Dependency | Reason |
|------------|--------|
| S65 boundary → S66 packaging | Evidence cites boundary + manifest |
| S65 manifest → S66 checklist v2 | Checklist references unified manifest |
| S66 evidence → S67 CI | CI validates packaged corpus |
| All → S68 gate | Gate verifies cumulative release train |

---

*Grounded in: S64 Baltic v2 gate (human ack 2026-06-22); user prioritization 2026-06-24 (release train); test baseline 1229 verified 2026-06-24; parallel kickoff pattern (S39–S64 proven). S65/S66/S67/S68 COMPLETE (gates PASS 0e/1232/0f/6/6/18/18/hash 17144800277401907079/ZERO, GitNexus 19792/37427/2455; S68 human ack ready "i provide the ack"). Update via `/sprint-plan` for S65+. Each sprint dispatched via `production/agentic/sprint-{N}-parallel-kickoff-*.md`. Cite production/release-train-scope-boundary-2026-06-24.md on all artifacts. **ALL S65-S68 COMPLETE.** ALL TRACKS COMPLETE. Cite boundary.*
