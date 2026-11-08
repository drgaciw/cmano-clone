# Sprint 75 — Theater package expansion v3 (E9)

**Dates:** After S74 (~2026-06-25; est. 8–10 days)
**Lead:** E9 Baltic v3 content
**Goal:** Extended Baltic OOB v3 for scenario family (additive theater data for v3 policies); optional regional slice spike (defer full second theater to S80 decision per roadmap). Use baltic-v3-* prefix only. Update manifests/catalog slices matching S74 content. Isolated hash family updates.
**Capacity:** Cloud for S75-01/02/03; local closeout.
**Model:** Per roadmap-execute-plan-062526.01.md §4/§5 + baltic-v3-scope-boundary-2026-06-25.md. Parallel after prior boundary. Isolated worktrees. GitNexus pre mandatory. verification-before on all. team-data + team-simulation pattern. Cite boundary + execute-plan + design + AGENTS.md + S74/S73 + v2 ref (no mutate).

Cites: production/baltic-v3-scope-boundary-2026-06-25.md + docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md §3/§5/§8 + docs/reports/roadmap-execute-plan-062526.01.md §4 + AGENTS.md + future-sprint-roadpmap-062526.01.md + S74/S73/S72 complete + v2 baltic (read ref).

## Tasks / Tracks (from execute-plan §4)

### S75-01/02 Extended OOB (Cloud, team-data)
- Create extended Baltic OOB / theater packages for v3 (data/ or assets/data/theater or catalog updates; baltic-v3- prefixed files).
- Align to S74 baltic-v3-*.policy.json units/platforms/sensors (additive).
- Optional: regional slice spike (document deferral if full theater not in scope).
- Update baltic-v3-scenario-manifest.yaml with theater refs + v3Enabled notes.
- GitNexus pre FIRST: list_repos canonical, detect (wt), impact on CatalogWriteGate (CRIT extend-only), other §5.
- verification-before: run+READ build 0e/0w, test 1232/0f, replay 6/6, C2 18/18, hash preserved, ZERO=0.

### S75-03 Theater hash family (Cloud, team-simulation)
- Generate / update isolated replay goldens using extended v3 theater OOB (tests/regression/replay-golden-baltic-v3-*.txt family).
- Ensure new fingerprints, preserve v2 hash 17144800277401907079 exactly.
- Verify replay gates + C2.

### Other tracks
- S75-04 Closeout (local, c-sharp-devops-engineer)

## Baseline @ Start (S74 COMPLETE)
- Tests 1232/0f
- ReplayGolden 6/6
- C2 18/18
- Build 0e/0w
- Hash 17144800277401907079 preserved (v2)
- ZERO DelegationBridge
- GitNexus: list_repos (cmano-clone: 20354/38059/2493), impacts §5 exact (Catalog 178 CRIT extend-only, Patrol 97 CRIT, DelegationBridge 127 CRIT, BalticReplayHarness 52 CRIT)
- S73 boundary + manifest + reindex + S74 scenarios/goldens COMPLETE. All v3 additive.

## Inputs
- v2/v3 baltic (read only): data/scenarios/baltic-v*-*.policy.json + replay-golden-baltic-v*-*.txt ; production/playtests/baltic-v3-scenario-manifest.yaml
- design/difficulty-curve.md , 2026-06-25-baltic-v3-content-expansion-design.md
- execute-plan §4 S75 track table + S74 artifacts

## Definition of Done (S75 tracks)
- Extended OOB / theater data created additive (baltic-v3-*) matching S74 policies.
- baltic-v3-scenario-manifest.yaml updated with theater sources.
- Isolated replay goldens for theater v3 (new hashes, family).
- sprint-75-theater-v3.md + agentic kickoff created/updated.
- sprint-status.yaml s75_status added (tracks, baseline, COMPLETE)
- All GitNexus pre + verification-before RUN+READ; no v2 mutation; hash/ZERO/Catalog extend-only preserved.
- "S75-01/02 COMPLETE", "S75-03 COMPLETE", "S75-04 COMPLETE. S75 COMPLETE"

## Verification (post)
- cd to respective worktrees; re-run verif gates (logs+counts)
- GitNexus detect low risk post.
- List created files under data/ , tests/regression/ , production/playtests/ , sprint artifacts.
- Full closeout smoke + GT block.

**S75 theater expansion COMPLETE** (tracks parallel). Low risk additive v3 only. Independent. Dispatching pattern followed. Cites mandatory.

**S75-04 COMPLETE. S75 COMPLETE (2026-06-25 closeout).** smoke-sprint-75-closeout-2026-06-25.md + s75_status + s75_complete + stage update + GT ready. GitNexus pre 20354/38059 + 0/0/none + 178/97/127/52 exact; verif 0e/1232/0f/6/6/18/18/hash/ZERO. All cites applied. GT ready for user submit commands after resolve. Cite boundary + execute-plan.

## GT Notes (for user post closeout)
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"
# GitNexus pre + verif (RUN+READ)
gt sync || git pull --ff-only
gt restack
# gt submit --stack --no-interactive for stack/sprint75/*
# interleaved verif after
