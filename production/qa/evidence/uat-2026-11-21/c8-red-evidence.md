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
