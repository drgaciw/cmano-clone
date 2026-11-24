# Sprint 6 Definition of Done Rubric (`sprint/s6-gauntlet-g2-gate`)

**Scope:**
- **Track A (Gauntlet Verification, Oracle Contracts & Release Gates):** `DRG-201` (typed mission-thread evidence & unified evidence ledger), `DRG-202` (three-layer combat oracle contracts), `DRG-195` (agentic decision provenance & degraded modes), `DRG-205` (replay, accessibility `ACC-11`, usability & CI release gates `VER-04`/`VER-06`), plus integrated verification/budgets/traceability tying in `DRG-193` (IFC mission-thread), `DRG-176` (combat-picture performance/declutter budgets), `DRG-203` (agentic resilience/property tests), `DRG-204` (baseline-regression policy & GitNexus test selection), and `DRG-187` (kill-chain traceability & slice acceptance matrix).
- **Track B (Slice G2 Integrated C2 UX Workflows, Usability & Live Unity `:8081` PlayMode):** `DRG-235` (`[Slice G2]` integrated C2 UX workflows: `CMD-01`, `CMD-07`, `CMD-10`; degraded-comms acceptance flow: plan/assign -> accelerate/alert -> inspect doctrine/refusal -> intervene/return -> lose link -> recover -> replay; empty/stale/unknown/comms-loss states; `1600x979` & `1280x779` + scale extremes; keyboard & pointer checks distinct from callback automation; live Unity `:8081` PlayMode + visually inspected screenshots) and `DRG-177` (combat tutorials & scenario-based usability tests).
- **Track C (Revision-Bound Owner Acceptance Pack & Final Closeout):** `DRG-244` (`[HYG-08]` Play Mode re-signoff evidence index covering Sprints 1–6 at HEAD SHA, explicitly distinguishing headless / proxy / fixture / Editor PlayMode / owner-only evidence, noting `DRG-208` was historical `9cd872c7`, and cataloging all untouched `hold:owner-go` issues).

**Untouched (`hold:owner-go` across Sprints 1–6):** `DRG-268`, `DRG-269`, `DRG-236`, `DRG-231`, `DRG-347`.
**Jev Kickoff Decision:** `gen-dec-1795446060-m3iRPU7mGnbEFQRvUsXD` (`three_tracks_gauntlet_g2_pack` = `1.0`, `require_honest_live_vs_fixture_labeling` = `0.88`).

## Hard Gates (`H1`–`H8`)

| Gate | Requirement | Target / Floor |
| --- | --- | --- |
| `H1` / `C1` | Clean `.NET 11` build (`0` errors, `0` warnings) & full test suite | `>= 4,987` passed, `0` failed, `0` skipped |
| `H2` | Linear honesty: post evidence comments on Sprint 6 issues (`DRG-201`, `DRG-202`, `DRG-195`, `DRG-205`, `DRG-235`, `DRG-177`, `DRG-193`, `DRG-176`, `DRG-187`, `DRG-203`, `DRG-204`, `DRG-244`); keep in `Backlog` prior to PR merge; keep all 5 `hold:owner-go` issues untouched | Verified via Linear MCP |
| `H3` / `C7` | Hard-no files untouched (`0` diff vs `4b5b4ca1`): `DelegationBridge.cs`, `SimulationSession.cs`, `CatalogWriteGate` existing write paths, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/*.db` | `0` unauthorized diffs |
| `H4` / `C2` | Determinism & golden preservation (`VER-04`) | `ReplayGolden` `17/17`, hash `17144800277401907079` preserved, `SeededRng` only |
| `H5` / `C8` | Strict `RED -> GREEN -> REFACTOR` TDD + verbatim RED evidence (`s6-c8-red-evidence.md`) + architecture fences | `31/31` architecture fences; ADR-010/007/001 respected |
| `H6` / `C10` | Owner-only Play Mode walk boundary | Explicitly preserved as owner-only in `DRG-244`; `DRG-208` historical `9cd872c7` never cited as current-HEAD acceptance |
| `H8` / `C3` / `C4` / `C12` / `C13` | `PlayModeSmokeHarnessTests` + `copy-delegation-assemblies.sh` + live Unity `:8081` PlayMode + visually inspected Slice G2 screenshots | `PlayModeSmokeHarnessTests >= 27/27`; `ProjectAegis.Unity.Tests > 61` passed, `0` Console errors |

## Sprint 6 Functional DoD

1. **Track A (`DRG-201`, `DRG-202`, `DRG-195`, `DRG-205`, `DRG-193`, `DRG-176`, `DRG-203`, `DRG-204`, `DRG-187`)**:
   - Typed mission-thread evidence model & unified evidence ledger extending existing gauntlet/oracle infrastructure (`GauntletOracleEvaluator`, `GauntletOracleEvalCommand`) without breaking existing strict-key tests.
   - Three-layer combat oracle contracts (sim truth, presentation projection, rendered/accessibility invariants) with explicit provenance & degraded-mode verification (`DRG-195`) and IFC mission-thread checks (`DRG-193`).
   - Combat-picture performance/declutter budgets (`DRG-176`), agentic resilience & deterministic property tests (`DRG-203`), baseline-regression / GitNexus test selection policy (`DRG-204`), and kill-chain traceability / slice acceptance matrix (`DRG-187`).
   - Release gates (`DRG-205`): `VER-04` multi-run seed invariance, `VER-06` CI gate & floor parity, `ACC-11` screen accessibility rules (C2 `>= 12` visible log rows at 100%, COMMS degrade triad & COMMS HUD legend tooltip copy, Platform Editor text-only buttons & readable disabled state).
2. **Track B (`DRG-235`, `DRG-177`)**:
   - Integrated Slice G2 C2 UX workflow validation (headless + live Unity `:8081` PlayMode tests in `Assets/Tests/Sprint6SliceG2PlayModeTests.cs`):
     - `CMD-01` persistent C2 chrome zones, `CMD-07` selection sync (map <-> OOB <-> contacts <-> right detail panel), `CMD-10` context actions routing through validated command facades.
     - Degraded-comms acceptance flow: plan/assign -> accelerate/alert -> inspect doctrine/refusal -> intervene/return -> lose link -> recover -> replay.
     - Empty, stale, unknown, and comms-loss states grounded in explicit fixtures (honestly labeled as fixture vs live-source composition).
     - `1600x979` and `1280x779` baseline layout reachability + supported UI scale extremes; keyboard & pointer input checks distinct from callback-only automation.
     - Combat tutorial / scenario-based usability validation (`DRG-177`).
   - Visually inspected live Unity `:8081` Game View screenshots + evidence report in `production/qa/evidence/sprint-6/s6-g2-c2-workflow-evidence.md`.
3. **Track C (`DRG-244` Owner Acceptance Pack & Final Closeout)**:
   - Revision-bound Play Mode re-signoff evidence index (`production/qa/evidence/sprint-6/s6-drg244-owner-acceptance-pack.md`) summarizing Sprints 1–6 commits, test floors, live PlayMode counts, screenshots, Jev DoD verdicts, honest evidence classification (headless / proxy / fixture / Editor PlayMode / owner-only), and the 5 untouched `hold:owner-go` issues.
   - Linear evidence comments posted (`H2`: keep `Backlog`).
   - Typesafe-AI Jev Round 1+ final Sprint 6 DoD gate (`more_rounds = 0`, all non-owner criteria `noul >= 0.80`), saved to `production/qa/evidence/sprint-6/jev-sprint6-dod-round1-decisions.json`, and rubric updated.
   - Clean atomic per-track commits on `sprint/s6-gauntlet-g2-gate`.

## Round Log

- **Round 0 (Kickoff)**: `gen-dec-1795446060-m3iRPU7mGnbEFQRvUsXD` — Three-track split (`three_tracks_gauntlet_g2_pack`, `p=1.0`, confidence `1.0`), honest live-vs-fixture labeling (`0.88`).

- **Round 1 (Pre-Commit Check)**: `gen-dec-1795460899-1QLj4tLANmsI8kU3u4NG` — `sprint6_verdict = pass_owner_walk` (`0.98`), `more_rounds = 0` (`0.87`); flagged missing commit SHAs, catalog SHA-256, and `S1`–`S6` score table.
- **Round 2 (Post Track A `84b85a35` & Track B `c03f5f44` + 12 Linear Comments)**: `gen-dec-1795461009-yLiraWqAAdu42FsmQc4J` — `sprint6_verdict = pass_owner_walk` (`0.99`, confidence `0.98`), `more_rounds = 0` (`score = 0.10`, `P(0) = 0.91`), `scored_s1_s6_pass = 0.98` (`12/12`), `h1 = 0.97`, `h2 = 0.96`, `h3 = 0.93`, `h4 = 0.93`, `h5 = 0.83`, `h6 = 0.94`, `h8 = 0.92`, `track_a = 0.85`, `track_b = 0.91`, `track_c = 0.76` (pre-Track-C commit).

## Scored Notion Rubric (`S1`–`S6`)

| Criterion | Score | Evidence |
| --- | --- | --- |
| `S1` Architecture Boundaries (ADR-010/007/001) | `2/2` | `C2CommsLegendTooltipBinder` pure presentation, `UiIaCommsTriadOracleTests` passing, `0` lines on `DelegationBridge.cs`/`SimulationSession.cs` |
| `S2` Determinism & Goldens (`VER-04`) | `2/2` | `ReplayGolden` `17/17`, hash `17144800277401907079` (`26` hits), multi-run seed invariance verified |
| `S3` TDD Rigor (`C8`/`H5`) | `2/2` | Verbatim RED->GREEN across M1/M2/M3/Track B headless & PlayMode in `s6-c8-red-evidence.md` |
| `S4` Live Unity `:8081` PlayMode | `2/2` | `66/66` PlayMode (`5/5` `Sprint6SliceG2PlayModeTests`), `0` Console errors, visually inspected `s6-g2-playmode-c2-live.png`, clean Edit Mode |
| `S5` Honest Evidence & Tracker Hygiene (`H2`/`H6`) | `2/2` | Explicit Headless/Proxy/Fixture/EditorPlayMode/Owner-Only separation, `DRG-208` historical boundary, 12 Linear comments (`Backlog`), 5 `hold:owner-go` untouched |
| `S6` Owner Acceptance Pack Completeness (`DRG-244`) | `2/2` | `s6-drg244-owner-acceptance-pack.md` indexes all 6 sprints, SHAs, floors, catalog SHA-256, screenshots, Jev IDs, and `pr-finish.md` PASS |
| **Total** | **`12 / 12`** | Floor `>= 9/12` exceeded |
- **Round 3 (Post Track C Commit `08eb237c`)**: `gen-dec-1795461056-Wacy1H5DKYvD16hdMEXe` — **`sprint6_verdict = pass_owner_walk` (`P = 1.0`, confidence `1.0`)**, **`more_rounds = 0` (`score = 0.08`, `P(0) = 0.93`, confidence `0.88`)**, `scored_s1_s6_pass = 0.98`, `h1 = 0.97`, `h2 = 0.95`, `h3 = 0.93`, `h4 = 0.91`, `h5 = 0.86`, `h6 = 0.95`, `h8 = 0.92`, `track_a = 0.87`, `track_b = 0.91`, `track_c = 0.75` (owner walk pending).
