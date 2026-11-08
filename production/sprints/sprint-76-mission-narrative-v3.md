# Sprint 76 — Mission events & narrative (E9)

**Dates:** After S75 (~2026-06-25; est. 8–10 days)
**Lead:** E9 Baltic v3 content
**Goal:** Extend v2 narrative-arc / contact-window / mission-event patterns to additive baltic-v3-* policies. Create mission transition events, contact-window arcs for v3 scenarios; briefing stubs for narrative. Update manifest. Isolated replay evidence where relevant. baltic-v3- prefix only.
**Capacity:** Cloud for S76-01/02/03; local closeout.
**Model:** Per roadmap-execute-plan-062526.01.md §4/§5 + baltic-v3-scope-boundary-2026-06-25.md. Parallel after prior. Isolated worktrees. GitNexus pre mandatory. verification-before on all. team-simulation + narrative pattern. Cite boundary + execute-plan + design + AGENTS.md + S75/S74 + v2 ref (no mutate).

Cites: production/baltic-v3-scope-boundary-2026-06-25.md + docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md §3/§5/§8 + docs/reports/roadmap-execute-plan-062526.01.md §4 + AGENTS.md + future-sprint-roadpmap-062526.01.md + S75/S74/S73/S72 complete + v2 baltic narrative (read ref).

## Tasks / Tracks (from execute-plan §4)

### S76-01/02 Contact-window arcs / mission events (Cloud, team-simulation)
- Extend v2 patterns: create baltic-v3- mission-event, contact-window-arc, narrative-arc policy JSONs in data/scenarios/ (using patterns from baltic-v2-*.policy.json).
- Align to existing v3 policies (S74/S75); add mission: { events, fireOrder with contact-window, pre-brief, briefing-stub, post-action }.
- Update baltic-v3-scenario-manifest.yaml with S76 slots (contact-window + event arcs).
- GitNexus pre FIRST: list_repos canonical, detect (wt), impact on CatalogWriteGate (CRIT extend-only), Patrol, etc.
- verification-before: run+READ build 0e/0w, test 1232/0f, replay 6/6, C2 18/18, hash preserved, ZERO=0.

### S76-03 Briefing stubs (Cloud, narrative-director)
- Create briefing stubs (narrative text or structured stubs, e.g. in design/ or data/briefings/ baltic-v3-*.md or json).
- Reference in mission events (e.g. "briefing-stub" entries).
- Integrate with manifest.

### Other tracks
- S76-04 Closeout (local, c-sharp-devops-engineer)

## Baseline @ Start (S75 COMPLETE)
- Tests 1232/0f
- ReplayGolden 6/6
- C2 18/18
- Build 0e/0w
- Hash 17144800277401907079 preserved (v2)
- ZERO DelegationBridge
- GitNexus: list_repos (cmano-clone: 20354/38059/2493), impacts §5 exact (Catalog 178 CRIT extend-only etc.)
- S73–S75 complete; manifest has S76 placeholder comment.

## Inputs
- v2 narrative (read only): data/scenarios/baltic-v2-contact-window-arc.policy.json, baltic-v2-mission-event.policy.json, baltic-v2-narrative-arc.policy.json
- v3 policies (extend): data/scenarios/baltic-v3-*.policy.json from S74/S75
- baltic-v3-scenario-manifest.yaml (update S76 section)
- design files, execute-plan §4 S76 table

## Definition of Done (S76 tracks)
- baltic-v3-mission-event.policy.json, baltic-v3-contact-window-arc.policy.json, baltic-v3-narrative-arc-*.policy.json (or variants) created (additive).
- baltic-v3-scenario-manifest.yaml updated with S76 entries.
- Briefing stubs created (referenced in events).
- sprint-76-mission-narrative-v3.md + agentic kickoff created.
- sprint-status.yaml s76_status added (tracks, baseline, COMPLETE)
- All GitNexus pre + verification-before RUN+READ; no v2 mutation; hash/ZERO preserved.
- "S76-01/02 COMPLETE", "S76-03 COMPLETE", "S76-04 COMPLETE. S76 COMPLETE"

## Verification (post)
- cd to respective worktrees; re-run verif gates.
- GitNexus detect low risk post.
- List new baltic-v3- mission/narrative files + stubs.
- Full closeout smoke + GT block.

**S76 mission/narrative COMPLETE** (tracks parallel). Low risk additive v3 only. Independent. Dispatching pattern followed. Cites mandatory.

## GT Notes (for user post closeout)
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"
# GitNexus pre + verif (RUN+READ)
gt sync || git pull --ff-only
gt restack
# gt submit --stack --no-interactive for stack/sprint76/*
# interleaved verif after

## S76-04 Closeout Status (2026-06-25)
**S76-04 COMPLETE. S76 COMPLETE.** 
- Smoke: production/qa/smoke-sprint-76-closeout-2026-06-25.md (full gates RUN+READ, GitNexus 20354/38059 + 178/97/127/52, GT ready)
- Status: sprint-status.yaml s76_status + s76_complete (S76-04/S76 COMPLETE)
- Stage: appended (remains Release)
- All per boundary + execute-plan §4/§5/§9 + AGENTS. GT ready for stack/sprint76/* . Independent closeout applied verification-before + cites. S76 COMPLETE.
Cites: production/baltic-v3-scope-boundary-2026-06-25.md + roadmap-execute-plan-062526.01.md §3/4/5/9 + future-sprint-roadpmap-062526.01.md + AGENTS.md + S75 complete.
