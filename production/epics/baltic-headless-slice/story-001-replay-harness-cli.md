# Story 001: Headless replay harness CLI

> **Epic**: Baltic Headless Vertical Slice
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 3h
> **Last Updated**: 2026-06-01

## Context

**GDD**: `design/gdd/order-log-replay.md`
**Requirement**: TR-log-003 — replay hash / fingerprint gate

**Governing ADRs**: ADR-003 (unified order log schema), ADR-004 (tick pipeline order)

**Engine**: .NET 8 headless | **Risk**: LOW

## Acceptance Criteria

- [ ] `dotnet run --project src/ProjectAegis.Delegation.Demo` accepts `--seed`, `--scenario`, `--ticks` (defaults: 42, `baltic-patrol`, 4).
- [ ] Stdout prints `SEED=<n> SCENARIO=<id> FINGERPRINT=<sha256-or-stable-hash>` on success.
- [ ] Two consecutive runs with identical args produce identical `FINGERPRINT` line.
- [ ] Uses `DelegationBridge` + `SimulationSession` engage path when `--engage` (default true).
- [ ] Exit code 0 on success; non-zero on invalid scenario id.

## Test Evidence

- `tests/`: extend `ReplayOrderLogFingerprintTests` or add `Delegation.Demo` integration test invoking same helper as CLI.
- Manual: run demo twice, diff stdout fingerprint lines.

## Implementation Notes

- Shared static `BalticReplayHarness.Run(seed, scenarioPolicyId, ticks)` callable from Demo and tests.
- Reuse engage-only agent wiring from `PlayModeSmokeHarnessTests`.

## QA Test Cases

```
Test: CLI prints stable fingerprint
  Given: seed=42, scenario=baltic-patrol, ticks=4
  When: Run harness twice
  Then: FINGERPRINT lines are byte-identical
```