# Waiver: C2TopBarPanelHost Sim `PlayerInfoModel` read (ADR-010 §3)

| Field | Value |
| --- | --- |
| **ID** | `waiver-c2-topbar-sim-read-2026-11-16` |
| **Date** | 2026-11-16 (CT) |
| **Status** | **Exited** (2026-11-22 — closed by [DRG-773](https://linear.app/drgamtd-workspace/issue/DRG-773) & [DRG-717](https://linear.app/drgamtd-workspace/issue/DRG-717)) |
| **ADR** | [ADR-010](../adr-010-headless-first-command-driven-ui.md) §3 — read models / projections are the UI contract |
| **Source** | [DRG-771](https://linear.app/drgamtd-workspace/issue/DRG-771) §8 item 8 (CMA-32) |
| **Owner go** | 7:40 PM CT Nov 16 · phrase **Accept jev's calls** · base `1552311d` |
| **Exit ticket** | [DRG-773](https://linear.app/drgamtd-workspace/issue/DRG-773) (Low) — route Sim read through a projection (**EXITED**) |

## What was waived

Unity runtime host `unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs` (landed in [#669](https://github.com/drgaciw/cmano-clone/pull/669), audit window S7) previously imported `ProjectAegis.Sim.Scenario` and read `Orchestrator.ScenarioPolicy.PlayerInfoModel` directly in `ResolvePlayerInfoModel()` to feed fog-mode presentation (`FogModeLabelBinder`).

ADR-010 §3 expects presentation surfaces to consume **read-only projections / view models**, not reach into Sim scenario policy types from Unity hosts.

## Exit resolution (2026-11-22)

All exit criteria are satisfied and this waiver is **closed / exited**:

1. **Projection route added (`LoopPolicyChromeBinder.Project`):** `src/ProjectAegis.Delegation.UnityAdapter/Presentation/LoopPolicyChromeBinder.cs` projects both fog-mode (`FogModePresentation`) and personality edit-policy (`PersonalityEditPolicyPresentation`) labels read-only from `DelegationBridge` without exposing Sim scenario policy types to Unity hosts.
2. **Sim import removed from `C2TopBarPanelHost.cs`:** `using ProjectAegis.Sim.Scenario;`, `ResolvePlayerInfoModel()`, and all direct `Orchestrator.ScenarioPolicy` / `PlayerInfoModel` references were removed from `unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs`.
3. **Read-only presentation preserved (ADR-010 §2–3):** `C2TopBarPanelHost` binds both `fog-mode-label` and `edit-policy-label` purely from `LoopPolicyChromeBinder.Project(bridgeHost.Bridge)` with no wall-clock and no sim mutation.
4. **Source-fence & PlayMode verification:** Headless source-fence contract `C2TopBarLoopPolicyHostContractTests` enforces zero `ProjectAegis.Sim.Scenario`, `ScenarioPolicy`, `PlayerInfoModel`, or `Orchestrator` references in `C2TopBarPanelHost.cs`, paired with PlayMode verification in `Sprint1UiWiringPlayModeTests`.

## Historical decision record

Per owner go (**Accept jev's calls**), the bypass was temporarily waived as read-only presentation use pending DRG-773. ADR-010 remained Accepted throughout and now has zero active waiver for `C2TopBarPanelHost.cs`.

## Related

- Audit finding: DRG-771 §8 item 8 · recommended options were “accept as read-only presentation use” or “require a projection”.
- Sibling presentation baseline (pre-S1, not covered by this waiver): `MapPlaceholderPanelHost` Sim import — do not expand under this document.
- Disjoint from Lane D waiver `waiver-mission-editor-cli-ai-host-2026-11-16.md` (ADR-030 / MissionEditor.Cli → Ai.Host).
