# QA Plan — Sprint 110 Gauntlet Correctness (2026-08-09)

## Scope under test

| ID | Behavior | Evidence |
|----|----------|----------|
| S110-01 | t3-s1/s2/s3 have `gauntlet.tier == 3` | JSON load + optional python count script |
| S110-02 | `verify_axis` fails unproven non-config axes | unit tests + CLI smoke |
| S110-03 | Residual dual retest | retest-defect.sh exit 0 |

## Test cases

1. **TC-61-1** Load three policies; assert `gauntlet["tier"] == 3`.  
2. **TC-61-2** Count tiered gauntlet policies ≥ 42 (was 39).  
3. **TC-63-1** Existing `test_verify_stress_axes.py` still green.  
4. **TC-63-2** Production entry returns non-zero when weapons axis evidence incomplete.  
5. **TC-63-3** Logistics config-only does not fail the gate by design.  
6. **TC-RETEST** SYN-T12 + MD-001 dual retest PASS or documented skip reason.

## Out of scope

Full ladder live batch (unless needed for expect regen). C# / Unity Editor.

## Sign-off

QA lead marks smoke-sprint-110 APPROVED when TC-61-* and TC-63-* pass and residual retest documented.
