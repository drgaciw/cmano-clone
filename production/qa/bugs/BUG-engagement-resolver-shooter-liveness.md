# Bug Report

## Summary
**Title**: Engagement resolver allows dead units to keep firing (shooter-liveness not checked)
**ID**: BUG-engagement-resolver-shooter-liveness
**Severity**: S2-High (combat resolution produces physically incorrect outcomes; sim does not crash, still runs)
**Priority**: P2 — needs human-supervised fix given CRITICAL blast radius (see below); not safe for unsupervised autonomous remediation
**Status**: Open — QUARANTINED-CRITICAL
**Reported**: 2026-07-27
**Reporter**: QA Gauntlet run `gauntlet-20260727-1455` (Tier 1), root-caused by c-sharp-architect investigation

## Classification
- **Category**: Gameplay (combat/engagement simulation)
- **System**: `ProjectAegis.Sim` engagement resolution
- **Frequency**: Always (deterministic — reproduces on every run where a unit is killed before the tick budget ends and it would otherwise fire again)
- **Regression**: No — long-standing. No recent commits touch `KilledTargetRegistry` or the resolver's shooter-side gating. Several of the 24 already-promoted gauntlet corpus scenarios have `gauntlet.expect.minKills` values (4–5) that only make sense if calibrated against this same overcounting behavior — the corpus appears to have been tuned around the bug, not against correct kill semantics.

## Environment
- **Build**: commit `fa4db95c` (trunk base for this QA run) + gauntlet branch `07-27-qa_gauntlet_gauntlet-20260727-1455`
- **Platform**: headless .NET 8 (Delegation.Demo batch harness); also reachable via the live Unity runtime bridge
- **Scenario**: Any gauntlet Tier 1 3-vs-3 scenario, e.g. `gauntlet-20260727-1455-t1-s1`, seed 42, 6 ticks

## Reproduction Steps
**Preconditions**: A scenario where at least one unit is killed before the last tick of the run.

1. Run the batch harness: `dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --scenarios gauntlet-20260727-1455-t1-s1 --seeds 42 --ticks 6 --csv-out out.csv`
2. Inspect the fingerprint trace for the scenario/seed.
3. Observe: at tick 1, engagement id=4 (fired by RED unit `421-orkan-pr-660-2015`) lands a `Kill` outcome against BLUE unit `f-341-absalon-2020`.
4. At tick 2, `f-341-absalon-2020` — the unit just killed — launches a **new** engagement (id=5) against `mrk-buyan-mod-pr-21631-buyan-m-2014`.

**Expected Result**: A unit that has been killed should not be able to initiate new engagements in subsequent ticks.
**Actual Result**: The dead unit fires again; `MvpEngagementResolver.Resolve` only validates the *target's* liveness (`_killedTargets.IsKilled(request.TargetId)`), never the *shooter's* (`request.ShooterUnitId`).

## Technical Context
- **Root cause file:line**: `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs:59`
- **Supporting evidence**: `src/ProjectAegis.Sim/Engage/KilledTargetRegistry.cs` (`IsKilled`/`MarkKilled` — correctly implemented, dedup-safe, but only ever consulted for the target side); `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs:374-379` (where `KilledTargets.MarkKilled` is actually called when a kill resolves)
- **Reproducing test (RED, not yet fixed)**: `src/ProjectAegis.Sim.Tests/Engage/MvpEngagementResolverTests.cs`, method `Killed_shooter_aborts_before_launch`
- **Likely affected files for a fix**: `MvpEngagementResolver.cs` (add shooter-liveness check) — but see blast radius below
- **Related systems**: Delegation session orchestration (`SimulationSession.RunExecutingTick`), the live Unity runtime bridge (`DelegationBridgeHost.RunTick`)
- **Related defect**: BUG-losses-scoring-side-unaware (companion — scoring credits kills to the wrong side; tracked separately, not yet filed)

## Evidence
- GitNexus `impact({target: "Resolve", file_path: "src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs", direction: "upstream"})`:
  - **risk: CRITICAL**
  - impactedCount: 42, direct: 37
  - affected_processes: `RunExecutingTick` (`src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs`, 6 hits) and `RunTick` (`unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs`, 2 affected processes / 4 hits, earliest_broken_step 3 — **the live Unity production runtime bridge, not just QA tooling**)
  - `Resolve` is an interface with **3 implementations**; GitNexus flags `epistemic: lower-bound` — actual impact may exceed what's traced, since callers binding via the interface (DI/dynamic dispatch) aren't followed to the concrete symbol.

## Related Issues
- Companion defect: side-unaware scoring in `LossesScoringProjection.Project` (`src/ProjectAegis.Delegation/Projection/LossesScoringProjection.cs:12`) — counts every `Kill`-coded outcome regardless of which side scored it, so an enemy kill against your own unit gets credited to your own kill tally. Confirmed by the same root-cause investigation. Tracked as a separate defect (not yet filed as its own bug report) since its fix is independent (side-threading into `DecisionLog`/`EngagementOutcomeRecord`, likely via `BalticV3SideRegistry`) and has a different (still-unassessed) blast radius.
- Full investigation notes: this session's QA Gauntlet run `gauntlet-20260727-1455`.

## Notes

**Why this is quarantined rather than fixed immediately**: this project's QA Gauntlet skill has an explicit autonomy boundary — "if GitNexus impact returns CRITICAL on a symbol a fix must touch, do NOT edit it" — and CLAUDE.md separately mandates never ignoring HIGH/CRITICAL impact-analysis risk. `Resolve`'s CRITICAL rating, its 3 interface implementations (each needing individual review before a safe fix), and its live exposure through the Unity production runtime bridge (not just test/QA code) together make this unsafe for unsupervised autonomous remediation in this session. A human-supervised fix should:
1. Review all 3 implementations of the `Resolve` interface individually — not just the one this investigation focused on.
2. Add the missing shooter-liveness check (mirroring the existing target-liveness check).
3. Re-run the full test suite, `replay-verify`, and the entire 24-scenario gauntlet corpus (not just Tier 1) — fixing this will very likely flip some currently-"passing" scenarios' actual kill counts, requiring `gauntlet.expect` recalibration across the corpus per the expect-regen runbook (`tools/qa-gauntlet/README-expect-regen.md`), not just at the two Tier 1 scenarios that surfaced it.
4. Confirm no behavior change to the Unity runtime bridge's live gameplay balance beyond "dead units correctly stop fighting" (i.e., rule out any hidden dependency on the current — buggy — behavior).
