# Unified Sprint 1 Definition of Done Rubric (Jev-Gated)

**Sprint:** Sprint 1 — Architecture Hygiene, Package Sync & Immediate UI Wiring (`sprint/s1-hygiene-wiring`)
**Jev Launch Decision:** `gen-dec-1795367423-dEFLndsX8qZVYPvUpX4Y` (2026-11-22, `typesafe/jev-1.14`)
**Unified Standard:** Combines live Notion **Project Aegis Rubrics (DRAFT v1)** (`H1`–`H8` + `S1`–`S6 ≥ 9/12`) with Play Mode UAT criteria (`C1`–`C13`).
**Pass Rule:** Every mechanical Hard Gate passes, every non-owner Jev `noul ≥ 0.80`, Scored `S1`–`S6 ≥ 9/12`, and `more_rounds = 0`. **Script-first:** Jev never overrides a failed mechanical gate.

## Scope (7 Non-Held Linear Issues — WIP ≤ 4, 3 Disjoint TDD Tracks)

| Track | Issues | Surface |
|---|---|---|
| Track A (Arch Hygiene) | `DRG-772`, `DRG-773` | `MissionEditor.Cli`, `Ai.Host`, `Delegation.UnityAdapter/Presentation/`, `C2TopBarPanelHost` |
| Track B (UI Wiring & PlayMode) | `DRG-370`, `DRG-351`, `DRG-717` | `RightUnitPanelHost`, `C2TopBarPanelHost` / briefing, UITK tooltips, `Assets/Tests/` |
| Track C (Fixtures & Package Hygiene) | `DRG-239`, `DRG-781` | `data/scenarios/`, `Delegation.UnityAdapter`, `unity/ProjectAegis/Packages/`, `.meta` hygiene |

## Hard Gates (All Must Be YES)

| ID | Gate | Mechanical Check | Owner-Only |
|---|---|---|---|
| H1 / C1 | Zero-warning build & full headless suite green | `dotnet build ProjectAegis.sln` (0 warnings, 0 errors); `dotnet test ProjectAegis.sln` (0 failures, ≥ 4,671) | no |
| H2 | Tracker honesty | Linear evidence comments posted; Linear status NEVER moved to Done before owner PR merge | no |
| H3 / C7 | Hard-no surfaces untouched | 0 diff on `DelegationBridge.cs` hotpath, `CatalogWriteGate`, `SimulationSession`, `MapPlaceholderPanelHost`, `GlobeMapProductHost`, replay goldens | no |
| H4 / C2 | Determinism preserved | `ReplayGolden` 17/17 pass; Baltic v2 hash `17144800277401907079` unchanged | no |
| H5 / C8 | Strict TDD RED→GREEN + source-fence tests | Every fix/feature has recorded pre-fix RED failure output + passing GREEN + ADR-010/007/001 fence tests | no |
| H6 / C10 | Owner Play Mode walk preserved | Agent supplies fresh Play Mode MCP screenshots + evidence; owner sign-off remains owner-only | **yes** |
| H8 / C3 / C12 | Smoke floor & shipped data integrity | `PlayModeSmokeHarnessTests` ≥ 27/27; `sha256sum assets/data/catalog/*.db` byte-identical | no |
| C4 / C13 | Unity Console & PlayMode Runner | 0 Unity Console `Error`/`Exception` entries across Play Mode scenarios; `ProjectAegis.Unity.Tests` PlayMode 100% pass (0 failures) | no |
| C9 / C11 | Live Unity MCP interactive verification | Every new/wired UI control exercised via Unity MCP `UatHarness.cs` / `script-execute` with observable state diff + screenshots | no |

## Scored Criteria (0 = missing, 1 = partial, 2 = complete; Pass ≥ 9 / 12)

- **S1**: Clean diff & zero review/compiler warnings.
- **S2**: Any deferred follow-ups explicitly ticketed with owner & Surface.
- **S3**: ADR / waiver / doc status updated accurately (`ADR-010`, `ADR-030`, `ADR-027`).
- **S4**: Linear evidence comments include branch, test counts, replay hash, and Play Mode evidence.
- **S5**: Scope honesty — no claim exceeds verified execution.
- **S6**: Test quality — exercises production composition & real edge cases, not stubs.

## Round Log

| Round | Date | Met | Not Met | Jev Scores | Notes |
|---|---|---|---|---|---|
| 0 | 2026-11-22 | Baseline | Sprint 1 pending | Launch decision `gen-dec-1795367423-dEFLndsX8qZVYPvUpX4Y` | 3 parallel TDD tracks approved (`0.99`), checkpoint after Sprint 1 (`0.83`), branch `sprint/s1-hygiene-wiring` (`0.87`) |
| 1 | 2026-11-22 | H1/C1, H3/H4, C4/C13, C9/C11, C12, H2/H6 | H5/C8 (0.47 — RED snippets omitted from payload), S1–S6 (0.78) | `gen-dec-1795368544-OxhxZyq3C44jjIHSHzZw`: `h1_c1 0.97, h3_h4 0.97, h5_c8 0.47, c4_c13 0.97, c9_c11 0.89, c12 0.96, h2_h6 0.97, s1_s6 0.78; more_rounds 0 (0.81); verdict pass_checkpoint_owner (0.91)` | Initial gate run before writing `s1-c8-red-evidence.md` and quoting posted Linear comment IDs |
| 2 | 2026-11-22 | **ALL 7 Hard & PlayMode Gates MET** (`h1_c1 0.98`, `h3_h4 0.98`, `h5_c8 0.87`, `c4_c13 0.98`, `c9_c11 0.93`, `c12 0.96`, `h2_h6 0.96`) | S1–S6 (`0.71` — PR URLs/review threads pending owner commit/submit); H6/C10 (owner-only walk) | `gen-dec-1795368602-eR7KhnCl9v7ipbuXT2lI`: `more_rounds 0 (0.96, score 0.05); sprint1_verdict pass_checkpoint_owner (0.99)` | **SPRINT 1 AGENT DoD MET.** Persisted `s1-c8-red-evidence.md`, posted all 7 Linear evidence comments (`H2` honored), 4,706/0 headless, 27/27 Unity PlayMode, 0 console errors, `s1-ui-wiring-playmode.png`. Ready for owner checkpoint & Sprint 2 GO |
