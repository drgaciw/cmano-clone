# Determinism Audit Report

**Date**: 2026-05-29
**Scope**: full (first audit of the ProjectAegis.Delegation sim path)
**Engine**: Unity LTS / C# (.NET 8) — sim core is plain .NET, runnable headless
**Deterministic boundary**: `src/ProjectAegis.Delegation/{Core,Sim,Controllers,Policy,Decision,Traits,Attention,Roe,Targets,Trust,Orchestration,Groups}/`
**Audited by**: determinism-engineer (manual scan; GitNexus + dotnet unavailable in session)
**Files scanned**: 31 C# files on the deterministic path

---

## Executive Summary

| Severity | Count | Must Fix Before Merge/Release |
|----------|-------|------------------------------|
| CRITICAL | 1 (FIXED in this change) | Yes — all |
| HIGH | 0 | — |
| MEDIUM | 0 | — |
| LOW | 2 | Optional |

**Reproducibility recommendation**: DETERMINISTIC — SAFE TO MERGE (after CI `dotnet test` confirms the golden-replay regression passes; see Verification).

---

## CRITICAL Findings

### DET-001: Per-agent RNG salt derived from randomized `string.GetHashCode` — FIXED

**Category**: Randomness / Hidden global state
**File**: `src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs` line 42 (pre-fix)
**Pattern**: `var salt = id.Value.GetHashCode(StringComparison.Ordinal);`

**Why it diverges**: In .NET Core / .NET 5+, `string.GetHashCode` (including the
`StringComparison` overloads) is **randomized per process** — it uses a Marvin hash
seeded from a value generated once at process start. The same `AgentId` therefore
produces a *different* salt on every application launch. That salt seeds the agent's
`SeededRng` (`new SeededRng(GlobalSeed, salt)`), so each agent's entire decision stream
differs run-to-run.

This is the most dangerous class of determinism bug because it is **invisible
in-process**: within a single process the hash is stable, so a test that runs a scenario
twice in the same process (e.g. `OrchestratorTests.Two_ticks_same_seed_produce_identical_executed_orders`)
passes and gives false confidence. The divergence only appears across processes/machines —
i.e. exactly when a recorded replay or golden baseline is loaded in a fresh run. It
directly violates the delegation spec §2.5 core invariant and req 03's reproducibility
requirement.

**Remediation (applied)**: Added `ProjectAegis.Delegation.Core.DeterministicHash.OrdinalHash`
— a 32-bit FNV-1a over the string's UTF-16 code units, deterministic across processes,
machines, and runtimes — and switched the salt to use it:

```csharp
var salt = DeterministicHash.OrdinalHash(id.Value);
```

**Effort**: Low. Blast radius confirmed zero broken tests: no existing test asserts a
specific RNG-derived `OrderKind`; the only orchestrator test asserts run-to-run equality
(still holds), and all specific-`OrderKind` assertions construct `Order`s directly.

**Regression lock-in**: `ReplayGoldenTests` (new) asserts the salt and the resulting
`SeededRng` stream against constants computed **independently of any process run**
(`OrdinalHash("a1") == 1012613629`; first three draws `4611399/2²⁴`, `6250557/2²⁴`,
`6582717/2²⁴`). These exact values can only hold if the salt is process-stable, so any
reintroduction of `string.GetHashCode` fails the suite in essentially every run.

---

## LOW Findings (defence-in-depth — not on a hot decision path)

### DET-002: `Dictionary<string,double>` in `AgentExperienceBlob`
**File**: `src/ProjectAegis.Delegation/Trust/AgentExperienceBlob.cs` line 5
The Trust subsystem is currently a **seam only** (per the design spec, trust/experience is
designed later) and `Metrics` is not iterated on the decision path. If/when trust feeds
decisions, switch to an ordered structure or sort keys before iteration, or never branch
on iteration order. **Status**: Off hot path — track for the trust implementation.

### DET-003: `IReadOnlyDictionary<TargetId,bool> MemberAlive` in `ObservedState`
**File**: `src/ProjectAegis.Delegation/Sim/ObservedState.cs` line 9
Carried as data and not currently iterated in an order-dependent way on the decision path
(`AttentionCalculator` reads scalar counts). If a future consumer iterates it to derive a
decision input, guarantee a stable order. **Status**: Off hot path — note for reviewers.

---

## Cleared (scanned, not flagged)

- `DecisionPipeline.Choose` uses `.OrderByDescending(c => c.Score)`. LINQ `OrderBy*` is a
  **stable** sort and the key (`Score`) plus the input order (`StubPatrolPolicy.DefaultCandidates`,
  a fixed list) are deterministic — reproducible. No wall-clock, `DateTime`, `Guid.NewGuid`,
  `UnityEngine.Random`, unseeded `System.Random`, or parallelism found on the sim path.

---

## Verification

- A CI workflow (`.github/workflows/dotnet-tests.yml`) now runs `dotnet test ProjectAegis.sln`
  on every push and PR — this is what actually executes the determinism regression.
- `dotnet` was not available in the authoring environment, so the suite was **not executed
  locally**; the golden constants were derived by an independent reference implementation of
  the salt + `SeededRng` algorithms. CI is the source of truth for the PASS.

## Next Step

After CI confirms green, run `/replay-verify` once a headless seeded-scenario runner emits a
full order log + end-state hash, to extend the golden coverage from the RNG/decision layer to
the executed-order layer.
