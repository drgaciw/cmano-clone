# S126 symbology subset — headless evidence pack — 2026-10-06

**Sprint:** [S126](../../sprints/sprint-126-symbology-subset.md) · **Task:** S126-07 W3-SYM-02 · **QA plan:** [S122–S127 rebaseline](../qa-plan-sprint-122-127-rebaseline-2026-09-30.md) (S126 row)
**Linear:** [DRG-231](https://linear.app/drgamtd-workspace/issue/DRG-231) SYM-MIL-01 · [DRG-232](https://linear.app/drgamtd-workspace/issue/DRG-232) SYM-CIV-01 (pending)
**Branch:** `cursor/s126-symbology-subset-7fee` (from `origin/main` `434b3cf6`) · **Code commit:** `50fda56a`
**Environment:** Cursor Cloud VM, .NET SDK 8.0.400, headless only. No Unity Editor, no Play Mode, no screenshots.
**Gate pack:** [N-GATE-SYM-01 MIL gate pack](../../../docs/superpowers/reviews/2026-10-06-n-gate-sym-01-mil-gate-pack.md)

This is headless engineering evidence. It is not QA sign-off, owner acceptance, Editor evidence or an APP-6 / MIL-STD-2525 certification claim.

## Must items

| ID | Task | Result | Evidence |
| --- | --- | --- | --- |
| S126-01 | W3-SYM-03 freeze | Done. Affiliation set frozen to Friendly / Hostile / Neutral / Unknown; W2-SYM-05 expansion stays **HOLD**; `CertificationClaimed = false` on every legend. | `Military_affiliation_outside_frozen_set_degrades_to_unknown` (5 cases), `Legend_surfaces_not_certified_disclaimer_for_every_selectable_profile` (2 cases) |
| S126-02 | N-GATE-SYM-01 wiki gate | Done, MIL-only, Git-side. Not published to the Notion design wiki (owner action). | Gate pack linked in the header |
| S126-03 | W2-SYM-01 canonical keys | Done. `SymbolKeyRegistry` schema `aegis-sym-keys/v1`, ordinal-sorted, unique, canonical-format keys; not referenced from sim / replay / decision / orchestration / `BalticReplayHarness` / `DelegationBridge` sources; `ProjectAegis.Sim` does not reference the Delegation assembly. | `Registry_*` tests, `Sim_replay_and_hash_sources_do_not_reference_symbology`, `Sim_assembly_does_not_reference_delegation_presentation` |
| S126-04 | SYM-03 profile switch | Done headless. Toggling profiles between bridge ticks leaves orders, decision-log fingerprint and the projected map picture unchanged; Baltic v2 `baltic-patrol` seed 42 × 4 ticks stays `WORLD_HASH=17144800277401907079` with matching detection hash and SHA-256 fingerprint. | `SymbologyProfileSwitchHashSafetyTests` (2), `Profile_round_trip_does_not_mutate_input_picture` |
| S126-05 | W3-SYM-01 ≥3 naval types | Done **MIL-only**: `naval.surface.combatant`, `naval.subsurface.submarine`, `naval.surface.unknown`, plus observer-safe `generic.unknown`. **CIV pending DRG-232; no dual-profile completion claim.** | `Military_scoped_naval_types_resolve_to_15_char_sidc_per_affiliation`, `Military_unobserved_type_never_invents_class_or_domain`, `Apply_military_profile_restyles_only_presentation_fields` |
| S126-06 | W2-SYM-03 disclaimer | Done in the presentation model: `SymbologyLegend.Disclaimer` = *"Symbology subset — not certified against NATO APP-6 or MIL-STD-2525. Presentation only."* Not yet bound into a Unity legend panel. | Legend tests |
| S126-07 | W3-SYM-02 evidence | This file. DRG-208 unchanged (not touched). | — |

## SYM-02 prerequisite — waiver

No Wave-1 SYM-02 profile scaffold existed on `origin/main` (only the affiliation-only `App6Sidc` resolver, `MapPictureProjection`, `MapSymbolLodClusterer`). SYM-02 was still queued in the S122.0-02 filing checklist. Per the sprint prerequisite (“SYM-02 scaffold Done **or** explicit waiver”), this sprint records a **waiver** and adds a minimal headless scaffold: `SymbologyProfile`, `SymbologyProfileCatalog`, `SymbologyProfileSwitch`, `SymbologyProjection`. The full SYM-02 scope (data manifest, Unity binding) remains open.

## Commands and results

All from the worktree root with `export PATH="$HOME/.dotnet:$PATH"`. Targeted only (shared VM); the orchestrator runs the full solution gates.

| Command | Result |
| --- | --- |
| `dotnet build src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -m:1 --no-incremental` | 0 warnings, 0 errors |
| `dotnet build src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 --no-incremental` | 0 warnings, 0 errors |
| `dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -m:1 --filter "FullyQualifiedName~SymbologyProfileTests"` | 28 / 28 passed |
| `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 --filter "FullyQualifiedName~SymbologyProfileSwitchHashSafetyTests"` | 2 / 2 passed |
| `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 --filter "FullyQualifiedName~ReplayGolden"` | 17 / 17 passed |
| `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"` | 25 / 25 passed |
| `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 --filter "FullyQualifiedName~MapPictureBridgeTests"` | 8 / 8 passed |
| `dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -m:1 --filter "FullyQualifiedName~Projection"` | 852 / 853; the one failure is pre-existing `UnityPluginEpicATypesTests` (gitignored Unity plugin DLL absent on the VM; produced by `tools/copy-delegation-assemblies.ps1`) |
| `grep -r "17144800277401907079" tests/ data/` | Present in Baltic v2 goldens; unchanged |
| `git diff origin/main --stat -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs tests/regression src/ProjectAegis.Data/WriteGate` | Empty |

## Blast radius

Additive only. No existing type, method or file under `src/` was edited. Eleven new types in ten files under `src/ProjectAegis.Delegation/Projection/` and two new test files. Nothing in sim, orders, replay, `DelegationBridge`, `CatalogWriteGate`, Unity assets or Baltic goldens calls or is called by the new types. GitNexus was unavailable; caller analysis was done with grep (no existing callers, since all symbols are new).

## Not delivered / pending

- CIV profile and dual-profile parity — pending DRG-232 scope decision.
- Profile screenshots, Unity legend panel binding, visual atlas review (MIL-AC-05/07 visual half) — Editor work, not done.
- Full naval atlas (MIL-AC-03 carrier…auxiliary) — SYM-04, out of sprint.
- Versioned data manifest (S126-09 W2-SYM-04, Should) and swarm integrity (S126-08, Should) — not done.
- QA execution sign-off and DRG-208 owner acceptance — unchanged and pending.
