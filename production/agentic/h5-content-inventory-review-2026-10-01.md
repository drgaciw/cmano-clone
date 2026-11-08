# H5 inventory review and local pilot decision inputs

**Date:** 2026-10-01. **Verdict:** PASS for read-only inventory consolidation; runtime pilot and owner acceptance remain gated. Reviewed [PR #687](https://github.com/drgaciw/cmano-clone/pull/687), head `6d0cf2e0c2f3aef283809f4dad48eb360f379f7b`, via `git show`, without applying its branch. Its only diff from `1902dc12` is `production/agentic/h5-content-inventory-2026-09-27.md`.

Fresh checks use clean-base worktree `planning-next-20261001` at `1902dc1299678ea17f75014406fd9c616e5576bd`. This review supplies [DRG-332](https://linear.app/drgamtd-workspace/issue/DRG-332) evidence and proposed inputs for [DRG-331](https://linear.app/drgamtd-workspace/issue/DRG-331) and [DRG-347](https://linear.app/drgamtd-workspace/issue/DRG-347); it accepts no ADR and approves no asset.

## Fresh corroboration and limits

`git ls-tree -r -l HEAD`, excluding `.meta`, reproduces these September 27 counts:

| Content path | Fresh count / repository bytes |
| --- | --- |
| `unity/ProjectAegis/Assets/UI/` | 68 / 135,531; 32 UXML, 34 USS |
| `unity/ProjectAegis/Assets/Resources/Fonts/` | 4 / 361,968 |
| `production/assets/audio/` | 2 / 26,968 |
| `data/scenarios/` | 129 / 548,030 |
| `tests/regression/` | 35 / 37,603 |

These are repository blob sizes, not imported texture memory or runtime loading measurements. The original QA-capture count remains historical and unremeasured. `design/assets/asset-manifest.md` still reports 42 assets: 15 Done, four Approved, 20 Specced, three In Production; existing statuses are unchanged. Its referenced `design/assets/specs/post-s93-residual-assets.md` remains missing.

Fresh `Packages/manifest.json` pins Addressables **2.9.1**; `ProjectSettings/ProjectVersion.txt` pins Editor **6000.3.22f1**. The specialist's 2.3.16/6000.3.14f1 guidance remains stale. No committed AddressableAssetSettings, content-state binary or StreamingAssets path was found. The descriptor `Assets/Addressables/Map/App6AtlasAddressablesManifest.json` remains 210 bytes, key `Map/App6FrameAtlas`; its Unity PNG is 167 bytes versus the production copy's 381. This proves different blobs, not pixel quality or an approved canonical artwork.

Source inspection confirms `src/ProjectAegis.Delegation/Projection/App6AddressablesCatalog.cs` parses JSON and checks key/path/existence. `Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs` calls that resolver and returns `App6AtlasCatalog.Default` on failure; `App6Sidc.cs` provides Unicode fallback. This metadata path is not proof of asynchronous Unity asset loading, decoding, handle release or Player-build fallback. No `LoadAssetAsync`/`Addressables.` calls were found in Runtime scripts. Headless assemblies have no Unity Addressables API references; Editor setup owns the package API.

GitNexus query/context used the explicit indexed checkout `C:/Users/dgorn/My Projects/cmano-clone/.claude/worktrees/project-requirements-review-f115ae`, indexed at the same base revision. It corroborated catalog methods and Editor setup definitions but returned no ranked execution flows. Same-name indexes and DRG-323/324 limitations remain; source checks control this verdict. Impact is N/A: no executable symbol edits.

## Proposed bounded pilot

Select **one local map-atlas presentation class**, retaining existing keys; exclude duplicate production chrome, fonts without consumers, scenes, remote/CDN/Cesium content and all scenario/catalog/glossary/replay inputs. DRG-331 must settle canonical PNG provenance and an explicit new loader surface before DRG-347 implementation; do not expand MapPlaceholderPanelHost or GlobeMapProductHost by default. ADR-010 §2–3, ADR-007 and ADR-001 preserve the presentation wall.

Proposed lifetime: presentation-owned loader caches one handle while views hold leases, releases on final lease/disposal, cancels stale requests on scene teardown and rejects late callbacks. No simulation tick waits for asset completion. Missing, corrupt, delayed or mismatched content chooses documented local/Unicode fallback, logs a diagnostic and preserves world/order/RNG/replay fingerprints.

Budget decision inputs, **not accepted limits**: nominate hardware/build/asset compression; measure cold/warm ready latency, p95/p99 frame cost and peak/residual memory against the existing local path. Candidate thresholds for discussion: no synchronous main-thread wait, p99 loader frame overhead ≤2ms, cold readiness ≤250ms, incremental memory ≤16MiB, zero leaked handles after repeated teardown. Headless fault fixtures prove invariants; Editor/Player profiling proves loading and budgets. No fresh runtime tests or performance numbers are claimed here.
