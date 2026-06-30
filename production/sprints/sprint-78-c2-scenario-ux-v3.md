# Sprint 78 — C2 scenario UX v3 (E4+E9)

**Dates:** After S77 (~2026-06-25; est. 6–8 days)
**Lead:** E4 + E9
**Goal:** Additive C2 scenario picker v3 (reads v3 manifest) + difficulty bands + tooltips UX. C2 proxy (18/18) must not regress. Additive UI only.
**Capacity:** Cloud for S78-01/02/03; local closeout.
**Model:** Per roadmap-execute-plan-062526.01.md §4/§5 + baltic-v3-scope-boundary-2026-06-25.md. Parallel after prior. Isolated worktrees. GitNexus pre mandatory. verification-before on all. unity-ui + ux pattern. Cite boundary + execute-plan + design + AGENTS.md + S77/S76 + v2 ref (no mutate).

Cites: production/baltic-v3-scope-boundary-2026-06-25.md + docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md §3/§5/§8 + docs/reports/roadmap-execute-plan-062526.01.md §4 + AGENTS.md + future-sprint-roadpmap-062526.01.md + S77/S76/S75/S74/S73/S72 complete + v2 C2 (read ref).

## Tasks / Tracks (from execute-plan §4)

### S78-01/02 Scenario picker v3 (Cloud, unity-ui-specialist)
- Implement additive C2 scenario picker v3 in Unity UI (picker that reads v3 manifest from production/playtests/baltic-v3-scenario-manifest.yaml or equivalent).
- Support v3 slots, bands, sources from manifest.
- Ensure C2 proxy tests (PlayModeSmokeHarnessTests) pass 18/18 no regression.
- GitNexus pre FIRST: list_repos canonical, detect (wt), impact on relevant (e.g. C2 related if CRIT, but focus additive).
- verification-before: run+READ build 0e/0w, test 1232/0f, replay 6/6, C2 18/18, hash preserved, ZERO=0.

### S78-03 Difficulty bands + tooltips (Cloud, ux-designer)
- Additive UI for difficulty bands + tooltips in C2 (integrate with picker or separate UX).
- Use bands from manifest (A/B/C per design/difficulty-curve.md).
- Tooltips for player guidance.
- No regress C2 proxy.

### Other tracks
- S78-04 Closeout (local, c-sharp-devops-engineer)

## Baseline @ Start (S77 COMPLETE)
- Tests 1232/0f
- ReplayGolden 6/6
- C2 18/18
- Build 0e/0w
- Hash 17144800277401907079 preserved (v2)
- ZERO DelegationBridge
- GitNexus: list_repos (cmano-clone: 20496/38203/2516), impacts §5 exact (Catalog 178 CRIT extend-only etc.)
- S73–S77 complete; v3 manifest has slots for C2.

## Inputs
- v3 manifest (read): production/playtests/baltic-v3-scenario-manifest.yaml (v3_slots, bands)
- Previous C2 UI code (additive only)
- design/difficulty-curve.md , execute-plan §4 S78 table
- C2 proxy tests for no regression

## Definition of Done (S78 tracks)
- Additive scenario picker v3 in C2 UI reading v3 manifest.
- Difficulty bands + tooltips UI added.
- sprint-78-c2-scenario-ux-v3.md + agentic kickoff created.
- sprint-status.yaml s78_status added (tracks, baseline, COMPLETE)
- All GitNexus pre + verification-before RUN+READ; no v2 mutation; hash/ZERO/C2 proxy preserved.
- "S78-01/02 COMPLETE", "S78-03 COMPLETE", "S78-04 COMPLETE. S78 COMPLETE"

## Verification (post)
- cd to respective worktrees; re-run verif gates (esp C2 18/18).
- GitNexus detect low risk post.
- List new UI changes/files.
- Full closeout smoke + GT block.

**S78 C2 UX COMPLETE** (tracks parallel). Low risk additive v3 only. Independent. Dispatching pattern followed. Cites mandatory.

**S78-04 COMPLETE. S78 COMPLETE.** (local closeout; GitNexus pre + verification-before RUN+READ applied; picker UI + bands/tooltips UI + manifest notes confirmed from S78-01/02/03 wts; smoke created; status/stage/plan updated main+wt; all gates 0e/0w 1232/0f 6/6 18/18 hash/ZERO preserved). Cite production/baltic-v3-scope-boundary-2026-06-25.md + roadmap-execute-plan-062526.01.md §3/4/5/9 + future-sprint-roadpmap-062526.01.md + AGENTS.md + S77/S76/S75/S74/S73 complete.

## GT Notes (for user post closeout)
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"
# GitNexus pre + verif (RUN+READ)
gt sync || git pull --ff-only
gt restack
# gt submit --stack --no-interactive for stack/sprint78/*
# interleaved verif after
