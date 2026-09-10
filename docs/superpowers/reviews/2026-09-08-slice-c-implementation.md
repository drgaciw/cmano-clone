# Slice C implementation and verification

**Play Mode follow-up:** [Acceptance evidence, 2026-09-08](2026-09-08-slice-c-playmode-acceptance.md) now records actual Unity execution, six passing Play Mode tests, screenshots and a layout correction. The original Editor-unavailable verdict below is historical. Technical smoke passed; owner acceptance remains pending, with the live group-scenario limitation documented in the follow-up. The implementation was committed as `37e6ba6d` after this original report.

Scope: DRG-171–175, DRG-191, DRG-192 and DRG-194 under [DRG-185](https://linear.app/drgamtd-workspace/issue/DRG-185). Implementation follows the [Notion requirements](https://app.notion.com/p/3c1f7cb4e4df810c96b0ea6e32cc6933) and its Agentic, ASBS and IBCS requirements. Existing completed headless foundations DRG-212/217/218/221/223/227/229 remain the domain contracts. Base: `b92b0b3d`; baseline 3,149 passing solution tests.

## Delivered behavior

| Story | Implementation | Behavioral evidence |
| --- | --- | --- |
| DRG-171 | Combat timeline filters platform, target, weapon family and outcome; exact event selection drives shared map/detail; return to live; bounded historical geometry archive | `CommandReviewTimelineTests`: filtering, frozen history, eviction, rewind, same-tick future-evidence exclusion, execution-detail phase cutoff |
| DRG-172 | Selected-contact advice shows confidence, assumptions, range/policy constraints, source evidence and authority separately; existing engagement controls remain the explicit fire path | `AdviceBridgeTests`: identity/time mismatch, stale links, withheld authority, absent model, false provider result, future contact evidence |
| DRG-173 | Provenance/confidence/freshness, emitter state and EW use current snapshot/log facts with textual UNKNOWN fallback | `StatusFrameTests`: per-unit comms, typed evidence, stale/future evidence, EW, emissions |
| DRG-174 | Platform, sensors, mounts, comms, mobility, readiness and recovery have separate knowledge states; damage/comms rows open related target-filtered combat history | `StatusFrameTests`: zero HP is OFFLINE, unknown components/recovery stay unknown, bounded source-sequenced timeline |
| DRG-175 | Explicit group/package membership, role responsibilities, split/lost/stale gaps; deliberate human hold/withdraw through existing command facade, exact member scope revalidated at execution | `CoordinationBridgeTests`, `CoordinationCommandBridgeTests`: delayed execution, detached/lost scope, replay refusal, current intent constraints, no implicit reattack |
| DRG-191 | Preferred, nonpreferred and excluded candidates expose effect, time, availability, commitment, conservation and total scores plus reasons | `AdviceBridgeTests`: preferred row retained, exclusion/commitment rationale and score dimensions; absent inputs are disclosed |
| DRG-192 | Supplied coverage polygons scale with map geometry; covered outlines and dashed GAP outlines have text labels; unknown geometry stays absent; clutter toggle and bounded pools | `CoordinationBridgeTests`, `CoverageMapViewTests`: source validation, loss/stale coverage, hidden history, polygon and segment limits |
| DRG-194 | Three discoverable read-only skills assess datalink, recommend resources and explain package evidence through existing auditable skill envelopes | `AdviceSkillServiceTests`: capability catalog, read lane, deterministic invocation provenance, no commands |

The deterministic acceptance fixture exercises review and advice, delayed human hold, two concrete unit effects, log-derived BDA/timeline inspection, and an explicit BDA-informed withdrawal decision with delayed unit effects. Fixture combat facts are deterministic test inputs; no visual evidence or live combat run is implied.

## Runtime composition and limits

`DelegationBridgeHost` builds status, advice and coordination once per simulation tick. Contact selection refreshes advice while paused. `CommandReviewView` reuses controls and clearly labels current decision support during historical inspection; group decisions are disabled during review. Coverage is hidden during historical review because it is not archived. At a nonfinal event within a tick, map/BDA use the preceding retained tick because end-of-tick snapshots cannot establish intra-tick ordering. If retained geometry is unavailable, the UI states that rather than substituting live positions.

Snapshot integrations can implement `IAdviceEvidenceSource` for exact current weapon/ranking projections, `ICoordinationFacts` for authored group/package assignments, intent and spatial coverage, and `IStatusUnitSource` / `IStatusSensorSource` / `IStatusElectronicWarfareSource` for typed component facts. Existing snapshots still expose current contact, chain, authority, tracked magazine and log status facts. They do not currently publish all precise weapon/range/ranking or spatial coverage inputs; these remain UNKNOWN. Scenario preview defaults are not relabeled as current measured facts. Group reattack remains advisory because the supplied authority projection does not establish actor/target/time-bound execution permission.

Advice and coverage do not write orders or simulation truth. Human group hold/withdraw uses `C2PlayerCommandBridge`; pending approved scopes survive communication delay, and the downstream adapter validates current group control and exact attached/live members. No changes to `DelegationBridge.cs`, `SimulationSession`, CatalogWriteGate, scene/prefab/meta files, or golden fixtures.

## Verification

All required commands were run and their outputs inspected after integration:

| Gate | Result |
| --- | --- |
| `dotnet build ProjectAegis.sln` | PASS — 0 errors, 0 warnings |
| `dotnet test ProjectAegis.sln -v minimal` | PASS — 3,216 tests, 0 failures; 67 above the fresh baseline |
| PlayModeSmokeHarnessTests, Debug | PASS — 24/24 |
| ReplayGoldenSuiteTests, Debug | PASS — 6/6 |
| `tools/verify-ci-local.ps1` | PASS — catalog import 67/67; Release build 0 errors/warnings; Release full suite 3,216/3,216; replay 6/6; smoke 24/24 |
| C2PanelPerfBenchTests and new 500-unit command-review dashboard budget | PASS — 100 ms maximum bind budget enforced |
| Unity 6000.3.22f1 offline runtime + test compilation | PASS — no errors; only the six pre-existing warnings listed below |
| Diff, new-file whitespace and local report links | PASS |
| Protected source paths and golden fixtures | PASS — untouched; Baltic v2 hash `17144800277401907079` preserved |

Solution totals: Sim 583, Delegation 1,102, MissionEditor CLI 115, Data.Excel 24, UnityAdapter 621, Data 771. The final independent code review confirmed all earlier findings resolved and found no new material regression in the integrated host/map/view code.

Unity MCP probe returned connection refused / HTTP 000 at `localhost:8080`. Per the project routing rule, no Editor was launched. Unity runtime and test sources compile with the installed 6000.3.22f1 compiler; this does not execute Unity tests or establish Play Mode visual acceptance. Existing warnings are CS8604 in CombatDomainsHotTickHost, CS8618 in GlobeMapProductHost, and four CS8600 warnings in MapSymbolPoolTests.

The adapter's public surface was published for **netstandard2.1**, then DLLs copied to the ignored `unity/ProjectAegis/Assets/Plugins/ProjectAegis` directory. Reproduce with `dotnet build src/ProjectAegis.Delegation.UnityAdapter -c Release`, then `dotnet publish` for `netstandard2.1` to a temporary directory and copy its DLLs. No net8.0 DLLs are committed as Unity plugins.

GitNexus review used context and upstream impact before existing-symbol changes. Host/map/explanation/selection seams had LOW impact; selection had two direct callers and one flow. Reanalysis failed with Ladybug WAL/FTS errors; source review covered new/unindexed symbols. Independent review findings on historical evidence, per-unit comms, zero-HP status, stale coverage, future log rows, provider results and intent/execution separation were corrected with regressions. No commits or remote tracker changes were made.

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** [pr-finish.md](../../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md)

**Skill:** [unity-csharp-architect](../../../production/agentic/skills/unity-csharp-architect/SKILL.md)

**ADRs:** [ADR-010 §2–3](../../architecture/adr-010-headless-first-command-driven-ui.md), [ADR-007](../../architecture/adr-007-c2-map-presentation.md), [ADR-001](../../architecture/adr-001-sim-assembly-boundary.md)

**Verdict:** BLOCKED — new Play Mode visual/lifecycle acceptance remains unavailable while Editor MCP is down. Headless implementation and final gate results are separate from that acceptance.

**Evidence:** Snapshot/bridge presentation reads; explicit human facade writes; existing assembly boundaries; DelegationBridge untouched; cached dirty/tick binds and bounded pools; headless command/performance tests; offline Unity compile; netstandard2.1 plugin refresh.

**N/A:** No new asmdef, Editor authoring, catalog write path, scene asset mutation, or structural waiver. Pure advice, history and coverage need no command because they only change presentation.

When Editor MCP is available, inspect the command-review foldout at normal and crowded map scales; select early and terminal timeline rows; verify current-vs-history labels, map/explanation identity, coverage labels/dashes and toggles; exercise human group hold/withdraw and prevented reattack; capture Console and Game View evidence. Run the new Unity tests and existing Play Mode smoke before recording visual acceptance.
