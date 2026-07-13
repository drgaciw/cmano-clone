# QA Gauntlet effectiveness plan — game-playing QA

**Date:** 2026-07-13  
**Status:** Planning deliverable (not implementation)  
**Method:** Superpowers `brainstorming` + sequential-thinking MCP  
**Skill under review:** `.grok/skills/qa-gauntlet/SKILL.md`  
**Reference AAR:** `production/qa/gauntlet/gauntlet-20260710-1352/AAR.md`

## 1. Purpose

Make `/qa-gauntlet` a more effective **game-playing** QA process: scenarios should exercise the doctrines, ORBATs, and outcomes the tier ladder claims — not only prove batch stability and deterministic fingerprints.

## 2. User decisions (locked)

| Question | Answer |
|---|---|
| What “game playing” success means first | **Balanced hybrid (phased):** deeper oracles + smarter process + multi-unit catalog ORBAT |
| Phase 2 first code milestone | **Full joint ORBAT early** — surface + air + sub in one milestone (PR stack OK) |

## 3. Current-state gaps

| Gap | Evidence | Impact |
|---|---|---|
| Seed-centric engage | `BalticReplayHarness` registers `u1` / `hostile-1` (plus optional UCAV/near-future); AAR harness note | Tier 4–5 “joint theater” is **metadata**, not fight |
| Weak automated oracles 3–5 | Skill lists victory/ROE/EMCON; recent evals emphasize rows/fingerprint/sanity | Green ladder can hide wrong gameplay behavior |
| Scenario template reuse | `data/scenarios/gauntlet-t*.policy.json` lightly re-tagged | Escalation axes under-exercised |
| Aspirational multi-agent process | Skill names many specialists; runs often sequential/template | Shallow generation |
| Catalog ahead of harness | ~79 multi-domain platforms in `baltic_patrol.db` | Catalog growth ≠ play fidelity |
| Learning loop open | Hindsight optional; no forced re-run of prior defects | Repeat scenario-data mistakes |

**Keep:** preflight suite, seeds `42,7,123`, `GauntletRosterValidator` oracle-0, determinism recheck, Graphite QA branches, UCAV/`RegisterNearFutureUnits` multi-unit hooks.

**Honest boundary:** This document does **not** claim the harness already multi-domain-engages catalog IDs.

## 4. Recommended approach: Hybrid-C (phased)

1. **Phase 1 — Oracle & process (skill-first)** — “green” means behavior correctness on current harness.  
2. **Phase 2 — Full joint catalog ORBAT (code epic)** — surface + air + sub catalog `platform_id`s in engage path; PR stack under one milestone.  
3. **Phase 3 — Theater fidelity & learning** — injects, dynamic victory, cross-run memory after P2 units actually fight.

### Alternatives not chosen as sole path

- **A. Harness-only first** — highest fidelity, but ladder stays shallow until code lands.  
- **B. Skill/oracle-only forever** — fast ceiling; never true multi-domain play.  

## 5. Phased work items (actionable)

### Phase 1 — Oracles & process

| ID | Item | Outcome | Effort / risk | Depends |
|---|---|---|---|---|
| P1.1 | Structured `gauntlet.expect` schema | Machine-checkable fields (side, minKills, maxMissiles, denialsMin, scoreRange, …) | S / low | policy JSON |
| P1.2 | Oracle evaluator tool | Writes `oracle-eval.json`; fails tier gate on miss (oracles 1–7) | M / low | P1.1, results.csv |
| P1.3 | Skill hard gates | Ban stability-only green; require intent+expect; document skipped subagents | S / low | P1.2 |
| P1.4 | Matrix completeness checklist | Each tier dim exercised in ≥1 policy | S / low | P1.3 |
| P1.5 | Regression anchors | Re-run prior-tier scenario; score tolerance | S / med | P1.2 |
| P1.6 | CI roster validation | `GauntletRosterValidator` over gauntlet artifacts | S / low | existing validator |

**P1 success metrics:** ≥80% scenarios with machine expect fields; negative test fails broken ROE fixture; AAR shows oracle matrix.

### Phase 2 — Joint catalog ORBAT early

| ID | Item | Outcome | Effort / risk | Depends |
|---|---|---|---|---|
| P2.0 | GitNexus impact spike | Blast on `BalticReplayHarness.Run`, goldens | S | — |
| P2.1 | Roster → unit plan adapter | `gauntlet.units[]` / roster → registerable plans | M / med | catalog fittings |
| P2.2 | Surface catalog engage | Real surface IDs fight with catalog envelopes | M / **HIGH** | P2.0–2.1 |
| P2.3 | Air domain engage | Air units in detection/engage | M / **HIGH** | P2.2 |
| P2.4 | Subsurface domain engage | Subs in detection/ASW path | M / **HIGH** | P2.2–2.3 |
| P2.5 | Gauntlet integration | Tier ≥3 joint mix; fingerprints show catalog unit ids | M / med | P2.2–2.4 |
| P2.6 | TDD + ReplayGolden | Multi-domain tests; goldens green or rebaselined with note | M / **HIGH** | all P2 |

**PR stack:** (1) adapter + surface opt-in → (2) air → (3) sub + policies → (4) default-on tier ≥3.

**P2 success metrics:** one run with ≥1 surface + ≥1 air + ≥1 sub **catalog** platform_id in fight; magazine max_range > 0; AAR harness note no longer “seed-only default.”

**Reuse:** `RegisterNearFutureUnits`, UCAV multi-unit paths in `BalticReplayHarness.cs`.

### Phase 3 — Theater & learning

| ID | Item | Outcome | Effort / risk |
|---|---|---|---|
| P3.1 | Mid-run ROE/EMCON injects oracle-checked | M / med |
| P3.2 | Weighted/dynamic victory wired to outcomes | M / med |
| P3.3 | Cross-run defect memory + forced re-run | S / low |
| P3.4 | Optional adversarial scenario pass | S / low |

## 6. Before → after process

| Step | Before | After |
|---|---|---|
| Generate | Templates + roster metadata | Matrix-complete + machine oracles + unit plan |
| Validate | oracle-0 IDs | oracle-0 + expect schema + (post-P2) units ⊆ roster |
| Execute | `u1`/`hostile-1` engage | catalog multi-domain units for tier ≥3 |
| Evaluate | rows / fingerprint / coarse score | oracles 1–7 fail-closed |
| Learn | AAR prose | AAR + evaluator JSON + defect queue |

## 7. Critical files

- Skill: `.grok/skills/qa-gauntlet/SKILL.md`  
- Harness: `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs`, `BalticBatchRunner.cs`  
- CLI: `src/ProjectAegis.Delegation.Demo/Program.cs`  
- Validator: `src/ProjectAegis.Data/Catalog/GauntletRosterValidator.cs`  
- Catalog: `assets/data/catalog/baltic_patrol.db`  
- Policies: `data/scenarios/gauntlet-*.policy.json`  
- Artifacts: `production/qa/gauntlet/<RUN_ID>/`

## 8. Implementation order (next goal)

1. P1.1–P1.3 (schema + evaluator + skill gates)  
2. P2.0 impact spike  
3. P2.1–P2.5 joint ORBAT PR stack  
4. Align skill tier claims with harness  
5. Focused gauntlet acceptance run  
6. Phase 3 when P2 AAR proves catalog units fight  

## 9. Out of scope for *this* planning goal

- Implementing P1–P3 code or full skill rewrite  
- Full 5-tier gauntlet as primary proof  
- Catalog harvest waves  
- Unity interactive playtests  

## 10. Risks

- Joint ORBAT early stresses ReplayGolden seed assumptions → opt-in + PR stack, not big-bang default.  
- Skill-only (P1) **cannot** close multi-domain play — do not market P1 as joint ORBAT done.  
- Incomplete domain physics → detection-only registration with honest AAR, no invented combat.

## 11. Open decisions for implementers

- Exact `gauntlet.expect` field names  
- Catalog unit ids replace vs alias `u1`  
- Golden rebaseline policy when fingerprints legitimately change  

## 12. Method evidence

- Brainstorming: problem framing, 2 clarifying questions, hybrid recommendation  
- Sequential-thinking: 5-step chain (gaps → options → user lock → harness reuse → deliverable boundary)  
- Scratch: `/tmp/grok-goal-a6716c010f5d/implementer/brainstorm-notes.md`, `sequential-thinking-notes.md`, `process-delta.md`
