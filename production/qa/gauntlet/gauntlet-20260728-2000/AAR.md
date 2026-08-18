# QA Gauntlet — After Action Review

**Run:** `gauntlet-20260728-2000`
**Date:** 2026-07-28
**Base SHA:** `5605a972` (branch `feat/platform-editor-uiux-productization`)
**Config:** 5 tiers · 4 scenarios/tier · seeds `42,7,123` · max-fix-attempts 3

---

## Verdict

**Ladder PASS (5/5 tiers + extra) — with one high-severity coverage defect filed.**

All 22 scenarios × 3 seeds executed clean, deterministic, and within their oracle bounds.
No sim-code defects surfaced. **No code was changed and no commits were made.**

The one defect found is not a crash or a wrong number — it is a **silent hole in the ladder
itself**: the EMCON dimension never reaches the engine, so one of the six escalating dimensions
has been passing vacuously.

---

## Preflight

| Gate | Result |
|---|---|
| Build | PASS — 0 warnings, 0 errors |
| Baseline suite | PASS — **1912 tests, 0 failures** (monotonic floor) |
| Replay determinism (`ReplayGoldenTests`) | PASS — 4/4 |
| PlayMode smoke (`PlayModeSmokeHarnessTests`) | PASS — 38/38 |
| Catalog (`baltic_patrol.db`) | PASS — opens, 30 tables incl. `platform_emcon` |
| GitNexus index | Re-indexed (was 9 commits behind) — 29,316 nodes / 55,528 edges |

> **Environment note:** `dotnet` is **not on PATH** in this shell; it resolves only at
> `~/.dotnet/dotnet`. Every command in this run exported it explicitly. Worth fixing in the
> shell profile or the gauntlet driver, or the skill's documented commands fail verbatim.

---

## Ladder results

| Tier | Ticks | Scenarios | Oracle | Determinism | Regression vs prior run |
|---|---|---|---|---|---|
| 1 | 6  | 4 | PASS | PASS | 12/12 identical |
| 2 | 10 | 4 | PASS | PASS | 12/12 identical |
| 3 | 16 | 4 | PASS | PASS | 12/12 identical |
| 4 | 24 | 4 | PASS | PASS | 12/12 identical |
| 5 | 40 | 4 | PASS | PASS | 12/12 identical |
| extra | 12 | 2 | PASS | PASS | — |

**Tier 5 recovered.** The prior run (`gauntlet-20260720-2000`) ended `ORACLE_FAIL` on tier 5.
The CSVs are byte-identical between the two runs, which proves the recovery came from
**recalibrated `expect` bounds** (`gauntlet-t5-roe-change.policy.json`, modified 2026-07-28),
not from any change in simulation behaviour.

### Oracle matrix

| # | Oracle | Result |
|---|---|---|
| 1 | Stability | PASS — 0 exceptions across 12 run logs |
| 2 | Determinism | PASS — repeat run byte-identical, all 6 tiers |
| 3 | Victory conditions | PASS — `allPassed: true` every tier |
| 4 | ROE compliance | PASS — denials/missiles within `expect` |
| 5 | **EMCON plausibility** | **FAIL — vacuous** (see defect below) |
| 6 | Regression | PASS — **60/60** pairs identical vs prior run |
| 7 | Sanity | PASS — no empty fingerprints, no non-finite scores |

### Supplementary

- **Seed sensitivity:** 22/22 scenarios yield 3 distinct fingerprints across 3 seeds — seeds are
  genuinely threaded through, not ignored.
- **Joint ORBAT:** every tier ≥3 scenario emits `CATALOG_UNIT:{platformId}:{domain}` tokens for
  **air + subsurface + surface**. Multi-domain claims are backed by fingerprint evidence.

---

## Defect

### `BUG-gauntlet-emcon-dimension-not-exercised` — high, `scenario-data`, OPEN

The ladder's EMCON dimension (T2 passive-only → T5 contested EM) **never reaches the engine**.

- `ScenarioGauntletJsonDto` declares only `Intent`, `Oracle`, `CatalogRefs`, `Units`
  (`ScenarioPolicyJsonDto.cs:65-75`). The three EMCON scenarios put their posture at
  `gauntlet.emcon` as a bare string (`"passive-blue-standin"`, `"phased"`, `"contested"`),
  which the deserializer silently discards.
- A real binding exists one level up — top-level `emcon` (`ScenarioPolicyJsonDto.cs:23`) —
  and **0 of 24** gauntlet scenarios use it.
- **0** `EMCON_OFF` tokens across 22 scenarios × 3 seeds × 2 runs.
- Positive control `baltic-patrol-emcon-off` → **10** `EMCON_OFF` tokens. The path works.
- Retrofit probe on `gauntlet-t2-escort-passive` with a real block: score **50 → −50**,
  kills **1 → 0**, missiles **4 → 0**. The block materially changes behaviour.

So oracle 5 has been unfalsifiable across tiers 2–5 — a tier can go green while the dimension it
claims to test is inert.

**Secondary finding (engine observability).** The retrofit probe changed behaviour decisively yet
*still* emitted zero `EMCON_OFF`. Two EMCON gates exist and only one is observable: the engage-side
gate logs `EMCON_OFF`, but the sensor-side gate (`DeterministicDetectionLoop.RollTick`,
`ScenarioContactSimulator.Tick`) silently `continue`s. When the passive unit is the *observer*, it
never gains a contact, never reaches the engage path, and leaves no trace.
**Any future EMCON oracle must assert on a control-sibling metric delta, not on `EMCON_OFF`** —
unless the passive unit is itself the shooter.

### Why it was filed and not fixed

Three reasons, in order of weight:

1. **An approved plan already owns it.** `docs/superpowers/plans/2026-07-27-gauntlet-variability.md`
   schedules exactly this retrofit across all three scenarios plus 10 new ones, and is **not landed
   on this branch**. An ad-hoc fix here would collide with it.
2. **It would destroy the regression baseline.** Rewriting shipped scenarios and their `expect`
   bounds breaks score-drift comparability — currently the ladder's strongest signal (0 drift
   across 60 pairs).
3. **Tiers 3–5 need engine work, not data work.** `ScenarioUnitEmconJsonDto` exposes a single
   static `Radar` string — no phasing, no triggers, no deception emitters. "Timed phases",
   "dynamic change on detection", and "contested EM" are **not representable** by the current
   engine, so the `scenario-data` regeneration path cannot close them.

This is the skill's documented "drop it and log why" branch, not a silent skip.

---

## Recommended follow-ups

1. **Land the variability plan** — closes the three EMCON retrofits at the root, regenerating each
   `expect` from a real batch CSV per `tools/qa-gauntlet/README-expect-regen.md`.
2. **Add a structural guard so this cannot recur** — fail `Invoke-ScenarioValidate` on unknown keys
   under `gauntlet.*`. A typo'd or invented QA field currently vanishes without warning; that is
   the root cause, and it will bite another dimension next.
3. **Reconcile the ladder matrix with engine capability** — either extend `ScenarioUnitEmconJsonDto`
   (phase/trigger/deception) or downgrade the T3–T5 EMCON wording. The matrix should not claim
   behaviour the engine cannot express.
4. **Fix the `dotnet` PATH assumption** in the skill or shell profile.
5. **Consider a vacuity check as a standing gate** — every ladder dimension should have to prove it
   produced an observable effect. EMCON passed for months precisely because nothing asserted it did
   anything.

---

## Sign-off

| Item | Value |
|---|---|
| Baseline suite | 1912 / 0 failures |
| Final suite | 1912 / 0 failures (no code changed → no re-run needed) |
| Test-count delta | 0 (monotonic floor held) |
| Replay golden | PASS 4/4 |
| Regression anchors | PASS 60/60 |
| Scenarios run | 22 × 3 seeds × 2 runs = **132 executions** |
| Defects found / fixed / quarantined | **1 / 0 / 0** |
| `QUARANTINED-CRITICAL` | **none** |
| Code changes | **none** (`detect_changes`: 0 C# symbols, 0 affected processes, risk low) |
| Commits | **none** — no fixes to commit; QA branch intentionally not created |
| Graphite submit | **not performed** (skill: submit only if fixes were committed) |

**Working-tree note.** This run made two incidental edits outside the artifact directory.
The mandated preflight re-index rewrote the GitNexus banner in `CLAUDE.md` and `AGENTS.md`:
it refreshed the symbol counts (accurate — kept) **and deleted the dual-checkout disambiguation
warning (inaccurate — restored)**. `list_repos` confirms both `cmano-clone` checkouts are still
indexed, so that warning is still required or future GitNexus calls fail with
*"Multiple repositories indexed"*. Net remaining diff in those two files is the count refresh only.
The user's 7 pre-existing WIP files were not touched, and `data/scenarios/` is unmodified.
