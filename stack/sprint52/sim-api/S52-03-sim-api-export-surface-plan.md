# S52 Track B: Sim API Export Surface Plan (Req 08) + DOTS Notes

**Epic:** E6 (Req 08 Agentic Architecture)
**Track:** sim-api (∥ benchmark per roadmap; separate code)
**Authority:**
- roadmap §10/§12: "Stable sim API export; expand S45 DOTS pilot"
- post-release-scope-boundary-2026-06-21.md §S52 E6 + §4: "stable sim API export"
- Game-Requirements/requirements/08-Agentic-Architecture.md (ARCH-0.x, ARCH-1.x, structured sim API, post-P0 external API P2; DOTS in §2)
- Game-Requirements/requirements/03-Simulation-Modes.md , 04-Agent-Delegation.md
- implementation-tracker: Req08 Partial → stable export + DOTS.
- S45 DOTS pilot (isolated); ADR-005.

**GitNexus, determinism, boundary cites (01/08):**
- Key symbols (CRITICAL/HIGH risk):
  - SimulationSession (CRITICAL, 61 direct, 228 impacted upstream; callers: DelegationBridge, CLI SimulateSample, Demo RunBatch, Orchestrator).
  - SimTickPipeline (CRITICAL, 11 direct; affects RunTick, EnableMvpEngagement, etc.).
  - BalticReplayHarness, BalticBatchRunner (HIGH for sim-binding; no concurrent changes).
  - SensorHotPath (MED per roadmap §5).
- Pre-flight mandatory: `gitnexus impact SimulationSession --direction upstream --includeTests true`; same for SimTickPipeline + detect_changes().
- Cite: `production/post-release-scope-boundary-2026-06-21.md` (E6, 01/08), this plan, roadmap-062126 §5/§10, determinism-audit-2026-06-20.md.
- Determinism: All API surface must preserve deterministic stepping (SimSeed, fixed Δt, stable order in Tick/engage). No side effects in getters. WorldHash / fingerprints identical. See ADR-003 (order log), ADR-004 (tick pipeline).
- DelegationBridge: ZERO touch default (CRITICAL invariant).
- Post any edit: replay 6/6 + full test floor.

**S51 Corpora Dependencies (noted):**
- Corpora (S51 E5) supply richer catalog + scenario data for API surface validation (large entity sets, TL variants).
- Benchmark track (∥) will consume scaled data; sim API plan must expose catalog binding + snapshot surfaces stably for external consumers.
- TL fork (S51) affects read paths — ensure export surface documents fork selection (read-only projection first).
- Prep: no hard dep; plan for "corpora-backed fixtures" in acceptance.

**Current Sim API Surface (headless/interactive parity):**
From code reads + GitNexus context:
- `ProjectAegis.Delegation.Orchestration.SimulationSession`:
  - Ctor(seed, engagement?, policyEvaluator?)
  - static BindMvpEngagement(...), BindMvpEngagementForScenario(...)
  - Phase, BeginExecution(), Tick(ObservedState), Orchestrator, Sim (SimTickPipeline)
  - Internal: EngageWorld, Magazines, etc. (MVP)
- `ProjectAegis.Sim.Core.SimTickPipeline` (implements ISimTickRunner):
  - Clock, Seed, LastWorldHash, Pending/LastProcessed Engagements, TickOnce? (via core)
  - Wires SimTickRunner + engagement resolver.
- `BalticReplayHarness.Result` + Run(seed, policyId, ticks, ...)
  - Rich snapshot: hashes, checkpoints, messages, scoring row, DecisionLog.
- `BalticBatchRunner`: BatchRequest/Row + Run/ExportCsv/Discover...
- Projections: ObservedState, DecisionLog, etc.
- No public stable "export" facade yet (post-P0 HTTP/gRPC noted in ARCH-0.5).
- DOTS: S45 pilot in unity/ + isolated; EntityKey in UnityAdapter; no hot-path migration without det signoff.

**Sim API Export Surface Plan (Req08 stable surface for S52):**
Goal: Document + (skeleton) stable public surface for headless, scriptable, external agent use (local-first). Supports batch, replay, future RL co-sim. Headless + in-editor parity. No breaking changes to existing harness.

1. **Surface Definition (target for S52-03/04):**
   - New or promoted: `ProjectAegis.Sim.Api` or extend `ProjectAegis.Delegation` facade (keep assembly boundary per ADR-001).
   - Core exports:
     - `ISimSession` / stable `SimulationSession` public members (document Phase, Tick contract, snapshot access).
     - `ISimTickRunner` / `SimTickPipeline` public API (Clock, Seed, LastWorldHash, step methods).
     - `IWorldSnapshot` or `SimWorldSnapshot` (from ISimWorldSnapshot pattern) — blittable-friendly for DOTS export.
     - Batch + harness results as DTOs (serializable JSON for export).
     - Catalog binding: `ICatalogReader` surface stable.
   - Export artifacts: extend Result with explicit `EntityCount`, perf counters (share with benchmark track).
   - External: documented "headless API" in docs/ (local scriptable via .NET hosting or CLI).

2. **Stability Contract:**
   - Additive only for S52 (no removal/rename of public on SimulationSession/SimTick* without major version).
   - Deterministic: same inputs → same outputs + hashes.
   - Snapshot: provide read-only projections (no mutation except via Orders).
   - Versioning: tie to DbReleaseRecord? or SimVersion (per separate release trains in req08).
   - Deprecate hidden paths if any.

3. **Export Formats (plan):**
   - Order log / checkpoints (existing).
   - JSON snapshot export (scenario state at tick N).
   - CSV metrics (from benchmark synergy).
   - Future (P2): gRPC/HTTP surface stub doc (ARCH-0.5) — not impl in S52.

4. **DOTS Expand Notes (S52 shared with dots-expand track):**
   - Expand S45 pilot: isolated fixture for entity store mirroring `TargetRegistry` / `ISimWorldSnapshot`.
   - Per ADR-005: world state in DOTS; rules stay pure C# (Sim).
   - Hot path: SensorHotPath, engage may move to Burst later (S53).
   - S52: document surface for ECS snapshot builder (e.g. `ToDotsEntities()` stub or interface).
   - Constraints: No production hot-path change; determinism sign-off; isolated tests only. Cite polish/release boundaries.
   - GitNexus: impact SensorHotPath, DOTS bridges.
   - Integration: keep SimTickPipeline as pure C# entry; DOTS adapter in UnityAdapter.

5. **Implementation Stubs / Plan Steps (S52):**
   - Audit current public surface (use GitNexus context + reverse doc).
   - Produce `docs/architecture/sim-api-surface.md` (or section in architecture.md) + TR updates.
   - Skeleton: `ISimExportSurface.cs` interface in Sim or Delegation (or in new Api ns).
     - `GetWorldSnapshot(tick)` , `GetPerfMetrics()` , `ExportJson()`.
   - Update harnesses to populate new fields (entityCount from registry/catalog).
   - Tests: surface contract tests (parity headless vs harness).
   - CLI/MCP exposure (additive).
   - DOTS: add notes/fixture in unity/ProjectAegis + engine-ref.

**Acceptance (plan complete / skeleton ready):**
- [ ] Surface doc produced (stable public API listed + examples).
- [ ] GitNexus impact reports + cites in plan + future changes.
- [ ] Determinism preserved (replay evidence).
- [ ] Entity/perf metrics exposed via API (align benchmark track).
- [ ] DOTS pilot expansion notes + no-regress evidence.
- [ ] Dep on S51 corpora noted for data scale.
- [ ] Boundary cite + roadmap §10/12.
- No DelegationBridge edits.

**Risks:**
- CRITICAL blast radius on SimulationSession/SimTickPipeline — changes only after impact + det review.
- DOTS must remain non-production until S53+.
- Export must not leak mutable state (projections only).
- Hash stability across API consumers.

**Refs:**
- ADR-001 (assembly), ADR-005 (DOTS), ADR-006 (data), architecture.md
- requirements/08-Agentic-Architecture.md (ARCH-*, interchange)
- design/gdd/simulation-core-time.md , agentic-infrastructure.md
- GitNexus reports (run on SimulationSession etc.)
- production/determinism/determinism-audit-2026-06-20.md , post-release-scope-boundary-2026-06-21.md

**Coord (parallel):** Benchmark track owns perf metrics impl; this track owns stable surface + export plan. Both verify det. Closeout aggregates.

**Verification-before-completion:** Prep artifacts only. Baseline invariants held. All cites present.
