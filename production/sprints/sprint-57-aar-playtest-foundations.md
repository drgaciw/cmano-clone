# Sprint 57 — AAR Code Remediation + Playtest Foundations (E1 Lead)

**Dates:** ~TBD (~8–10 days)  
**Trunk:** `main` @ post-S56 internal engineering gate  
**Predecessor:** Sprint 56 — COMPLETE (21/21 program exit; human ack 2026-06-22)  
**Stage:** Release (Baltic v2 program opens; no stage advance)  
**Authority:** [`docs/reports/future-sprint-roadpmap-062226.md`](../../docs/reports/future-sprint-roadpmap-062226.md) §10 S57; [`production/baltic-v2-scope-boundary-2026-06-22.md`](../baltic-v2-scope-boundary-2026-06-22.md) (publish with S57-01)

> **READY TO PLAN** — Baltic v2 program kickoff. E1 lead (AAR Topic 1 code fix); playtest harness prep for S63. E9 scenario content (**S58**) blocked until S57 AAR policy lands.

## Sprint Goal

Implement the **playtest AAR Topic 1 code fix** — stop proposing high-priority `Engage` on confirmed destroyed targets before resolver abort — and add isolated replay evidence. Prepare automated playtest harness scaffolding for the S63 loop. Retain comms degradation model (AAR Topic 2 — no regression).

**S57 does not close Baltic v2** — scenario content wave lands in S58+ per roadmap §10.

## Capacity

- Total days: 10
- Buffer (20%): 2 days
- **Effective dev-days:** **8**
- **Commit target:** **5 stories** (S57-01..05)
- **Test baseline:** ≥ **1228** (S56 floor); monotonic growth expected
- **Parallelism:** **4 tracks** (AAR code, replay goldens, playtest prep, closeout)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S57-01 | **Re-baseline + Baltic v2 gate matrix** — build/test; cite boundary | c-sharp-devops-engineer | 1 | Boundary draft | 0 errors; `gate-matrix-baltic-v2-2026-06-22.md` |
| S57-02 | **Sprint 57 QA plan** — AAR + replay scope; blocks AAR track | team-qa | 0.5 | S57-01 | `production/qa/qa-plan-sprint-57-2026-06-22.md` |
| S57-03 | **S57-01 AAR — destroyed-target pre-filter** — policy + perceived state | team-simulation | 3 | S57-02 | See §S57-03 below |
| S57-04 | **Isolated replay golden** — re-engage regression fixture | c-sharp-test-engineer | 1.5 | S57-03 mid-sprint | See §S57-04 below |
| S57-05 | **Closeout** — smoke, evidence, sprint-status | c-sharp-devops-engineer | 0.5 | S57-03+ | `smoke-sprint-57-closeout-2026-06-22.md`; gates PASS |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S57-06 | **Playtest harness prep** — batch template + scenario manifest stub | qa-lead | 1.5 | S57-01 | See §S57-06 below |
| S57-07 | **AAR Topic 2 retain doc** — comms effectiveness sign-off (no code) | writer | 0.5 | — | Extends `s56-aar-remediation-track`; cites game-players-report §2.b |

---

## S57-03 — AAR Topic 1: Destroyed-target pre-filter

**Authority:** [`game-players-report-0620206.md`](../../game-players-report-0620206.md) Topic 1; [`production/qa/s56-aar-remediation-track-2026-06-21.md`](../qa/s56-aar-remediation-track-2026-06-21.md) §2.1; [`design/gdd/engagement-fire-control.md`](../../design/gdd/engagement-fire-control.md)

**Problem:** `PatrolCandidateEngagePolicy` returns unconditional `Engage` (score 99.0). `MvpEngagementResolver` aborts late with `TargetDestroyed`, flooding order log with wasted attempts (seeds 123, 789 in playtest AAR).

**Scope (minimal, deterministic):**

| Deliverable | Detail |
|-------------|--------|
| Perceived state extension | Additive field on `PerceivedState` (e.g. `DestroyedContactCount` or `HasDestroyedPrimaryContact`) — **no wall-clock**; populated from harness/projection |
| Policy pre-filter | `PatrolCandidateEngagePolicy.GenerateCandidates` omits or scores `Engage` at 0 when primary target destroyed per perceived state |
| Harness wiring | `BalticReplayHarness` projects killed-target set into perceived state before policy invocation (reuse existing `KilledTargets` path) |
| Unit tests | Policy tests: destroyed → no Engage candidate; alive → Engage retained |
| Integration | Existing `ReplayGoldenSuiteTests` **6/6 PASS** on production `baltic-patrol` (hash unchanged) |

**Preferred approach (from S56 analysis):**

1. Extend `PerceivedState` additively (do not break existing constructors — optional/default).
2. Harness injects killed-set snapshot into perception each tick.
3. Policy filters before returning candidates; resolver late-abort remains as safety net.

**GitNexus (pre-edit, mandatory):**

| Symbol | Expected risk |
|--------|---------------|
| `PatrolCandidateEngagePolicy` | **CRITICAL** |
| `PerceivedState` | HIGH |
| `BalticReplayHarness` | HIGH |
| `MvpEngagementResolver` | **CRITICAL** — prefer no edit; late-abort unchanged |
| `DelegationBridge` | **CRITICAL** — **ZERO touch** |

**Hash policy:**

- Production `baltic-patrol.policy.json` world hash **`17144800277401907079`** must remain unchanged unless determinism-engineer + golden ADR explicitly approves.
- If policy change alters production replay ticks, **stop** and use isolated fixture only until ADR.

**Out of S57-03:** `EngageOnlyPolicy` / `StubPatrolPolicy` generalization (follow-on if needed); DelegationBridge edits; personality preset retraining.

---

## S57-04 — Isolated replay golden (re-engage regression)

**Scope:**

| Deliverable | Detail |
|-------------|--------|
| Fixture policy | `data/scenarios/baltic-patrol-destroyed-target-reengage.policy.json` (isolated; not production default) |
| Golden file | `tests/regression/replay-golden-baltic-destroyed-reengage-2026-06-22.txt` |
| Test class | Harness test asserting: after kill tick, **zero** subsequent `Engage` proposals (or zero `TARGET_DESTROYED` abort floods) |
| Pin | Isolated world hash documented in test; **not** mixed into ReplayGolden 6/6 production suite unless promoted |

**Gate:** New golden adds to test count (monotonic ≥1228); production ReplayGolden **6/6** unchanged.

---

## S57-06 — Playtest harness prep (S63 pipeline)

**Scope (scaffolding only — no human sessions this sprint):**

| Deliverable | Detail |
|-------------|--------|
| Manifest stub | `production/playtests/baltic-v2-scenario-manifest.yaml` — lists baseline + future S58 slots |
| Batch script stub | `tools/playtest/run-baltic-v2-batch.sh` — wraps `BalticBatchRunner` / dotnet test filters |
| Template | `production/playtests/templates/human-session-template-2026-06-22.md` — Band A/B/C checklist from [`difficulty-curve.md`](../../design/difficulty-curve.md) |

**Out of S57-06:** Full S63 automated loop; human facilitation sessions.

---

## Explicitly Out of Scope (S57)

- S58+ scenario content (new patrol/mission JSON) — blocked until S57-03 merges
- E7 commercial launch
- CatalogWriteGate / corpora changes
- Production Baltic hash change without ADR
- DelegationBridge edits
- C2 scenario picker UI (S62)
- Second theater content (S64 decision)

## GitNexus / Hard Gates

- Full headless tests ≥ **1228**; monotonic
- ReplayGolden **6/6** (production suite)
- C2 proxy **18/18+**
- Baltic production hash **`17144800277401907079`** immutable unless ADR
- CatalogWriteGate **extend-only** (N/A this sprint unless touched)
- `impact()` before every symbol edit; `detect_changes()` before commit
- Every artifact cites **`baltic-v2-scope-boundary-2026-06-22.md`** + roadmap §10 S57

## Definition of Done

- [ ] Must Have S57-01..05 complete
- [ ] QA plan MET; smoke PASS
- [ ] AAR Topic 1 policy fix landed with unit + isolated golden tests
- [ ] Production ReplayGolden 6/6 + hash unchanged (or ADR if changed)
- [ ] Playtest prep stubs committed (Should Have S57-06)
- [ ] sprint-status.yaml updated (S57 complete; S58 ready)

## Parallel Execution Model

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| AAR policy fix | `stack/sprint57/aar-code` | Cloud | S57-03 |
| Replay goldens | `stack/sprint57/replay-goldens` | Cloud | S57-04 (starts after S57-03 mid-sprint) |
| Playtest prep | `stack/sprint57/playtest-prep` | Cloud | S57-06 |
| Baseline + QA | `stack/sprint57/baseline-qa` | Cloud | S57-01, S57-02 |
| Closeout | `stack/sprint57/closeout` | **Local** | S57-05, S57-07 |

See [`sprint-57-parallel-kickoff-2026-06-22.md`](../agentic/sprint-57-parallel-kickoff-2026-06-22.md) (publish with dispatch).

## Quality Gates

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~PatrolCandidate|PerceivedState"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests|PlayModeSmokeHarnessTests|DestroyedReengage"
```

## Related Artifacts

| Artifact | Path |
|----------|------|
| Roadmap | `docs/reports/future-sprint-roadpmap-062226.md` §10 S57 |
| Scope boundary | `production/baltic-v2-scope-boundary-2026-06-22.md` |
| S56 AAR analysis | `production/qa/s56-aar-remediation-track-2026-06-21.md` |
| Playtest AAR | `game-players-report-0620206.md` |
| S56 gate | `production/gate-checks/s56-internal-engineering-gate-2026-06-21.md` |
| Req 04 | `Game-Requirements/requirements/04-Agent-Delegation.md` |
| Req 17 | `Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md` |

---

*Planning artifact 2026-06-22. Publish boundary → `/qa-plan sprint` → `/dev-story dispatch S57-01`.*
## S57 Closeout (100% per this execution)
- AAR policy + harness + golden + manifest: implemented + verified (build 0e, tests 0f including new policy filter test, replay subsets PASS)
- Artifacts: baltic-patrol-destroyed...policy.json , replay-golden-...-destroyed-reengage , baltic-v2 manifest
- Gates: invariants hold; GitNexus used; verification-before applied
- Ready for S58 dispatch per roadmap
