# Slice B — DRG-165–170 implementation and verification

Implementation is present in the working tree on base `0cd321c1`. The owner explicitly cleared the DRG-208 prerequisite in this task: “I give my signoff proceed with DRG-165–170”. No commit, push, issue transition or new visual acceptance is implied by this report.

## Delivered behavior

| Story | Implementation | Evidence |
| --- | --- | --- |
| DRG-165 | Simulation-authored target, family, salvo and fire-control evidence; immutable chronological combat lifecycle projection; stable order-log correlation; causal outcome matching and exact policy refusal deduplication | `CombatEventLogProjectionTests`, `EngagementOrderLogContractTests` |
| DRG-166 | Six synthetic actual-resolver legs: permitted/refused Missile, Gun and Laser; deterministic combined log/events/map poses; three independently loadable scenario policies | `SliceBCombatScenarioTests`, `CombatMapIntegrationTests`, scenario family binding and loader tests |
| DRG-167 | Distinct glyph, line pattern and textual motion/family semantics; affiliation, clearance and outcome labels; bounded effects; inspection through map labels and paginated event history | `CombatMapPresenterTests`; offline compiled `CombatMapView` and `CombatMapViewTests` |
| DRG-168 | Shared event identity across map/history/explanation; known constraints, policy, fire-control and salvo evidence; corrective next action; missing evidence remains UNKNOWN | `CombatDetailPresenterTests`, `CombatSelectionRegressionTests` |
| DRG-169 | Existing Slice A contact quality/provenance/freshness/targetability preserved; separate engagement and posture fields; BDA requires exact contact/target association, including assessment without an engagement | Contact panel integration; observer-isolation regression; a terminal kill alone never fabricates BDA |
| DRG-170 | Tactical/Operational/Theater density; selected engagement retained; aggregate inspection resolves a member; no false cross-location theater trajectory; replay cutoff includes fractional contact-frame time | Map, integration and selection regression tests |

The live map consumes `CombatPresentationFrame`, with explicit contact-id to target-id pose aliases. Inspection is presentation state on `C2PresentationController` and cannot replace friendly command selection. History holds at most 200 reusable buttons per page; effects reuse at most 64 slots with at most 24 line segments each. Projection rebuilds happen on a changed frame, inspection or density selection, not every render frame.

Scenario inputs are explicitly synthetic and are not production catalog or OSINT claims. `data/scenarios/slice-b-combat-acceptance.json` drives the six-leg headless fixture. `slice-b-missile.policy.json`, `slice-b-gun.policy.json` and `slice-b-laser.policy.json` exercise normal scenario loading. Family metadata does not implement new weapon physics; unknown legacy families stay unknown. Motion labels describe the displayed event semantics, not a newly simulated projectile trajectory.

## Executed checks

All output was read, including individual native-command outcomes inside the local CI script.

| Check | Result |
| --- | --- |
| `dotnet build ProjectAegis.sln` | 0 warnings, 0 errors |
| Final `dotnet test ProjectAegis.sln --no-build -v minimal` after build | 3,149 passed, 0 failed, 0 skipped; Data 771, Sim 583, Delegation 1,102, Adapter 554, CLI 115, Excel 24 |
| `tools/verify-ci-local.ps1` | PASS: Release build 0 warnings/errors; 3,148 tests before the final added performance test; catalog import 67/67; ReplayGolden 6/6; PlayModeSmokeHarness 24/24 |
| Final added performance test, Release | 1/1 passed, bringing tested Release coverage to 3,149; p95 9.432 ms, max 11.144 ms for 1,200 legs, n=20 after 3 warmups |
| Debug smoke + replay + old/new bind benches | 32/32; combat p95 7.599 ms, max 11.973 ms; existing rich C2 max 0.005 ms; budget <100 ms |
| Offline Unity runtime compile | Successful using installed Unity 6000.3.22f1 Roslyn and cached complete runtime response file plus new source |
| Offline Unity tests compile | Successful, including new history-pool regression; tests were not executed in Editor |
| Invariants | `DelegationBridge.cs`, CatalogWriteGate and golden files unchanged; Baltic v2 hash `17144800277401907079` still present; no scene/prefab/meta YAML edits |
| Diff hygiene | `git diff --check` clean; Git reports only existing CRLF normalization notices |

Offline Unity runtime compilation reports two existing warnings outside this change: `CombatDomainsHotTickHost.cs:147` CS8604 and `GlobeMapProductHost.cs:50` CS8618. Test compilation reports four existing CS8600 warnings in `MapSymbolPoolTests.cs`. No new warnings remain. The .NET solution build is warning-free.

Adapter dependencies were rebuilt in Release, published for **netstandard2.1**, and copied into the ignored `unity/ProjectAegis/Assets/Plugins/ProjectAegis` directory for compilation. No net8.0 plugin DLLs are included in the changeset. No assemblies or asmdefs were added.

Independent review found and verified corrections for map contact identity, aggregate inspection, fractional replay cutoff, policy denial deduplication, ROE reason classification, paused UI rebind, pooling, and independent scenario-family provenance. The last binding regression failed before the correction and passed afterward. GitNexus impact was run before existing-symbol changes; its attempted index refresh hit a local Ladybug WAL assertion, so impact results came from the existing graph.

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** [pr-finish.md](../agentic/skills/unity-csharp-architect/checklists/pr-finish.md)

**Skill:** [unity-csharp-architect](../agentic/skills/unity-csharp-architect/SKILL.md)

**ADRs:** [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md), [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md), [ADR-001](../../docs/architecture/adr-001-sim-assembly-boundary.md)

**Verdict:** BLOCKED

The implementation and headless checks are complete. Last-mile Unity visual/lifecycle acceptance remains unavailable: the final `localhost:8080` probe failed with connection refused/HTTP 000. The prior owner signoff authorizes this work; it is not evidence that these new visuals have been exercised.

- §2.1 Presentation: checked. Read-only frame/projection inputs; no UI writes to world truth or logs; inspection state is local presentation state; missing evidence fails closed.
- §2.2 Commands: checked/N/A. New controls only inspect history or choose display density. They issue no authority command and do not enter replay hashes. Residual risk is visual selection clarity, covered headlessly and pending Editor inspection.
- §2.3 Assemblies: checked. Existing assembly boundaries; no UnityEngine dependency in core/adapter; DelegationBridge zero-touch. New assembly edge list N/A.
- §2.4 Hosts/DI/allocation: checked headlessly and by source review. Thin existing hosts; no added Find/Resources lookup or singleton; cached dirty binds and pooled UI elements; numeric bind budget passes. Actual UI Toolkit frame cost remains unprofiled.
- §2.5 Editor authoring: N/A; no EditorWindow, authoring or scene changes.
- §2.6 Tests: pure logic and integration checks pass in the matching headless projects; Unity pool/lifecycle test compiles but is not run. **Outstanding:** Editor execution, Console inspection and Game View evidence.
- §2.7 Hygiene: correct presentation ADRs and checklist linked; plugin refresh documented; no structural waiver required. No PR was requested or created; this block is ready to reuse in its body.

## Remaining visual acceptance

When the project Editor/MCP is available, exercise the new runtime in the existing smoke map with each `slice-b-*` policy and permitted/refused fixture cases. Verify all three densities, non-color family recognition, selected-event detail/history agreement, contact BDA/UNKNOWN handling, keyboard inspection, pagination beyond 200 facts, and document recreation while paused. Run `CombatMapViewTests`, inspect Console logs and capture Game View evidence. Do not mark the visual sprint acceptance complete until this evidence exists.
