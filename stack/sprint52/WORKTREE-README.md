# S52 E6 Prep Worktree Convention (Planning Only)

**Per roadmap §0/§10/§12 (future-sprint-roadpmap-062126.md)** and **post-release-scope-boundary-2026-06-21.md** (01/08 rows).

## Tracks
- `benchmark/` — Req01 Multi-k headless benchmark skeleton (MVP gate). Depends: S51 corpora.
- `sim-api/` — Req08 Sim API export surface plan + DOTS expand notes. Parallel to benchmark.
- `dots-expand/` — DOTS pilot expansion (mentioned for completeness; separate track).

**Isolated planning location.** Full implementation worktrees per dispatch: `.worktrees/stack/sprint52/{benchmark,sim-api,dots-expand,closeout}`

**Cites:**
- `production/post-release-scope-boundary-2026-06-21.md` §S52 E6 (Req 01 + Req 08)
- `docs/reports/future-sprint-roadpmap.md` (and -062126) §3, §10, §12
- GitNexus impact() + determinism-audit-2026-06-20.md + replay invariants required.
- verification-before-completion enforced.

**Baseline at prep start (S48/S49 carry):** 1227/1227 tests, ReplayGolden 6/6, C2 18/18, Baltic hash `17144800277401907079` immutable.

**S51 corpora dep:** Full corpora (CI ingest) provide scalable scenario data + TL forks for multi-k benchmark test cases and sim surface validation. No direct code dep on S51 work until merge gate.

**GitNexus pre-flight (from roadmap §5):** BalticBatchRunner, SimulationSession, SimTickPipeline, SensorHotPath are HIGH/CRITICAL. Run `gitnexus impact` before any future edit.

No code changes in this prep; skeletons/plans only. Determinism notes: all benchmark paths must be seeded, stable-order, no wall-clock in hash paths.

See sibling track dirs for detailed prep stubs.
