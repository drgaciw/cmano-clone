# GitNexus Validation: Research → Requirements Improvements

**Date:** 2026-05-29  
**Scope:** Validate May 29 requirement updates (docs 01, 04, 06–10) against `docs/research/*.md` and indexed codebase  
**Tools:** `npx gitnexus status|detect-changes|query|impact|context --repo cmano-clone`  
**Index:** cmano-clone @ `10dae1a` (status: up-to-date; working tree has uncommitted C# not yet in indexed commit)

---

## Verdict: **CONCERNS**

The research-driven requirement improvements are **internally consistent** and **correctly traceable** to the three research supplements. GitNexus confirms the **implemented foundation aligns with the agentic/architecture research**, but **most near-future and speculative gameplay mechanics exist only in requirements** — not yet as code symbols. Several requirement statements should be tightened to reflect current blast radius and greenfield scope.

| Area | Research coverage in reqs | Code alignment (GitNexus) | Verdict |
|------|---------------------------|---------------------------|---------|
| Five-layer architecture (08) | Full | **Partial** — Sim + Delegation exist; DB/scenario/automation layers doc-only | CONCERNS |
| Dual-track DB + provenance (06) | Full | **None** — no DB intelligence symbols | CONCERNS |
| Monte Carlo / experiment agent (07) | Full | **None** | CONCERNS |
| Operator copilot (07) | Full | **None** | CONCERNS |
| Determinism + replay (08, 17) | Full | **Strong** — `SeededRng`, `ReplayGoldenTests`, `SimTickRunnerTests` | PASS |
| Policy / ROE chain (04, 13) | Full | **Strong** — `AutonomyGate` → `RoePolicyAdapter` → `PolicyEvaluator` | PASS |
| LAWS autonomy modes (04, 10) | Full | **Partial** — enum + gate; no `ACCOUNTABILITY_EVENT`, escalation | CONCERNS |
| Near-future systems (09) | Full | **None** — sensors, DEW, swarms, hypersonics greenfield | CONCERNS |
| Speculative TL / escalation (10) | Full | **None** — Kessler, JADC2, quantum doctrine not indexed | CONCERNS |
| Engagement pipeline (14) | Referenced | **Stub** — `IEngagementResolver`, `SimTickPipeline`, `SimulationSession` | CONCERNS |

---

## GitNexus Evidence

### Index & change detection

```
npx gitnexus status --repo cmano-clone
→ Up-to-date @ 10dae1a

npx gitnexus detect-changes --repo cmano-clone
→ 35 files, 29 symbols, 21 processes affected
→ Risk: CRITICAL (uncommitted Delegation/Sim wiring)
```

**Note:** Requirement markdown edits are **not** in the code graph. Validation used **cross-reference**: research themes → requirement sections → `query`/`impact`/`context` on expected implementation symbols.

### Confirmed execution flows (research-aligned)

| Flow | Steps | Validates requirement |
|------|-------|----------------------|
| `Evaluate → IsFireAction` | `AutonomyGate` → `RoePolicyAdapter` → `PolicyEvaluator` | Doc 04/08/13 policy chain |
| `Tick → AttentionDegradation` | `DelegationBridge.Tick` → `AgentController.TryDecide` | Doc 04 delegation |
| `Tick → Resolve` | `SimulationSession.Tick` → `SimTickPipeline.TickOnce` → `IEngagementResolver.Resolve` | Doc 08 sim kernel + doc 14 stub |
| `ConfigureSimulationMode → BindPolicySnapshot` | `AssignAgents` → `AgentController.BindPolicySnapshot` | Doc 13 snapshot on assign |

### Blast radius (implementation risk if extending research reqs)

| Symbol | Risk | Direct dependents | Implication for requirements |
|--------|------|-------------------|------------------------------|
| `AgentController` | **HIGH** | 16; processes `ConfigureSimulationMode`, `Rejoin` | Doc 04/09 swarm autonomy must extend `TryDecide` + tests, not parallel controller |
| `DecisionLog` | **MEDIUM** | 11; processes `Tick`, `LogEngagementResults` | Doc 07 AAR + doc 17 order log must **extend** `AppendPolicyDenial`, not new log |
| `PolicyEvaluator` | **MEDIUM** | 6; Policy + Roe modules | Doc 09 CCA modes should map through `ActionRequest` / `PolicyContext`, not bypass sim policy |
| `PolicySnapshotRegistry` | **LOW** | 1 (tests only) | Doc 06 provenance should wire registry into `DelegationOrchestrator` before claiming “integrated” |
| `SimTickRunner` | **LOW** (isolated) | Tests only; **no processes** | Doc 08 should prefer `SimTickPipeline` as canonical tick entry (already wired to engage) |

### Query gaps (research concepts with **zero** code symbols)

GitNexus `query` returned **no implementation** for:

- `database provenance monte carlo experiment batch`
- Sensor detection, EW duel, CEW, quantum doctrine (only req 15/09 markdown + `PerceivedStateFactory`)
- `MonteCarlo`, `JADC2`, `Kessler`, `Hypersonic`, `Provenance` (grep confirms no matches under `src/`)

This **validates** the research traceability doc’s open gaps (req 13–20) and confirms doc 09/10 content is **forward-looking**, not implementation claims.

---

## Research Document → Requirement Validation

### `agentic-cmano-research.md`

| Research claim | Requirement home | GitNexus validation |
|----------------|------------------|---------------------|
| Five subsystems (sim, DB, scenario, UX, automation) | Doc 08 table | Sim (`ProjectAegis.Sim`) + Delegation + Unity bridge exist; DB/scenario/automation **requirements only** |
| DB dual-track + provenance | Doc 06 | **Not implemented** — correct as requirement, premature as “ready for implementation” without schema epic |
| Propose-not-auto-merge agents | Doc 06 §8 | **N/A in code** — no agent write path |
| Monte Carlo experiment system | Doc 07 §6 | **Not implemented** — aligns with Phase 5 roadmap label |
| Operator copilot | Doc 07 §7 | **Not implemented** |
| Clean-room boundaries | Doc 01 §5 | **N/A to code graph** — process requirement, PASS |
| Deterministic sim | Doc 08 | **PASS** — `ReplayGoldenTests`, `OrchestratorTests.Two_ticks_same_seed…`, `SimTickRunnerTests` |

### `near-future-tech-research.md`

| Research decision | Requirement lock | Code status |
|-------------------|------------------|-------------|
| Swarm max 500 v1.0 | Doc 09 Resolved | No swarm entity/system code |
| DEW thermal/power | Doc 09 §2 | No DEW symbols |
| Quantum = doctrine unlock | Doc 09 §3 | No sensor/doctrine sim |
| Limited SDA/ASAT | Doc 09 §3 | No space domain code |
| 5 gap entities (Replicator, C-UAS, JADC2, hypersonic defense, SOSUS) | Doc 09 marked *(new)* | **Correctly flagged P0/P1** — all greenfield |

**Validation:** Resolved decisions in doc 09 are **research-faithful**. Priority tags (P0 hypersonic defense, P1 C-UAS) are appropriate given engagement stub only.

### `speculative-systems-research.md`

| Research decision | Requirement lock | Code status |
|-------------------|------------------|-------------|
| TL-0–TL-5 slider | Doc 10 | No `TechnologyLevel` enum in code |
| 5-tier escalation | Doc 10 | No escalation meter symbols |
| LAWS ROE modes | Doc 10 + doc 04 | `AutonomyLevel` + `AutonomyGateTests` **partial match**; missing accountability/escalation events |
| MaRV → doc 09 migration note | Doc 10 | Correct — hypersonics belong in near-future tier |
| BLACK_PROJECT_MODE | Doc 10 | No scenario flag in code |

---

## Requirement Improvements — Specific Findings

### PASS (keep as written)

1. **Technology Level framework (09/10)** — Correctly separates near-future vs speculative; matches research TRL tables.
2. **Resolved open questions** — All eight former open questions in docs 09/10 now match research supplements (swarm 500, DEW modeling, quantum abstraction, SDA scope, TL slider, escalation modeling).
3. **Traceability matrix** (`research-traceability.md`) — Accurate mapping; downstream GDD candidates correctly identified.
4. **Policy chain architecture** — Doc 08 five-layer table matches ADR-001/002 and GitNexus `Evaluate` process.
5. **Autonomy enum alignment** — Doc 04 `HUMAN_IN_LOOP` / `HUMAN_ON_LOOP` / `FULL_AUTONOMOUS` names align with `AutonomyLevel` + `AutonomyGate` behavior.

### CONCERNS (amend or sequence)

1. **Doc 06 status “ready for schema design”** — Valid, but GitNexus shows **zero** database layer symbols. Add explicit dependency: DB epic blocks doc 09 entity population.
2. **Doc 07 Experiment Agent P1** — Correct priority; should cross-reference `SimulationSession` + `DecisionLog` as **existing Monte Carlo inputs**, not greenfield session model.
3. **Doc 08 `SimTickRunner` as tick entry** — GitNexus shows `SimTickPipeline` is the active engage-integrated path. Requirement should name **`SimTickPipeline`** as canonical (ADR-004) to avoid duplicate tick stories.
4. **Doc 09 “deterministic at max swarm 500”** — No swarm code; add **performance budget placeholder** in req 08 NFR until ECS swarm systems exist.
5. **Doc 10 LAWS accountability** — Requirements describe `ACCOUNTABILITY_EVENT`; code has policy denial logging (`AppendPolicyDenial`) but not civilian/escalation events. Wire doc 10 to **extend `PolicyDenialRecord` / order log**, not new parallel event bus.
6. **`PolicySnapshotRegistry` under-integrated** — Requirement doc 06/13 imply snapshot provenance; registry only hit by tests (LOW impact). Orchestrator should be named as integration point in doc 13.

### FAIL (none)

No research findings were **misrepresented** or **contradicted** by requirements. No incorrect claims that features already exist in code (status lines say “research-integrated” / “GDD authoring”, not “implemented”).

---

## Critical Path (GitNexus-ordered)

Implementing research-backed requirements should follow blast-radius order:

```
1. DecisionLog schema (+ policy/engage entries)     ← MEDIUM risk, doc 17/07
2. PolicyEvaluator + PolicySnapshotRegistry wiring  ← MEDIUM risk, doc 13/06
3. IEngagementResolver (beyond stub)                ← new, doc 14/09 hypersonic defense
4. Sensor/contact sim (feeds ObservedState)         ← greenfield, doc 15/09
5. Database Intelligence Layer                      ← greenfield, doc 06
6. Monte Carlo / experiment runner                  ← greenfield, doc 07
7. TL-gated entity DB branches                      ← doc 09/10 content
```

Changing `AgentController.TryDecide` or `AutonomyGate.Evaluate` without updating golden tests (`ReplayGoldenTests`, `AutonomyGateTests`, `PolicyDenialLogTests`) will break **HIGH**-risk paths per `detect-changes`.

---

## Recommended Next Actions

1. **Update doc 08** — Reference `SimTickPipeline` + `SimulationSession` as implemented tick boundary (GitNexus `Tick → Resolve`).
2. **Update doc 13** — Explicit integration: `PolicySnapshotRegistry.Capture` on agent assign (`BindPolicySnapshot` flow).
3. **Run `/design-review`** on docs 13–20 using this validation’s gap list (HYPERSONIC_ALERT UI, KESSLER meter, JADC2 entity).
4. **Re-run `npx gitnexus analyze`** after committing current Sim/Delegation work so `detect-changes` reflects new baseline.
5. **Before doc 09 database population** — Run `gitnexus_impact` on `IEngagementResolver` and `PolicyEvaluator` when adding hypersonic/DEW action types.

---

## Commands Used

```bash
npx gitnexus status --repo cmano-clone
npx gitnexus detect-changes --repo cmano-clone
npx gitnexus query --repo cmano-clone -l 8 "policy evaluator ROE doctrine"
npx gitnexus query --repo cmano-clone -l 8 "delegation orchestrator agent autonomy"
npx gitnexus query --repo cmano-clone -l 8 "sim tick deterministic replay seed"
npx gitnexus query --repo cmano-clone -l 6 "SimTickPipeline engagement resolver policy snapshot"
npx gitnexus query --repo cmano-clone -l 5 "engagement sensor detection electronic warfare"
npx gitnexus impact --repo cmano-clone PolicyEvaluator --direction upstream
npx gitnexus impact --repo cmano-clone AgentController --direction upstream
npx gitnexus impact --repo cmano-clone DecisionLog --direction upstream
npx gitnexus impact --repo cmano-clone PolicySnapshotRegistry --direction upstream
npx gitnexus context --repo cmano-clone SimTickRunner
npx gitnexus context --repo cmano-clone DelegationOrchestrator
npx gitnexus context --repo cmano-clone AutonomyGate
```
