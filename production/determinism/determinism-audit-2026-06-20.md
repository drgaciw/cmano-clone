# Determinism Audit Report — S41-04

**Date**: 2026-06-20  
**Scope**: full (S41-04 pass per sprint-41-polish-hardening-release-preflight.md + polish-scope-boundary-2026-06-19.md)  
**Engine**: .NET 8.0.400 / C# (headless sim core in ProjectAegis.Sim + Delegation; UnityAdapter for harness only)  
**Deterministic boundary** (per determinism-audit skill + previous 2026-05-29):  
- `src/ProjectAegis.Sim/{Core,Sensors,Engage,Policy,Scenario,Time,Glossary,Catalog,Logistics}`  
- `src/ProjectAegis.Delegation/{Core,Decision,Controllers,Orchestration,Policy,Sim,Projection,Replay}` (Hindsight exempt per AGENTS.md)  
- Harness: `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` + tests (presentation-adjacent only for gate)  
**Audited by**: determinism-engineer + team-simulation (csharpexpert systematic scan) via GitNexus MCP + direct source + dotnet  
**Files scanned**: 200+ .cs on deterministic path (full src/ProjectAegis.Sim + core Delegation; 923 .cs total solution scan)  
**Commit / HEAD**: c4d6e52 (main)  
**Cites**: production/polish-scope-boundary-2026-06-19.md (Baltic hash pin `17144800277401907079`, ReplayGolden 6/6, no prod code changes); production/sprints/sprint-41-polish-hardening-release-preflight.md (S41-04 must); docs/reports/future-sprint-roadpmap.md §Horizon 3; production/determinism/determinism-audit-2026-05-29.md + replay-2026-06-02.md + replay-2026-06-04.md; AGENTS.md (GitNexus impact mandatory, determinism rules); .claude/skills/determinism-audit/SKILL.md + replay-verify/SKILL.md; ADR-004 (tick pipeline order).

---

## Executive Summary

| Severity | Count | Must Fix Before Merge/Release |
|----------|-------|------------------------------|
| CRITICAL | 0 | — |
| HIGH | 0 | — |
| MEDIUM | 0 | — |
| LOW | 0 (defence-in-depth notes only) | Optional |

**Reproducibility recommendation**: **DETERMINISTIC — SAFE** (replay verified). No production code changes executed (audit + evidence only). Baltic world hash preserved exactly.

**Verification gates passed**:
- `ReplayGoldenSuiteTests` 6/6 PASS (engage/comms/classify/stale/spoof/readiness).
- Seeded Baltic (seed=42, baltic-patrol, 4 ticks): WORLD_HASH=`17144800277401907079` (exact match to golden in `tests/regression/replay-golden-baltic-engage-2026-06-02.txt`); A vs B match across fresh processes.
- GitNexus re-index @ HEAD: ✅ up-to-date.
- Full sln tests: no regressions in exercised paths (1226+ tests baseline range per AGENTS post-S40).

---

## Audit Execution (Commands + GitNexus)

### 1. Baseline Confirmation (S41-01 prerequisite)
```bash
export PATH="$HOME/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet restore ProjectAegis.sln
dotnet test ProjectAegis.sln --filter "FullyQualifiedName~ReplayGoldenSuiteTests" -v normal
# Result: 6/6 Passed (Pinned_regression_case_matches_golden_hashes for all catalog cases)
```
- PlayModeSmokeHarnessTests path (proxy baseline): `dotnet test ... --filter PlayModeSmokeHarnessTests` → 18+ passed (headless harness subset).
- Full sln (no-build): multiple assemblies 0 failures (279 Sim.Tests + 245 Delegation.Tests + 252 UnityAdapter.Tests + 403 Data.Tests + ...).

### 2. GitNexus Health + Context Queries (pre + post re-index)
```bash
node .gitnexus/run.cjs status
# Pre: stale (indexed ec0d3d9 vs HEAD c4d6e52)
node .gitnexus/run.cjs analyze
# Post: ✅ up-to-date @ c4d6e52 ; 17,797 nodes | 35,790 edges | 386 clusters | 300 flows
```
MCP calls (via use_tool after search_tool):
- `gitnexus__list_repos` → cmano-clone (17797 nodes post).
- `gitnexus__query` "tick pipeline simulation core deterministic order" → SimTickRunner, ISimTickRunner, SeededRng, DeterministicDetectionLoop, SimTickPipelineTests, DetectionWorldHash.
- `gitnexus__query` "RNG SeededRng random seed determinism" → SeededRng (Sim + Delegation/Decision), ReplayGoldenTests, DeterministicDetectionLoopTests, CombatOutcomeResolver.
- `gitnexus__query` "wall clock DateTime.Now..." → only Unity/presentation + ICatalogClock (data write gate; no sim tick path).
- `gitnexus__query` "unordered Dictionary..." → CatalogSortKeyComparer, Deterministic* helpers, explicit .Sort(Ordinal), no hot-path reliance on Hash/Dict iteration order.
- `gitnexus__context` uid=Class:src/ProjectAegis.Sim/Core/SeededRng.cs:SeededRng → static UnitFloat + Mix (pure, seeded).
- `gitnexus__context` uid=Class:src/ProjectAegis.Sim/Core/SimTickRunner.cs:SimTickRunner → fixed Clock + MixWorldHash (impl ISimTickRunner).
- `gitnexus__impact` (summaryOnly) SeededRng upstream → LOW risk (0 direct).
- `gitnexus__impact` SimTickRunner upstream → HIGH (expected; 24 impacted, 2 d=1, affects Orchestration/Engage/Time via DelegationBridge etc.; read-only analysis).

### 3. Replay-Verify Equivalent (seeded Baltic + golden)
```bash
# Suite (A vs B + golden assert)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/... --filter FullyQualifiedName~ReplayGoldenSuiteTests
# 6/6 PASS

# Direct demo (order log + hash; two fresh processes)
dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed 42 --scenario baltic-patrol --ticks 4
# Output (both runs):
# FINGERPRINT_SHA256=080a4cbf18f620043a4e6401ac1f60749e9604760b91d2adfc770de816649917
# DETECTION_WORLD_HASH=15600
# WORLD_HASH=17144800277401907079
# (order log lines match; checkpoints include final hash; KILL_CONFIRMED etc.)
```
- Matches `tests/regression/replay-golden-baltic-engage-2026-06-02.txt` exactly.
- Full catalog cases (via suite) cover engage/classify/comms/stale/spoof/readiness (seeds 42/11/7).

### 4. Static Pattern Scan (determinism-audit categories)
**Category 1: Time/Wall-clock** (grep DateTime.Now/UtcNow, Time.deltaTime, Stopwatch, TickCount etc.):
- **Zero hits** in Sim/ core Delegation deterministic paths.
- Presentation/UnityAdapter + data ICatalogClock only (exempt per boundary). SimTickRunner uses fixed SimClock + delta.

**Category 2: Collection Ordering**:
- Extensive use of `StringComparer.Ordinal`, `SortedSet<string>(Ordinal)`, explicit `Array.Sort(TrialSortComparer)`, `.OrderBy` on stable keys (Score or ids), `List` explicit index loops in hot paths (SimulationSession, PdDetection...).
- Dictionaries/HashSets keyed by Ordinal strings (no iteration for decision order); .Keys.ToList() + .Sort(Ordinal) before use (DatalinkSidePictureMerger).
- CatalogSortKeyComparer for stable import/hashes.
- No `foreach` over raw Dictionary on decision/sensors paths that feeds RNG/branch.

**Category 3: Randomness**:
- All RNG via `SeededRng` (Sim/Core static UnitFloat + Mix; Delegation/Decision instance xorshift) or injected.
- Agent salt: `DeterministicHash.OrdinalHash(id.Value)` (FNV-1a UTF-16, process-stable; replaces prior GetHashCode).
- No `new Random(`, `UnityEngine.Random`, `Guid.NewGuid` (tests only for temp paths), unseeded.
- Golden constants in ReplayGoldenTests lock the streams.

**Category 4: Float Order Sensitivity**:
- DecisionPipeline: weights from pool (IReadOnlyList or post-OrderByDescending stable), `weights.Sum()`, sequential `acc +=` over ordered array. Deterministic source order.
- CombatOutcomeResolver / detection: single `SeededRng.UnitFloat` draws (no accumulations over entity sets).
- No `Sum`/`Aggregate` over unordered collections feeding branches.
- Mix hashes are integer bit ops (deterministic).

**Category 5: Concurrency**:
- **Zero** `Parallel.For`, `IJob*`, `Task.Run`, `async`/`await`, `[BurstCompile]`, `.Schedule` on sim path.
- Hindsight async confined to dev memory (AGENTS: "Do not use Hindsight recall/reflect inside simulation Tick()").

**Category 6: Hidden Global / Mutable State**:
- Readonly statics only: Defaults (Scenario*Settings), Catalogs (PersonalityCatalog.All), singletons (comparers, Instance), Json options.
- No mutable static fields on path.
- Culture: explicit Ordinal / Invariant in key paths; no `float.Parse` without culture on hot path.
- ScenarioPolicyRepository.EnsureDefaultJsonLoaded (idempotent, deterministic fixture).

**Cleared (examples from scan + GitNexus)**:
- PdDetectionContactSimulator: _trials pre-sorted via DeterministicDetectionLoop.TrialSortComparer (Observer/Sensor/Target Ordinal); uses SortedSet + HashSet(Ordinal); explicit loops.
- DatalinkSidePictureMerger: explicit Sort(Ordinal) on unitIds/sides/observers; BuildObserversBySideSorted deterministic.
- DelegationOrchestrator.CreateAgent: salt via DeterministicHash → SeededRng.
- CombatDomain validators, PolicyEvaluator, Roe/Emcon: pure data-driven.
- SimTickPipeline / HeadlessSnapshot: fixed tick advance.

---

## GitNexus Index Health Note (Post S41-04)
- Pre: stale (1 commit behind).
- Post `node .gitnexus/run.cjs analyze`: ✅ up-to-date @ c4d6e52.
- Stats: 17,797 nodes, 35,790 edges, 386 communities, 300 flows (incremental +53 importers).
- Context/impact queries for tick/RNG/ordering/wall-clock successfully returned core symbols (SeededRng, SimTickRunner, DeterministicDetectionLoop, etc.) with no drift.
- Per AGENTS.md: index health recorded; ready for downstream S41 tracks.

---

## Issues Found + Recommended (Read-Only) Notes for Later Sprints
- **None blocking**. No CRITICAL/HIGH/MEDIUM.
- **LOW / hygiene (not on deterministic hot path; no change in S41)**:
  - Duplicate SeededRng (Sim/Core static vs Delegation/Decision instance) — consider consolidation post-Polish (track in structural-debt ADR S41-03).
  - Allocation notes in source (P2 follow-ups for explicit loops vs LINQ) — perf only, order preserved.
  - HashSet< string > for destroyed/bda (membership only; iteration not order-dependent per current use).
  - Off-path: AgentExperienceBlob Dictionary, ObservedState IReadOnlyDictionary (from 2026-05-29; still off hot path).
- **Future watch** (per boundary): any new RNG stream, float reduction over contacts, or collection iter in controllers must re-run full audit + replay-verify. Extend-only on CatalogWriteGate.
- **Platform float sensitivity**: note for cross-platform (not exercised here); hashes use integer mixes.

All findings verified by direct code + golden match + GitNexus.

---

## Next Step (per skill + plan)
- Pair with S41-05 evidence pack + S41-06 scope packet.
- `/replay-verify` mandatory on any future sim touch per boundary.
- Update replay report if new baselines (not required; hash preserved).
- Hindsight retain (if applicable): "S41-04: determinism clean @ c4d6e52; Baltic hash 17144800277401907079 locked; GitNexus reindexed."

**Verdict**: S41-04 COMPLETE. No blockers for closeout. ReplayGolden 6/6 + hash unchanged. GitNexus ✅.

---
*Generated read-only per S41-04 (no production code). Cites polish-scope-boundary-2026-06-19.md §Determinism / ReplayGolden maintenance + hash discipline; sprint-41 S41-04 ACs; roadmap §Horizon 3.*
