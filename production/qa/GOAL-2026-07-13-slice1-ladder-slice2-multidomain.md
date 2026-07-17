# Implementation Goal: Gauntlet Slice 1 + Slice 2

**Date:** 2026-07-13  
**Plan:** `docs/superpowers/plans/2026-07-13-gauntlet-slice1-ladder-injects-slice2-multidomain.md`  
**Execution:** subagent-driven-development  
**Exit:** batch + oracle + CI package for both slices

## Goal statement

Deliver **two closed product goals** for Project Aegis QA gauntlet:

1. **Slice 1 — Ladder injects:** `gauntlet-t4-random-inject`, `gauntlet-t5-cascade`, `gauntlet-t5-roe-change` prove real mid-run injects (`CommsStateChange`; ROE mid-run only where ROE actually changes), with hard unit tests, batch oracle pass, and CI fixture inclusion.
2. **Slice 2 — Multidomain default (t3+):** Every `gauntlet-t3|t4|t5-*.policy.json` defaults to concurrent multi-domain engage via Visby→Sovremenny, Gripen→Steregushchiy, Gotland→Buyan (Buyan unit + detection retarget), with expanded tests, batch oracle pass, and CI samples (`joint-orbat-smoke`, `t3-emcon-phases`).

## Already green (do not re-architect)

| Layer | Status |
|-------|--------|
| Harness multi-agent blues + PreferredHostileByShooter | Done |
| Comms timeline → CommsStateChange | Done |
| Contact trigger → ApplyRoeToUnits | Done |
| Ladder inject policy `comms[]` on t4/t5 inject trio | Done (needs test hardening) |
| Full triple on joint-orbat-smoke, t3-emcon-phases, multidomain-shooters | Done |
| Weak LadderInject OR-assert tests | Present (upgrade) |
| LadderMultiDomain tests for joint + emcon only | Present (expand) |

## Residual work (this goal)

| ID | Work | Priority |
|----|------|----------|
| S1-T | Harden inject tests (require mid-run comms; cascade Denied; roe-change mid-run ROE) | P0 |
| S1-P | Remove hollow Free→Free ROE triggers on t4/cascade | P1 |
| S2-D | Propagate Buyan + Gotland→Buyan to all remaining t3–t5 policies | P0 |
| S2-T | Expand multi-domain tests + IsCatalogRed Buyan | P0 |
| V-B | Demo batch seeds 42,7,123 + recalibrate expects + oracle allPassed | P0 |
| V-C | Expand gauntlet-oracle.yml + local dry-run | P0 |
| V-N | QA note `gauntlet-ladder-injects-multidomain-2026-07-13.md` | P1 |

## Acceptance checklist

- [x] Slice 1 unit tests green (inject mid-run CommsStateChange)
- [x] Slice 2 joint-orbat-smoke + t3-emcon multi-domain True|Launched (goal bar; not every t3+)
- [x] `gauntlet_oracle_eval` allPassed with fingerprint gates on critical set
- [x] Criterion 3: strip inject / multi-domain tokens → oracle fails; harness negative tests
- [x] CI workflow stages inject trio + joint-orbat-smoke + t3-emcon-phases + strip fail-closed
- [x] Local CI dry-run allPassed + fail-closed strip
- [x] QA note written
- [ ] Changes committed (user-approved)

## Out of scope

ReplayGolden synthetic cleanup; mass CMO scrape; full 5-tier GHA matrix; oracle fingerprint schema v2.

## Orchestration

1. Task 1 inject tests (exclusive on inject trio JSON if ROE fix needed)
2. Task 2 Buyan roll-out on non-inject policies **in parallel** with Task 1; then inject trio Buyan merge
3. Task 3 batch/oracle (serial after 1–2)
4. Task 4 CI (serial after 3)
5. Task 5 suite + note (serial)
