# Sprint 74 — Scenario content wave 2 + goldens (E9)

**Dates:** After S73 (~2026-06-25; est. 8–10 days)
**Lead:** E9 Baltic v3 content
**Goal:** 3-5+ new `baltic-v3-*` policies (patrol variants, mission, comms-challenged) per roadmap-execute-plan-062526.01.md §4 and baltic-v3 design; isolated replay goldens. Use baltic-v3- prefix only. Per difficulty-curve bands A/B/C. 
**Capacity:** Cloud for S74-01/02 scenarios + goldens; local closeout.
**Model:** Per roadmap-execute-plan-062526.01.md §4/5 + baltic-v3-scope-boundary-2026-06-25.md. Parallel after S73-01 boundary. Isolated worktrees. GitNexus pre mandatory. verification-before on all. team-simulation skill pattern. Cite boundary + execute-plan + design + AGENTS.md + S72/S69 + v2 ref (no mutate).

Cites: production/baltic-v3-scope-boundary-2026-06-25.md + docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md §3/§5/§8 + docs/reports/roadmap-execute-plan-062526.01.md §4 + AGENTS.md + future-sprint-roadpmap-062526.01.md + S72/S69 complete + v2 baltic (read ref).

## Tasks / Tracks (from execute-plan §4)

### S74-01/02 Patrol/mission variants v3 (this subagent: Cloud, team-simulation)
- Create 3-5 baltic-v3-*.policy.json in data/scenarios/ (worktree .worktrees/stack/sprint74/scenarios used): e.g. baltic-v3-patrol (A), baltic-v3-patrol-comms (B), baltic-v3-classify (A), baltic-v3-mission-band-b (B), baltic-v3-mission-roe-band-c (C) + comms-challenged.
- Matching design/difficulty-curve.md (Band A NPE low load, B mid, C stress/comms).
- Isolated replay goldens: tests/regression/replay-golden-baltic-v3-*.txt (new fingerprints, new WORLD_HASH family, structure from v2 but v3 content).
- GitNexus pre FIRST: list_repos canonical, detect unstaged (wt), impact on CatalogWriteGate (CRIT extend-only), PatrolCandidateEngagePolicy (CRIT), etc. Report exact.
- verification-before: run+READ build 0e/0w, test 1232/0f, replay 6/6, C2 18/18, hash preserved, ZERO=0.

### Other tracks (parallel in sprint)
- S74-03 Difficulty Band B/C fixtures (cloud)
- S74-04 Isolated replay goldens (cloud, c-sharp-test-engineer)
- S74-05 Closeout (local)

## Baseline @ Start (S73 foundations COMPLETE)
- Tests 1232/0f
- ReplayGolden 6/6
- C2 18/18
- Build 0e/0w
- Hash 17144800277401907079 preserved (v2)
- ZERO DelegationBridge
- GitNexus: list_repos (cmano-clone: 20354/38059/...), impacts §5 exact (Catalog 178 CRIT extend-only, Patrol 97 CRIT)
- baltic-v3-scope-boundary published, manifest v3, re-index done. S73 closeout.

## Inputs
- v2 baltic (read only): data/scenarios/baltic-v2-*.policy.json + replay-golden-baltic-v2-*.txt ; production/playtests/baltic-v2-*.yaml
- design/difficulty-curve.md , 2026-06-25-baltic-v3-...-design.md
- execute-plan §4 S74 track table

## Definition of Done (S74-01/02)
- 5 baltic-v3-*.policy.json created (additive, isolated prefix)
- 5 replay-golden-baltic-v3-*.txt created (new hashes/content)
- baltic-v3-scenario-manifest.yaml updated (sources, v3Enabled, bands)
- sprint-74-scenario-wave-v3.md + agentic kickoff created
- sprint-status.yaml s74_status added (tracks, baseline, COMPLETE)
- All GitNexus pre + verification-before RUN+READ; no v2 mutation; Catalog extend-only / ZERO / hash preserved
- "S74-01/02 COMPLETE"

## Verification (post)
- cd to worktree .worktrees/stack/sprint74/scenarios ; re-run verif gates (logs+counts)
- GitNexus detect on wt post (low)
- List created: data/scenarios/baltic-v3-*.policy.json (5), tests/regression/replay-golden-baltic-v3-*.txt (5), production/playtests/baltic-v3-scenario-manifest.yaml (upd), production/sprints/sprint-74-..., production/agentic/sprint-74-..., sprint-status.yaml (upd)

**S74-01/02 COMPLETE** (this subagent). Low risk additive v3 only. Independent. Dispatching pattern followed.
