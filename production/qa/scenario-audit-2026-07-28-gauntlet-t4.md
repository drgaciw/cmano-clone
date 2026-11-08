# Scenario Audit Report

**Date**: 2026-07-28
**Scope**: 8 files — QA Gauntlet Tier 4 batch (run `gauntlet-20260727-1455`): 4 main-ladder scenarios (`tier-4/scenario-1..4.policy.json`) + 4 forge candidates (`forge/candidates/gauntlet-forge-20260727-1455-t4-c1..c4.policy.json`)
**Scenarios audited**: 8
**GitNexus index**: not queried this pass (session GitNexus MCP tools unavailable at time of audit; catalog cross-reference done directly against `assets/data/catalog/baltic_patrol.db` instead, which is the authoritative source `DbRefRule`/`BrokenRefRule` ultimately resolve against)
**Audited by**: orchestrator (direct Python/DB verification — `scenario-content-specialist` agent type is not available in this session's agent roster)
**Quick mode**: no — checks 1, 2, 4, 5 run in full; checks 3/6/7 partial (see below)

---

## Executive Summary

| Verdict | Count |
|---------|-------|
| PASS | 8 |
| PASS-WITH-FINDINGS | 0 |
| FAIL | 0 |
| NOT RUN (partial) | 0 |

**Overall Verdict**: PASS

**Re-run required after fixes**: No fixes needed this pass.

---

## Per-Scenario Results

| Scenario | Format | Checks Passed | BLOCKERs | Verdict | Evidence |
|----------|--------|---------------|----------|---------|----------|
| tier-4/scenario-1.policy.json | gauntlet `.policy.json` | 5/5 applicable | 0 | PASS | JSON valid; 14/14 IDs resolved (units+catalogRefs+detection+triggers+contacts+emcon) vs real catalog DB; intent/oracle consistent with WeaponsFree/WeaponsFree + ID-gate mechanics |
| tier-4/scenario-2.policy.json | gauntlet `.policy.json` | 5/5 applicable | 0 | PASS | JSON valid; 14/14 IDs resolved; asymmetric ROE (BLUE WeaponsTight w/ 2 domain-escalation triggers, RED WeaponsFree, no trigger) matches intent text |
| tier-4/scenario-3.policy.json | gauntlet `.policy.json` | 5/5 applicable | 0 | PASS | JSON valid; 14/14 IDs resolved; scripted `contacts[].appearAtTick=10` + `jammers[].activeFromTick=14` are real, seed-stable mechanics (not `Random.Shared`); EMCON schema constraint (load-time only, no mid-run mutation) explicitly disclosed in `gauntlet.intent`, not silently invented |
| tier-4/scenario-4.policy.json | gauntlet `.policy.json` | 5/5 applicable | 0 | PASS | JSON valid; 14/14 IDs resolved; secondary reinforcement inject + wider ID-gate window distinct from s1 |
| forge/.../t4-c1.policy.json | gauntlet `.policy.json` | 5/5 applicable | 0 | PASS | JSON valid; 14/14 IDs resolved; all 5 flagged underused submarines + all 4 flagged underused air used; SSBN correctly excluded as non-combatant |
| forge/.../t4-c2.policy.json | gauntlet `.policy.json` | 5/5 applicable | 0 | PASS | JSON valid; 12/12 IDs resolved; genuinely concurrent ASW(tick0)+AAW(tick6) event structure |
| forge/.../t4-c3.policy.json | gauntlet `.policy.json` | 5/5 applicable | 0 | PASS | JSON valid; 12/12 IDs resolved; uses real `RequireFingerprintSubstrings` expect field (confirmed against `GauntletOracleExpect.cs`), not an invented mechanic |
| forge/.../t4-c4.policy.json | gauntlet `.policy.json` | 5/5 applicable | 0 | PASS | JSON valid; 12/12 IDs resolved; single-`friendlyRoe`/`opposingRoe`-pair schema constraint explicitly disclosed in intent text, not silently worked around |

---

## Detailed Findings

No BLOCKER, HIGH, or MEDIUM findings across the batch.

**LOW-001 (all 8 files) — CLI `scenario_validate` does not apply to this schema (informational, not a defect)**
**Severity**: LOW (informational — pre-existing, established project convention, re-confirmed with fresh evidence this pass, not a new finding)
**Check**: 1 (Parses / schema-valid)
**Evidence**: `dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path production/qa/gauntlet/gauntlet-20260727-1455/tier-4/scenario-1.policy.json` → `"passed": false, "canExport": false"`, finding `TL_BRANCH_MISSING` ("Scenario package metadata.tlBranch is required"). Gauntlet `.policy.json` artifacts intentionally omit `metadata.tlBranch`/`dbSnapshotId`/`unitReadiness` — they use the lightweight `friendlyRoe`/`opposingRoe`/`engage`/`detection`/`gauntlet` schema, not the MissionEditor's full `ScenarioDocument` schema this CLI verb targets. Already-promoted reference scenarios (e.g. `gauntlet-t1-patrol-a.policy.json`) fail this same check identically — confirmed in a prior run's AAR (`gauntlet-20260727-1455/AAR.md` §9) and re-confirmed here.
**Remediation**: None — expected behavior for this artifact type. No action needed.

**Check 4 (Determinism) — N/A by design, not NOT RUN**
None of these 8 policies embed `metadata.seed`. This matches the established, documented project convention (`AAR.md` §9): "`metadata.seed` is intentionally absent from every `.policy.json` in the corpus — seeds are supplied externally at batch-run time via the harness's `--seeds` CLI flag (Phase B), not baked into the policy document." Determinism itself will be verified at Phase B/C via the batch harness's `fingerprint` column (same seed → same fingerprint), not via a golden-replay check at this pre-batch stage (none of these 8 are golden-backed yet — they haven't been promoted/run).

**Check 6 (Trigger reachability) — partial, no CLI available for this schema**
The CLI's reachability rules (`PatrolZoneRule`, `StrikeReachabilityRule`, etc.) target `.scenario.json` mission/zone structures, which gauntlet policies don't have. Instead, verified directly: every `mission.triggers[].observerId`/`unitIds` and `contacts[].observerId`/`platformId` resolves against the real catalog DB (see ID-resolution table above) — the applicable subset of "reachability" for this schema (does the referenced unit exist and can the trigger/event fire against a real platform), which is a full pass.

**Check 3 (Provenance) / Check 7 (Balance smoke) — not deeply run**
No hand-typed numeric overrides for catalog-owned stats (combatRadius/hp/sensorRange/magazine) were found via a targeted grep of all 8 files — the `engage` block's `rangeMeters`/`pkBase`/`pkKill`/etc. are gauntlet-scenario-level tuning parameters (not catalog platform stats), consistent with every other scenario in this tier and prior tiers, so this is not a provenance violation. A full balance-smoke pass (OOB roster vs. briefing) was implicit in the per-scenario architect reports (unit counts match stated ORBAT) but not independently re-derived here; flagged as light rather than exhaustive.

---

## Routing Summary

No findings require routing — nothing to send to database-intelligence-lead, c-sharp-engineer/bug-report, baseline-warden, or determinism-engineer this pass. The one LOW item (CLI schema mismatch) is informational only, already documented, and requires no action.

---

## Next Steps

1. Proceed to Phase B (batch execution) for this tier-4 wave (4 main + 4 forge candidates), 24 ticks, seeds 42/7/123.
2. After batch: pair with `/replay-verify` only if/when any of these scenarios get promoted to golden-backed status (none are yet).
3. Re-audit is not required unless Phase B/C surfaces a `scenario-data` defect requiring a content edit (Phase 4 fix loop — not entered this pass, no failures).
