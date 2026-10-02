# DRG-197 GitNexus reindex + knowledge-graph verification evidence (2026-09-27)

**Issue:** DRG-197 (parent DRG-186, Combat Interaction UX)
**Date:** 2026-09-27, ~14:20–14:35 CT
**Integrated tip covered:** `7a097f93ee25d73c1ecfeb2e7fc33e9dfbc6ab54` (`main`, #678 DRG-262 attack-options menu preview parity)
**Tooling:** GitNexus CLI 1.6.12 (`/home/box/.local/lib/node_modules/gitnexus`), Node v20.19.2, linux x64; `user-gitnexus` MCP server (`gitnexus@latest mcp`, same 1.6.12 build)
**Scope:** Tooling and evidence only. No changes under `src/` or `unity/`, no `.gitnexus` config changes.

> **Sequencing note:** This evidence covers tip `7a097f93` only. Per the issue's sequencing, a re-run is planned after the current wave (DRG-183, DRG-169, DRG-258) lands. DRG-197 stays In Progress until that re-run.

## 1. Freshness: how it was confirmed

| Check | Result |
|---|---|
| `git rev-parse HEAD` / `origin/main` (shared checkout `/workspace/cmano-clone`) | `7a097f93ee25d73c1ecfeb2e7fc33e9dfbc6ab54` / same |
| GitHub `main` head (GitHub MCP `list_commits`) | `7a097f93ee25d73c1ecfeb2e7fc33e9dfbc6ab54` |
| MCP `list_repos` → `cmano-clone` | `lastCommit` `7a097f93…`, `indexedAt` 2026-09-27T19:23:32.892Z (**14:23:32 CT**), branch `main`, contentRetention `full` |
| `npx gitnexus status` (shared checkout) | Indexed commit `7a097f9` = current commit `7a097f9`; "Index content: matches all 5487 covered file(s)"; **Status: ✅ up-to-date** (no stale-state warning); indexed runner identity = current runner identity |
| `.gitnexus/meta.json` capabilities | graph `ladybugdb` available; **FTS `ladybugdb-fts` available**; vectorSearch `unavailable` |

**Reindex:** The shared index had already been rebuilt at tip by another lane (14:23:32 CT); per lane rules, that checkout was treated as read-only and not re-analyzed there. To get independent evidence of a clean full reindex at the same commit, I ran the documented full reindex in an **isolated scratch clone** at `7a097f93` with a separate `GITNEXUS_HOME` (the global `~/.gitnexus/registry.json` was verified unchanged afterwards):

```
GITNEXUS_HOME=<isolated> gitnexus analyze --force --index-only .
→ Repository indexed successfully (34.5s); exit 0
→ 40,695 nodes | 85,459 edges | 941 clusters | 817 flows
→ indexedAt 2026-09-27T19:27:00.839Z (14:27:00 CT); status ✅ up-to-date @ 7a097f9
```

No Ladybug WAL / `UNREACHABLE_CODE` assertions and no FTS-unavailable messages appeared in this run (the Slice G preflight on 2026-09-08 had hit both on another machine). The run logged these analyzer warnings:

- 124 × `callable-value-flow: candidate set exceeded the cap (33 > 32); no partial CALLS emitted`, all from one site (`src/ProjectAegis.Delegation.Tests/AfterAction/AfterActionLedgerProjectionTests.cs:52:21`).
- `name-fallback resolution: 457 call sites (281 distinct caller-file/name pairs)` (csharp).
- `[scope-resolution] 24 property read/write site(s)` defined only in another language, so they were not linked.
- `[processes] 817 flows reported … 2301 of 2501 candidate entry point(s) never ranked in … 12 trace(s) cut at maxTraceDepth, 1809 callee(s) skipped at maxBranching, 45 walk(s) cut by the per-entry trace budget.`
- 8 files over 512 KB skipped (`docs/reference/cmano-db/*.md`, among others).

## 2. Index stats

| Source | files | nodes | edges | communities | processes | embeddings |
|---|---|---|---|---|---|---|
| Shared index `meta.json` / MCP `list_repos` | 5489 | 40,697 | 87,192 | 941 | 817 | 0 |
| Shared index, **actual DB count** (Cypher, fresh CLI, 14:32 CT) | — | 40,695 | 85,459 | — | — | — |
| Isolated rebuild at same commit | 5489 | 40,695 | 85,459 | 941 | 817 | 0 |

The shared on-disk graph matches the isolated rebuild exactly (the top edge-type counts also match: CONTAINS 24,152 · CALLS 20,371 · HAS_METHOD 8,619 · MEMBER_OF 8,158 · ACCESSES 7,192 · IMPORTS 5,872). The node and edge counts in `meta.json` / `list_repos` are off by +2 nodes and +1,733 edges (see Gaps).

## 3. Trace verification (query → context → impact upstream, summaryOnly)

The results below come from **fresh CLI processes** (isolated rebuild, cross-checked against the shared index at 14:32 CT). For C2AuthorityProjector.Project, KillChainContactStateProjection and RightUnitPanelHost, the shared index returned the same numbers. MCP-served results were **not** used (see Gap G1). "Production" means non-test code. `impact` excludes tests by default.

### (a) Kill-chain contact-state projection

- **query** "kill chain contact state projection": returned 5 processes (`RunCatalogKillChainReport → ResolveRepoRelative`, three `Project → *PlatformEntry` flows, `Project → StaleThresholdDivisor`). Definitions surfaced `KillChainContactStateBridge`, `KillChainContactPanelBinder`, `SliceAContactFrameBridge`, `SensorToShooterProjection` and `KillChainContactStateProjectionTests`.
- **context** `Class:src/ProjectAegis.Delegation/Projection/KillChainContactStateProjection.cs:KillChainContactStateProjection`: epistemic `exact`. Incoming: IMPORTS only from 2 docs (`kill-chain-contact-state.md`, `slice-a-contact-evidence-frame.md`). Outgoing: 19 HAS_METHOD. 0 processes.
- **impact (class)**: risk **LOW**, impactedCount 7, direct 2, byDepth {1:2, 2:4, 3:1}, processes 0, modules 0, epistemic `exact`. Every hit is a markdown IMPORTS edge.
- **`Project#5~DecisionLog?,…` method**: context shows 1 incoming CALL, from a test (`Empty_log_yields_empty_snapshot`). The second overload, `Project#5~IReadOnlyList<ContactChangeRecord>?,…`, has 0 callers. Impact: risk **UNKNOWN**, 0 impacted.
- **Bridge** `KillChainContactStateBridge.Build#2`: risk **LOW**, 3 impacted (d1 `SliceAContactFrame.Build` → d2 `DelegationBridgeHost.RunTick` → d3 `SimplePlayModeSimHost.Update`), 1 process (`RunTick`), module `Bridge`.
- **Gap:** Text search finds **3 production call sites** of `KillChainContactStateProjection.Project(` (`KillChainContactStateBridge.cs:31`, `SensorToShooterProjection.cs:30`, `TrackCustodyProjection.cs:31`). None of them has a CALLS edge in the graph (checked with Cypher). The chain bridge → frame → host → PlayMode *is* traced. The projection's own upstream is **not**.

### (b) Agent-callable C2 skill contract (`src/ProjectAegis.Delegation/Skills/`)

- **query** "C2 authority skill contract envelope validator": 0 processes. Definitions: `SkillEnvelopeValidator` (+`ValidateEngageCommand`), `C2AuthorityProjection` (+`FromEnvelope`), `SkillEnvelope`, `SkillEnvelopeValidation`, `SkillEnvelopeValidatorTests`.
- **context** `C2AuthorityProjector` (class): epistemic `exact`. 14 IMPORTS in (3 docs plus test/bridge/presentation files). 14 HAS_METHOD out (`Project`, `ProjectApprove`, `ProjectEngage`, `ProjectTargeting`, `ResolvePendingApproval`, and others).
- **impact** `C2AuthorityProjector.Project#1`: risk **HIGH**, impactedCount 6, direct 4, byDepth {1:4, 2:1, 3:1}, processes 2 (`RunTick`, `Project`), modules 3 (`Bridge`, `EscalationGate`, `TargetabilityAccept`), epistemic `exact`. d1 = `SliceAContactFrame.Build`, `EscalationGateProjection.Project` ×2, `TargetabilityAcceptProjection.Project`. This **matches text search exactly** (4 production call sites). Context shows 16 incoming calls including tests, and 7+ `Project → …` processes (`RoeProjection`, `C2TargetingAuthority`, `IsSharedSa`, …).
- **impact** `C2AuthorityProjector` (class): risk MEDIUM, 55 impacted, direct 5, byDepth {1:5, 2:8, 3:42}.
- **`SkillEnvelopeValidator.Validate#2`**: 14 incoming calls, all from `SkillEnvelopeValidatorTests`. Impact risk **UNKNOWN**, 0 impacted. This is consistent with text search: there is no production caller of `SkillEnvelopeValidator.Validate(` at this tip.
- **`SkillCatalog.TryGet#2`**: risk LOW, 1 impacted (d1 `SkillEnvelopeValidator.Validate`), module `Skills`. Consistent.
- Class-level impact for `SkillEnvelopeValidator` and `SkillCatalog` returns UNKNOWN/0. Static-class callers bind to methods, not the class node.

### (c) Provenance / confidence presentation

- **query** "contact provenance confidence projection contact detail": 0 processes. Definitions: `IdentityClassProjection`, `ContactDetailProjectionTests` (`Project_builds_provenance_classification_staleness`), `SliceAContactPresenter`, `SliceAContactPresentationTests` (`Provenance_exposes_source_confidence_age_and_comms_without_color`), `AdviceRuntimeEvidenceBridge`.
- **context** `ContactProvenanceProjection` (class): epistemic `exact`. 1 IMPORTS in (`contact-provenance.md`). 10 HAS_METHOD (`Project` ×2, `ProjectContact`, `ResolveConfidence`, `ComputeEffectiveStaleThreshold`, …).
- **impact** class: risk **LOW**, 3 impacted (docs only), direct 1.
- **`ContactProvenanceProjection.Project#6`**: 1 incoming CALL, from a test. It participates in 10 processes (`Project → CatalogPlatformEntry`, `Project → StaleThresholdDivisor`, …). Impact risk **UNKNOWN**, 0 impacted. The `Project#7` overload has 0 callers.
- **Gap:** Text search finds **4 production call sites** of `ContactProvenanceProjection.Project(` (`TargetabilityAcceptProjection.cs:34`, `SliceAContactFrame.cs:66`, `IdentityClassProjection.cs:34`, `TrackCustodyProjection.cs:43`). None of them has a CALLS edge in the graph.
- **`ContactDetailProjection.Project#4`**: risk **LOW**, 4 impacted: d1 `ContactDetailApplyState.ProjectAndApply` → d2 `ContactDetailPanelHost.Refresh` → d3 `ContactDetailPanelHost.LateUpdate` / `OnEnable`. Modules `Projection`, `Runtime`. This matches text search (1 production call site). `Project#3` has test callers only (UNKNOWN).

**epistemic / causes:** Every fresh-CLI `context`/`impact` above reported `epistemic: "exact"`. The CLI JSON did not emit a `causes` or `boundaries` object for these exact results, so `receiverTyping` and the other causes were not observed as > 0. The MCP-server responses did carry causes (`dispatchBoundary: 15`, `receiverTyping: 0`), but they were captured during the corrupted-read window (G1) and are **not** trusted.

## 4. Wave Surface sanity checks (DRG-258 / DRG-169 / DRG-183)

| Check | Result |
|---|---|
| `impact` upstream summaryOnly `RightUnitPanelHost` (DRG-258) | `Class:unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs:RightUnitPanelHost`: risk **UNKNOWN**, 0 impacted, epistemic `exact`. Context: 20 HAS_METHOD, 4 HAS_PROPERTY, 0 incoming. The only production reference is the generic type argument `CreatePanelHost<RightUnitPanelHost>(` in `unity/ProjectAegis/Assets/Editor/DelegationSmokeSceneBuilder.cs:66`, which is not recorded as an edge. Expected for a scene-instantiated MonoBehaviour. Confirm by text search before edits. |
| `query` "contact combat card targetability harness" (DRG-169 / DRG-183) | 0 processes, 14 definitions. None is a combat-card or targetability-harness symbol (hits were `PlayModeSmokeHarnessTests`, `WatchAttentionQueue.Cards`, `KillChainContactStateProjectionTests`, …). Text search finds no `CombatCard` / `TargetabilityHarness` symbols at `7a097f93`, which is expected because these lanes have not landed. |

## 5. Cycle check

`check` (cycles), fresh CLI, on both the shared index and the isolated rebuild: `status: "clean"`, `enumeration: "complete"`, `cycleCount: 0`, **`componentCount: 0`**.

## 6. Gaps found

- **G1: MCP server read the index incorrectly after the rebuild (operational, material for other lanes).** Every running `user-gitnexus` MCP server process was started before the 14:23 CT rebuild (the newest at 14:08 CT). After the rebuild, MCP `context`/`impact`/`cypher` responses contained corrupted string columns: empty or garbage `id` values, NUL bytes, and ids pointing to unrelated `Folder:production/qa/gauntlet/...` nodes. Symbol UIDs were not found, and unrelated classes collided on an empty id. For example, `KillChainContactStateProjection` and `C2AuthorityProjector` returned the same incoming list and a CRITICAL / 140-direct impact made of MissionEditor CLI flows, and `RightUnitPanelHost` flipped between UNKNOWN/0 and CRITICAL/157. Fresh CLI processes also showed corrupted reads intermittently between about 14:25 and 14:28 CT. From about 14:31 CT, fresh CLI reads of the same unchanged `lbug` file (mtime 14:23:33 CT) were clean and matched the isolated rebuild. MCP responses in the same window (about 14:31–14:32 CT) were still corrupted. **Action:** restart / reload the `user-gitnexus` MCP server after any reindex before trusting MCP results. Until then, use the CLI.
- **G2: Missing CALLS edges for overloaded static `Project(...)` methods.** 3 production call sites of `KillChainContactStateProjection.Project` and 4 of `ContactProvenanceProjection.Project` have no CALLS edge. Both classes declare two `Project` overloads. The single-overload `C2AuthorityProjector.Project` resolves correctly. Upstream impact for these two projections is therefore **under-reported, and reported as exact** (LOW/UNKNOWN). Treat it as a floor and confirm by text search before editing these projections.
- **G3: `meta.json` / `list_repos` stats disagree with the DB.** The metadata reports 40,697 nodes / 87,192 edges, but the DB contains 40,695 / 85,459, which matches the isolated rebuild.
- **G4: No embeddings (0); vector search unavailable.** `query` ranking is BM25/FTS only (hybrid RRF has no semantic leg). FTS was available (not the FTS-unavailable failure from 2026-09-08).
- **G5: Process coverage is partial by design.** 2301 of 2501 candidate entry points never ranked in, and 1809 callees were skipped at maxBranching. `processes_affected: 0` on these projections does not mean no runtime flow exists.
- **G6: Class-level impact on static classes and scene MonoBehaviours is uninformative.** Callers bind to methods (`SkillEnvelopeValidator`, `SkillCatalog`), or reach the class through generic type arguments or scene wiring (`RightUnitPanelHost`). Use method UIDs.
- **G7: `SkillEnvelopeValidator.Validate` has no production caller at this tip** (tests only). This is a fact about the code, not an index defect. It is recorded here because the "agent-callable" contract is currently exercised only by tests.
- Minor: 8 files over 512 KB are excluded from the index, and the callable-value-flow cap was hit at one test site.

---
*DRG-197, 2026-09-27 CT. Evidence at tip `7a097f93`; post-wave re-run pending (DRG-183, DRG-169, DRG-258).*
