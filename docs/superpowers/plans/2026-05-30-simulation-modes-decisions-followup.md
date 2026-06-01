# Simulation Modes Decisions — Implementation Plan

**Goal:** Wire locked req 03 decisions in stacked PRs DELEG-6–9 on the delegation Graphite stack.

**Design:** `docs/superpowers/specs/2026-05-30-simulation-modes-decisions-design.md`

---

## Tasks

- [x] DELEG-6 — docs + req 03 resolved section
- [x] DELEG-7 — `AllowDualSideControl` on `ScenarioPolicyProfile` + JSON loader + tests
- [x] DELEG-8 — dual-side branch in `SimulationModeConfigurator.Apply` + tests
- [x] DELEG-9 — `AttachReplayViewer` on orchestrator + bridge guard + tests
- [x] `dotnet test ProjectAegis.sln`
- [x] `npx gitnexus detect_changes --repo cmano-clone`
- [ ] `gt submit --stack --no-interactive`
