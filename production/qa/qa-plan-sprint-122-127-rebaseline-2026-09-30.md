# S122–S127 QA plan — September 30 rebaseline

**Date:** 2026-09-30. **Stage:** Release. **Plan maturity:** authored; execution and QA sign-off pending. This shared plan supplies the mandatory QA plan for each S122.0–S127 sprint. A plan's existence is not a passed test or owner acceptance.

## Entry and evidence contract

Before implementation, name the sprint's delivered criteria, fixture, test layer and responsible reviewer. Before a symbol edit, obtain path-specific upstream GitNexus impact; record direct callers, affected processes and risk, and inspect source when the graph is ambiguous. DRG-197/323/324 remain graph-quality work; an unqualified same-name index is insufficient. HIGH/CRITICAL impact requires an explicit warning before the edit.

Every result identifies requirement/criterion, issue, scenario and seed, build commit, local delta, catalog/policy identity, toolchain, test layer, command or Editor procedure, pass/fail and evidence path. An owner acceptance row additionally names the accepting owner and dated decision. Existing evidence from another revision remains historical until its applicability is explicitly checked. DRG-208's acceptance target must be reconciled across Linear and Notion before the walk; this plan does not select a new product revision by inference.

## Common regression gates

Run and read these on the delivered checkout, preserving logs and counts:

```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter ReplayGoldenSuiteTests
rg "17144800277401907079" tests data
.\tools\verify-ci-local.ps1
```

Pass: build zero warnings/errors; solution zero failures and at least the accepted baseline (never below 1,638); smoke at least 20 with zero failures; canonical Baltic v2 replay suite exactly six passing cases. Verify that the replay filter selects the six canonical cases; zero discovered or unrelated cases fails coverage. Preserve hash `17144800277401907079`, v3 isolation, and zero changes to DelegationBridge hotpaths or existing CatalogWriteGate write paths in the sprint diff. Read CI parity replay and secret-scan results separately. SDK policy is 8.0.400; record actual resolved SDK and any mismatch instead of treating a roll-forward build as proof of exact-SDK compliance.

For UI/presentation, headless proof precedes Editor evidence. Use ADR-010 §2–3, ADR-007 and ADR-001; run the Unity architect PR-finish checklist and include PASS/FAIL/BLOCKED. Editor evidence requires recorded Editor version, scene, Console logs and Game View screenshots. If :8080 or the Editor is unavailable, keep headless results and mark Editor coverage BLOCKED. No screenshots or owner approval may be inferred from proxy smoke.

## Sprint criterion mapping

| Sprint / criteria | Procedure and measurable pass condition | Evidence / reviewer |
| --- | --- | --- |
| S122.0 hygiene | Inspect archive/create-smoke or explicit waiver, cycles and row assignment, remainder queue, path/host owner collision card, bug-ledger dispositions and CI guard. No closed bug without required human decision. | Administrative evidence; tracker owner. Historical cap numbers must be refreshed before action. |
| S122 tracker + G1 | Each in-scope matrix row classifies document maturity, implemented behavior, test layer and owner acceptance independently. Each claimed implementation has a source/evidence link. Graph report identifies exact checkout. Requirement references and hub pins resolve; floor consumers agree. | DRG-234/242/188/197; docs/tooling reviewer. Missing live automation proof remains pending. |
| S123 entry + Slice A | Load a real package, display available briefing or explicit absent-content state, select each offered mode and valid side, reject Begin when mode/side absent, Begin and tick once. Invalid package resolution preserves active state. Slice A targetability harness passes; record refusal/scoring behavior for owner decision. Optional fixture enrichment does not gate the minimal path. | DRG-243/246/183/244; headless tests plus Editor golden-path capture; interim owner walk on DRG-208, which remains open. |
| S124 commit + delegation | Known commit displays projected constraints and magazine cost; refusal/drop produces visible reason matching log. Verify views do not query/mutate sim authority. Initial Assign Agent and successful rebrief are exercised; failed command preserves state. | Commit/refusal and delegation fixtures; ADR fence review and Editor evidence. |
| S125 editor honesty | Save unfinished document succeeds where contract allows; invalid Export/Play reports blockers without success. Validate dbRef/snapshot binding, workbook silent-drop counts, quarantine and LatLon diagnostics. Capability disclosure is separately mapped to the gated H4 follow-on below. | DRG-236/274; H4 follow-on disclosure DRG-345 has its own gated delivery scope and is not silently added to S125 Must. Headless authoring fixtures plus ME/PE success and recovery screenshots. Excel-primary stays ADR-011. |
| S126 symbology | At least three scoped naval types have correct canonical keys, profile rendering and visible subset disclaimer. Toggle profile on identical run leaves world/order/replay fingerprints unchanged. CIV profile only when explicit scope decision exists; otherwise MIL-only and CIV pending. | DRG-231/232; headless hash comparison plus profile screenshots. No APP-6 certification claim. |
| S127 gates | Evidence index covers entry, commit, editor and scoped symbology at the reconciled acceptance revision and delta. All shipped criteria meet common gates, no unresolved S1/S2 defect; deferred rows name issues. Owner executes and records final Play Mode decision. | DRG-208/205/235/236. QA decision and owner decision recorded separately. |

## Follow-on milestone verification design

H4 bounded existing-map edit → validate → save → export/play (DRG-346) requires success and recovery fixtures, unsupported-role disclosure (DRG-345) and provenance. H5 local presentation-content pilot (DRG-347) requires one agreed content class, measured loading/performance budgets, missing/delayed/mismatched asset fallbacks, and equal simulation/order/RNG/replay state across loading outcomes. H6 complete save contract (DRG-348) precedes restoration vertical slice (DRG-349): uninterrupted vs restored run equality for state and order fingerprints under identical inputs, plus corruption/incompatibility rejection without partial replacement. Replay verification checkpoints do not establish restoration. Multiplayer receives a separate later contract, feasibility evidence and scope decision.

Planning consistency check (DRG-342): run `.\tools\Test-PlanningArtifacts.ps1` against `production/planning-reconciliation-2026-09-30.json`; read pointer, link, ownership and reference failures. This check verifies planning coherence, not feature execution or owner acceptance.

These are planned checks, not executed feature coverage. Scope, architecture and performance-budget decisions precede implementation scheduling.

## Exit and result ledger

All Must criteria require evidence and QA approval; no missing-plan warning waiver. APPROVED WITH CONDITIONS may cover named noncritical deferred criteria only; it cannot waive missing Must proof, unresolved S1/S2, replay/determinism failures or owner acceptance. A blocked sprint records missing evidence and remains incomplete, including S127 when DRG-208 is blocked. Planning closeout and execution closeout are separate records.

| Gate | Current result |
| --- | --- |
| Shared plan authored | Complete, 2026-09-30 |
| Sprint-specific implementation criteria reviewed | Pending |
| Fresh local-checkout regression run | 2026-09-30: build zero warnings/errors; solution 3,352/0 failed/0 skipped; smoke 26/26; canonical replay 6/6. Local dirty `4190fb1`, SDK 8.0.425; target revision/Editor/owner acceptance unverified. Full provenance in coordinating closeout. |
| Live Editor coverage | Pending |
| QA execution sign-off | Pending |
| DRG-208 final owner acceptance | Pending |
