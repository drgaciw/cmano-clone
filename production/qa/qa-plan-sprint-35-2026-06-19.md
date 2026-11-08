# QA Plan — Sprint 35

**Date:** 2026-06-19  
**Sprint:** Polish Phase 1 Entry — UX Foundation, Sim Perf P0, C2 Hardening  
**Review mode:** Lean  
**Stage:** Production (`production/stage.txt`) — gate **CONCERNS (uplifted)**  
**Stories in scope:** 14 planned; **8 must-have** (S35-01..07, S35-14)  
**Authority:** [polish-scope-boundary-2026-06-19.md](../polish-scope-boundary-2026-06-19.md)

## Scope

Sprint 35 enters **Polish Phase 1** by closing gate Sprint 0 foundations (accessibility, interaction patterns, difficulty curve, Unity C2 frame budget), landing **sim perf P0** quick wins, and hardening **C2/Platform Editor** player-facing polish — without globe/Cesium, delegation badges, loadout/magazine Unity, TL Phase 5, or tracker MVP-complete claims.

**Hard gates (all merges):** ZERO touch `DelegationBridge.cs`; `CatalogWriteGate` extend-only; ReplayGolden **6/6** on default path; production Baltic hash `17144800277401907079` unchanged.

**Baseline (S35-01):** `main` @ `8de98b1` — **1193/1193** PASS; ReplayGolden **6/6**; Baltic hash `17144800277401907079`; GitNexus **16,508 / 33,453** @ HEAD.

## Story Classification

| Story | Type | Automated Required | Manual Required | Blocker? |
|-------|------|--------------------|-----------------|----------|
| S35-01 Full-sln re-baseline | Config | Yes — full sln + ReplayGolden | Smoke doc audit | **Yes** — blocks S35-02+ |
| S35-02 Sprint 35 QA plan | Config | — | Plan review | **Yes** — blocks S35-03+ |
| S35-03 UX foundation docs | UI | — | Lean doc review (3 files) | No |
| S35-04 Unity C2 frame budget | Integration | Yes — panel-bind timing; C2 filters 18/18 | Profiler capture (Editor host) | No |
| S35-05 Sim perf P0 detection | Logic | Yes — `PdDetection\|DeterministicDetection`; ReplayGolden 6/6; `/replay-verify` | Hash audit | **Yes** — sprint fails if hash breaks |
| S35-06 C2 onboarding + tooltips | UI | Yes — C2 checks 1–13 **61/61** | Comms legend legibility review | No |
| S35-07 C2 sign-off refresh | Integration + UI | Yes — C2 checks 1–18 **61/61 + 58/58** | Lean C2 checklist refresh | No |
| S35-08 AegisTokens USS | UI | Yes — Platform filters **≥51/51** | Token visual spot-check | No (should-have) |
| S35-09 Live Editor evidence | Visual / UI | Yes — headless proxy unchanged | Editor PNG capture (12) | No (should-have) |
| S35-10 Sim perf P1 LINQ | Logic | Yes — `DecisionLog\|DatalinkSidePicture`; ReplayGolden 6/6; `/replay-verify` | Hash audit | No (should-have) |
| S35-11 Playtest session 7 | Config | — | Structured proxy + optional think-aloud | No (should-have) |
| S35-12 Platform validation polish | Logic | Yes — extend-only `CatalogWriteGate` tests | Finding message review | No (should-have) |
| S35-13 Stage advance → Polish | Config | — | Producer user ack | No (should-have) |
| S35-14 Closeout hygiene | Config | Yes — ≥1193 sln; ReplayGolden; C2 18/18 | Tracker/smoke audit | **Yes** — sprint gate |

**Smoke check (day-1):** **PASS** — `production/qa/smoke-sprint-35-baseline-2026-06-19.md` (1193/1193; ReplayGolden 6/6; Baltic hash unchanged; GitNexus 16,508/33,453)

## C2 Headless Proxy Gate (18/18)

Per ADR-010 and `production/qa/c2-automated-proxy-2026-06-02.md`. Merge authority on lean Linux host; live Editor walkthrough advisory only.

### Checks 1–13 — filter **61/61**

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu" -v minimal
```

| Filter segment | Proxy checks | Baseline @ S34 |
|----------------|--------------|----------------|
| `PlayModeSmoke` | 1 (batch) | 17/17 |
| `C2Selection` | 2–4 | selection / detail / map |
| `OobTree` | 5–6 | OOB tree + filter |
| `LossesScoring` | 7 | losses panel |
| `BalticReplay` | 8–11 | classify / engage / stale / readiness |
| `FuelState` | 12 | fuel strip |
| `AttackMenu` | 13 | attack options |
| **Combined** | **1–13** | **61/61** |

**S35-06 gate:** checks 1–13 must remain **≥61/61** after tooltip/onboarding USS/UXML changes.  
**S35-07 gate:** re-confirm **61/61** unchanged or PASS WITH NOTES.

### Checks 14–18 — filter **58/58**

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog" -v minimal
```

| Filter segment | Manual check | Baseline @ S34 |
|----------------|--------------|----------------|
| `PlatformImport` | 14 — import staging diff | 10/10 |
| `PlatformCatalogViewer` | 14 extension — damage columns | 11/11 |
| `Doctrine` | 15 — ROE override | 7/7 |
| `C2TopBar` | 16 — Begin Execution | 5/5 |
| `PlatformComms` | 17 — Phase G comms | 12/12 |
| `PlatformLinkCatalog` | 18 — Phase H link catalog | 13/13 |
| **Combined** | **14–18** | **58/58** |

### Full 18/18 proxy gate

Run both filters on every C2-touching PR (S35-04, S35-06, S35-07, S35-08, S35-14). Optional wrapper:

```bash
# Invoke-ManualQaHeadlessGate.ps1 (Windows/macOS) or documented filter pair above
```

**Sprint fails** if combined proxy drops below **18/18** (61/61 + 58/58).

## Hard Gates — Sim / Determinism (S35-05, S35-10)

Mandatory on any merge touching `PdDetectionContactSimulator`, `DeterministicDetectionLoop`, `DecisionLog`, `DatalinkSidePictureMerger`, or `BalticReplayHarness`.

| Gate | Command / artifact | Expected |
|------|-------------------|----------|
| ReplayGolden | `dotnet test …UnityAdapter.Tests --filter "ReplayGoldenSuiteTests"` | **6/6** PASS |
| Production Baltic hash | `data/scenarios/baltic-patrol.policy.json` world hash | `17144800277401907079` **unchanged** |
| Replay verify | `/replay-verify` on merge | **PASS** |
| DelegationBridge | `git diff HEAD -- **/DelegationBridge.cs` | **ZERO** diff |
| S35-05 filters | `dotnet test ProjectAegis.Sim.Tests --filter "PdDetection\|DeterministicDetection"` | all PASS |
| S35-10 filters | `dotnet test --filter "DecisionLog\|DatalinkSidePicture"` | all PASS; hash-identical merge output |

**S35-05 acceptance:** no hot-path `_trials.First(t => t.ContactId == …)`; no per-tick `OrderBy().ToArray()` in `RollTick`; sort key preserved ObserverId → SensorId → TargetId.

**S35-10 acceptance:** chronological entry order unchanged; datalink merge output hash-identical on `baltic-patrol-datalink-comms` + catalog-latency fixtures.

## Automated Test Requirements

| Story | Filter / path | Expected |
|-------|---------------|----------|
| S35-01, S35-14 | `dotnet test ProjectAegis.sln` | **≥1193/1193** PASS |
| S35-01, S35-04..07, S35-14 | `ReplayGoldenSuiteTests` | **6/6** |
| S35-04 | Panel-bind timing test (UnityAdapter) | **< 100 ms** wall |
| S35-04, S35-06, S35-07, S35-14 | C2 checks 1–13 filter (above) | **61/61** |
| S35-04, S35-07, S35-14 | C2 checks 14–18 filter (above) | **58/58** |
| S35-05 | `PdDetection\|DeterministicDetection` (Sim.Tests) | all PASS |
| S35-05, S35-10 | `/replay-verify` | PASS |
| S35-06 | `grep DelegationBadge\|TrustSignal\|HYPERSONIC_ALERT` in `unity/ProjectAegis/Assets/` | zero **new** refs |
| S35-08 | `PlatformImport\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog` | **≥51/51** |
| S35-10 | `DecisionLog\|DatalinkSidePicture` | all PASS |
| S35-12 | `WriteGate\|Catalog` validation extensions | PASS; extend-only |

## Manual QA Scope (Lean)

| Story | Manual action | Lean substitute |
|-------|---------------|-----------------|
| S35-03 | UX doc completeness vs gate r2 gaps | Lean review note in story evidence |
| S35-04 | Unity Profiler 300-frame capture @ 16.67 ms P0 | Headless panel-bind **< 100 ms** + perf backlog doc |
| S35-06 | Comms DEGRADED/DENIED legend legibility | Headless 61/61 + playtest session 7 (S35-11) |
| S35-07 | C2 checklist refresh checks 1–18 | Headless proxy 61/61 + 58/58 + `sprint-35-c2-signoff-*.md` |
| S35-09 | Live Editor PNG re-capture (12) | Protocol placeholders + headless proxy unchanged |
| S35-11 | Facilitated think-aloud on S35 polish items | Proxy report synthesizing headless + UX docs |

## Playtest Protocol

**Corpus (sessions 1–6):** `production/playtests/` — gate baseline per polish-scope-boundary §Playtest corpus.

| # | File | Focus |
|---|------|-------|
| 1 | `playtest-2026-06-19-npe-baltic-c2.md` | NPE — first impressions, C2 classify/comms |
| 2 | `playtest-2026-06-19-midgame-delegation-catalog.md` | Mid-game — Platform Editor, LinkCatalog, doctrine |
| 3 | `playtest-2026-06-19-difficulty-baltic-scenarios.md` | Difficulty — ReplayGolden, lag/comms fixtures |
| 4 | `human/playtest-2026-06-19-npe-baltic-c2-thinkaloud.md` | Facilitated NPE (PASS WITH NOTES) |
| 5 | `human/playtest-2026-06-19-midgame-delegation-catalog-thinkaloud.md` | Facilitated mid-game (PASS WITH NOTES) |
| 6 | `human/playtest-2026-06-19-difficulty-baltic-scenarios-thinkaloud.md` | Facilitated difficulty (PASS WITH NOTES) |

**Fun hypothesis:** `production/playtests/fun-hypothesis-validation-2026-06-19.md` — **VALIDATED WITH NOTES**.

### Session 7 protocol (S35-11 — should-have)

Post-UX-foundation + post-S35-06 tooltip wave. Run after S35-03 merged; optional before sprint closeout.

| Step | Action |
|------|--------|
| 1 | Author proxy: `production/playtests/playtest-YYYY-MM-DD-s35-polish-validation.md` |
| 2 | Cover axes: NPE onboarding copy (S35-06), comms degrade legend, datalink lag helper (S34-07 vocabulary) |
| 3 | Cross-reference S35-03 docs: `design/accessibility-requirements.md`, `design/ux/interaction-patterns.md`, `design/difficulty-curve.md` |
| 4 | Cite automated gate: C2 checks 1–13 filter **61/61** after S35-06 |
| 5 | Optional human companion: `production/playtests/human/playtest-YYYY-MM-DD-s35-polish-thinkaloud.md` |
| 6 | Triage findings; update fun-hypothesis doc if verdict changes |

**Anchors:** `production/qa/c2-manual-signoff-2026-06-02.md`; `production/qa/sprint-35-c2-signoff-*.md` (post-S35-07).

## Smoke Closeout Checklist (S35-14)

Execute at sprint closeout tip after all must-have stories merged.

```bash
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog" -v minimal
git diff HEAD -- "**/DelegationBridge.cs"   # expect empty
```

| Check | Target | Evidence path |
|-------|--------|---------------|
| Build | 0 errors | `smoke-sprint-35-closeout-*.md` |
| Full sln | **≥1193/1193** PASS | same |
| ReplayGolden | **6/6** PASS | same |
| Baltic hash | `17144800277401907079` | same + `tests/regression/replay-golden-*.txt` |
| C2 proxy | **61/61 + 58/58** (18/18) | same + `sprint-35-c2-signoff-*.md` |
| DelegationBridge | ZERO diff | same |
| GitNexus @ tip | indexed | `production/agentic/sprint-35-gitnexus-*.md` |
| Carry-forward | S35-15/16/17 if dropped | closeout smoke §deferred |

## Out of Scope

Per [polish-scope-boundary-2026-06-19.md](../polish-scope-boundary-2026-06-19.md) §Explicitly Out of Scope:

- Cesium/globe production, `HYPERSONIC_ALERT` UI, hypersonic DOTS spawn
- C2 delegation badges + trust emit-only UX (Req 04 — **S36+**)
- Loadout/magazine Unity surfacing (Req 16/21)
- TL Phase 5 physical SQLite forks, full corpora in CI, full ECCM Phase 2
- DOTS/ECS hot-path migration, 5k-entity scale proof
- **ZERO touch** on `DelegationBridge.cs`
- Tracker rows 01–21 → MVP-complete claims
- S35-15 CI hygiene (6th deferral OK)
- S35-16 dependency-graph plan-only (nice-to-have)
- Live Unity Editor screenshots as hard gate (advisory polish only)

## Entry Criteria

1. Smoke check PASS at `production/qa/smoke-sprint-35-baseline-2026-06-19.md` — **met** (S35-01)
2. Build stable — `dotnet build ProjectAegis.sln` 0 errors @ `8de98b1` — **met**
3. QA plan merged — `production/qa/qa-plan-sprint-35-2026-06-19.md` — **this document** (S35-02)
4. Polish scope boundary acknowledged — **met**

## Exit Criteria

- All must-have stories (S35-01..07, S35-14) PASS or PASS WITH NOTES
- No open S1/S2 bugs in delivered paths
- C2 proxy **18/18** maintained (61/61 + 58/58)
- ReplayGolden **6/6**; full sln **≥1193/1193**; Baltic hash unchanged
- UX foundation trio committed (S35-03)
- Smoke closeout: `production/qa/smoke-sprint-35-closeout-*.md`
- QA sign-off report: `production/qa/qa-signoff-sprint-35-2026-06-19.md` (sprint closeout)