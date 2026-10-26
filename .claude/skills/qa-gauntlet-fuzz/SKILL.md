---
name: qa-gauntlet-fuzz
description: >
  Malformed-input / robustness track for QA Gauntlet. The ladder only ever feeds
  the sim well-formed, validated scenarios, so parsers and deserializers never see
  hostile input. This track feeds malformed catalog rows, corrupt/partial scenario
  JSON, out-of-range fields, and truncated replay files, and asserts the code fails
  *gracefully* (typed validation error, no crash, no silent bad state) rather than
  throwing unhandled or corrupting the sim. Use when /qa-gauntlet-fuzz,
  /team-qa-gauntlet --mode fuzz, or hardening scenario/catalog/replay ingestion.
argument-hint: "[--run-id <id>] [--targets scenario,catalog,replay] [--iterations 200] [--seed 42]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Bash, Task
---

# QA Gauntlet Fuzz — Malformed-Input Robustness

**Owns:** does ingestion *fail closed and legibly* on bad input?  
**Does not own:** functional oracles (`/qa-gauntlet`), variance of *valid* scenarios
(`/qa-gauntlet-forge`), or perf (`/qa-gauntlet-perf`). Forge widens the space of
*valid* pressure; fuzz attacks the *validation boundary itself*.

Never edit `src/`, `unity/`, catalog DB, `DelegationBridge.cs`, or locked eval.
Artifacts stay under `production/qa/gauntlet/<RUN_ID>/`. Fuzz *finds* defects; fixes
route to `/qa-gauntlet-remediation`.

## Targets & expected behavior

| Target | Mutations | Expected (PASS) |
|--------|-----------|-----------------|
| Scenario JSON | missing required keys, wrong types, out-of-range numbers, unknown IDs, duplicate units, cyclic/oversized structures | Validator rejects with a typed/`scenario-data` error; **no** unhandled exception, **no** partial load |
| Catalog rows | absent platform/sensor/weapon IDs, negative ranges, malformed enums, truncated tables | Catalog resolve fails closed; CatalogWriteGate untouched; clear error |
| Replay files | truncated stream, flipped bytes, wrong header/version, mismatched hash | Replay loader rejects and reports; **never** produces a "green" divergent replay |

**Seeded & reproducible:** every fuzz iteration is derived from `--seed` so a crash
is replayable. Record the exact mutated payload for any failure.

## Oracle (the only thing that counts)

A finding is a **defect** when input causes: an unhandled exception / process crash,
a hang, silent acceptance of invalid data, or a corrupted post-load sim state.
Graceful rejection with a typed error is **PASS**. "It rejected the input" is the
success case — do not file those.

## Procedure

1. Seed corpus from valid samples under `data/scenarios/`, the catalog DB, and a
   known-good replay; snapshot them read-only.
2. Apply mutation families per target for `--iterations`; feed each through the real
   ingestion path (mission-editor validate CLI / catalog resolve / replay loader).
3. Classify each outcome via the oracle above; write
   `production/qa/gauntlet/<RUN_ID>/fuzz/findings.json` (crash | hang | silent-accept
   | corrupt-state | graceful-reject) with the reproducer payload path.
4. Route: any non-graceful outcome → `/qa-gauntlet-remediation` (class `sim-code`,
   or `scenario-data` if it is a validator-coverage gap). UCA if the crash surfaces
   in a presentation/UnityAdapter path.

> **Harness note:** the mutation driver is `tools/qa-gauntlet/fuzz_ingest.py`
> **(to be implemented)**. Until it lands, drive a small deterministic mutation set
> by hand through the existing validate CLI / loaders and mark the AAR
> `BLOCKED (fuzz-harness)` rather than claiming full coverage.

## Hard invariants

- ZERO-touch `DelegationBridge.cs`; never mutate locked eval or the golden hash.
- Fuzz reads real ingestion code paths — do not stub them to make input "valid."
- A crash reproducer is retained, never deleted, until the fix + regression test land.
- Seeded and reproducible: no unseeded randomness in the mutator.

## See also

- `/qa-gauntlet` — functional ladder (valid input)
- `/qa-gauntlet-forge` — variance of *valid* scenarios (complementary, not overlap)
- `/qa-gauntlet-remediation` — Phase D TDD for findings
- `/team-qa-gauntlet --mode fuzz`
