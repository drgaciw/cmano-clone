# Determinism & replay — developer guide

How Project Aegis keeps its simulation **reproducible**, and the rules you must follow so
your change does not break replay. Determinism is a hard invariant: a given
`(scenario, seed)` must produce the **identical** order log and end-state on every run and
on every platform (req 03 / req 04, delegation spec §2.5, [ADR-004](../architecture/adr-004-tick-pipeline-order.md)).

> **Why it matters.** A non-reproducible build is broken *even if every unit test passes*:
> replay debugging, after-action review, golden regression, and cross-machine agreement all
> depend on bit-for-bit reproduction. This is why `DateTime.UtcNow`, `Random.Shared`, and
> unordered iteration are effectively banned in the sim/delegation hot path.

This page is the *developer* reference (rules, hashing model, golden workflow, pitfalls).
For the **process gates** that prove reproducibility, use the
[`replay-verify`](../../.claude/skills/replay-verify/SKILL.md) and
[`determinism-audit`](../../.claude/skills/determinism-audit/SKILL.md) skills.

---

## The determinism contract

Everything downstream of a tick is a **pure function of** `(observed state, traits, seed)`:

```
DelegationBridge.Tick(snapshot, orderSink)
  → DelegationOrchestrator.Tick()
    → DecisionPipeline.Choose(candidates, traits, attention, rng)   // trait-weighted softmax
      → AutonomyGate → RoePolicyAdapter → IPolicyEvaluator
        → IOrderSink.ApplyOrder(...)
```

The pipeline draws **only** from seeded RNG, never from wall-clock time, process-global
randomness, or hash-table enumeration order. Two consequences follow:

- **Intra-run stability** — two fresh processes with the same `(scenario, seed)` must match.
  A mismatch here (`A != B`) is the worst case: the sim itself is unstable, and it is
  release-blocking.
- **Baseline stability** — a run must match its recorded golden unless the behavior change
  was intentional (then the golden is deliberately re-recorded; see below).

---

## Two hashes: world-state vs. order-log fingerprint

Reproducibility is asserted on two independent artifacts. Both must be stable.

| Artifact | Source | What it captures |
|----------|--------|------------------|
| **World-state hash** | [`SimWorldHash`](../../src/ProjectAegis.Sim/Core/SimWorldHash.cs) | Layered fold of the sim end-state: `Combine(coreHash, detectionHash, engageMix[, killMix])` (ADR-004 layers: core → detection → engage → combat-outcome). The harness exposes `WorldHash` and `DetectionWorldHash`. |
| **Order-log replay fingerprint** | [`DecisionLog.ComputeFingerprint()`](../../src/ProjectAegis.Delegation/Decision/DecisionLog.cs) → [`OrderLogReplayFingerprint.ComputeSha256Hex`](../../src/ProjectAegis.Delegation/Replay/OrderLogReplayFingerprint.cs) | A newline-delimited canonical text of every order-log entry (`kind|sequenceId|simTime|payload`), then SHA-256 hex. Captures the *decisions*, not just the end-state. |

The fingerprint is built from the **chronological** order log, sorted by monotonic
`SequenceId` (ADR-003). Any code that appends to the log in a non-deterministic order — or
formats a payload field non-deterministically — will change the fingerprint even when the
world-state hash looks stable, which is exactly why the golden files pin **both**.

---

## Seeded RNG

There are two RNG utilities. Use them; never introduce a new randomness source.

**Sim core — stateless, coordinate-addressed**
[`ProjectAegis.Sim.Core.SeededRng`](../../src/ProjectAegis.Sim/Core/SeededRng.cs) returns a
unit float in `[0,1)` as a pure function of `(seed, domain, entityId, simTick, drawIndex)`.
Because it is stateless, draw order does not matter for the *value* of a given draw — but the
`drawIndex` and `domain` do. Domains are enumerated in
[`RngDomain`](../../src/ProjectAegis.Sim/Core/RngDomain.cs): `Detection`, `Engage`,
`AgentDecision`, `Logistics`, `Combat`, `MineHazard`. Pick the correct domain so unrelated
subsystems can never alias each other's draw stream.

```csharp
double pk = SeededRng.UnitFloat(seed, RngDomain.Engage, shooterId, simTick, drawIndex: 0);
```

**Delegation — per-agent stateful stream**
[`ProjectAegis.Delegation.Decision.SeededRng`](../../src/ProjectAegis.Delegation/Decision/SeededRng.cs)
is a small xorshift stream seeded from `(globalSeed, agentSalt)` used inside the decision
pipeline. Here **draw order is significant**: each `NextUnit()` advances state, so reordering
draws changes every subsequent value. Keep the number and order of draws per tick stable.

---

## Float formatting is where determinism bugs hide

Floating-point values that reach a fingerprint or a hash must be **culture-invariant** and
**tail-quantized**, or they will diverge across locales (decimal separator) and platforms
(`double` round-trip noise, e.g. `0.1 + 0.2`). All such values go through
[`FingerprintFloat`](../../src/ProjectAegis.Delegation/Decision/FingerprintFloat.cs):

- `FingerprintFloat.Format(double)` — fractional values (scores, RNG draws, Pk, fuel, HP):
  invariant culture, `"0.######"` (max 6 decimals, trailing zeros trimmed).
- `FingerprintFloat.Time(double)` — integer-valued sim ticks: invariant-culture round-trip.
- Both collapse IEEE-754 **negative zero** (`-0.0`) to `+0.0`. `-0.0 == 0.0` numerically, but
  `ToString` renders `"-0"`; an arithmetic path that produces `-0.0` for a "no change"
  quantity would otherwise desync the fingerprint from a state-identical run.

**Rule:** never `ToString()` a float into a fingerprint/hash directly. Route it through
`FingerprintFloat`.

---

## The replay golden workflow

Golden fixtures live in [`tests/regression/`](../../tests/regression/) (`replay-golden-*.txt`)
and pin `WORLD_HASH`, `DETECTION_WORLD_HASH`, and (optionally) `FINGERPRINT_SHA256`. The
headless runner is
[`BalticReplayHarness.Run(seed, scenarioPolicyId, ticks, ...)`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs),
which returns a `Result` carrying the fingerprint, both hashes, checkpoints, and the decision
log.

**CI gate — [`ReplayGoldenSuiteTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenSuiteTests.cs)**
runs every case in
[`ReplayGoldenRegressionCatalog.All`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenRegressionCatalog.cs).
Each case runs the harness **twice** (asserting `A == B` — intra-run stability), asserts the
pinned hashes match the golden, and asserts required fingerprint fragments are present.

Verify locally:

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~ReplayGoldenSuiteTests
```

Regenerate a golden's values via the console demo (fields map 1:1 to the golden file):

```bash
dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed <SEED> --scenario <policy-id> --ticks <N>
# SEED=… SCENARIO=… TICKS=… ENGAGEMENTS=…
# FINGERPRINT=…
# FINGERPRINT_SHA256=…
# DETECTION_WORLD_HASH=…
# WORLD_HASH=…
```

Copy `WORLD_HASH`, `DETECTION_WORLD_HASH`, and `FINGERPRINT_SHA256` into the matching
`replay-golden-*.txt`. The seed / scenario / ticks per file are the source of truth in
`ReplayGoldenRegressionCatalog` (see also [`tests/regression/README.md`](../../tests/regression/README.md)).

> **Never re-record a golden to make a red test go green.** A changed golden is a behavior
> change. Only re-record after confirming the change is intentional, and say so in the commit
> message. If you cannot run the harness in your environment, record the result as **NOT RUN**
> — that is not a PASS (mirrors the `replay-verify` gate).

---

## Hard invariants

These are enforced repo-wide; see [`AGENTS.md` → Hard Invariants](../../AGENTS.md#hard-invariants--never-break-these).

- **Production Baltic v2 replay hash `17144800277401907079`** must be preserved unless an ADR
  explicitly changes it. Verify: `grep -r "17144800277401907079" tests/ data/`.
- **ReplayGolden 6/6** (core Baltic v2 suite) and **≥1232** solution tests, 0 failures
  (monotonic — never regress).
- **Baltic v3 isolation** — `baltic-v3-*` policies and goldens are independent; never touch v2
  goldens when editing v3, and vice-versa.

---

## Common determinism pitfalls

Each rule below has caused a real regression in this codebase — the fixes are cited so you can
read the failure mode.

| Pitfall | Rule | Seen in |
|---------|------|---------|
| Wall-clock / global RNG in the sim path | Never call `DateTime.UtcNow`, `DateTime.Now`, `Random.Shared`, `Guid.NewGuid()`, or `Environment.TickCount` in delegation/sim code. Draw from `SeededRng`. | Enforced by `determinism-audit`; hot-path count is currently **0**. |
| Raw float `ToString` in a fingerprint | Route every float through `FingerprintFloat.Format` / `.Time`. | `fix(delegation): format replay-fingerprint floats invariantly` (`2abef95`). |
| Negative zero rendering as `"-0"` | Covered by `FingerprintFloat` (collapses `-0.0` → `+0.0`); do not bypass it. | `qa-loop-07: normalize negative zero in replay-fingerprint float formatting`. |
| Non-ordinal append / transition order | Emit order-log/contact transitions in a deterministic ordinal order; do not rely on enumeration or set order. | `qa-loop-01: emit contact transitions in deterministic ordinal order`; `qa-r2-07: non-ordinal contact ordering in stale-loss transitions`. |
| Culture-sensitive string mapping | Use `StringComparison.Ordinal` / ordinal (case-insensitive where intended) for lookups and posture mapping; never default culture. | `qa-r2-09 / qa-loop-05: case-insensitive EMCON posture mapping in CatalogRadarEmconResolver`. |
| Dictionary / `HashSet` iteration order | Enumeration order of `Dictionary`/`HashSet` is not guaranteed stable — sort by a stable key before appending to the log or hashing. | General rule (order-log fingerprint is `SequenceId`-sorted). |
| Float order-of-operations across paths | Keep arithmetic operand order identical across code paths that must agree; a reordered subtraction can flip a sign or a low bit. | See `FingerprintFloat` doc-comment rationale. |

Quick self-check before you touch sim/controller/policy code:

```bash
# Should return nothing new in hot paths
grep -rn "DateTime.UtcNow\|DateTime.Now\|Random.Shared\|Guid.NewGuid" src --include=*.cs | grep -v Tests
```

---

## Before you merge a sim / controller / policy change

1. `dotnet build ProjectAegis.sln` — 0 errors, 0 warnings.
2. `dotnet test ProjectAegis.sln -v minimal` — ≥1232, 0 failures.
3. `ReplayGoldenSuiteTests` — 6/6 (command above).
4. `grep -r "17144800277401907079" tests/ data/` — production hash still present.
5. If you *intended* a behavior change, re-record only the affected isolated goldens and note
   the reason in the commit; otherwise a golden diff means a regression.
6. Run the [`determinism-audit`](../../.claude/skills/determinism-audit/SKILL.md) (static scan)
   and [`replay-verify`](../../.claude/skills/replay-verify/SKILL.md) (dynamic proof) skills —
   neither alone is sufficient; together they are the sim merge gate.

---

## Related docs

| Topic | Doc |
|-------|-----|
| Deterministic sim core (hashes, seeded RNG, tick) | [`ProjectAegis.Sim/README.md`](../../src/ProjectAegis.Sim/README.md) |
| Replay harness / golden regeneration (console) | [`ProjectAegis.Delegation.Demo/README.md`](../../src/ProjectAegis.Delegation.Demo/README.md) |
| Tick pipeline order + world-hash layers | [`adr-004-tick-pipeline-order.md`](../architecture/adr-004-tick-pipeline-order.md) |
| Order-log schema | [`adr-003-order-log-schema.md`](../architecture/adr-003-order-log-schema.md) |
| Abort-reason codes pinned in fingerprints | [`abort-reason-catalog.md`](abort-reason-catalog.md) |
| Sim assembly boundary | [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) |
| Golden fixtures + regenerate steps | [`tests/regression/README.md`](../../tests/regression/README.md) |
| Reproducibility gate (dynamic) | [`replay-verify` skill](../../.claude/skills/replay-verify/SKILL.md) |
| Non-determinism static scan | [`determinism-audit` skill](../../.claude/skills/determinism-audit/SKILL.md) |
| Hard invariants + verification block | [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) |
| CI wiring (post-merge replay golden) | [`ci-and-branch-protection.md`](ci-and-branch-protection.md) |
