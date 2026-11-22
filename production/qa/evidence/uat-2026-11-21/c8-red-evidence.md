# UAT C8 - runtime red evidence for D24 and D25

Evidence only; no production change. Each fix's test file was copied unchanged into a scratch
worktree at the fix commit's parent, together with a minimal stub of the new helper that
reproduces the OLD behaviour. Command (per worktree):

    export PATH="$HOME/.dotnet:$PATH"
    dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "FullyQualifiedName~<TestClass>"

Worktrees were removed afterwards.

## D24 - disable attack options when no unit is selected

- Fix commit: `1a289e233b6b6eeada03cf6a9de1eccb21ebc6d5`
- Parent: `abfbcd363403b0ed78bd0c0318b37d9568892556`
- Test class: `UnitDetailSelectionGateTests`
- Failing test: `Apply_with_no_selection_disables_every_attack_option_with_NO_SELECTION`
  (`Apply_with_selection_is_passthrough` passes on old behaviour, as expected)
- Result: `Failed: 1, Passed: 1, Total: 2`

Stub (`UnitDetailSelectionGate.cs`): old behaviour = primary-unit entry passed through unchanged.

    public static class UnitDetailSelectionGate {
        public const string NoSelectionReason = "NO_SELECTION";
        public static UnitDetailEntry? Apply(UnitDetailEntry? entry, string? selectedUnitId) => entry;
    }

Failure message:

    Assert.That(gated.AttackMenu, Has.All.Matches<EngageAttackOptions.AttackOption>(
        o => !o.Enabled && o.DisabledReason == UnitDetailSelectionGate.NoSelectionReason))
    Expected: all items value matching lambda expression
    But was:  < <AttackOption { Id = fire-single, Label = Fire single, Enabled = True, DisabledReason =  }>,
                <AttackOption { Id = hold-fire, Label = Hold fire, Enabled = True, DisabledReason =  }> >
    First non-matching item at index [0]: <AttackOption { Id = fire-single, ... Enabled = True ... }>
    at UnitDetailSelectionGateTests.Apply_with_no_selection_disables_every_attack_option_with_NO_SELECTION() line 24

Fidelity caveat: the pre-fix `C2PresentationController` returned `UnitDetailBridge.BuildPrimary(...)`
directly, i.e. an entry with Fire enabled, which is what the passthrough stub models. The test
exercises the gate contract only; it does not assert the controller call site. The wiring line in
`C2PresentationController` is not covered by this test.

## D25 - confirm successful toolbar orders with ISSUED status

- Fix commit: `47bcddaccf002c795f0687cf8f7f7d85fdd984b1`
- Parent: `1a289e233b6b6eeada03cf6a9de1eccb21ebc6d5`
- Test class: `OrderIssuedStatusTests`
- Failing tests: `FormatIssued_includes_command_and_unit`, `FormatIssued_without_unit_still_confirms`
- Result: `Failed: 2, Passed: 0, Total: 2`

Stub (`OrderIssuedStatus.cs`): old behaviour = `SetStatus(null)` shown no confirmation text.

    public static string FormatIssued(string commandId, string? unitId) => string.Empty;

Failure messages:

    Assert.That(OrderIssuedStatus.FormatIssued("hold", "u1"), Is.EqualTo("ISSUED: hold -> u1"))
    Expected string length 17 but was 0. Strings differ at index 0.
    Expected: "ISSUED: hold -> u1"   But was: <string.Empty>

    Assert.That(OrderIssuedStatus.FormatIssued("rtb", null), Is.EqualTo("ISSUED: rtb"))
    Expected string length 11 but was 0. Strings differ at index 0.
    Expected: "ISSUED: rtb"   But was: <string.Empty>

(The arrow in the real assertion is the Unicode character U+2192.)

Fidelity caveat: the old code had no formatter; it cleared the status in
`UnitOrderToolbarHost.OnOrderClicked`. The empty-string stub is a model of that, not a literal
pre-fix run. The host wiring (including set-after-Refresh ordering) lives in a Unity MonoBehaviour
that is not compiled headlessly and is not asserted by this test.

## D24 — controller-level red on real pre-fix code (round 6)

- Fix commit: `1a289e233b6b6eeada03cf6a9de1eccb21ebc6d5`
- Parent (tested): `abfbcd363403b0ed78bd0c0318b37d9568892556` (`1a289e23^`, detached worktree, removed afterwards)
- Test: `C2PresentationControllerUnitDetailSelectionTests.ResolveUnitDetail_after_hostile_contact_selected_disables_all_attack_options_with_NO_SELECTION`
  (`src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/`)
- Method: only the new test file was copied into the parent worktree; production code is unmodified
  pre-fix code. **No stub was used.** The test drives a real `C2PresentationController`
  (`SelectFriendlyUnit("u1")` then `SelectHostileContact`) and a real `DelegationBridge`, then calls
  `ResolveUnitDetail`, the call site `DelegationBridgeHost` uses to feed the right unit panel. It only
  uses APIs that exist at the parent.
- RED at parent: `Failed: 1, Passed: 0, Total: 1`

      Assert.That(afterContact.AttackMenu.Select(o => o.Enabled), Has.All.False)
      Expected: all items False
      But was:  < True, True, True >

  i.e. after selecting a hostile contact, the old controller still returned the primary-unit fallback
  with all three attack options (including Fire) enabled.
- GREEN on `uat/round5-jev-decisions`: `Passed: 1, Failed: 0, Total: 1`; full UnityAdapter test project
  results are recorded in the commit report.

## D25 — PlayMode red on real pre-fix toolbar code (round 6d, 2026-11-21 15:24)

- Test: `UnitOrderToolbarIssuedStatusPlayModeTests` (`unity/ProjectAegis/Assets/Tests/`, commit `ffeeac44`), 2 tests, run in the Unity Editor (6000.3.27f2) Test Runner through Unity-MCP `tests-run`, PlayMode.
- GREEN on `uat/round5-jev-decisions`: `Passed 2 / Failed 0`.
- Old behaviour restored: a patch reverting only the `OnOrderClicked` success branch of
  `Assets/Scripts/Runtime/UnitOrderToolbarHost.cs` to the pre-fix `SetStatus(null); Refresh();`
  (no issued-status message) — real pre-fix code, **no stub**. Editor recompiled (0 compile errors).
- RED: `Passed 1 / Failed 1`

      Hold_click_with_selected_unit_shows_issued_status
      Expected: String containing "ISSUED: hold → u1"
      But was:  <string.Empty>

- Patch reverted (`git diff` on the file empty), recompiled, GREEN again: `Passed 2 / Failed 0`.

With this, D20, D24 and D25 all have red evidence on real pre-fix code.

## Scene-config fixes — scene guard tests red on the real parent scenes (round 7, commit `5672ea45`)

Two UAT fixes changed only `Assets/Scenes/DelegationSmoke.unity` and had no test until round 7:
- `f787b3a2`: unit-order toolbar added to the scene with UIDocument sorting order 12 (D15 subset).
- `716b3dfd`: Doctrine Inheritance panel `showPanel: 1` (D37).

`DelegationSmokeSceneGuardTests` (`src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/`):
- The test resolves each MonoBehaviour by the script GUID it reads from the `.cs.meta` file.
- It was run in detached worktrees at each fix's parent. Only the test file was copied in; the scene content is the real pre-fix scene, and no stub was used.

| Parent | Result | Failure |
|--------|--------|---------|
| `f787b3a2^` | Failed 2 / Passed 0 | toolbar test: `Expected: property Count equal to 1, But was: 0` (no `UnitOrderToolbarHost` in the scene). The doctrine test also fails, because `716b3dfd` comes later. |
| `716b3dfd^` | Failed 1 / Passed 1 | doctrine test: `Expected: "1", But was: "0"` (`showPanel`). |

Both worktrees were removed afterwards. GREEN on the branch: 2/2, and the full UnityAdapter project passes 1291/1291.

With this, no fix in rounds 3–7 is without a test that failed first on real pre-fix content.


## Round 8 — RED evidence on real pre-fix code (2026-11-22)

- **D40** (`PlayModeSmokeOrbatSeeder.AdvanceDecisionLog` synthetic `emcon: Active -> Passive` row):
  - Test: `PlayModeSmokeHarnessTests.AdvanceDecisionLog_does_not_emit_synthetic_emcon_policy_update`
  - RED on unmodified `PlayModeSmokeOrbatSeeder.cs`:
    `D40: synthetic emcon policy update contradicts active unit EMCON posture. Expected: False, But was: True`
  - GREEN after removing the canned `simTime >= 5.0` EMCON row: `PlayModeSmokeHarnessTests` 27/27 passed; verified live in all 5 Play Mode scenarios (`r8-d40-comms-clean.png`).
- **D32** (`CoverageMapViewTests.Source_declares_gap_dashes_covered_solids_geometry_scaling_and_segment_cap`):
  - RED in Unity PlayMode Test Runner (`tests-run`): `System.IO.DirectoryNotFoundException : Could not find a part of the path "/home/username01/cmano-clone/ProjectAegis/Assets/Scripts/Runtime/CoverageMapView.cs"`.
  - GREEN after resolving via `UnityEngine.Application.dataPath`: 22/22 passed.
- **D33** (`SliceAContactPanelSmokeTests.Contact_panel_binds_once_per_frame_and_clears_on_unit_selection`):
  - RED in Unity PlayMode Test Runner (`tests-run`): `Expected: String containing "UNKNOWN", But was: "SRC: u1 | ref observer:u1|target:hostile-1"` and `But was: "COMMS: no degradation reported"`.
  - GREEN after updating assertions to match real observer provenance & nominal comms formatting: 22/22 passed.
- **Manifest mirror drift** (`UnityPackageManifestMirrorTests.Template_dependencies_match_live_manifest`):
  - RED on Editor-updated `manifest.json` (`1.14.0`/`2.1.4`) vs `manifest.template.json` (`1.13.0`/`2.1.1`).
  - GREEN after syncing `manifest.template.json` per ADR-027 §3 (`UnityAdapter.Tests` 1,292/1,292).
