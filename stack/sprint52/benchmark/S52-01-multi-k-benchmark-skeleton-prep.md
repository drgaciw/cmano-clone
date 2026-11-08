# S52 Track A: Multi-k Headless Benchmark Skeleton Prep (Req 01 MVP Gate)

**Epic:** E6 (Req 01 Project Overview)
**Track:** benchmark (parallel to sim-api)
**Authority:** 
- roadmap §10/§12: "Multi-thousand-entity headless benchmark as MVP-done gate"
- post-release-scope-boundary-2026-06-21.md §S52: "Multi-thousand-entity headless benchmark as **MVP-done** for Req 01"
- Game-Requirements/requirements/01-Project-Overview.md (success: 5k+ entities)
- Game-Requirements/requirements/07-Agentic-Infrastructure.md (INF-5.1: record entity count, ticks, wall-clock; INF-5.2 swarm LOD)
- production/perf/perf-profile-polish-baseline-2026-06-19.md (current baselines, 5k notes)
- S51 corpora dep noted.

**GitNexus boundary cite (01/08):** 
- Symbols: BalticBatchRunner (HIGH per roadmap), SimulationSession (CRITICAL, 228 impacted upstream per gitnexus__impact), SimTickPipeline (CRITICAL), BalticReplayHarness (CRITICAL).
- Run `gitnexus impact BalticBatchRunner --direction upstream` + `SimulationSession` + `SimTickPipeline` before any S52 impl edits.
- Pre-flight: impact(), detect_changes() before commit. Cite this prep + post-release-boundary + roadmap.
- From gitnexus: affected processes include RunBatch (Demo), EnableMvpEngagement (Bridge), CLI commands. Modules: Baltic, Orchestration, etc.

**Determinism notes (from determinism-audit-2026-06-20.md + audits):**
- All paths seeded (SimSeed, globalSeed).
- Stable ordering (OrderBy ordinal, no unordered dict/enum iter in hash paths).
- NO wall-clock in WorldHash / fingerprint / scoring paths.
- ReplayGolden 6/6 gate after any harness/batch touch. Baltic hash pinned `17144800277401907079`.
- Harness: BalticReplayHarness + BalticBatchRunner + SimulationSession must preserve fingerprints.
- Hindsight exempt only outside tick/policy per AGENTS.md.
- determinism-engineer sign-off required for S52 merges (per boundary + roadmap).

**S51 Corpora Dependencies:**
- S51 corpora-ci provides full scenario corpus (beyond current ~30 baltic-*.policy.json) for multi-k stress cases.
- TL fork selection (S51) for testing different data tiers in benchmark without changing hashes.
- Benchmark data: use generated large ORBATs from corpora (entity count 1k–10k) + near_future_archetypes.json (maxSwarmEntities:5000).
- Note: Benchmark prep must not assume S51 runtime; plan for fixture data + policy overrides. Cite `production/post-release-scope-boundary-2026-06-21.md` dep table: 06 (corpora) → 01 (benchmark data).

**Current State (pre-S52):**
- BalticBatchRunner.Run(BatchRequest) + ExportCsv (scenarios, seeds, ticks).
- BalticReplayHarness.Result includes ScoringCsvRow, WorldHash, etc. (no explicit entityCount/ticks/walltime yet).
- INF-5.1 not yet implemented (records only basic; no perf metrics).
- Existing benches in perf-profile-polish-baseline use fixed small Baltic; no multi-k skeleton.
- Entity model: mostly TargetRegistry/DictionaryEngageWorldQuery; DOTS pilot isolated in S45 (not production hotpath).
- tools/batch-replay/ + Demo --batch.

**Req01 MVP Gate Criteria (skeleton targets):**
- Headless benchmark that can ingest/run scenarios with configurable 1000+ / 5000+ entities (synthetic or from corpora).
- Records per run: entityCount, ticksExecuted, wallClockMs, avgTickMs, worldHash/fingerprint, memory/GC if measurable.
- CSV/JSON artifact output (extend ExportCsv or new BenchmarkResult).
- Supports TimeCompressionMode.HeadlessBatch parity.
- Auto-LOD flag for swarm (INF-5.2) stub (no schema change to order log).
- Reproducible: same seed+policy → same hashes + metrics within tolerance.
- Integrates with existing ReplayGolden, does not regress 6/6.
- Smoke: run on baltic-patrol scaled + 1 synthetic multi-k fixture.

**Proposed Multi-k Benchmark Skeleton (planning outline — implement in S52-01/02):**

1. New or extended: `src/ProjectAegis.Delegation.UnityAdapter/Baltic/MultiKHeadlessBenchmark.cs` (or extend BalticBatchRunner with perf mode).
   - Record `BatchRequest` + perf fields: `EntityCount`, `WallClock`, `Ticks`, `AvgTickDuration`.
   - Use `Stopwatch` around harness runs (deterministic: outside hash path).
   - Query entity count: from catalogReader + scenario policy ORBAT expansion (use near future archetypes for scale).
   - For skeleton: support policy override `entityScale` or synthetic generator stub.

2. Metrics collector:
   ```csharp
   public sealed record BenchmarkResult(
       string ScenarioId, int Seed, int Ticks,
       int EntityCount,  // key for multi-k MVP
       long WallClockMs,
       double AvgTickMs,
       string WorldHash,
       string Fingerprint,
       // ...
   );
   ```

3. Runner entry:
   - `MultiKHeadlessBenchmark.Run(BenchmarkRequest req)` → list of results.
   - Export to JSON + append to perf CSV.
   - CLI hook in MissionEditor.Cli (additive).

4. Test skeleton:
   - In UnityAdapter.Tests/Baltic/: MultiKBenchmarkTests.cs
   - Assert metrics emitted, entityCount > threshold for scale scenario.
   - Verify determinism (A/B run match hashes + metrics shape).
   - Gate against small baseline from perf-profile.

5. Scenario scaling note:
   - Leverage S51 corpora for large scenarios.
   - For prep: stub `GenerateScaledOrbat(scenario, k)` using archetypes (defer full gen to S50 infra).
   - Swarm LOD: policy flag `enableSwarmLod` → lower sensor rate (no order-log change).

**Acceptance for skeleton (MVP gate ready):**
- [ ] Metrics artifact includes entity count, ticks, wall time (INF-5.1).
- [ ] Runs 1k+ entity config without crash (synthetic or scaled baltic).
- [ ] No regression to existing batch/replay (replay 6/6, tests pass).
- [ ] GitNexus impact + determinism cite in any future code.
- [ ] Planning stubs + this doc + update to perf-profile or new bench doc.
- Evidence: sample run JSON/CSV + test.

**Risks / Notes:**
- Performance numbers advisory (INF-5.3); no mutation of sim params.
- Keep sim rules pure C#; DOTS in separate track (expand S45 pilot).
- Boundary: touch Baltic* only with impact() + det signoff.
- Future: tie to 10k soak (ARCH-NFR-4), cloud batch farm.

**Verification before completion (prep):** Baseline held, no code edits in this phase. GitNexus + boundary cited here. Artifacts produced.

**Refs:**
- ADR-001 (sim boundary), ADR-004 (tick order), ADR-005 (DOTS).
- design/gdd/simulation-core-time.md , agentic-infrastructure.md
- production/determinism/ , production/perf/
- `docs/reports/future-sprint-roadpmap-062126.md` §12 dep matrix: S51 corpora → S52 benchmark.

**Next (S52 impl):** After S51 close, dispatch to .worktrees/stack/sprint52/benchmark ; use team-simulation skill patterns; full story readiness.
