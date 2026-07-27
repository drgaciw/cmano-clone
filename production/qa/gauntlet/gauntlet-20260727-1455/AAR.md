# After-Action Report — QA Gauntlet `gauntlet-20260727-1455`

**Run**: `gauntlet-20260727-1455` | **Started**: 2026-07-27T14:55Z | **Trunk base**: `fa4db95c` (main) | **Branch**: `07-27-qa_gauntlet_gauntlet-20260727-1455`
> **SUPERSEDED IN PART — 2026-07-27, later same day.** Both defects below were
> subsequently **FIXED** in a follow-up session under explicit human authorization
> to override the CRITICAL autonomy rail. See §10 "Post-AAR Remediation" at the
> end of this document. The quarantine narrative below is preserved as the
> accurate record of the original run.

**Outcome**: **HALTED after Tier 1 by explicit human decision.** Tiers 2-5 were never run. Tier 1 surfaced two long-standing, compounding engine defects in combat resolution and scoring that would recur identically at every subsequent tier (they are not tier-specific) — continuing the ladder without new information would only burn budget re-discovering the same root cause at higher platform counts and mission complexity.

---

## 1. Executive Summary

Preflight (Phase 0) passed cleanly: full solution baseline 1924/1924, replay-verify 17/17, smoke 21/21, catalog gate green (schema `005`, 79 platform rows). Tier 1 (4 main 3v3 patrol scenarios + 4 forge candidates, 3 seeds each, 6-tick budget) executed and audited clean at the scenario-data level (8/8 artifacts PASS), but **all 4 main scenarios and all 4 forge candidates failed `gauntlet_oracle_eval`** — every seed-42 run scored above `maxScore` (e.g. 400 > 280 for `t1-s1`). Root-cause investigation (c-sharp-architect) traced the failure to two independent, compounding engine bugs, not a scenario or oracle miscalibration:

1. **`MvpEngagementResolver.Resolve`** only checks target liveness, not shooter liveness — a killed unit keeps firing in subsequent ticks.
2. **`LossesScoringProjection.Project`** counts every `Kill`-coded outcome in the shared `DecisionLog` with no side filter — enemy kills against your own units get credited to your own kill tally.

Both defects reach **CRITICAL** GitNexus impact on their real fix surfaces (42 and 381 impacted symbols respectively), and Bug 1's blast radius reaches the live Unity runtime bridge (`DelegationBridgeHost.RunTick`). Per this skill's autonomy boundary and CLAUDE.md's "never ignore HIGH/CRITICAL impact" rule, **neither was fixed in this run** — both are quarantined for human-supervised remediation. The human operator halted the run after Tier 1 rather than proceeding to Tiers 2-5, since higher tiers only add platforms/events/ROE complexity on top of the same broken engagement-resolution and scoring code paths — there is no new information a Tier 2-5 run would surface about these two defects.

A secondary, corpus-wide finding: several of the 24 already-promoted gauntlet scenarios have `gauntlet.expect.minKills` values (4-5) that only make sense if they were calibrated around this same overcounting/miscrediting behavior, not against correct kill semantics. This is flagged as a corpus-wide recalibration item, not just a Tier 1 issue (§4).

---

## 2. Ladder Results Table

| Tier | Mission / Platform Mix | Status | Result |
|---|---|---|---|
| **1** | Single patrol, 3 surface/side (~6), survive 6 ticks, weapons-free both sides, unrestricted EMCON | **QUARANTINED** | 4/4 main scenarios + 4/4 forge candidates FAIL `gauntlet_oracle_eval` (score > maxScore, every seed-42 run). Root cause: 2 engine defects (below), not scenario/oracle error. Scenario-data audit itself was clean (8/8 PASS). |
| 2 | Strike/escort, 3 surface + 1 air/side (~8) | **not run — human halted after tier 1** | — |
| 3 | Escort+strike combined, ~12/side | **not run — human halted after tier 1** | — |
| 4 | ASW/AAW multi-mission, ~14/side | **not run — human halted after tier 1** | — |
| 5 | Multi-domain theater op, ~16/side | **not run — human halted after tier 1** | — |

**Halt rationale**: both Tier 1 defects live in shared engagement-resolution (`MvpEngagementResolver`) and scoring (`LossesScoringProjection`) code that every tier's scenarios route through identically. Tiers 2-5 add mission/platform/ROE/EMCON complexity on top of that same broken substrate — they would not reveal anything new about these two defects, only reproduce them at higher cost. Escalating the ladder before a human-supervised fix would waste run budget without new signal.

---

## 3. Defects

### Defect class counts

| Class | Count |
|-------|-------|
| `sim-code` fixed | 0 |
| `oracle` recalibrated | 0 |
| `scenario-data` | 0 |
| `flaky` | 0 |
| quarantined | 2 |

### BUG-engagement-resolver-shooter-liveness (QUARANTINED-CRITICAL)

- **Root cause**: `MvpEngagementResolver.Resolve` (`src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs:59`) validates only the *target's* liveness via `_killedTargets.IsKilled(request.TargetId)` — it never checks `request.ShooterUnitId`. A unit killed at tick N still fires new engagements at tick N+1. Reproduced deterministically in `gauntlet-20260727-1455-t1-s1` seed 42: tick 1, RED `421-orkan-pr-660-2015` kills BLUE `f-341-absalon-2020`; tick 2, the now-dead `f-341-absalon-2020` launches a new engagement against `mrk-buyan-mod-pr-21631-buyan-m-2014`.
- **GitNexus impact** (`impact({target: "Resolve", file_path: ".../MvpEngagementResolver.cs", direction: "upstream"})`): **risk CRITICAL**, impactedCount **42** (direct 37). Affected processes: `RunExecutingTick` (`src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs`, 6 hits) and `RunTick` (`unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs` — the **live Unity production runtime bridge**, not just QA tooling, 2 processes / 4 hits, earliest_broken_step 3). GitNexus flags `epistemic: lower-bound` — `Resolve` is an interface with 3 implementations, and dynamic-dispatch callers aren't fully traced, so actual impact may exceed 42.
- **Fix / quarantine reason**: **Quarantined, not fixed.** CRITICAL impact + reach into the live Unity runtime bridge + 3 untested interface implementations makes this unsafe for unsupervised remediation under this skill's explicit autonomy boundary ("if GitNexus impact returns CRITICAL on a symbol a fix must touch, do NOT edit it") and CLAUDE.md's "never ignore HIGH/CRITICAL impact-analysis risk."
- **Regression test written (RED, not yet enabled)**: `src/ProjectAegis.Sim.Tests/Engage/MvpEngagementResolverTests.cs`, `Killed_shooter_aborts_before_launch`, marked `Skip` pending human-supervised fix.
- **Bug report**: `production/qa/bugs/BUG-engagement-resolver-shooter-liveness.md`

### BUG-losses-scoring-side-unaware (QUARANTINED-CRITICAL)

- **Root cause**: `LossesScoringProjection.Project` (`src/ProjectAegis.Delegation/Projection/LossesScoringProjection.cs:12`) — `log.EngagementOutcomes.Count(o => o.OutcomeCode == EngagementOutcomeCodes.Kill)` has no side/faction filter, confirmed by grep (no `side` reference anywhere in the file). Every `Kill`-coded outcome in the shared `DecisionLog` is counted toward the side being scored, regardless of who fired it. Reproduced in `gauntlet-20260727-1455-t1-s1` seed 42: of 4 `Kill`-coded outcomes, 3 are BLUE-fired and 1 is RED-fired (against BLUE's own `f-341-absalon-2020`) — yet BLUE's reported `kills` is 4, not 3.
- **Compounds with** BUG-engagement-resolver-shooter-liveness: some of the "extra" kill events being miscredited are themselves only possible because dead shooters were allowed to keep firing.
- **GitNexus impact** (`impact({target: "EngagementOutcomeRecord", direction: "upstream", kind: "Record"})` on the constructor candidate needing a new `side` parameter): **risk CRITICAL**, impactedCount **381** — larger than the companion defect, because `EngagementOutcomeRecord` is a shared record type consumed/constructed across a wide surface.
- **Fix / quarantine reason**: **Quarantined, not fixed.** 381-symbol CRITICAL blast radius on the natural fix surface, same autonomy boundary as above. Recommended approach not implemented: thread a `side` field through `DecisionLog`/`EngagementOutcomeRecord` (sourced from `BalticV3SideRegistry`), filtering the count — but a narrower side-lookup-by-unit-id approach should be evaluated first to avoid the full 381-symbol ripple of a constructor-parameter addition (see §7).
- **Regression test**: none written this run (`regression_test: null` in manifest) — recommend adding one alongside the fix.
- **Bug report**: `production/qa/bugs/BUG-losses-scoring-side-unaware.md`

---

## 4. Determinism Findings

**PASS.** `replay-verify` (Phase 0 preflight, `production/determinism/replay-2026-07-27.md`): 17/17 `ReplayGolden` fixtures (Baltic checkpoint/engage/intercept/kill/magazine/comms/classify/salvo + regression catalog) matched on both the full-suite baseline run and an isolated filtered re-run, 0 divergence. No first-divergent-tick findings. Note: this environment run does not itself prove cross-platform (Windows/Linux) float parity — out of scope for this preflight gate.

Tier 1 batch execution itself showed no run-to-run nondeterminism within a seed — the two engine defects above are **deterministic and reproducible**, not intermittent: every scenario/seed combination that has at least one kill exhibits both behaviors identically (see `results.csv` — score/kills are stable per seed across all 4 main scenarios: seed 42 → score 400/kills 4 in every one of `t1-s1..s4`; seed 7 → 200/2; seed 123 → 100 or 200/1 or 2).

---

## 5. Balance / Score Trends

Tier 1 alone shows no balance drift (only one tier ran), but the root-cause investigation surfaced a **corpus-wide implication**: several already-promoted scenarios in `production/qa/gauntlet/corpus/index.yaml` / `data/scenarios/` carry `gauntlet.expect.minKills` values of 4-5, which only make sense if they were tuned against the same overcounting/miscrediting bugs rather than correct kill semantics:

- `gauntlet-t3-emcon-phases.policy.json` — `minKills: 5`
- `gauntlet-joint-orbat-smoke.policy.json` — `minKills: 5`
- `gauntlet-t5-cascade.policy.json` — `minKills: 4`
- `gauntlet-t4-random-inject.policy.json` — `minKills: 4`

Both bug reports independently flag this same pattern ("the corpus appears to have been tuned around the bug, not against correct kill semantics"). This is not a Tier 1-only finding — it implicates the calibration of the entire 24-scenario promoted corpus and should be re-audited once the two defects are fixed (see §7).

Within Tier 1 itself: all 4 main scenarios' seed-42 runs scored exactly 400 (4 kills, 6 missiles fired) against `maxScore` envelopes of 260-320 — a consistent ~25-54% overshoot depending on scenario, consistent with a systemic (not scenario-specific) cause. Forge candidates c1-c4 reproduced the identical pattern (400/4/6 at seed 42), confirming the defect is roster/platform-mix-independent within Tier 1's dimensions.

---

## 6. Flaky-Test Notes

None observed this run. Both defects are deterministic (100% reproduction rate across all seeds where at least one kill occurs), not intermittent. No test in the 1924-test baseline suite or the 17/17 replay-golden suite showed run-to-run variance.

---

## 7. Forge Promote Summary

**0 promotes, 4 discards, no recipe down-weighting.**

- **Phase `pre`**: Hindsight bank `qa-gauntlet-forge` unreachable (`curl http://localhost:8888` → HTTP 000) — proceeded on-disk-only per forge contract. Corpus snapshot: 20 coverage-map cells / 24 promoted scenarios, 17 recipes, empty hard-case pool. Only `platform-swap-underused` (weight 1.2) was tier-1-eligible; plan written to `forge/mid-tier-plan.yaml`.
- **Phase `post-oracle`**: all 4 candidates (`gauntlet-forge-20260727-1455-t1-c1..c4`) batch-executed alongside the 4 main scenarios and hit the identical hard-gate failure (`gauntlet_oracle_eval` score > maxScore), same two root causes.
- **Disposition**: all 4 discarded with reason `engine-defect-blocked` (not `recipe-quality`) — forge correctly attributed the failure to the engine, not the recipe. `platform-swap-underused` was **not** down-weighted, since penalizing the only viable tier-1 recipe for an engine-level failure would be a false signal.
- **Hard-case signatures captured** (on-disk only, Hindsight retain skipped — server still unreachable at `final`): `dead-shooter-fires-next-tick` (BUG-engagement-resolver-shooter-liveness), `enemy-kill-credited-to-own-side` (BUG-losses-scoring-side-unaware). Not yet mirrored into the committed `corpus/hard-cases/` pool — forge recommends the human do so once the defects are fixed and a regression scenario can be promoted.
- **Corpus commits**: none — no promotes, no weight deltas, nothing under `production/qa/gauntlet/corpus/` changed this run.

---

## 8. Recommended Follow-Ups

1. **Human-supervised fix — BUG-engagement-resolver-shooter-liveness**: review **all 3 implementations** of the `Resolve` interface individually (GitNexus flagged `epistemic: lower-bound` — dynamic dispatch means the traced 42-symbol impact may undercount) before adding the shooter-liveness check. Confirm no hidden dependency on the current (buggy) behavior in the live Unity runtime bridge (`DelegationBridgeHost.RunTick`).
2. **Human-supervised fix — BUG-losses-scoring-side-unaware**: before touching `EngagementOutcomeRecord`'s constructor (381-symbol CRITICAL blast radius), evaluate whether a **narrower side-lookup-by-unit-id approach** (e.g., keyed lookup against `BalticV3SideRegistry` at scoring time) can filter `LossesScoringProjection.Project` without adding a new field to the shared record type. Only fall back to threading a `side` field through the record if the narrower approach proves infeasible.
3. **Fix both defects together, then re-verify holistically**: re-run the full `dotnet test ProjectAegis.sln` suite, `replay-verify`, and the entire 24-scenario promoted corpus (not just Tier 1) after both fixes land — correct scoring depends on correct engagement/kill semantics first.
4. **Corpus-wide `gauntlet.expect` recalibration**: run the expect-regen runbook (`tools/qa-gauntlet/README-expect-regen.md`) across all 24 promoted scenarios once both defects are fixed, since kill/score values will shift systemically, not just for the Tier 1 scenarios that surfaced this.
5. **Audit the existing corpus for bug-calibrated envelopes**: specifically re-examine `gauntlet-t3-emcon-phases`, `gauntlet-joint-orbat-smoke`, `gauntlet-t5-cascade`, and `gauntlet-t4-random-inject` (`minKills` 4-5, §4) — determine whether their expect envelopes were unknowingly tuned around the overcounting/miscrediting behavior and need re-baselining rather than just re-running.
6. **Tooling gap — batch harness scenario discovery**: `Delegation.Demo --batch` only auto-discovers scenarios from a hardcoded `data/scenarios/` directory with no override flag. This forced the orchestrator to place scenario copies there directly (main scenarios permanently, forge candidates temporarily, removed after scoring) instead of pointing the harness at the run's own `tier-N/` artifact directory. Recommend adding a `--scenario-dir` (or similar) override flag before the next gauntlet run, to avoid this friction recurring at every tier.
7. **Resume ladder after fix**: once both defects are fixed and corpus expect envelopes are recalibrated, resume this run via `--resume gauntlet-20260727-1455` starting at Tier 2, or start a fresh run — Tier 1's scenario-data and roster artifacts remain valid and reusable (only the oracle/engine layer needs re-verification).

---

## 9. Infrastructure Notes (not defects)

- **GitNexus index required a full clean+rebuild during Phase 0.** Incremental `analyze` hit an FTS index corruption error ("document for node offset 8295 is missing during delete"). Recovered via full rebuild (29,338 nodes / 55,559 edges / 516 clusters / 300 flows). This added time to Phase 0 but did not affect the validity of any impact/context query in this run. Note also: two checkouts are registered in the GitNexus global registry — every `impact`/`detect_changes`/`context` call in this run explicitly passed `repo=/home/username01/cmano-clone` to avoid ambiguity.
- **Two Phase A2 validation adaptations were needed**, both confirmed as schema mismatches rather than findings:
  - `scenario_validate` CLI does not apply to gauntlet `.policy.json` artifacts — it targets the MissionEditor's full `ScenarioDocument` schema (`metadata.tlBranch`/`dbSnapshotId`/`unitReadiness`), not the lightweight gauntlet policy schema (`friendlyRoe`/`opposingRoe`/`engage`/`detection`/`gauntlet`/`id`). Confirmed by checking that the already-promoted references `gauntlet-t1-patrol-a/b.policy.json` fail the same CLI check identically.
  - `metadata.seed` is intentionally absent from every `.policy.json` in the corpus — seeds are supplied externally at batch-run time via the harness's `--seeds` CLI flag (Phase B), not baked into the policy document. Not a finding.
- **Batch harness scenario-discovery gap** (see §7 item 6 for the recommendation): `Delegation.Demo --batch` has no way to point at a run's own tier-N artifact directory; scenarios must be physically copied into the hardcoded `data/scenarios/` directory to be discovered. Flagging here as friction that will recur for whoever runs the next gauntlet, in addition to being a recommended follow-up.

---

## detect_changes vs `main`

`detect_changes({scope: "compare", base_ref: "main", repo: "/home/username01/cmano-clone"})`: **risk_level LOW**, 39 changed symbols across 23 files, **0 affected execution processes**. The entire branch diff is exactly the shape expected for a quarantine-only run: QA reports/manifests/bug reports (docs), Tier 1 scenario/roster/oracle-eval artifacts (data), and one test file touched (`MvpEngagementResolverTests.cs` — only the new `Skip`-marked regression test, no production code). No `src/` production symbol was modified; the two defects remain exactly as found, untouched, per the autonomy boundary.

---

## Sign-off Reference

| Metric | Value |
|---|---|
| Baseline test count (Phase 0) | 1924 passed / 1924 total, 0 failed |
| **Final test count (independently re-run by qa-lead)** | **1924 passed / 1925 total, 0 failed, 1 skipped** — the +1 is the intentionally-quarantined `Killed_shooter_aborts_before_launch` regression test; no hidden failures |
| Replay-golden | 17/17 PASS (independently re-run by qa-lead after the test-file edit — confirmed no regression) |
| Smoke | 21/21 PASS |
| Catalog gate | PASS (schema `005`, 79 platform rows) |
| Scenario-data audit (Tier 1) | 8/8 PASS (`production/qa/scenario-audit-2026-07-27-gauntlet-t1.md`) |
| Oracle eval (Tier 1) | 0/4 main scenarios PASS, 0/4 forge candidates PASS — both quarantined engine defects, not scenario/oracle error |
| Regression anchors | N/A — Tier 1 is the first tier run this session, no prior tier to anchor against |
| Commits this run | None to `src/` (fixes withheld per autonomy boundary); 1 Skip-marked regression test added for Bug 1; 2 bug reports filed; run artifacts + manifest committed to QA branch |
| Forge promotes | 0 (4 discards, `engine-defect-blocked`, no recipe down-weighting) |
| Human decision | Halt after Tier 1 — both defects are tier-agnostic; Tiers 2-5 would not surface new information |

### QA Lead Sign-off

**READY FOR HUMAN REVIEW.** Independent re-verification (not trusting the orchestrator's self-report) confirmed: full suite 1924/1925/0-failed/1-skipped exactly matches Phase 0 baseline plus the one intentional quarantine skip; ReplayGolden 17/17 unaffected by the test-file edit. One documentation inconsistency was found and has since been corrected: `BUG-engagement-resolver-shooter-liveness.md` originally described its companion defect as "not yet filed" when `BUG-losses-scoring-side-unaware.md` was in fact already filed — both reports now cross-reference each other correctly. Both reports' severity labels were also corrected from the non-standard "S2-High" to this project's actual taxonomy label, "S2-Major." No test/build blockers.


---

## 10. Post-AAR Remediation (2026-07-27, follow-up session)

The human explicitly authorized overriding the `/qa-gauntlet` CRITICAL autonomy
rail. Both quarantined defects were then fixed with full TDD discipline. This
section supersedes the "quarantined, not fixed" disposition recorded above.

### Outcome

| Metric | At AAR time | After remediation |
|---|---|---|
| Defects quarantined | 2 | **0** |
| `sim-code` fixed | 0 | **2** |
| Tier 1 oracle | 0/4 scenarios pass | **4/4, `allPassed: true`** |
| Corpus (24 pre-existing) | not assessed | **24/24 pass** at correct per-tier tick budgets |
| Test suite | 1924 passed, 1 skipped | **1928 passed, 0 failed, 0 skipped** |
| ReplayGolden | 17/17 | **17/17 — no golden hash moved; Baltic v2 hash untouched** |
| Determinism | n/a | **12/12 identical fingerprints across two independent runs** |

### What the CRITICAL warnings actually turned out to mean

Both blast-radius figures that drove the original quarantine proved to be
**upper bounds on where to look, not on what to change** — which is worth
recording for future triage:

- **Bug 1 (42 symbols, 3 interface implementations):** all 3 implementations were
  reviewed individually as the quarantine required. `RecordingEngagementResolver`
  and `StubEngagementResolver` are deliberate test doubles with no killed-target
  logic; `MvpEngagementResolver` was the only real gate. One file changed.
- **Bug 2 (381 symbols):** that figure was for adding a `side` field to
  `EngagementOutcomeRecord`'s constructor. It was never necessary — the record
  already carries `ShooterTargetId`, and `TargetId` wraps a *string* unit id,
  which is exactly the key `BalticV3SideRegistry` takes. The actual fix surface
  was 3 callers.

The quarantine was still the right call at the time: that analysis had not been
done, and the rail exists precisely to force it to happen under human oversight
rather than mid-run.

### Corpus re-baseline, and a methodology correction

An initial corpus sweep ran all 24 scenarios at 10 ticks and reported 9
failures. That was **invalid** — the expect-regen runbook explicitly forbids
calibrating from CI's 10-tick smoke. Re-run at correct per-tier budgets
(T1=6, T2=10, T3=16, T4=24, T5=40) the true figure was **4**, and the corrected
number is what the regen was based on.

Of those 4: two were denial-ceiling drift (dead shooters' blocked attempts are
now logged as denials rather than launches), and two — `gauntlet-t1-patrol-c`
and `gauntlet-t2-escort-passive` — had `minKills: 1` assertions that were only
ever satisfiable *because of* Bug 2. Both scenarios' own stated intents are
restrictive postures ("tight Blue ROE", "passive-EMCON, low Pd"), so blue
scoring zero kills is correct behaviour. Their envelopes were corrected to
`minKills: 0` while **preserving meaningful `minDenials` floors**, so they still
assert that ROE/EMCON gating actually fires rather than becoming no-ops.

### New issue surfaced (not fixed — design question)

`production/qa/bugs/BUG-scoring-penalises-roe-correct-refusals.md`: score
deducts 5 per denial without distinguishing a *correct* ROE refusal from a
genuine failure. Scenarios designed around restraint therefore score deeply
negative for behaving exactly as intended (`gauntlet-t2-escort-passive`: −200
for staying passive). Filed with four defensible options and no code change
proposed — it is a game-design call, not a QA call.

### Remaining known gap

`C2TopBarProjection` still uses the unfiltered (count-all) scoring path. It has
no side context, and guessing the player's side would be a behaviour change
beyond the scope of this defect. Follow-up: give the C2 top bar a side source.

### Commits

| Commit | Change |
|---|---|
| `94e615d1` | Bug 1 — shooter-liveness gate + `ShooterDestroyed` abort reason + manifest entry |
| `48c648e1` | Bug 2 — side-aware scoring via narrow `scoredSide` filter |
| `7ebda974` | Tier-1 expect regen from post-fix batch |
| `c6e0c61a` | Corpus expect regen (4 scenarios) + new design-question bug report |
