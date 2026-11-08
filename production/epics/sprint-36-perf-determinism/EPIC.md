# Epic: Sprint 36 — Perf / Determinism (P1 Follow + Audit)

**Sprint:** 36  
**Authority:** `production/perf/perf-profile-polish-baseline-2026-06-19.md` (post-S35) + `production/polish-scope-boundary-2026-06-19.md`  
**Follows:** S35-sim-perf (detection P0, DecisionLog/Datalink P1, re-profile)  
**Boundary:** Polish P0/P1 only. ReplayGolden + world-hash immutable discipline. **ZERO** touch `DelegationBridge.cs` (per ADR-001, S35 control manifest, polish boundary). GitNexus planning only on Datalink/DecisionLog paths.

Headless hot-path determinism + replay maintenance. No DOTS/ECS. Re-profile + harness polish within P0/P1 budgets.

## Stories

| # | Story | Type | Status | ADR | File |
|---|-------|------|--------|-----|------|
| 01 | Determinism audit P1 follow-up | Logic | Ready | ADR-001/004/005 | story-036-01-determinism-audit-p1.md |
| 05 | Replay golden + harness maintenance | Logic | Ready | ADR-003/004 | story-036-05-replay-golden-maintenance.md |
| 10 | DecisionLog immutable hash + P1 polish | Logic | Ready | ADR-003 | story-036-10-decisionlog-hash-polish.md |
| 15 | Datalink merger GitNexus planning + re-audit | Integration | Ready | ADR-001/004 | story-036-15-datalink-gitnexus-plan.md |
| 20 | Perf re-profile + replay-verify gate | Config | Ready | N/A (appendix) | story-036-20-perf-reprofile-verify.md |

**Governing ADRs (all stories):** ADR-001 (Sim/Delegation boundary — ZERO touch), ADR-004 (tick pipeline order), ADR-005 (sim core). ADR-003 for all log/fingerprint work.
**GDDs loaded:** simulation-core-time.md, sensor-detection-ew.md
**TRs:** TR-simcore-005, TR-sensor-002/004, TR-log-001/003
**Control Manifest rules (enforced):** Replay-verify required; hashes immutable; seeded deterministic paths only; ZERO DelegationBridge edits; GitNexus for critical paths (planning).

## References
- Boundary: `production/polish-scope-boundary-2026-06-19.md`
- S35 perf: `production/epics/sprint-35-sim-perf/EPIC.md` + stories + `production/agentic/sprint-35-perf-reprofile-2026-06-19.md`
- Replay golden: `production/determinism/replay-2026-06-*.md`, `tests/regression/replay-golden-*.txt`
- GitNexus: Planning only for `DecisionLog`, `DatalinkSidePictureMerger`, `BalticReplayHarness` (no edits)
- GDDs: `design/gdd/simulation-core-time.md`, `design/gdd/sensor-detection-ew.md`
- ADRs: ADR-001, ADR-004, ADR-005 (and ADR-003 for log)
- TRs: TR-simcore-005 (world hash), TR-sensor-002 (det loop), TR-log-001/003, TR-sensor-004
- Control: Replay-verify any implied change; hash immutable; ZERO Delegation; use seeded deterministic paths only.
- Skills: `/replay-verify`, determinism-audit, perf-profile

**Sprint gate:** All stories require `/replay-verify` PASS + 6/6 ReplayGolden + no hash drift before close. Stories cite "hash immutable" explicitly.
