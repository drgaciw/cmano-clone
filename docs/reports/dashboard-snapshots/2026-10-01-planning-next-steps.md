# Planning next steps — scoped dashboard, 2026-10-01

**Owner:** drg amtd. **Stage:** Release. **Source base:** `1902dc1299678ea17f75014406fd9c616e5576bd`. **Checkout:** isolated `.worktrees/planning-next-20261001`; only the reviewed planning changes are included. The original dirty detached checkout and other stacks remain separate. The verified-clean local trunk was fast-forwarded through `gt sync --no-restack`; unrelated stacks were not restacked or cleaned.

Foundation: [Graphite draft #690](https://app.graphite.com/github/pr/drgaciw/cmano-clone/690), commit `f52cdcdf1f4026bbf892758ef7683425e03882d3`, 30 explicit planning/checker paths. Stacked [draft #691](https://app.graphite.com/github/pr/drgaciw/cmano-clone/691) records the accepted ADR-017 direction, H4 boundary, horizon briefs, and current delivery preflight. Both initial heads passed Buildkite, .NET CI, ReSharper and gauntlet checks; later documentation-only publication-status amendments have their own current CI status on the PR. No merge or owner Play Mode acceptance is included.

## Current regression verification

| Gate | Fresh result |
| --- | --- |
| Exact SDK | 8.0.400, installed in task-local TEMP; no global SDK/configuration change |
| Debug and Release build | 0 errors, 0 warnings; `-m:1` |
| Debug and Release full solution | **3,425 passed, 0 failed/skipped** in each configuration: Delegation1108, UnityAdapter820, Sim584, Data773, CLI116, Excel24 |
| Proxy PlayModeSmokeHarness | **25/25** in each configuration |
| Canonical ReplayGoldenSuite | **6/6** in Release |
| `verify-ci-local.ps1` | PASS with process-local exact-SDK wrapper adding `-m:1` and throwing on native failure; catalog67/67, no tracked proprietary `.db3` |
| Planning checker | Foundation40 finite checks; follow-up extends declared references to **49**, including these reviews/decision and the pending Notion text. External links are metadata, not complete VER-07/product acceptance |
| Hash and scope | v2 hash `17144800277401907079` present in 18 data/regression paths; no source/data/Unity/golden diff |

First fresh full run failed the existing Unity plugin export guard because ignored DLLs were absent in the new worktree. The approved `tools/copy-delegation-assemblies.ps1` generated **netstandard2.1** plugins using SDK8.0.400; the full suite was rerun successfully. No DLLs or temporary publish outputs enter the review. Separate gitleaks was not installed/run. Release final command output is in task TEMP `aegis-planning-ci-20261001-final.log`; executable source remains exactly the base above. Later documentation-only edits require checker/reference and diff verification, rather than treating these tests as their criterion-level proof.

## Decisions, delivery and acceptance

- ADR-017 Accepted from explicit human “proceed with these recommended next steps”: shared engine-free core and CLI/MCP, in-client v1, optional later Scenario Lab with no fork. Repository review/integration remains pending; no separate meeting or implementation is inferred.
- [H4 review](../../../production/agentic/h4-phase2-gui-ux-review-2026-10-01.md): 20 unresolved findings; bounded [scope draft](../../../production/scenario-editor-phase2-gui-scope-boundary-2026-10-01.md) supports DRG-333/345/346. Excel-primary ADR-011 retained.
- [H5 review](../../../production/agentic/h5-content-inventory-review-2026-10-01.md): inventory corroborated, current metadata resolver does not prove Addressables asynchronous loading/lifetime. Pilot budgets and ADR remain pending.
- [H6 review](../../../production/agentic/h6-save-model-review-2026-10-01.md): changes required before implementation-ready feasibility; checkpoints contain hashes, not restore state. Complete contract and differential proof precede the save slice; multiplayer stays separate.
- [G1/harness review](../../superpowers/reviews/2026-10-01-g1-harness-review.md): #662 needs criterion/owner/definition corrections. #682 contributes headless projection acceptance; its own tested revision and result are recorded separately there.
- [Play-entry/fixture preflight](../../superpowers/reviews/2026-10-01-play-entry-fixture-preflight.md): missing validated commit/ORBAT/Planning path and positive live authored group/coverage composition. DRG-243/239 delivery remains open; DRG-243 retains DRG-197 blocker.

GitNexus staged foundation: 30 files, 58 document sections, zero affected execution processes, LOW. Compare scope also reports unstaged documentation in this provider; use the explicit staged/local diff to bound committed scope. CatalogWriteGate upstream query is CRITICAL (102 direct, 265 total, five processes, interface lower-bound); existing write paths remain untouched. Path/index and overload issues DRG-197/323/324 stay open.

Unity `:8080` probe failed; no new Editor captures. DRG-208 remains human-only and pending its entry/harness/fixture/evidence predecessors. Notion initially returned HTTP500 `Cross-cell memcached access is not allowed`, then recovered. After fetching the page/specification, automatic approval review rejected the full mirror as an unauthorized export of sensitive planning details. GitHub confirmed `drgaciw/cmano-clone` is public; the safer update through the same Notion connector published only the public #690/#691 links. Reconciliation and horizon intake were refetched and the links verified. Their historical September30 content and native Draft/unverified status remain intact. The [publication record and retained draft](../../superpowers/reviews/2026-10-01-notion-followup-pending.md) distinguish published links from unpublished detailed text; no bypass or full-mirror success is claimed.
