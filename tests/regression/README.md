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