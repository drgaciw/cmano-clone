# Definition of Done

Project Aegis (`cmano-clone`) uses three gates. A pull request, a sprint, and a release are each done only when every item in that tier is met or waived.

This document was drafted with the Jev router and approved by the owner on 2026-10-29.

The PR checklist in [`.github/pull_request_template.md`](../../.github/pull_request_template.md) mirrors the PR tier. Sprint files may add sprint-specific items. They never drop a project item from this document.

## Waivers

Only the owner (drg) can waive a blocker at any tier. A waiver is written on the pull request or the Linear issue and names the item waived. CMANO, CI, and other agents cannot waive.

## PR

Each item names who checks it. CI checks are green on the exact pull-request head SHA.

| # | Item | Checker |
| --- | --- | --- |
| 1 | PR body has `Closes DRG-xxx` and a CMANO Surface line. | CMANO |
| 2 | CI is green on the exact head: [Build and test](../../.github/workflows/dotnet-ci.yml) (`dotnet-ci`), [inspect](../../.github/workflows/resharper.yml) with no new JetBrains (`jb`) findings (`ReSharper regression gate`, `tools/resharper/inspect.ps1 -Check`), [oracle](../../.github/workflows/gauntlet-oracle.yml) (`gauntlet-oracle`), [qa-gauntlet pytest](../../.github/workflows/gauntlet-pytest.yml) (`gauntlet-pytest`), and Buildkite [`buildkite/cmano-clone`](../../.buildkite/pipeline.yml). | CI |
| 3 | GitNexus `impact` (upstream) is run against the GitNexus index (`gitnexus-index`; repo index name `cmano-clone`, see [gitnexus-index-health.md](gitnexus-index-health.md)) and the result is linked in the PR. **CRITICAL** blocks merge until the owner clears it. | CMANO |
| 4 | No hard-no files unless the owner has written sign-off or the change is split out: `DelegationBridge` Tick / hotpath, `CatalogWriteGate` writes, `SimulationSession`, `MapPlaceholderPanelHost`, `GlobeMapProductHost`, CMD-31 / CMD-32 / CMD-34 seams, overlay hosts, and S122 / Launch. | CMANO; owner signs off |
| 5 | No silent ReplayGolden re-bless. Baltic hash `17144800277401907079` is unchanged unless the PR states the reason and the owner has OK'd it. | CI + CMANO |
| 6 | The pr-finish verdict from [`production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`](../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md) is pasted in the PR body. Only **PASS** is mergeable. | CMANO |
| 7 | QA is **APPROVED**, or **APPROVED WITH CONDITIONS** with a DRG id per condition. | CMANO |
| 8 | G2 and G3 defects from this PR are closed, or deferred with a DRG id. | CMANO |
| 9 | Player-visible change: [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208) owner Play Mode acceptance is recorded, or missing evidence is listed. Otherwise the PR states `N/A: no player-visible change`. | owner, human gate |
| 10 | No force-push or rebase of an owner stack after review starts. | CMANO |
| 11 | The owner merges. Linear status is updated after merge. | owner; CMANO |

`/smoke-check sprint` is a sprint gate. It is not a PR gate.

## Sprint

| # | Item | Checker |
| --- | --- | --- |
| 1 | Every story in the sprint meets the PR tier, or it is moved out of the sprint with a DRG id. | CMANO |
| 2 | The sprint file's Definition of Done section is ticked, and each tick has evidence. Sprint DoD sections may add items. They never drop a project item from this document. | CMANO |
| 3 | `/smoke-check sprint` passes on `main` after the last merge of the sprint. This check moved here from the PR tier. | CMANO |
| 4 | [DRG-205](https://linear.app/drgamtd-workspace/issue/DRG-205) gate evidence is attached. | CMANO |
| 5 | Open G2 and G3 defects are zero, or each one is deferred with a DRG id and a target sprint. | CMANO |
| 6 | The owner completes a Play Mode walk for player-visible stories. | owner |
| 7 | Linear stories are Done or carried. None remain stale In Progress. | CMANO |

## Release

| # | Item | Checker |
| --- | --- | --- |
| 1 | Every sprint in the release meets the sprint tier. | CMANO |
| 2 | Full CI and Buildkite are green on the release commit. ReplayGolden and Baltic hash `17144800277401907079` are verified on that commit. | CI |
| 3 | No G2 or G3 defect is open without an owner waiver. | owner |
| 4 | Full [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208) Play Mode acceptance is recorded on the release build. | owner, human gate |
| 5 | S122 / Launch gate items are satisfied. | owner |
| 6 | Release notes list the merged DRG ids and the deferrals. | CMANO |
| 7 | The owner records release GO. | owner |
