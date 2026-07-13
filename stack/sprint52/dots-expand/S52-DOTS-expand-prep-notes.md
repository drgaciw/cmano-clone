# S52 DOTS Expand Prep Notes (parallel to benchmark + sim-api)

Per roadmap §10: DOTS determinism review shared across E6 tracks. Expand S45 pilot.

**Key cites:** ADR-005, post-release-scope-boundary (E6), future-sprint-roadpmap §12 (S52 DOTS expand track `stack/sprint52/dots-expand`), S45 artifacts in production.

**Dependencies:** S52 sim-api surface for snapshot export; benchmark for scale test cases; S51 corpora data.

**Prep summary:** 
- Isolated fixtures only (no hot path migration).
- GitNexus impact on SensorHotPath + DOTS bridges.
- Determinism sign-off mandatory.
- See sim-api plan for surface notes (ToDotsEntities stub, EntityKey).
- Full impl in dedicated track per dispatch.

**Verification:** Planning stub. No code. Boundary + roadmap cited.
