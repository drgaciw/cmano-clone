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