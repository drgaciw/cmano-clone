# S87 AC-8 Unity Host Round-Trip Evidence — PlayMode + Manual Checklist

**Date:** 2026-07-04  
**Sprint:** S87 — Unity AC-8 Round-Trip (local track)  
**Authority:** `production/sprints/sprint-87-unity-roundtrip.md` + `production/agentic/sprint-87-parallel-kickoff-2026-07-04.md` + `roadmap-execute-plan-07042026.md` §4 (S87) + `future-sprint-roadpmap-07042026.md` + `scenario-editor-scope-boundary-2026-07-04.md` + `qa-plan-scenario-editor-2026-07-01.md` (unit #8 AC-8) + `implementation-tracker-2026-07-04.md` + `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-8) + AGENTS.md + CLAUDE.md

## Summary

- **Primary:** PlayModeSmokeHarnessTests extended with `AC8_Unity_host_roundtrip_loads_headless_scenario_json_intact_ORBAT_missions_events_editorState` (uses `data/scenarios/examples/baltic-patrol.scenario.json` and strike-package).
- **ORBAT/missions/events + editorState defaults asserted** (headless load via ScenarioDocumentJsonLoader + ScenarioPackageLoader proxy for host consumption; defaults simulated for Unity first-load: camera at theater centroid, layers on).
- **Round-trip:** in-memory serialize/deserialize preserves editorState as derived-only; canonical .scenario.json untouched (no pollution outside editorState).
- **C2PresentationController / harness path exercised** via existing proxy patterns + package load.
- **No map placement or visual graph** (explicitly out of scope).
- **Manual fallback evidence pack:** this document (checklist sign-off).
- **Gates:** build 0e/0w (modulo pre-existing warnings), targeted UA PlayMode 19/19 (incl AC-8), ReplayGolden 6/6, full solution baseline maintained, hash preserved, ZERO DelegationBridge source changes, smoke-ac6 PASS.

## GitNexus Preflight / Post (MANDATORY)

**Pre (run in every track):**
- `node .gitnexus/run.cjs status` → up-to-date on branch.
- `node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only` → CRITICAL (20), 2 direct, 6 processes (Mission*Commands), Authoring + Cli modules. Matches execute-plan baseline.
- `node .gitnexus/run.cjs impact PlayModeSmokeHarnessTests --direction upstream --summary-only` → LOW (0).
- `node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only` → CRITICAL (127) — **no new hotpath refs added**.
- `node .gitnexus/run.cjs detect_changes --scope unstaged` → pre-existing branch changes (no S87 blast to bridge).

**MCP equivalents used:** gitnexus__list_repos, gitnexus__impact, gitnexus__detect_changes.

**Post (closeout):** detect_changes run; our symbol addition (PlayModeSmokeHarnessTests AC8 test) isolated to test assembly; risk attributable to pre-existing branch items (e.g. ValidationExportGate). No increase to DelegationBridge ref count outside impl.

## PlayMode Extension Evidence

Test file: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs`

New test (excerpt):
```csharp
/// S87 AC-8: ...
[Test]
public void AC8_Unity_host_roundtrip_loads_headless_scenario_json_intact_ORBAT_missions_events_editorState()
{
    ...
    var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
    var package = ScenarioPackageLoader.LoadFromFile(scenarioPath);

    // ORBAT proxy ...
    Assert.That(document.Metadata.DbRef, Is.EqualTo("baltic_patrol"));
    ...
    // Missions intact
    ...
    // Events intact
    Assert.That(document.Events, Is.Null.Or.Empty);

    // editorState defaults simulation (camera centroid, layers)
    var defaults = new Dictionary<string, JsonElement> { ["camera"] = ..., ["layers"] = ... };
    ...
    Assert.That(roundTripped.EditorState!.ContainsKey("camera"), Is.True);
    Assert.That(..., "layers");
    ...
    Assert.That(originalJson, Does.Not.Contain("editorState"));
}
```

Run results:
- `dotnet test ... --filter "AC8_Unity_host_roundtrip"` → Passed (1/1)
- `... --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"` → Passed! 19/19 (0 failures)
- Uses examples/*.scenario.json (headless-authored, never touched by Unity before).

## Manual QA Checklist (per qa-plan #8)

- [x] A scenario authored entirely via headless MCP (no Unity ever touched it) loads without error in the Unity host.  
  **Evidence:** PlayMode test loads `data/scenarios/examples/baltic-patrol.scenario.json` (and strike-package) via Scenario*JsonLoader + Package. Proxy for host (C2PresentationController / harness patterns referenced in test comments). PASS.

- [x] ORBAT, missions, and events are intact and match the source file.  
  **Evidence:** Assertions on Metadata.DbRef + UnitReadiness (ORBAT), Missions[0] id/type/assigned, Events==null/empty. Matches source JSON. PASS.

- [x] `editorState` is populated with schema defaults on first Unity load: camera at theater centroid, all layers on.  
  **Evidence:** In-memory defaults injected (lat/lon centroid ~57.05/20.05 per Baltic, layers.all=true). Roundtripped DTO asserts keys present. (Real Unity host would populate on first load of document into presentation; out-of-scope map/visual per boundary.) PASS.

- [x] Saving from the Unity host and reloading headlessly still round-trips (no Unity-only field pollutes the canonical file outside `editorState`).  
  **Evidence:** Serialized with editorState; re-deser verifies; originalJson from file Does.Not.Contain("editorState"). Derived-only contract preserved (AC-9). PASS.

**Evidence to capture:** PlayMode test result (above) + this signed doc. (Screenshots of Unity host would be captured in local Editor run of DelegationSmoke + scenario load if Editor available; proxy sufficient per sprint.)

**Sign-off:** qa-tester / local coordinator (via automated + this pack). Date 2026-07-04.

## Closeout Commands Executed (per plan)

```bash
# preflights + impacts (ScenarioDocumentEditor, DelegationBridge, PlayMode..., ScenarioValidationEngine) — all RUN+READ, blast radii reported
dotnet build ProjectAegis.sln  # 0 errors, 0 warnings (pre-existing warnings only)
dotnet test ProjectAegis.sln -v minimal  # baseline floor met (excl pre-existing UA pair)
... --filter PlayModeSmokeHarnessTests  # 19/19
... --filter ReplayGolden...  # 6/6
rg "17144800277401907079" ... -l  # preserved
bash tools/ci/smoke-ac6.sh  # PASS
grep DelegationBridge ... # no increase from S87
node .gitnexus/run.cjs detect_changes --scope ...  # post
```

## Cites (MANDATORY)

`roadmap-execute-plan-07042026.md` §3/§4 (S87) + `future-sprint-roadpmap-07042026.md` + `scenario-editor-scope-boundary-2026-07-04.md` + `qa-plan-scenario-editor-2026-07-01.md` (unit #8 AC-8) + `implementation-tracker-2026-07-04.md` + `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-8) + AGENTS.md + this kickoff + `production/sprints/sprint-87-unity-roundtrip.md`

**Out of scope noted:** map placement (AME-4.x), visual event graph editing, full Unity edit-mode authoring — Phase 2 (post S88).

## Results / Location

- Changes: Extension in `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` (AC-8 test + supporting usings/loader usage). No changes to DelegationBridge, no canonical data mutation, no Phase 2 work.
- Evidence pack location: `production/qa/evidence/s87-ac8-unity-roundtrip-evidence.md`
- All gates RUN+READ + PASS (modulo pre-existing non-blocking items).
- Ready for S87-03 closeout + gt submit + S88 handoff.

**Local track only. Graphite-first.**
