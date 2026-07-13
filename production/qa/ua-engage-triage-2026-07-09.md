# UA Engage Test Triage — BalticReplayHarnessPolicyEngageTests

**Date:** 2026-07-09  
**Sprint:** S89-02  
**Authority:** [`post-editor-hygiene-scope-boundary-2026-07-09.md`](../post-editor-hygiene-scope-boundary-2026-07-09.md), [`qa-plan-sprint-89-post-editor-hygiene-2026-07-09.md`](qa-plan-sprint-89-post-editor-hygiene-2026-07-09.md), [`implementation-tracker-2026-07-04.md`](../../Game-Requirements/implementation-tracker-2026-07-04.md) (req 14)

## Verdict

**DISPOSITION: GREEN — include in gate (no waive).**

All three tests in `BalticReplayHarnessPolicyEngageTests` pass at post–Platform Editor baseline. The historically documented 2 failures are **not present** on trunk @ `223a5fe` (2026-07-09).

## RUN+READ evidence

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~BalticReplayHarnessPolicyEngageTests" -v minimal
```

**Result (2026-07-09):** Passed! **3/3**, 0 failed, 511 ms.

| Test | Status |
|------|--------|
| `Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log` | **PASS** |
| `Restricted_engagement_scenario_fingerprint_is_deterministic` | **PASS** |
| *(third test in suite)* | **PASS** |

Prior gate logs: [`gates-post-editor-hygiene-2026-07-09.log`](evidence/gates-post-editor-hygiene-2026-07-09.log) — 3/3.

## Historical context

| Period | Status | Notes |
|--------|--------|-------|
| S81–S86 (2026-07-04) | 2 failures documented | Excluded from editor-program gate; owned by S86 triage |
| S86 closeout | 3/3 reported | `smoke-sprint-86-closeout-2026-07-04.md` |
| Post-PE (2026-07-09) | **3/3** | PE + hygiene gates confirm green |

**Root cause (historical):** Contract/representation drift on engagement log surface and fingerprint expectations — resolved by PE/editor program work without altering Baltic v2 replay hash.

## Gate policy (S89+)

| Gate | Rule |
|------|------|
| Full solution test | **≥1599 / 0f** — no UA exclusion |
| UA engage filter | **3/3 required** — failures block closeout |
| ReplayGolden | **6/6** unchanged |
| Hash | `17144800277401907079` preserved |
| DelegationBridge | **ZERO** hotpath edits |

**Do not** re-introduce "2 known UA failures excluded from gate" language in active agent docs or standing invariants.

## Tracker req 14

**Status:** **CLOSED @ green** — UA engage residuals no longer open. Implementation tracker updated 2026-07-09.

## If regressions reappear

1. GitNexus `impact BalticReplayHarness` (CRITICAL 54) before any harness edit.
2. TDD fix only — no hash change without ADR.
3. Waive path requires explicit user ack + documented exclusion (not default).

---
*S89-02 deliverable. Cite boundary + execute-plan + qa-plan-sprint-89.*
