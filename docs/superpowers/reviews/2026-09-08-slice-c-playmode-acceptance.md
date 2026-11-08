# Slice C — Play Mode acceptance evidence

**Technical smoke result: PASS. Owner acceptance: pending review of this package.**

Scope: DRG-171–175, DRG-191, DRG-192 and DRG-194; implementation commit `37e6ba6d` plus the seven-line `CommandReviewView` layout correction described below. [DRG-185](https://linear.app/drgamtd-workspace/issue/DRG-185/epic-slice-c-command-learn-adapt) remains unchanged in Linear. This package replaces the earlier lack of Editor evidence; it does not record a human signoff or claim a full group mission was played in Unity.

## Actual Editor execution

Unity **6000.3.22f1**, Windows, `Assets/Scenes/DelegationSmoke.unity`, scenario `baltic-patrol-classify`, 2026-09-08. The Editor was initially closed and was launched for this task. MCP ping returned `pong` at `http://localhost:8080`; its root HTTP 400 is a missing-session response, not an outage. Installed MCP package was 0.90.0. No package upgrade was performed.

The real scenario ran until combat/BDA and stale contact evidence existed. UI checks used existing UI Toolkit click callbacks and presentation selection, with the simulation clock paused for inspection. They were automated callback checks, not hardware mouse-input tests. No MCP script mutated `DelegationBridge`, appended simulation logs, or issued orders through a direct sink.

| Check | Observed result |
| --- | --- |
| Historical review | Selecting `t=52.417`, hostile-1 → u1, `AuthorizationRefused: TARGET_DESTROYED` produced `REVIEW t=52.417`, disabled command-review decision controls, and disclosed unavailable historical map evidence instead of showing live positions. |
| Return to live | Normal button callback restored `LIVE` and enabled command-review decision controls. |
| Filters | An unmatched outcome returned 0 events; clearing it restored 242 events. |
| Selected-contact advice | Contact `c1` displayed `Stale`, UNKNOWN confidence, refresh guidance and explicit advisory/no-authority wording. |
| Read-only skills | Assess datalink, Recommend resources and Explain mission package each returned its distinct skill ID and stale/no-grounded-recommendation result. |
| Missing group | Hold, Withdraw and reattack review were all prevented with `UNKNOWN_GROUP`. |
| Coverage visibility | Toggle changed the host's presentation flag false/true. Live scene correctly reported no usable authored spatial evidence. |
| Coverage visual fixture | Supplied immutable geometry rendered two polygons and 20 segments, with solid green COVERED and dashed orange GAP outlines and text labels at 1600×979 and 1280×779 render resolutions. Fixture was explicitly labeled, disposed and removed. |
| Layout | Expanded review content uses a shrinking, bounded scroll viewport. Controls and sections remained reachable by scrolling at both sizes. |
| Lifecycle | Tests below verified pooled controls, history suppression, disposal and document recreation while paused. |

### Screenshots

Historical review: blank historical map with explicit unavailable-evidence labels; command-review decisions disabled. Existing unit-detail controls continue to represent current unit state.

![Historical review](slice-c-playmode-2026-09-08/historical-review.png)

Live selected-contact advice: stale evidence, UNKNOWN confidence and authority, with the advisory boundary visible.

![Live advice](slice-c-playmode-2026-09-08/live-advice.png)

Coverage **presentation fixture**, not live scenario coverage:

![Wide coverage fixture](slice-c-playmode-2026-09-08/coverage-fixture-wide.png)

![Compact coverage fixture](slice-c-playmode-2026-09-08/coverage-fixture-compact.png)

The exact temporary fixture input is preserved in [coverage-presentation-fixture.json](slice-c-playmode-2026-09-08/coverage-presentation-fixture.json). It only constructs a view and immutable projection DTOs. It is not a scenario asset or a live coverage provider.

## Executed tests

All six tests below actually executed inside Unity **PlayMode**, after the layout correction. MCP's `TotalTests: 15` includes discovery tree nodes; the actual passing leaf-test counts are 1 + 4 + 1.

| Unity class | Passed | Raw result |
| --- | ---: | --- |
| `CommandReviewViewTests` | 1 | [Result](slice-c-playmode-2026-09-08/command-review-tests.json) |
| `CoverageMapViewTests` | 4 | [Result](slice-c-playmode-2026-09-08/coverage-tests.json) |
| `CombatMapViewTests` | 1 | [Result](slice-c-playmode-2026-09-08/combat-map-tests.json) |

Reproduction uses the simple class name, without a namespace filter:

```powershell
unity-mcp-cli run-tool tests-run --url http://localhost:8080 --input '{"testMode":"PlayMode","testClass":"CommandReviewViewTests","includePassingTests":true,"includeLogs":true,"includeStacktrace":true}' --raw --timeout 120000
```

Repeat for the other two classes. Initial fully-qualified/namespace discovery attempts found no tests; those attempts were corrected and are not counted as passing execution.

Headless gates were rerun after the layout correction: Debug build **0 warnings / 0 errors**, full suite **3,216 passed**, PlayModeSmokeHarness **24/24**. `tools/verify-ci-local.ps1` also passed: catalog import **67/67**, Release build **0 warnings / 0 errors**, full suite **3,216/3,216**, replay goldens **6/6**, smoke **24/24**. Protected bridge/write-gate/golden paths are unchanged; Baltic v2 hash `17144800277401907079` remains present.

## Findings and cleanup

- Fixed review-panel overflow pressure by allowing the root, content container and scroll body to shrink, with a 45% root height cap and a zero minimum content height. No command, projection or simulation behavior changed. GitNexus upstream impact could not resolve the new/unindexed view; source inspection limited the change to constructor styles. Final detect-changes reported LOW, one source file and no indexed affected flows; the known stale-index limitation still applies.
- Final live interaction checks returned no Error entries for the preceding three minutes and no Exception entries for the preceding twenty minutes. The entire Editor session was **not** error-free: Unity AI's `DragAndDropCache` emitted a corrupt/empty cache error and persistent-object errors during domain reloads. The incorrect initial test filter also produced an MCP diagnostic. These were tooling diagnostics, not Slice C runtime exceptions.
- The dependency resolver reintroduced `UNITY_MCP_READY` into player defines, causing one structural test failure during the first full suite. Cleanup through the Editor API was re-applied by the resolver. After closing the Editor started for this task, the two resolver-only changes (`ProjectSettings.asset` and the Unsafe DLL importer metadata) were restored exactly from HEAD. The subsequent full suite passed. This remains an Editor-startup hygiene issue; no invariant was weakened.
- Unity generated six missing `.cs.meta` files for the combat/review/coverage views and their tests. They remain available as generated metadata; no scene, prefab or metadata YAML was hand-authored. Scene status after testing was saved/non-dirty, with the original 13 roots. Play Mode was stopped and the task-launched Editor closed.
- The layout correction, generated metadata and this evidence are uncommitted. No push, tracker completion or Notion mutation was performed.

## Acceptance boundary

The smoke scene has no configured task group and no authored coverage geometry. Consequently, this run proves safe missing-group behavior and coverage rendering with a separate presentation fixture. It does **not** prove a live Unity group Hold → BDA → Withdraw scenario. The real delayed command effects and assessment-driven retasking remain covered by `CoordinationEndToEndAcceptanceTests` in the 3,216-test headless suite; its combat facts are deterministic fixture inputs.

DRG-185's exit gate calls for review, recommendation, human decision, delegated group response and assessment-based retasking. Owner acceptance should consider the combined headless and Play Mode evidence with this limitation explicit. A requirement for that entire positive group flow to be shown in one live Unity scenario would remain additional acceptance work. No owner signoff is inferred from automated tests.

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** [pr-finish.md](../../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md)

**Skill:** [unity-csharp-architect](../../../production/agentic/skills/unity-csharp-architect/SKILL.md)

**ADRs:** [ADR-010 §2–3](../../architecture/adr-010-headless-first-command-driven-ui.md), [ADR-007](../../architecture/adr-007-c2-map-presentation.md), [ADR-001](../../architecture/adr-001-sim-assembly-boundary.md)

**Verdict: PASS** — architecture and scoped technical Play Mode smoke. Owner acceptance is separate and pending.

**Evidence:** Reviewed applicable checklist sections 2.1–2.4 and 2.6–2.7. The follow-up change contains constructor layout styles only; no new authority path, assembly edge, hot-path allocation or mutable simulation state. Existing explicit facade command paths remain covered headlessly. Unity lifecycle and rendering evidence is now available. Protected source and replay fixtures remain untouched.

**N/A:** Section 2.5 authoring changes, new asmdefs, catalog/SQLite, additional plugin API refresh and structural waivers. The layout change is pure presentation and issues no command; residual risk is visual sizing, exercised at the two captured resolutions.
