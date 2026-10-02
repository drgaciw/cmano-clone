# 26 - Verification, CI Gates & QA Gauntlet

**Review / acceptance state (2026-09-30):** Draft maturity retained; formal document promotion and criterion-level owner acceptance are pending. Implementation/test mappings below identify subsets, not complete product signoff. Live evidence: [tracker](../implementation-tracker.md#post-mvp-requirement-evidence-22-27) and [RTM](../../docs/architecture/requirements-traceability.md).

**Last Updated:** 2026-09-30 (additive evidence reconciliation)
**Related:** [01-Project-Overview.md](../requirements/01-Project-Overview.md) · [07-Agentic-Infrastructure.md](../requirements/07-Agentic-Infrastructure.md) · [08-Agentic-Architecture.md](../requirements/08-Agentic-Architecture.md) · [17-Replay-AAR-And-Order-Log.md](../requirements/17-Replay-AAR-And-Order-Log.md)  
**Status:** Draft — ready for design review  
**Requirement IDs:** `VER-01` … `VER-07`  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)  
**Engineering Runbooks:** [qa-gauntlet.md](../../docs/engineering/qa-gauntlet.md) · [qa-gauntlet-saboteur.md](../../docs/engineering/qa-gauntlet-saboteur.md) · [gauntlet-oracle-baseline.md](../../docs/engineering/gauntlet-oracle-baseline.md)  
**Tracker:** [current draft 26 evidence](../implementation-tracker.md#post-mvp-requirement-evidence-22-27) — **Draft / shipped apparatus subset; proposed acceptance extension** (Release stage)

---

## Purpose

Define the functional and operational requirements for the automated verification pipeline, deterministic continuous integration (CI) gates, the fail-closed QA Gauntlet, oracle evaluation machinery, and mutation/saboteur calibration in Project Aegis.

This document promotes the autonomous QA and verification system from an internal engineering convention into a first-class, auditable requirement document. It reconciles historical gaps where Monte Carlo/batch testing and CI evaluation were described purely as "studio process" or infrastructure stubs without explicit Functional Requirements or Acceptance Criteria.

---

## Additive INF-6.x gap clarification (historical grades retained)

In requirement document `07-Agentic-Infrastructure.md`, section 6 (*Experiment & Monte Carlo Agent*) historically described batch execution, parameter sweeps, and headless workers as "P1 for v1.0 / infrastructure stub acceptable", leaving formal verification gates and oracle evaluation out of the core functional criteria.

This draft specifies forward verification scope independently from the frozen S56 INF-6.x grades:

- **INF-6.1 … INF-6.5:** Historical product/infrastructure grades remain unchanged. The wider verification contract and its remaining acceptance gaps are detailed under **VER-01 … VER-07** below; landed verification subsets require their own evidence.
- **Headless-First Verification:** Automated testing, Monte Carlo stress runs, and fail-closed oracle evaluation are hard requirements for all simulation pipelines, not optional post-release tools.
- **Contract Reference:** All future verification contracts and CI gate criteria trace directly to `VER-01 … VER-07`.

---

## Architecture and Pipeline Overview

The verification ecosystem operates on a tiered, fail-closed evaluation pipeline executed either locally via developer tools or automatically in CI pipelines (e.g., GitHub Actions and Buildkite).

```
data/scenarios/gauntlet-*.policy.json      # Scenario policy + engage config + gauntlet.expect
        │
        ▼
ProjectAegis.Delegation.Demo  --batch      # BalticBatchRunner → BalticReplayHarness (headless sim)
        │  writes results.csv (LossesScoringCsvExporter schema)
        ▼
ProjectAegis.MissionEditor.Cli  gauntlet_oracle_eval   # GauntletOracleEvaluator (fail-closed)
        │  filters CSV rows to policy ID, checks bounds + fingerprint gates
        ▼
oracle-eval.json   { ok, allPassed, scenarios[] }      # exit 0 iff allPassed
```

### Key Components

| Component | Assembly / Path | Responsibility |
|---|---|---|
| **Batch Sim Harness** | `ProjectAegis.Delegation.UnityAdapter` (`BalticBatchRunner`, `BalticReplayHarness`) | Headless scenario execution across seeds and tick limits. Emits structured tokens (`CATALOG_UNIT`, `MAGAZINE_SEED`, `Engagement`, `CommsStateChange`, etc.). |
| **Scoring & CSV Exporter** | `ProjectAegis.Delegation` (`LossesScoringCsvExporter`) | Serializes execution outcomes (kills, missiles fired, policy denials, score, and decision log fingerprint). |
| **Oracle Evaluator** | `ProjectAegis.Data` (`GauntletOracleEvaluator`, `GauntletOracleExpect`) | Validates batch output against declared numeric envelopes and required fingerprint evidence tokens. |
| **Roster Validator** | `ProjectAegis.Data` (`GauntletRosterValidator`) | Pre-flight validation ensuring catalog platform refs and detection observer/targets resolve against tier rosters. |
| **CLI Evaluation Command** | `ProjectAegis.MissionEditor.Cli` (`GauntletOracleEvalCommand`) | Exposes `gauntlet_oracle_eval` verb for headless scripts and CI jobs. |
| **Saboteur Mutation Harness** | `tools/qa-gauntlet/saboteur.py` (`tools/qa-gauntlet/mutants/catalog.yaml`) | Applies calibrated mutant patches across isolated worktrees to verify oracle kill-rate and sensitivity. |
| **CI Gate Pipelines** | `.github/workflows/gauntlet-oracle.yml`, `.buildkite/pipeline.yml`, `tools/buildkite/dotnet-ci.sh` | Orchestrates CI builds, test execution against AGENTS.md floors, secret scanning, and oracle gates. |

---

## Functional Requirements

### 1. QA Gauntlet Escalating Complexity Ladder (`VER-01`)

The verification engine shall provide an automated, escalating multi-tier complexity ladder (Tiers 1 through 5, plus extra joint/multi-domain scenarios) to evaluate tactical AI, doctrine enforcement, and sim kinematics under varying operational stress.

- **Tier 1 (T1) — Basic Patrol & Geometry:** Verifies basic sensor contact, baseline patrol routes, and standard contact generation (e.g. 6 ticks).
- **Tier 2 (T2) — Escort, Strike & Salvo Boundaries:** Verifies active/passive EMCON behavior, basic strike execution, salvo size boundaries, and logistics triggers (e.g. 10 ticks).
- **Tier 3 (T3) — EMCON Phases, ID & Event Chains:** Verifies multi-stage EMCON transitions, contact classification/identification, event triggers, and weapon engagement lockouts (e.g. 16 ticks).
- **Tier 4 (T4) — Multi-Mission, Asymmetric ROE & Random Injects:** Verifies multi-mission coordination, asymmetric weapons-tight/weapons-free postures, mid-run jamming/cyber injects, and weighted target selection (e.g. 24 ticks).
- **Tier 5 (T5) — Theater Cascade & Dynamic Objectives:** Full theater-scale operations, dynamic victory conditions, cascading target destruction, and severe comms degradation (e.g. 40 ticks).
- **Joint / Multi-Domain Extra:** Air-surface-subsurface shooter integration and catalog-backed OOB smoke tests.

**Acceptance Criteria:**

- [ ] **VER-01.1** The ladder scenario manifest (`tools/qa-gauntlet/ladder.yaml`) shall remain the single authoritative source of truth for scenario membership and tick counts per tier.
- [ ] **VER-01.2** Execution across all tiers must execute headlessly without rendering or Unity Editor dependencies.
- [ ] **VER-01.3** Scenario policies must be schema-validated using `GauntletPolicyStrictKeys` before execution.

---

### 2. Fail-Closed Oracle Evaluation (`VER-02`)

All automated scenario verification runs must be evaluated by a fail-closed oracle evaluator (`GauntletOracleEvaluator`). A scenario run is deemed successful if and only if all evaluated metrics fall strictly within declared envelopes and all required qualitative evidence is present in the execution fingerprint.

- **Numeric Envelope Gates:**
  - `side`: Observed side matches expected side (case-insensitive).
  - `minKills` / `maxKills`: Total targets destroyed must satisfy bounds.
  - `maxMissilesFired`: Ammunition consumption must not exceed upper limit.
  - `minDenials` / `maxDenials`: Policy/doctrine rejections must fall within bounded window.
  - `minScore` / `maxScore`: Final scenario score must fall within bounded window.
- **Qualitative Fingerprint & Evidence Gates:**
  - `requireNonEmptyFingerprint`: Defaults to `true`; requires a non-empty `DecisionLog.ComputeFingerprint()`.
  - `requireFingerprintSubstrings`: List of mandatory evidence tokens (e.g., `CommsStateChange`, `Degraded`, `EventFired`) proving injects or state transitions occurred.
  - `requireTrueLaunchedShooters`: List of mandatory unit IDs that must appear as shooters in `Engagement|...|True|Launched` tokens, verifying multi-domain weapon release.

**Acceptance Criteria:**

- [ ] **VER-02.1** Any missing `gauntlet.expect` block, empty CSV, or scenario ID mismatch must trigger an immediate fail-closed failure (`Passed: false`).
- [ ] **VER-02.2** The evaluator must support dual profile resolution: default `--profile ladder` evaluating `gauntlet.expect`, and `--profile ci` evaluating `gauntlet.expectCi` (or falling back to `gauntlet.expect` when absent).
- [ ] **VER-02.3** Stripping or tampering with required evidence tokens in the fingerprint while maintaining numeric scores must result in an oracle failure (exit code non-zero).

---

### 3. Multi-Domain & Cross-Domain Verification Oracles (`VER-03`)

The verification framework shall provide specialized oracle checks across distinct simulation domains:

- **Kinematics & Platform Movement:** Verification of waypoint progression, speed clamps, fuel consumption, and physical bounds.
- **Sensor Detection & EMCON Posture:** Verification of active radar, passive ESM, IR/optical detections, and EMCON silence compliance.
- **Weapons Engagement & Fire Control:** Verification of salvo release limits, target deconfliction, intercept geometry, and abort codes.
- **Logistics & Ordnance State Bands:** Verification of Bingo, Joker, Shotgun, and Winchester transitions and engagement gates.
- **Comms & Network Degradation:** Verification of datalink partitioning, share lag, and degraded C2 states.
- **UI / C2 Presentation Source Contracts (`UiIa*`):** Verification of UI Toolkit contracts and planning gate invariants headlessly without running full graphic tests (`UiIaPlanningGateOracleTests`, `UiIaSelectionSyncOracleTests`, `UiIaCommsTriadOracleTests`, `UiIaPanelSettingsOracleTests`).

**Acceptance Criteria:**

- [ ] **VER-03.1** Domain-specific engagement denials must be verified against the canonical abort reasons in `abort_reason_manifest.json`.
- [ ] **VER-03.2** Logistics state transitions must produce traceable state records (`OrdnanceStateChangeRecord`) and block firing orders when Winchester/Bingo gates are closed.
- [ ] **VER-03.3** UI/C2 structural source contracts (`UiIa*`) must be validated via headless unit/oracle tests without requiring Unity UI Toolkit runtime display.

---

### 4. Deterministic Replay & Seed Invariance (`VER-04`)

The simulation engine and batch runners must guarantee 100% bit-level reproducibility when executed with identical seeds and scenario inputs.

- **SimClock & RNG Mix:** Simulation stepping must rely strictly on `SeededRng` and fixed timestep (`SimTickRunner`). Never use `System.Random.Shared` or wall-clock timestamps in the simulation hotpath.
- **Replay Golden Suite:** Shipped regression test suite asserting exact output fingerprints for standard baseline scenarios.
- **Golden Hash Invariant:** The Baltic v2 production replay golden hash **`17144800277401907079`** must remain strictly preserved.

**Acceptance Criteria:**

- [ ] **VER-04.1** Running a scenario N times with the same seed and binary build must yield identical order-log replay fingerprints and identical CSV metric outputs.
- [ ] **VER-04.2** All ReplayGolden suite tests (e.g., 6/6) must pass unconditionally on all CI runs.
- [ ] **VER-04.3** Any modification that alters the golden hash `17144800277401907079` requires an approved Architecture Decision Record (ADR) and explicit human review.

---

### 5. Mutation Testing & Saboteur Calibration (`VER-05`)

To ensure that the verification oracles remain sensitive and avoid false confidence, the test harness includes a mutation testing tool (`saboteur.py`) executing a catalog of deliberate code defects (`tools/qa-gauntlet/mutants/catalog.yaml`).

- **Mutant Roles:**
  - `control`: No-op modifications (e.g. comments) that **must survive** to prove the pipeline does not fail on cosmetic changes.
  - `defect`: Real architectural or logic corruptions (e.g. inverted ROE, disabled EMCON gate, salvo off-by-one) that **must be caught** by oracles or test suites.
  - `expected-miss`: Known edge-case bugs tracked until dedicated oracle pins land.
- **Safety Invariants:**
  - Mutants are applied only in throwaway git worktrees (`.worktrees/saboteur-<id>`).
  - Mutants are strictly forbidden from targeting locked evaluation machinery (`GauntletOracleEvaluator.cs`, `DelegationBridge.cs`, `Demo/Program.cs`).

**Acceptance Criteria:**

- [ ] **VER-05.1** Saboteur mutation execution must calculate and report the mutant kill rate (`caught / total_defects`).
- [ ] **VER-05.2** Any surviving `defect` mutant represents an identified oracle blind spot and must result in a registered defect in `production/qa/bugs/` and a corresponding test/oracle fix.
- [ ] **VER-05.3** The mutation runner must reject any catalog patch targeting locked evaluation infrastructure.

---

### 6. Continuous Integration (CI) Gates & Test Floors (`VER-06`)

All pull requests and branch integrations are gated by automated CI workflows enforcing deterministic test baselines and hard invariants.

- **Numeric Test Floors by Reference:** Test floor requirements are defined **strictly by reference to `AGENTS.md` §Hard Invariants** (`dotnet test ProjectAegis.sln -v minimal`). Individual documents and test scripts must cite `AGENTS.md` rather than inventing conflicting local numeric thresholds.
- **Mandatory CI Pipeline Stages:**
  - Clean build without warnings/errors in `Release` configuration.
  - Full test suite execution meeting or exceeding the current baseline floor with 0 failures.
  - Secret scanning (e.g., Gitleaks).
  - Headless C2 proxy smoke harness execution (PlayModeSmokeHarness).
  - Clean-room catalog gate (no proprietary CMO `.db3` files committed).
  - Deterministic Baltic Replay Golden suite validation.
  - Gauntlet Oracle PR evaluation workflow (`gauntlet-oracle.yml`).

**Acceptance Criteria:**

- [ ] **VER-06.1** CI scripts (`dotnet-ci.sh`, `verify-ci-local.ps1`) and documentation must reference `AGENTS.md` as the authoritative source for test floor counts.
- [ ] **VER-06.2** PR gate `gauntlet-oracle.yml` must execute a real headless batch run, validate against `gauntlet_oracle_eval --profile ci`, and verify fail-closed behavior on stripped tokens.
- [ ] **VER-06.3** Zero behavior modifications to `DelegationBridge.cs` hotpath shall be verified via git inspection during preflight checks.

---

### 7. Provenance & Artifact Traceability (`VER-07`)

All CI and headless verification runs must record complete execution provenance to enable instant diagnosis of drift, regressions, or nondeterminism.

- **Baseline Provenance Context:** Every oracle run writes a structured JSON artifact (`baseline-context.json`) containing:
  - Repository commit SHA.
  - Seed and tick counts.
  - SHA-256 checksum of the catalog database (`baltic_patrol.db`).
  - SHA-256 checksums of all staged policy JSON files.
  - SHA-256 checksum of the resulting `results.csv`.
- **Review Protocol:** When an oracle check fails, developers must compare `baseline-context.json` with the last green run to distinguish between intended catalog/policy updates and sim engine regressions.

**Acceptance Criteria:**

- [ ] **VER-07.1** Every CI gauntlet oracle run must upload `baseline-context.json` alongside `results.csv` and `oracle-eval.json`.
- [ ] **VER-07.2** Relaxing an oracle envelope is prohibited without documenting the root cause and verifying that qualitative fingerprint tokens remain intact.

**Proposed revision-bound acceptance extension (2026-09-30):** engineering authorization to update planning does not grant product acceptance. Reuse [DRG-201](https://linear.app/drgamtd-workspace/issue/DRG-201), [DRG-234](https://linear.app/drgamtd-workspace/issue/DRG-234), and [DRG-244](https://linear.app/drgamtd-workspace/issue/DRG-244) for evidence integration; [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208) remains an explicit owner acceptance gate.

- [ ] **VER-07.3** Each criterion-level evidence record shall include requirement and acceptance-criterion IDs; repository commit SHA; build/package identity; local-delta identity (clean or a hashed patch plus relevant untracked input manifest); scenario ID/hash and seed; catalog ID/hash; policy IDs/hashes; test layer; command/filter or interaction steps; result; artifact location/hash; collection time; and owner acceptance status/reference. Missing required provenance makes the record incomplete.
- [ ] **VER-07.4** Accepted evidence shall identify the owner's explicit decision, owner, date, and exact accepted revision/build/local delta and input identities. Automated green checks shall not imply owner acceptance. An unaccepted record shall state `pending` with owner/reference fields unset rather than inventing an approver.
- [ ] **VER-07.5** A different commit, build, local delta, scenario, catalog, or policy identity shall require criterion reassessment. Historical evidence remains attached to its original revision; carrying it forward requires explicit assessment and owner confirmation, never automatic acceptance of a newer build.
- [ ] **VER-07.6** Acceptance evidence validation shall exercise missing fields, mismatched revisions/inputs, stale historical evidence, and engineering-pass records without owner acceptance. A complete headless record shall identify its layer and shall not stand in for Editor/Play Mode acceptance where the criterion requires that layer.

**Implementation boundary:** existing `baseline-context.json` oracle provenance provides a subset. The complete criterion-level schema and acceptance validator are backlog work, not an implemented claim. See [2026-09-30 reconciliation](../../docs/superpowers/specs/2026-09-30-requirements-planning-reconciliation.md).

---

## Non-Functional Requirements

- **Performance & Headless Execution:** The full gauntlet verification suite (T1–T5) must be capable of executing in headless CLI environments under 1000×+ time compression.
- **Fail-Closed Safety:** All evaluation tools must default to failing on missing fields, unexpected keys, empty datasets, or unknown error states.
- **No Floating-Point Nondeterminism:** Scoring and metric calculations across platforms and architectures must use standardized string formatting (`CultureInfo.InvariantCulture`) and deterministic float comparisons.
- **Toolchain Alignment:** All CI and local test harnesses must standardize on .NET SDK 8.0.400 as specified in `global.json`.

---

## Acceptance Summary & Traceability

| Requirement ID | Summary | Primary Implementation Symbols | Verification Test / Hook |
|---|---|---|---|
| **VER-01** | QA Gauntlet Ladder | `tools/qa-gauntlet/ladder.yaml`, `BalticBatchRunner` | `BalticReplayHarnessGauntletTier12CatalogTests`, `BalticReplayHarnessGauntletTier35CatalogTests` |
| **VER-02** | Fail-Closed Oracle Evaluator | `GauntletOracleEvaluator`, `GauntletOracleExpect`, `GauntletPolicyStrictKeys` | `GauntletOracleEvaluatorTests`, `GauntletOracleEvalCommandTests`, `GauntletPolicyStrictKeysTests` |
| **VER-03** | Multi-Domain Verification Oracles | `GauntletRosterValidator`, `UiIaSourceReader`, `OrdnanceStateBands` | `GauntletRosterValidatorTests`, `UiIaPlanningGateOracleTests`, `UiIaCommsTriadOracleTests` |
| **VER-04** | Deterministic Replay & Seed Invariance | `SeededRng`, `SimTickRunner`, `DecisionLog.ComputeFingerprint` | `ReplayGoldenSuiteTests`, AGENTS.md golden hash grep (`17144800277401907079`) |
| **VER-05** | Mutation Testing & Saboteur Calibration | `tools/qa-gauntlet/saboteur.py`, `mutants/catalog.yaml` | `test_saboteur.py`, `test_evaluate_run.py`, `test_gate_stress_proof.py` |
| **VER-06** | CI Gates & Test Floors | `.github/workflows/gauntlet-oracle.yml`, `tools/buildkite/dotnet-ci.sh` | AGENTS.md §Hard Invariants test baseline (`dotnet test ProjectAegis.sln`) |
| **VER-07** | Provenance & Artifact Traceability | `GauntletOracleEvaluationResult`, `baseline-context.json` | `GauntletLadderOracleExpectCalibrationTests`, CI artifact upload |
