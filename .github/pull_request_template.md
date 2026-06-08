## Summary

<!-- What changed and why -->

## Verification

- [ ] `dotnet test ProjectAegis.sln -c Release` passes locally
- [ ] PlayMode smoke (if delegation/Unity adapter touched):  
      `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests`
- [ ] Sim/controller changes: replay golden or `/replay-verify` considered
- [ ] If GitHub Actions billing blocked, attach local gate evidence per [production/qa/sprint-19-ci-local-gate-2026-06-08.md](../production/qa/sprint-19-ci-local-gate-2026-06-08.md)

## CI

PRs must pass **`.NET CI`** (`build_test`). Graphite stacks also run **Graphite CI** when the optimizer does not skip.

See [docs/engineering/ci-and-branch-protection.md](../docs/engineering/ci-and-branch-protection.md).