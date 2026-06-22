# Baltic v2 Scope Boundary — Content Expansion Program (S57–S64)

**Date:** 2026-06-22  
**Status:** PUBLISHED (per roadmap-062226 §6; supersedes post-release-scope-boundary-2026-06-21.md for S57+)  
**Authority:** [`docs/reports/future-sprint-roadpmap-062226.md`](../docs/reports/future-sprint-roadpmap-062226.md) §3, §6, §10; S56 gate PASS + human ack 2026-06-22  
**Lead Epic:** E9 Baltic content (S58–S64); S57 E1 prerequisite  
**Exit:** S64 Baltic v2 content gate + playtest sign-off  

---

## Purpose

S57–S64 expands Baltic v1 content depth (scenarios, theater OOB, mission events, catalog slices) + AAR-driven gameplay fixes + structured playtest loop. Stage remains **Release**. No commercial launch (E7 out of scope).

Every story / commit **must** cite this boundary + roadmap-062226 §10 row + epic/theme ID.

## Standing Invariants (S57+)

- Test baseline **≥1228** (monotonic from S56); never regress.
- **ReplayGolden 6/6** every sprint; new isolated goldens for v2 content (`baltic-v2-*` family).
- **C2 proxy 18/18+**.
- Production Baltic hash **`17144800277401907079`** immutable unless golden ADR + determinism review.
- **CatalogWriteGate extend-only**; **ZERO DelegationBridge** (ADR required for any deviation).
- GitNexus: `impact()` before CRITICAL edits (PatrolCandidateEngagePolicy, CatalogWriteGate...); `detect_changes()` before commit.
- Single owner per CRITICAL symbol per sprint (no concurrent edits).
- All artifacts cite this doc + roadmap §0/§10/§12.

## S57–S64 Committed Scope (from roadmap §3/§10)

| Sprint | Lead | Parallel Tracks | Key Deliverables |
|--------|------|-----------------|------------------|
| **S57** | E1 | AAR policy fix ∥ replay goldens ∥ playtest prep | PatrolCandidateEngagePolicy destroyed-target pre-filter; isolated re-engage golden; harness stubs |
| **S58** | E9 | Patrol/mission variants ∥ Band B/C fixtures (goldens trail) | 3–5 new `baltic-v2-*` scenarios |
| **S59** | E9 | Extended OOB ∥ theater hash family | Second-side compositions; isolated hashes |
| **S60** | E9 | Mission arcs ∥ briefing stubs | Contact-window + MissionTransition scenarios |
| **S61** | E9+E5 | Catalog slices → Platform Editor (pipeline) | Unit/loadout content; Excel round-trip |
| **S62** | E4+E9 | Scenario picker ∥ difficulty bands + tooltips | C2 UX for v2 manifest |
| **S63** | E1 | Automated batch ∥ human template | Full v2 playtest loop + ≥1 human session per band |
| **S64** | Gate | Aggregation + sign-off | Content-complete gate; optional theater decision |

**Out of scope (unless new decision):** E7 commercial, multiplayer, full Req re-litigation, art §5/§7 production.

## CRITICAL Symbols & Coordination (S57+)

- `PatrolCandidateEngagePolicy` (S57): CRITICAL — single AAR track owner.
- `CatalogWriteGate` (S61): extend-only.
- `DelegationBridge`: ZERO touch.
- `BalticReplayHarness` + perceived/killed state: HIGH — coordinate with policy.
- New v2 policies: isolated `baltic-v2-*` prefix until S64 promotion.

## Pre-flight (per track)

- GitNexus impact() + report risk.
- Worktree isolation confirmed.
- Cite this boundary + roadmap-062226.md §10.
- Baseline test pass before edit.
- verification-before-completion on all claims.

## Program Exit (S64)

- ≥8 playable Baltic v2 scenarios (Bands A/B/C).
- AAR Topic 1 verified in replay + playtest.
- Human playtest sign-off (≥1 per band).
- All gates + human ack.

*Published 2026-06-22. Cite for all S57+ work.*
