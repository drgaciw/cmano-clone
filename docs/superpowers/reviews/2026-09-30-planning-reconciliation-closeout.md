# Requirements, backlog and roadmap reconciliation — 2026-09-30

**Scope:** user-authorized planning/artifact updates, executed with three parallel workers and an integrator. **Owner:** drg amtd. **Stage:** Release. No commit, merge, feature implementation or owner Play Mode acceptance is included.

The requirements, active roadmap, sprint plans, dashboards, Linear dependencies and live Notion design pages were reconciled. Historical S56 grades and dated evidence remain historical. New feature clauses remain proposed and their future implementation stories remain gated.

## Changed artifacts and disposition

| Area | Change | Result / remaining boundary |
| --- | --- | --- |
| Requirements | Index exposes drafts 23–27; tracker and RTM separate maturity, implementation subsets, candidate tests and owner acceptance. Broken draft links and incorrect ADR-011 package-format claim corrected. | Existing S56 grades retained; proposed clauses are not automatically accepted. |
| New criteria | AME-6.11 capability disclosure; RPL-29–31 same-build/catalog save-resume; LIB-05.1–3 presentation loading failure isolation; VER-07.3–6 revision-bound evidence. | [Reconciliation spec](../specs/2026-09-30-requirements-planning-reconciliation.md) maps tests, dependencies and pending decisions. |
| Roadmap | [September 30 snapshot](../../reports/future-sprint-roadmap-09302026.md), stable alias, dashboard snapshot and status YAMLs synchronized. | S122–S127 active; original windows are forecasts, not completion. H4–H6 unscheduled, gated follow-on milestones. |
| Sprint governance | S123 Must 4.0d including 0.5d owner walk; 0.25d enrichment moved to Should. Shared mandatory [QA plan](../../../production/qa/qa-plan-sprint-122-127-rebaseline-2026-09-30.md); S127 blocked acceptance remains incomplete. | Sprint-specific execution, Editor proof and owner signoff remain pending. |
| Capability backlog | July sweep/date and catalog counts marked historical; selected support-role, weapon-flight and save/resume findings reconciled. | General EW capability does not execute a support-role label; presentation InFlight is not authoritative kinematics; checkpoint hashes are not saved state. |
| Traceability tooling | [Read-only checker](../../../tools/Test-PlanningArtifacts.ps1) and [finite manifest](../../../production/planning-reconciliation-2026-09-30.json). | 40 declared checks pass; external links are metadata only. This is a planning subset, not complete VER-07 validation or product acceptance. |
| Live Notion | Hub, wiki root, H4–H6 intake, Combat UX status and DRG-244 index refreshed; new [reconciliation page](https://app.notion.com/p/3ebf7cb4e4df81cd80b7ff559888a0bd). [Core/Weapons/Map design refresh](2026-09-30-core-design-refresh.md) published and refetched. | All eight mandatory sections and native person ownership verified; Draft design and delivery state remain distinct from human approval. Frozen Git mirror untouched. |
| Linear | Eight assigned children created; existing workflow/evidence/scope/ADR dependencies extended; H4–H6 milestone descriptions updated. DRG-208 title now matches its existing target. | Existing blocker relations and owner-only walk preserved. No future feature is marked Done. |

## Concrete backlog

| Issue | Deliverable | Dependency / disposition |
| --- | --- | --- |
| [DRG-342](https://linear.app/drgamtd-workspace/issue/DRG-342) | Active-artifact consistency checker | Parent DRG-188; implemented locally, review pending. |
| [DRG-343](https://linear.app/drgamtd-workspace/issue/DRG-343) | Core Simulation wiki draft | Parent DRG-256; eight source-backed sections, owner review pending. |
| [DRG-344](https://linear.app/drgamtd-workspace/issue/DRG-344) | Weapons wiki draft | Parent DRG-187; eight source-backed sections, owner review pending. |
| [DRG-345](https://linear.app/drgamtd-workspace/issue/DRG-345) | Unsupported mission role disclosure before Export/Play; draft Save allowed | Parent DRG-327; blocked by DRG-333/334/335. |
| [DRG-346](https://linear.app/drgamtd-workspace/issue/DRG-346) | Existing-map edit → validate → save → export → Play slice | Parent DRG-327; blocked by DRG-333/334/335. |
| [DRG-347](https://linear.app/drgamtd-workspace/issue/DRG-347) | Local presentation loading pilot and failure isolation | Parent DRG-326; blocked by inventory DRG-332 and ADR DRG-331. |
| [DRG-348](https://linear.app/drgamtd-workspace/issue/DRG-348) | Complete save-state / compatibility contract | Parent DRG-325; blocked by DRG-328/329/330. |
| [DRG-349](https://linear.app/drgamtd-workspace/issue/DRG-349) | Deterministic same-build save/resume slice | Parent DRG-325; additionally blocked by contract DRG-348. |

Existing DRG-275 owns Map/C2 design refresh; DRG-201 owns typed revision-bound evidence; DRG-244 owns the owner evidence index. These were extended rather than duplicated. DRG-331 is blocked by DRG-332; DRG-329 by DRG-328/330; DRG-333 by DRG-334/335. Relations were fetched after writes; initial missing DRG-333 links on DRG-345/346 were repaired and verified.

H4 reuses shipped editor surfaces. H5 starts with local presentation content. H6 plans save/resume before a separate later multiplayer scope. Platform WYSIWYG, manual/autosave/retention, architecture decisions and any frozen-hub waiver remain explicit questions. No new sprint dates or ADR acceptance were invented.

## Verification and provenance

Inspected local HEAD: `4190fb1b49cd15775b6b995b7c245d73828cd7a2`, with extensive pre-existing local changes preserved. Live Linear reports integration / owner-walk target `1902dc1299678ea17f75014406fd9c616e5576bd`; the following local checks do not validate that different revision.

Declared source-input snapshot: 1,825 existing tracked and nonignored untracked files under `src`, `data`, `tests`, `Game-Requirements`, plus the solution, SDK/build properties and requirements RTM. Sorted UTF-8 `SHA256  relative-path` rows joined by LF were hashed to `f63dc0f6d6b6503eb00413c57e57522117c70472fc24222e984e68a3ff35e257` at `2026-09-30T23:31:21.5567961Z`. This identifies the declared inspected/test input set; it is not a complete reproducible build or accepted VER-07 envelope.

| Check | Executed result |
| --- | --- |
| `dotnet build ProjectAegis.sln -m:1 --no-restore` | PASS; 0 warnings, 0 errors. |
| `dotnet test ProjectAegis.sln -v minimal --no-build --no-restore -m:1` | Corrected rerun PASS: Delegation 1109, UnityAdapter 743, Sim 585, Data 775, CLI 116, Excel 24 = **3352**, 0 failed, 0 skipped. |
| Headless `PlayModeSmokeHarnessTests`, same project / no-build / no-restore / single worker | PASS **26/26**. |
| Canonical `ReplayGoldenSuiteTests`, same project / no-build / no-restore / single worker | PASS **6/6**. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-PlanningArtifacts.ps1` | PASS **40 declared checks** on Windows PowerShell 5.1. |
| Checker TEMP fixtures | Worker verified one positive and nine negative cases: missing file/anchor/clause, contradictory current pointer, missing owner/revision/kind/result/owner-acceptance status. Each negative exits 1; repository inputs unchanged. |
| `tools/verify-ci-local.ps1` (Release) | PASS: restore; catalog firewall/import checks 67/67 and no tracked proprietary `.db3`; Release build 0 warnings/errors; full suite 3352/3352, replay 6/6, smoke 26/26. A process-local `dotnet` wrapper forwarded `-m:1` and threw on any nonzero native exit; repository script unchanged. |
| Scoped local Markdown validation | Requirements worker: 252 targets + 12 anchors passed. Roadmap link/whitespace checks passed; finite manifest independently passes after integration. |
| Hash and protected files | Hash `17144800277401907079` retained in 18 paths under tests/data. Diff empty for DelegationBridge.cs, regression goldens, existing CatalogWriteGate paths and production stage. |

The first fresh full run failed one `Wave4RtmIndexHonestyPinsTests` check because the expanded RTM header displaced its historical corpus W4 stamp. The stamp was restored alongside the additive September reconciliation. Targeted 5/5 and full 3352/3352 reruns pass; the test and runtime code were not changed to bypass the check.

Resolved SDK is **8.0.425** under the existing roll-forward configuration. Exact **8.0.400** policy compliance is unverified. Single-worker execution avoids the previously observed default-parallel MSBuild failure. Headless results are not Editor/Game View proof or owner acceptance. The Release CI script passed as detailed above. Secret scan is a separate Buildkite step, absent from `verify-ci-local.ps1`; `gitleaks` is not available on the local command path and that separate scan was not executed. Release script success does not establish secret-scan parity.

## GitNexus and architecture review

Path-qualified GitNexus query/context and local source were used together. Duplicate checkout names and known DRG-323/324 corruption/overload limitations prevent treating an unqualified graph as sole proof. `SimulationSession` upstream analysis showed provisional CRITICAL reach (111 direct / 360 impacted / four processes / twelve modules); no edits to that symbol were made. Future H6 implementation must repeat exact-symbol impact analysis on its delivered checkout and warn before high-risk changes.

Final `detect_changes(unstaged)` covers the entire dirty checkout, not only this request: 131 indexed sections/symbols, 149 files, four affected processes, medium aggregate risk. Reported processes include map selection and scenario-path loading already affected by pre-existing code changes. This planning task changes no existing product C# symbol; the checker is a new tooling file. The report must not be presented as a clean product diff or a pre-commit clearance.

Unity architect PR-finish verdict for this artifact scope:

```text
PASS — source-backed documentation/planning boundary and headless verification.
PASS — ADR-010 §2–3, ADR-007, ADR-001; ADR-011 Excel-primary preserved.
PASS — no authored runtime/host/asmdef/plugin/scene/prefab or bridge hotpath changes.
N/A  — Editor mutation, visual capture and assembly changes for this docs/tooling task.
PENDING — future feature execution, sprint QA signoff and DRG-208 owner acceptance.
```

## Remaining decisions

The requested artifact reconciliation is reviewable independently of feature delivery. Owner review of the design drafts and proposed criteria, accepted scope/ADRs for H4–H6, delivered-checkout Editor captures and the human DRG-208 walk remain their respective tracked work. Neither this report nor automated green checks grants those approvals. Local updates remain uncommitted.
