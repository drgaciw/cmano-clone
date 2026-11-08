# Sprint 52 E6 Prep — Benchmark + Sim API (parallel track)

**Date:** 2026-06-21 (subagent prep)
**Scope:** Per roadmap §10/§12 (future-sprint-roadpmap-062126.md), post-release-scope-boundary-2026-06-21.md (Req 01 + 08, E6).
**Status:** Prep complete. Skeletons/plans produced. No implementation edits.

## Parallel Tracks (fan-out)
- Req01: Multi-k headless benchmark skeleton (MVP gate for entity scale).
- Req08: Sim API export surface plan (stable export + S45 DOTS pilot expand).

**Worktree convention (per §0.2):** `stack/sprint52/{benchmark, sim-api, dots-expand, closeout}` (planning stubs under cmano-clone/stack/sprint52/; impl in .worktrees/... later).

**Deps:** S51 corpora-ci + tl-fork (data for benchmarks + TL surface tests). See dep graph in roadmap.

## Artifacts Produced
- `stack/sprint52/WORKTREE-README.md`
- `stack/sprint52/benchmark/S52-01-multi-k-benchmark-skeleton-prep.md` (skeleton plan, INF-5.1 metrics, GitNexus cites)
- `stack/sprint52/sim-api/S52-03-sim-api-export-surface-plan.md` (surface audit, stability contract, DOTS notes)
- `stack/sprint52/dots-expand/S52-DOTS-expand-prep-notes.md`
- Updates: sprint-status.yaml, sprint-49-parallel-dispatch-2026-06-21.md

## Key Notes Embedded
- GitNexus: impact() + detect_changes() on BalticBatchRunner, SimulationSession (CRITICAL 228+ impacted), SimTickPipeline, etc. (from gitnexus__* calls).
- Determinism: audit-2026-06-20.md, pinned hash, Replay 6/6, seeded only.
- Boundary: cite post-release-scope-boundary-2026-06-21.md + roadmap.
- verification-before-completion: baseline 1227/1227 + 6/6 + 18/18 held (no changes).

**Next:** Full sprint-plan after S51; dispatch per roadmap to worktrees.

Cites: production/post-release-scope-boundary-2026-06-21.md, docs/reports/future-sprint-roadpmap-062126.md, GitNexus MCP results, determinism audits.
