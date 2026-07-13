# Replay regression goldens (req 17)

Pinned world/detection hashes and optional `FINGERPRINT_SHA256` for Baltic harness runs.

> Determinism model, RNG/float-formatting rules, and pitfalls that break these goldens:
> [`docs/engineering/determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md).

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

## S67-02 Regression Baseline Lock (pinned from S66 audit)

**Locked per** production/release-train-scope-boundary-2026-06-24.md (S67: Regression baseline lock; standing invariants: test baseline monotonic, ReplayGolden 6/6, C2 18/18+, hash `17144800277401907079` immutable w/o golden ADR; GitNexus detect_changes + context pre; all artifacts cite boundary + roadmap §0/5/7/10 + execute-plan). S66 results audited/verified: 1232/0f, 6/6 replay incl Baltic v2, hash 17144800277401907079.

**Pinned values (S67 baseline, verification-before RUN+READ):**
- Tests: 1232/0f (279 Sim + 247 Del + 406 Data + 252 UA + 43 Cli + 5 Excel); >=1229 monotonic
- ReplayGolden: 6/6 PASS
- C2: 18/18 PASS
- Hash: 17144800277401907079 preserved in goldens
- ZERO hotpath DelegationBridge
- GitNexus: detect_changes (risk medium doc-only expected); context(ReplayGoldenRegressionCatalog)

**Core Replay 6/6 goldens (from ReplayGoldenRegressionCatalog.All):**
- replay-golden-baltic-engage-2026-06-02.txt (baltic-patrol seed42 ticks4)
- replay-golden-baltic-comms-2026-06-02.txt (baltic-patrol-comms seed42 ticks6)
- replay-golden-baltic-classify-2026-06-02.txt (baltic-patrol-classify seed42 ticks4)
- replay-golden-baltic-stale-2026-06-04.txt (baltic-patrol-stale seed11 ticks3)
- replay-golden-baltic-spoof-2026-06-04.txt (baltic-patrol-spoof seed7 ticks5)
- replay-golden-baltic-readiness-2026-06-04.txt (baltic-patrol-readiness seed7 ticks5)

**Baltic v2 goldens (9 for S66 manifest, included in regression):** replay-golden-baltic-v2-comms-challenged-2026-06-22.txt, -jammed, -mission-event (hash), -patrol (hash), -patrol-band-b (hash), -patrol-band-c (hash), -patrol-mission-v2 (hash), -theater, -theater-alt (dated 2026-06-22).

**Verification commands (pinned for S67+ use pre any merge):**
```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build ProjectAegis.sln -c Release
dotnet test ProjectAegis.sln -v minimal --no-build --no-restore
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build --no-restore -v minimal --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build --no-restore -v minimal --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"
grep -r 17144800277401907079 tests/regression/ --include="*.txt" | head -5
# GitNexus (MCP search_tool first): gitnexus__detect_changes(scope=compare base_ref=main); gitnexus__context on ReplayGoldenRegressionCatalog
```

**S66 audit evidence (smoke-sprint-66-closeout.md):** Build 0e/0w; test 1232/0f (full RUN+READ); replay 6/6 (171ms); C2 18/18 (259ms); hash hits incl v2; GitNexus list/detect/impact pre (Catalog CRITICAL178 etc per boundary §5). No regression.

**Status:** S67-02 LOCKED. Cite production/release-train-scope-boundary-2026-06-24.md exactly. verification-before complete (GitNexus first + gates RUN). Goldens + catalog are the manifests. S67-02 COMPLETE.