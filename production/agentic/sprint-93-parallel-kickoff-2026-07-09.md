# Sprint 93 Parallel Kickoff — First Binary Asset Wave

**Date:** 2026-07-09  
**Sprint:** S93  
**Authority:** [`docs/superpowers/plans/2026-07-09-short-term-dashboard-wave.md`](../../docs/superpowers/plans/2026-07-09-short-term-dashboard-wave.md), [`s93-asset-production-scope-boundary-2026-07-09.md`](../s93-asset-production-scope-boundary-2026-07-09.md), [`sprint-93-asset-production-wave.md`](../sprints/sprint-93-asset-production-wave.md), [`qa-plan-sprint-93-asset-production-2026-07-09.md`](../qa/qa-plan-sprint-93-asset-production-2026-07-09.md), [`smoke-sprint-91-closeout-2026-07-09.md`](../qa/smoke-sprint-91-closeout-2026-07-09.md), AGENTS.md

## Context

S89–S92 post-editor hygiene **COMPLETE** (human ack 2026-07-09). S91 delivered **specs only** — manifest **38 Specced / 0 In Production / 0 Done**. User approved **full S93 sprint** to close dashboard short-term item 5.

**S93 focuses on (3 parallel cloud tracks + local closeout):**
- C2 tokens + APP-6 atlas + top bar USS (S93-01)
- Store capsules + logos (S93-02)
- Baltic theater framing + band-B overlay sheet (S93-03)
- Local closeout + manifest bump (S93-04)

## GitNexus status (pre-kickoff)

| Item | Value |
|------|-------|
| Indexed commit | `223a5fe` (re-analyze at closeout) |
| ScenarioDocumentEditor | **233 CRITICAL** |
| CatalogWriteGate | **186 CRITICAL** |
| DelegationBridge | **145 CRITICAL** |
| PatrolCandidateEngagePolicy | **113 CRITICAL** |
| BalticReplayHarness | **54 CRITICAL** |

**Expect low risk** — assets/docs only; no symbol edits unless accidental.

## Baseline @ kickoff

Evidence: [`production/qa/evidence/gates-post-editor-hygiene-2026-07-09.log`](../qa/evidence/gates-post-editor-hygiene-2026-07-09.log)

- Build: **0e/0w**
- Full sln: **1599/0f**
- ReplayGolden: **6/6**
- C2 proxy: **20/20**
- Hash: **18** paths
- ZERO DelegationBridge hotpath

## Dispatch Model

- Use `dispatching-parallel-agents` for S93-01, S93-02, S93-03.
- **Isolated worktrees:** `.worktrees/stack/sprint93/asset-c2`, `/asset-store`, `/asset-baltic`
- **Graphite stacks:**
  - `gt create stack/sprint93/asset-c2`
  - `gt create stack/sprint93/asset-store`
  - `gt create stack/sprint93/asset-baltic`
- **Mandatory per track:** GitNexus `detect_changes` + cite boundary + sprint-93 plan
- Use `gt` for stack work (no raw `git push` on stack branches)

## Track Assignments

### S93-01 C2 + tokens (Cloud, technical-artist)

**Output:** `production/assets/c2/`
- `App6FrameAtlas.png` (112×16)
- `AegisTokens.uss`
- `C2TopBarPanel.uss`

**Constraints:** Art bible §3 semantic colors; no C# hotpath; no Unity import required this sprint.

### S93-02 Store capsules (Cloud, art-director)

**Output:** `production/assets/store/`
- `ProjectAegis_BalticMainCapsule_v1.png` (616×353)
- `ProjectAegis_SmallCapsule.png` (231×87)
- `ProjectAegis_Logo_Dark_v1.png`, `ProjectAegis_Icon_v1.png` (ASSET-025)

**Constraints:** E7 prep only — **no store upload**; cite commercial-launch boundary.

### S93-03 Baltic refs (Cloud, art-director)

**Output:** `production/assets/baltic/`
- `baltic-theater-framing-v1.png` (1920×1080 placeholder)
- `ASSET-019-band-b-contact-overlay-spec.md`

**Constraints:** No Baltic hash change; no policy edits.

### S93-04 Closeout (Local, producer)

**Output:** manifest bump, smoke closeout, gates log, sprint-status, dashboard hygiene.

## Wave Schedule

| Day | Action |
|-----|--------|
| 1 | Phase 0 baseline; dispatch S93-01 ∥ S93-02 ∥ S93-03 |
| 2–4 | Track execution; file audit |
| 5 | S93-04 closeout; dashboard items 4–6 hygiene |

---
*Kickoff for S93. Cite s93 boundary + S91 closeout on every track.*
