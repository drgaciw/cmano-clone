# Sprint 80 — Baltic v3 content gate (E9)

**Dates:** After S79 (~2026-06-25; est. 5–7 days)
**Lead:** Gate (devops) + Producer (sign-off)
**Goal:** Full verification + human ack for S73–S79. Produce gate artifact. Optional v3 promotion decision. Stage stays Release.
**Capacity:** Both tracks local.
**Model:** Per roadmap-execute-plan-062526.01.md §4/§5 + baltic-v3-scope-boundary-2026-06-25.md. Serial (gate then sign-off). Isolated worktrees. GitNexus pre mandatory. verification-before on all. devops + producer pattern. Cite boundary + execute-plan + design + AGENTS.md + S79+ complete.

Cites: production/baltic-v3-scope-boundary-2026-06-25.md + docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md §3/§5/§8 + docs/reports/roadmap-execute-plan-062526.01.md §4 + AGENTS.md + future-sprint-roadpmap-062526.01.md + S79/S78/S77/S76/S75/S74/S73/S72 complete + v2 refs (read only).

## Tasks / Tracks (from execute-plan §4)

### S80-01 Gate verification (Local, c-sharp-devops-engineer)
- Full RUN+READ verification-before on all gates: build 0e/0w, tests 1232/0f, replay 6/6+, C2 18/18, hash preserved or ADR, ZERO=0.
- Confirm S73–S79 closeouts PASS.
- Index v3 manifest + goldens in production/qa/evidence/baltic-v3-playtest-index.md.
- Playtest sign-off evidence (auto + human per band).
- GitNexus CRITICAL §5 exact match.
- GitNexus pre FIRST: list_repos, detect, impact on 178/97/127/52.
- verification-before: run+READ all.
- Produce production/gate-checks/s80-baltic-v3-content-gate-2026-06-*.md with full checklist.

### S80-02 Human sign-off (Local, producer)
- Review gate artifact.
- Human ack: "Baltic v3 content-complete".
- Optional: decide on promote v3 corpus (explicit only).
- Stage remains Release (no advance unless separate decision).

## Baseline @ Start (S79 COMPLETE)
- Tests 1232/0f
- ReplayGolden 6/6+
- C2 18/18
- Build 0e/0w
- Hash 17144800277401907079 preserved (v2)
- ZERO DelegationBridge
- GitNexus: list_repos (cmano-clone: 20496/38203/2516), impacts §5 exact (Catalog 178 CRIT etc.)
- S73–S79 complete; v3 manifest + goldens ready.

## Inputs
- All prior smoke-*/status/stage/plan/kickoff from S73–S79.
- production/playtests/baltic-v3-scenario-manifest.yaml
- tests/regression/replay-golden-baltic-v3-*.txt
- production/qa/evidence/baltic-v3-playtest-index.md (to update)
- baltic-v3-scope-boundary-2026-06-25.md , execute-plan §4 S80, future-roadmap.

## Definition of Done (S80 tracks)
- Gate verification: all exit criteria checklist PASS, gate artifact produced with RUN+READ + cites.
- Human sign-off: ack provided, optional promotion decision noted.
- sprint-80-baltic-v3-gate.md + agentic kickoff created/updated.
- sprint-status.yaml s80_status added ("S80-02 COMPLETE. S80 COMPLETE").
- stage.txt appended (S80 gate + ack; remains Release).
- All GitNexus pre + verification-before RUN+READ; no v2 mutation; hash/ZERO preserved or ADR.
- "S80-01 COMPLETE", "S80-02 COMPLETE. S80 COMPLETE"
- Smoke closeout + GT prep if applicable.

## Verification (post)
- cd to worktrees; re-run all gates (RUN+READ).
- GitNexus detect low.
- List gate artifact, index, status updates.
- Full closeout smoke + GT block if needed.

**S80 Baltic v3 content gate COMPLETE.** Serial verification → human ack. Low risk. Stage **stay Release**. Dispatching pattern followed. Cites mandatory.

## GT Notes (for user post closeout)
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"
# GitNexus pre + verif (RUN+READ)
gt sync || git pull --ff-only
gt restack
# gt submit --stack --no-interactive for stack/sprint80/*
# interleaved verif after
# S73-S80 payload processing note: Group2 finalized 2026-06-28 via dispatching-parallel-agents + verification-before + GitNexus pre (see commit body for cites).
