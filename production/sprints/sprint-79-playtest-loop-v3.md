# Sprint 79 — Playtest loop v3 (E1+E9)

**Dates:** After S78 (~2026-06-25; est. 8–10 days)
**Lead:** E1 + E9
**Goal:** Playtest loop v3 (auto + human): batch against full v3 manifest; human session template + ≥1 per difficulty band; artifacts in production/playtests/human/. 
**Capacity:** Cloud for S79-01/02 auto; local for human + closeout.
**Model:** Per roadmap-execute-plan-062526.01.md §4/§5 + baltic-v3-scope-boundary-2026-06-25.md. Parallel after prior. Isolated worktrees. GitNexus pre mandatory. verification-before on all. qa-lead + qa-tester pattern. Cite boundary + execute-plan + design + AGENTS.md + S78/S77 + v2 ref (no mutate).

Cites: production/baltic-v3-scope-boundary-2026-06-25.md + docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md §3/§5/§8 + docs/reports/roadmap-execute-plan-062526.01.md §4 + AGENTS.md + future-sprint-roadpmap-062526.01.md + S78/S77/S76/S75/S74/S73/S72 complete + v2 playtests (read ref).

## Tasks / Tracks (from execute-plan §4)

### S79-01/02 Automated playtest batch (Cloud, qa-lead)
- Implement/ extend automated batch runner against full v3 manifest (production/playtests/baltic-v3-scenario-manifest.yaml).
- Run batch for v3 scenarios per bands/seeds.
- Capture artifacts in production/playtests/ (auto/ or human/ as appropriate).
- GitNexus pre FIRST: list_repos canonical, detect (wt), impact on relevant.
- verification-before: run+READ build 0e/0w, test 1232/0f, replay 6/6, C2 18/18, hash preserved, ZERO=0.

### S79-03 Human session template (Local, qa-tester)
- Create human session template + ≥1 session per difficulty band (A/B/C from manifest).
- Artifacts in production/playtests/human/.
- Use v3 manifest for scenarios.

### Other tracks
- S79-04 Closeout (local, c-sharp-devops-engineer)

## Baseline @ Start (S78 COMPLETE)
- Tests 1232/0f
- ReplayGolden 6/6
- C2 18/18
- Build 0e/0w
- Hash 17144800277401907079 preserved (v2)
- ZERO DelegationBridge
- GitNexus: list_repos (cmano-clone: 20496/38203/2516), impacts §5 exact (Catalog 178 CRIT extend-only etc.)
- S73–S78 complete; v3 manifest full.

## Inputs
- v3 manifest (read): production/playtests/baltic-v3-scenario-manifest.yaml (full v3_slots, bands)
- Previous playtest code (baltic-v2, batch runners)
- design files, execute-plan §4 S79 table

## Definition of Done (S79 tracks)
- Automated batch run against full v3 manifest.
- Human session template + ≥1 per band in production/playtests/human/.
- sprint-79-playtest-loop-v3.md + agentic kickoff created.
- sprint-status.yaml s79_status added (tracks, baseline, COMPLETE)
- All GitNexus pre + verification-before RUN+READ; no v2 mutation; hash/ZERO preserved.
- "S79-01/02 COMPLETE", "S79-03 COMPLETE", "S79-04 COMPLETE. S79 COMPLETE"

## Verification (post)
- cd to respective worktrees; re-run verif gates.
- GitNexus detect low risk post.
- List batch outputs, human artifacts.
- Full closeout smoke + GT block.

**S79 playtest loop v3 COMPLETE** (tracks parallel). Low risk additive v3 only. Independent. Dispatching pattern followed. Cites mandatory.

## GT Notes (for user post closeout)
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"
# GitNexus pre + verif (RUN+READ)
gt sync || git pull --ff-only
gt restack
# gt submit --stack --no-interactive for stack/sprint79/*
# interleaved verif after

**S79-04 COMPLETE. S79 COMPLETE.** (closeout finalization). All tracks aggregate confirmed. GitNexus pre + verification-before applied. Cite boundary + execute-plan.
