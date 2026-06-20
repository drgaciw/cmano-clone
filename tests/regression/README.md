# Replay regression goldens (req 17)

Pinned world/detection hashes and optional `FINGERPRINT_SHA256` for Baltic harness runs.

## CI gate

`ReplayGoldenSuiteTests` runs on every PR via Buildkite ([`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) / [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh)).

## Regenerate a golden

From repo root:

```bash
dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed <SEED> --scenario <policy-id> --ticks <N>
```

Copy `WORLD_HASH`, `DETECTION_WORLD_HASH`, and `FINGERPRINT_SHA256` into the matching `replay-golden-*.txt` file. See `ReplayGoldenRegressionCatalog.cs` for seed/scenario/ticks per file.

## Local verify

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter FullyQualifiedName~ReplayGoldenSuiteTests
```

## S39-05 replay maint note (isolated track only)

S39-05 (Perf P1 follow-up + replay maintenance): ReplayGoldenSuiteTests 6/6 verified conceptually + run (see perf-profile-polish-baseline-2026-06-19.md delta append). Maintenance only via isolated fixtures (`replay-golden-*.txt`); **production Baltic hash unchanged** (`17144800277401907079`); no `DelegationBridge.cs` edits. Per `production/polish-scope-boundary-2026-06-19.md` + sprint-39 S39-05 ACs + qa-plan-sprint-39-2026-06-20.md. Determinism spot clean (no prod hash touch).

S39-05 COMPLETE - isolated track.