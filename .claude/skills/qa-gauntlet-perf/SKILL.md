---
name: qa-gauntlet-perf
description: >
  Performance / soak axis for QA Gauntlet: layers a tick-time budget and a
  managed-heap growth ceiling onto any ladder tier, measured over the tier's tick
  budget (not the CI 10-tick smoke). Composes with the ladder like the weapons/ew/
  logistics stress axes but proves a *non-functional* property. Use when
  /qa-gauntlet-perf, /team-qa-gauntlet --mode perf, or a run needs regression
  protection against per-tick slowdown or unbounded memory growth over long runs.
argument-hint: "[--run-id <id>] [--tiers 1,2,3,4,5] [--seeds 42,7,123] [--baseline <sha>]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Bash, Task
---

# QA Gauntlet Perf — Tick-Time & Heap-Growth Axis

Tiers own tactical complexity; stress axes own *pressure*. This axis owns the
**non-functional** budget: the sim must not get slower per tick or leak managed
memory as scenarios escalate. It is **differential** — a candidate is judged
against a committed baseline, never an absolute wall-clock number (hardware varies).

**Not** a substitute for `/perf-profile` (deep single-capture spike analysis) or
`/soak-test` (long-duration stability). This axis is the **regression gate** that
composes with the ladder; those skills are the investigation tools it hands off to.

## Metrics & gates (differential, hard-fail)

| Metric | Source | Gate |
|--------|--------|------|
| Mean tick time | headless batch timing per tier | > baseline + tolerance (default +15%) → FAIL |
| p95 tick time | same | > baseline p95 + tolerance → FAIL |
| Managed-heap growth | `GC.GetTotalMemory(false)` sampled start vs end of a soak run | net growth above ceiling after settling → FAIL |

- Measure at **tier ticks** (T1=6 … T5=40 per `qa-gauntlet-forge/program.md`), and a
  dedicated long soak run (>= 500 ticks, fixed seed) for the heap metric.
- Determinism first: a perf run that changes the world-state fingerprint is invalid —
  measurement must not perturb sim behavior. Timing/allocation probes live in the
  harness, never on the deterministic path.

## Procedure

1. **Baseline**: resolve `--baseline` (default: last committed
   `production/qa/gauntlet/perf/baseline.json`, else current trunk SHA). If none
   exists, this run *records* the baseline and reports NO-GATE (first run).
2. **Measure**: run the shipped ladder driver with timing enabled per tier + the
   soak run; write `production/qa/gauntlet/<RUN_ID>/perf/measurements.json`.
3. **Gate**: compare against baseline with tolerances; write `perf-report.json`
   (`PASS | FAIL | NO-GATE`). LLM must not override a script FAIL.
4. **Route**: FAIL → hand the capture to `/perf-profile` (spike analysis) and open a
   defect via `/qa-gauntlet-remediation` (class `sim-code`); heap growth → also
   consider `/soak-test` for longer confirmation.

> **Harness note:** the timing/heap driver is `tools/qa-gauntlet/perf_gate.py`
> **(to be implemented)** — until it lands, drive measurement through the existing
> `run-gauntlet.sh` batch plus `dotnet` Stopwatch/allocation probes in the harness
> only, and mark the AAR `BLOCKED (perf-harness)` rather than claiming PASS.

## Hard invariants

- ZERO-touch `DelegationBridge.cs`; preserve Baltic v2 hash `17144800277401907079`.
- Differential only — never gate on absolute ms; never edit locked eval.
- Probes off the deterministic path; fingerprint must match the functional ladder run.
- Baseline regen is deliberate and committed (`qa(perf): rebaseline …`) with a reason.

## See also

- `/qa-gauntlet` — ladder owner
- `/qa-gauntlet-stress` — functional pressure axes (weapons/ew/logistics)
- `/perf-profile` — single-capture spike analysis (investigation)
- `/soak-test` — long-duration stability (heap confirmation)
- `/team-qa-gauntlet --mode perf`
