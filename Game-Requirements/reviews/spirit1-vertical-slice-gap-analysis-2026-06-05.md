# Gap Analysis Report — "Spirit 1" (Vertical Slice MVP / Phase 1)

**Date:** 2026-06-05
**Analyst:** GitNexus-assisted review (knowledge graph + source cross-reference)
**Repo:** `cmano-clone` @ `eeed8e1` · index built 2026-06-05 22:13 UTC
**Scope confirmed with requester:** "Spirit 1" = the **Baltic-Style Vertical Slice (MVP)** milestone
(`production/milestones/vertical-slice-mvp.md`), cross-referenced against `Game-Requirements/`.

---

## 0. Premise corrections (read first)

Two anchors in the original request did not match the indexed repository. Both are corrected
here so the rest of the report rests on real data.

| Claim in request | Reality (verified) | Source |
|---|---|---|
| Milestone named **"Spirit 1"** | No such milestone/epic/sprint exists. "Spirit" occurs only as the **B‑2A Spirit** bomber in the CMANO unit DB and as figurative prose ("ADR spirit", "spiritual successor"). Real milestone vocabulary = **MVP / Phase 1 / Vertical Slice / Sprint N**. | repo-wide grep; `production/milestones/` |
| Graph state **45 nodes / 63 edges** | Live index holds **8,934 nodes / 18,746 edges** (1,190 files · 173 communities · 300 processes). 45/63 ≈ 0.5 % of the real graph — likely a stale or single-cluster figure, not the current `cmano-clone` index. | `list_repos`; `gitnexus://repo/cmano-clone/context` |

**Graph-health caveat:** the index's full-text/semantic search is currently degraded
(`embeddings: 0`; the `query` tool returns "FTS indexes missing — keyword search degraded").
Symbol/edge/Cypher data is intact and was used throughout; natural-language search was not
relied upon. Recommend `npx gitnexus analyze --force` to rebuild FTS/embeddings.

---

## 1. Requirement → code/symbol mapping

The MVP defines **5 Must-Ship + 2 Should-Ship** feature sets. Every one maps to live graph
symbols *and* a dedicated test suite. (`✅` = confirmed node in graph; paths are real files.)

### Must Ship

| # | MVP feature | Primary symbols (graph-verified) | Tests (graph-verified) |
|---|---|---|---|
| 1 | **Headless plan→fight→replay** | `SimulationSession` ✅ (`src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs`), `BalticReplayHarness` ✅, `DelegationBridge.EnableMvpEngagement`, CLI `ScenarioSimulateSampleCommand.Run`, `BalticBatchRunner.Run`; processes `OnEnable→ComputeExecuteSimTick`, `OnEnable→Classify` | `SimulationSessionPhaseTests` (`Tick_is_no_op_while_planning`, `BeginExecution_allows_ticks_and_advances_sim`), `BalticReplayHarnessReplayTests.Replay_scenario_emits_stable_checkpoints`, full `ReplayGoldenBaltic*` suite |
| 2 | **Pd detection + catalog basePd** | `TryGetBasePd` / `InferBasePd` ✅, `CatalogSensorBinding`, `DetectionBindingKey`, `ICatalogReader`→`InMemoryCatalogReader`/`SqliteCatalogReader`/`NullCatalogReader`; process `RunCatalogImportMarkdown→InferBasePd` | `BalticReplayHarnessPdDetectionTests.Baltic_patrol_pd_detection_fingerprint_is_stable`, `BalticReplayHarnessCatalogTests.Catalog_scenario_produces_deterministic_detection_hash` |
| 3 | **Order log + checkpoints + C1 combat** | `CombatDomainValidator` ✅, `CheckpointIntervalTicks`/`ScenarioReplayJsonDto`, `EngageAttackOptions`, `PolicyEvaluator` | `IOrderLogContractTests`, `OrderLogC1RowTypesTests`, `OrderLogFingerprintTests`, `OrderLogReplayFingerprintSha256Tests`, `EngagementOrderLogContractTests`, `ReplayGoldenTests` |
| 4 | **Contact Classify/Identify FSM** | DTO lifecycle `ContactLifecycle`/`ClassifyAfterTicks`/`IdentifyAfterTicks`/`ContactIdentified`; process `OnEnable→Classify`, `OnEnable→FromContactChange` | `BalticReplayHarnessContactTests`, `ContactChangeOrderLogTests`, `ReplayGoldenBalticClassifyTests.Classify_scenario_emits_classified_and_identified_without_engage`, `BalticReplayHarnessStaleTests` |
| 5 | **Minimal sensor C2 UI** | `SensorC2PanelHost` ✅ (`unity/.../Runtime/SensorC2PanelHost.cs`), `RightUnitPanelHost`, `UnitDetailPanel`; bridge `SensorC2Bridge`, `SensorC2PanelBinder` | `SensorC2BridgeTests`, `SensorC2PanelBinderIntegrationTests`, PlayMode `Baltic_patrol_sensor_c2_snapshot_matches_harness_run` |

### Should Ship

| # | MVP feature | Primary symbols | Tests |
|---|---|---|---|
| 6 | **CMO catalog import pipeline** | `CmoMarkdownImporter` ✅, `CatalogJsonImporter`, `CatalogWriteGate` (`ProposeSensorBatch`/`UpsertSensor`/`TryReadCurrentBasePd`), `CatalogQuarantinePromoter`, `ValidationPipeline`, `ScenarioPackage`, migrations 001–005 | `CmoMarkdownImporterUnitTests.InferBasePd_maps_range_units_deterministically`, `CatalogJsonRoundTripTests` |
| 7 | **Message log UI bridge** | `MessageLog*` projection + binder (Projection cluster, 101 symbols) | `MessageLogProjectionTests`, `MessageLogPanelBinderTests`, `MessageLogBridgeTests`, `MessageLogPanelBinderIntegrationTests.Harness_combat_messages_bind_to_panel_rows` |

**Coverage:** 7/7 MVP feature sets are present in the graph with implementation **and** automated
tests. No Must-Ship feature is missing a structural presence.

---

## 2. Gaps (requirement defined, no / partial structural presence)

These are **scoped gaps within the MVP**, not absences of the whole feature. Severity reflects
MVP impact, not full-game impact.

| Gap | Evidence | Severity | Note |
|---|---|---|---|
| **G1 — Sensor C2 MonoBehaviour is an isolated graph node** | `context(SensorC2PanelHost)` → `incoming: {}`, `processes: []`; only outgoing edges are its own Unity lifecycle methods. Wiring to the sim is captured one layer away in `SensorC2Bridge`/`SensorC2PanelBinder` (adapter/test assemblies), not as an edge into the host. | **Medium (structural/traceability)** | Classic Unity `.asmdef` boundary: the Unity assembly is indexed but cross-assembly calls into `ProjectAegis.*` core aren't materialised as graph edges. Functionally tested via bridge; **not** traceable through the graph as a wired path. |
| **G2 — Classify "FSM" is scripted lifecycle, not an emergent state machine** | No `*Fsm`/`*StateMachine` class surfaces; classification is driven by `ClassifyAfterTicks`/`IdentifyAfterTicks` scenario-policy DTO fields + order-log transitions. | **Low (by design for MVP)** | Matches req 15 MVP intent (deterministic, replay-stable). Full sensor-confidence FSM is post-MVP ("ECCM Phase 2; datalink delay" in tracker). |
| **G3 — Unity Editor not in CI** | Milestone Risk Register "Unity Editor not in CI — Mitigated" via headless PlayMode harness; reinforced by G1 (Unity layer poorly edge-linked in graph). | **Low (mitigated)** | Headless adapter covers smoke; true editor PlayMode unverified in CI. |
| **G4 — "Full platform import" beyond export bridge** | Tracker req 06: P0 complete (`CatalogWriteGate`, `ValidationPipeline`, migrations) but "Full platform import / balance drift / TL branching" pending; milestone ships **export bridge only** ("manual catalog only" cut path). | **Low (Should-Ship, cuttable)** | Within the MVP's declared cut scope. |
| **G5 — Graph index FTS/embeddings missing** | `embeddings: 0`; `query` degraded. | **Medium (tooling, not product)** | Blocks semantic requirement-trace queries; rebuild recommended. |

**No critical (Must-Ship) functional gap was found.** The Must-Ship loop is fully present,
edge-linked in the `src/` core, and golden-tested.

---

## 3. Completeness score per feature set

Scoring = (structural presence + test coverage + graph traceability), weighted to MVP scope.

| Feature set | Tier | Completeness | Rationale |
|---|---|---:|---|
| Headless plan→fight→replay | Must | **100 %** | Impl + CLI + demo + 20+ replay/golden/checkpoint tests; core loop edge-linked. |
| Pd detection + catalog basePd | Must | **100 %** | basePd readers across 4 impls; deterministic detection-hash tests. |
| Order log + checkpoints + C1 combat | Must | **100 %** | Exhaustive OrderLog contract/fingerprint suite; SHA-256 replay determinism. |
| Contact Classify/Identify FSM | Must | **95 %** | Functionally complete + golden-tested; scripted-lifecycle vs. true FSM (G2). |
| Minimal sensor C2 UI | Must | **85 %** | Functionally present + bridge-tested; MonoBehaviour host unlinked in graph (G1), Unity not in CI (G3). |
| CMO catalog import pipeline | Should | **90 %** | Importer + write-gate + quarantine + migrations; full platform import pending (G4). |
| Message log UI bridge | Should | **100 %** | Full categories + tabbed drawer + binder integration tests. |

**Weighted MVP milestone completeness: ≈ 95 %.**
Consistent with the milestone doc's own success criteria (all 9 boxes checked, 359/359 tests
PASS @ Sprint 11, replay golden PASS, gate **PROCEED**).

> **Scope distinction (important):** `implementation-tracker-2026-06-04.md` marks requirements
> **01–20 all "Partial."** That tracker scores the **full multi-year game corpus**, not the MVP
> slice. The Vertical Slice draws a thin vertical through those docs and completes *that slice*.
> A 95 % MVP and a "Partial" full-corpus status are both true and not in conflict.

---

## 4. Architecture vs. actual relationships (discrepancies)

| Intended (docs/architecture, milestone) | Actual (graph) | Verdict |
|---|---|---|
| Tick pipeline: scenario load → detection/engage w/ policy gates → order log + checkpoints → combat → AAR projection | Confirmed as edges/processes: `RunCatalogImportMarkdown→InferBasePd`, `OnEnable→ComputeExecuteSimTick`, `OnEnable→Classify`, `BuildPrimary→ResolvedUnitPolicy`, `TrySelectAttackOption→OnAppended`. | ✅ **Matches.** |
| `SimulationSession` / DecisionLog is a shared hub (Risk Register: "HIGH blast radius") | `impact(SimulationSession, upstream)` = **CRITICAL**, 68 impacted symbols, 10 direct callers, 5 processes, 6 modules (Baltic 43, Bridge 14). | ✅ **Matches & quantified.** Risk is real and still **Open** — gate impact analysis before edits here. |
| Unity C2 presentation consumes sim projections (UI Toolkit `SensorC2PanelHost`) | Host node has **zero incoming edges** and **no process participation**; binding only visible via `SensorC2Bridge`/`SensorC2PanelBinder` in adapter/test assemblies. | ⚠️ **Discrepancy (G1).** Functionally wired, structurally invisible to the graph — Unity `.asmdef` boundary not crossed by indexed edges. |
| Determinism is a first-class contract | `ReplayGoldenSuiteTests`, SHA-256 fingerprint tests, `CheckpointIntervalTicks`, CI step in `dotnet-reusable.yml`. | ✅ **Matches** (strongest-covered area). |

---

## 5. Recommendations (prioritised)

1. **Rebuild the graph index** — `npx gitnexus analyze --force` to restore FTS/embeddings (G5); enables semantic requirement-trace queries this report had to work around.
2. **Close the Unity traceability gap (G1)** — add an explicit adapter seam (e.g., an interface in `ProjectAegis.Delegation.UnityAdapter` that `SensorC2PanelHost` calls) so the host links into the core graph, making the C2 path auditable by `impact`/`context`.
3. **Treat `SimulationSession` as a frozen hub** — keep the milestone's "impact before edit" gate; consider extracting the C1 combat/engage step behind a narrower interface to shrink the CRITICAL blast radius.
4. **Label the FSM honestly (G2)** — if a true sensor-confidence FSM is intended post-MVP, track it as a named symbol so the requirement (req 15) and code stay traceable; today it's scenario-policy fields.
5. **Re-issue any "Spirit 1" planning artifacts** using the real milestone name to avoid the phantom-milestone confusion that triggered this review.

---

## Appendix — methods & evidence

- Tools: `list_repos`, `cypher` (symbol existence + pattern mapping), `impact` (blast radius),
  `context` (360° symbol view), resources `context`/`clusters`/`processes`.
- Symbol existence verified by direct Cypher `name IN [...]` against the live graph (28 hits for
  the 20 core MVP symbols, all at expected `src/` paths).
- `query` (NL/semantic) returned empty due to missing FTS indexes; **not** used as evidence.
- Cross-referenced docs: `production/milestones/vertical-slice-mvp.md`,
  `Game-Requirements/implementation-tracker-2026-06-04.md`,
  `Game-Requirements/Game-Requirements-Index.md`.
