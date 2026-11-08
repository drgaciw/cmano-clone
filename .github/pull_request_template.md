## Summary

<!-- What changed and why -->

## Verification

- [ ] `dotnet test ProjectAegis.sln -c Release` passes locally
- [ ] PlayMode smoke (if delegation/Unity adapter touched):  
      `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests`
- [ ] Sim/controller changes: replay golden or `/replay-verify` considered
- [ ] No proprietary CMO `*.db3` files added (catalog feeds via markdown/fixtures only — [dual-track](../docs/engineering/dual-track-cmo-analysis-and-catalog.md))
- [ ] If GitHub Actions billing blocked, attach local gate evidence per [production/qa/sprint-19-ci-local-gate-2026-06-08.md](../production/qa/sprint-19-ci-local-gate-2026-06-08.md)

## CI

PRs must pass **Buildkite** (`buildkite/cmano-clone`). Graphite optimizer may skip redundant stack runs.

See [docs/engineering/buildkite-ci.md](../docs/engineering/buildkite-ci.md) and [ci-and-branch-protection.md](../docs/engineering/ci-and-branch-protection.md).

## Definition of Done

PR tier. Sprint and Release tiers, waivers, and the checker for each item: [definition-of-done.md](../docs/engineering/definition-of-done.md).

- [ ] PR body has `Closes DRG-xxx` and a CMANO Surface line (CMANO).
- [ ] CI is green on the exact head: Build and test, inspect with no new `jb` findings, oracle, qa-gauntlet pytest, and Buildkite `buildkite/cmano-clone` (CI).
- [ ] GitNexus `impact` was run against the GitNexus index (`gitnexus-index`) and linked in the PR. CRITICAL blocks until the owner clears it (CMANO).
- [ ] No hard-no files (`DelegationBridge` Tick/hotpath, `CatalogWriteGate` writes, `SimulationSession`, `MapPlaceholderPanelHost`, `GlobeMapProductHost`, CMD-31/32/34, overlay hosts, S122/Launch) unless the owner has written sign-off or the change is split (CMANO; owner signs off).
- [ ] No silent ReplayGolden re-bless. Baltic hash `17144800277401907079` is unchanged unless the reason is stated and the owner has OK'd it (CI + CMANO).
- [ ] pr-finish verdict ([`pr-finish.md`](../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md)) is pasted in the PR body. Only PASS is mergeable (CMANO).
- [ ] QA is APPROVED, or APPROVED WITH CONDITIONS with a DRG id per condition (CMANO).
- [ ] G2/G3 defects from this PR are closed or deferred with a DRG id (CMANO).
- [ ] Player-visible change: DRG-208 owner Play Mode acceptance, or missing evidence is listed. Otherwise state `N/A: no player-visible change` (owner, human gate).
- [ ] No force-push or rebase of an owner stack after review starts (CMANO).
- [ ] Owner merges. Linear status is updated after merge (owner; CMANO).