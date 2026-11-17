# Waiver: C2TopBarPanelHost Sim `PlayerInfoModel` read (ADR-010 §3)

| Field | Value |
| --- | --- |
| **ID** | `waiver-c2-topbar-sim-read-2026-11-16` |
| **Date** | 2026-11-16 (CT) |
| **Status** | Active (temporary) |
| **ADR** | [ADR-010](../adr-010-headless-first-command-driven-ui.md) §3 — read models / projections are the UI contract |
| **Source** | [DRG-771](https://linear.app/drgamtd-workspace/issue/DRG-771) §8 item 8 (CMA-32) |
| **Owner go** | 7:40 PM CT Nov 16 · phrase **Accept jev's calls** · base `1552311d` |
| **Exit ticket** | [DRG-773](https://linear.app/drgamtd-workspace/issue/DRG-773) (Low) — route Sim read through a projection |

## What is waived

Unity runtime host `unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs` (landed in [#669](https://github.com/drgaciw/cmano-clone/pull/669), audit window S7) imports `ProjectAegis.Sim.Scenario` and reads `Orchestrator.ScenarioPolicy.PlayerInfoModel` directly in `ResolvePlayerInfoModel()` (approx. lines 515–518) to feed fog-mode presentation (`FogModeLabelBinder`).

ADR-010 §3 expects presentation surfaces to consume **read-only projections / view models**, not reach into Sim scenario policy types from Unity hosts.

## Decision recorded

Per owner go (**Accept jev's calls**), this bypass is **waived as read-only presentation use** pending an explicit projection route. Scope of the waiver:

- **Allowed while active:** the existing read-only path from `C2TopBarPanelHost` → `bridgeHost.Bridge.Orchestrator.ScenarioPolicy.PlayerInfoModel` (or `PlayerInfoModel.FullTransparency` fallback) for fog-mode label binding only.
- **Not allowed:** any write into Sim/Delegation state from this host; any new Unity → Sim type imports beyond this documented use; mutation of `DelegationBridge` hotpath, `SimulationSession`, `CatalogWriteGate`, or `MapPlaceholderPanelHost`.

This waiver does **not** amend ADR-010 status. ADR-010 remains Accepted; the projection contract still applies to new work.

## Exit criteria

Close this waiver when [DRG-773](https://linear.app/drgamtd-workspace/issue/DRG-773) lands and:

1. Exposes `PlayerInfoModel` (or an equivalent fog/info-model field) on a headless projection consumed by the top bar (for example extending an existing top-bar / loop-policy projection rather than a new Sim import in Unity).
2. Removes `using ProjectAegis.Sim.Scenario` from `C2TopBarPanelHost.cs` (and any direct `PlayerInfoModel` / `ScenarioPolicy` type use in that host).
3. Keeps presentation read-only (ADR-010 §2–3); no Unity code authority over sim policy.
4. Headless gates remain green; Unity MCP evidence only if the implementing PR touches Editor/visual surfaces.

Until then, treat the #669 path as an acknowledged, bounded exception — not a precedent for additional Sim reads from Unity hosts.

## Related

- Audit finding: DRG-771 §8 item 8 · recommended options were “accept as read-only presentation use” or “require a projection”.
- Sibling presentation baseline (pre-S1, not covered by this waiver): `MapPlaceholderPanelHost` Sim import — do not expand under this document.
- Disjoint from Lane D waiver `waiver-mission-editor-cli-ai-host-2026-11-16.md` (ADR-030 / MissionEditor.Cli → Ai.Host).
