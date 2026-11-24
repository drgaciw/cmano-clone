# Baseline-Regression & GitNexus Test-Selection Policy (`DRG-204`)

> **Requirements:** `ARCH-NFR-3`, `VER-06` (`VER-06.1`–`VER-06.3`), `DRG-204`, `DRG-323`  
> **C# Contract:** [`src/ProjectAegis.Data/Verification/BaselineRegressionSelectionPolicy.cs`](../../src/ProjectAegis.Data/Verification/BaselineRegressionSelectionPolicy.cs) (`ProjectAegis.Data.Verification`)  
> **Automated Pin Tests:** [`src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs`](../../src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs)

---

## 1. Purpose & Requirements

This policy governs how Project Aegis separates **fast inner-loop PR verification** from **blocking baseline-regression merge/release gates**, while enforcing **GitNexus** index integrity (`DRG-323`) and fail-closed conservative fallback (`VER-06`, `ARCH-NFR-3`):

- **`ARCH-NFR-3`:** Run `gitnexus impact` before moving or refactoring symbols across `Sim ↔ Delegation ↔ Data` assembly boundaries (`AGENTS.md` §GitNexus).
- **`VER-06`:** Continuous integration and local parity gates enforce monotonic test floors (`production/test-floor.json` + `AGENTS.md` §Hard Invariants), clean `Release` builds (`0` errors, `0` warnings), `ReplayGolden` invariance (Baltic v2 hash `17144800277401907079`), `PlayModeSmokeHarnessTests`, `31/31` architecture fences, clean-room catalog (`no *.db3`), and zero `DelegationBridge.cs` hotpath modifications.

---

## 2. Tiered Verification Matrix

| Tier (`TestRunTier`) | Scope & Intended Use | Blocking for Merge / Release? | Required Verification |
|---|---|---|---|
| `FastPrInnerLoop` | Local developer inner loop & advisory PR pre-flight | **No** (never substitutes for blocking CI) | Transitive closure selected by `GitNexusTestSelectionEngine.BuildPlan` + `UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests` (`31/31`) when `src/` changes |
| `BlockingMergeBaseline` | `.buildkite/pipeline.yml`, `.github/workflows/dotnet-ci.yml`, `.github/workflows/gauntlet-oracle.yml`, `tools/buildkite/dotnet-ci.sh` | **Yes** | Full `ProjectAegis.sln` Release build (`0` errors, `0` warnings), full 9-assembly test suite (`>= solutionMinPass` / active `AGENTS.md` floor, `0` failed, `0` skipped), `ReplayGoldenSuiteTests`, `PlayModeSmokeHarnessTests`, `31/31` architecture fences, `CmoMarkdown` / zero tracked `*.db3`, Baltic v2 hash `17144800277401907079`, zero `DelegationBridge.cs` hotpath diff |
| `ReleaseVerification` | Release-train & sprint closeout parity | **Yes** | Everything in `BlockingMergeBaseline` plus `netstandard2.1` Release build (`src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj`), monotonic ratchet validation on `production/test-floor.json`, `tools/verify-ci-local.ps1`, and live Unity Editor PlayMode / `DRG-244` owner-acceptance separation |

---

## 3. Exact Transitive Assembly & Path Dependency Matrix

Pinned to `src/ProjectAegis.Data.Tests/Architecture/CsprojReferenceGraphPinTests.cs`:

| Changed Product / Doc Root | Selected Test Assemblies (`FastPrInnerLoop`) |
|---|---|
| `src/ProjectAegis.Data/` | `ProjectAegis.Data.Tests`, `ProjectAegis.Data.Excel.Tests`, `ProjectAegis.Sim.Tests`, `ProjectAegis.Delegation.Tests`, `ProjectAegis.Ai.Host.Tests`, `ProjectAegis.Delegation.UnityAdapter.Tests`, `ProjectAegis.Delegation.Demo.Tests`, `ProjectAegis.MissionEditor.Cli.Tests` |
| `src/ProjectAegis.Ai.Contracts/` | `ProjectAegis.Ai.Contracts.Tests`, `ProjectAegis.Ai.Host.Tests`, `ProjectAegis.Delegation.UnityAdapter.Tests`, `ProjectAegis.Delegation.Demo.Tests`, `ProjectAegis.MissionEditor.Cli.Tests` |
| `src/ProjectAegis.Sim/` or `src/ProjectAegis.Sim.Benchmark/` | `ProjectAegis.Sim.Tests`, `ProjectAegis.Delegation.Tests`, `ProjectAegis.Ai.Host.Tests`, `ProjectAegis.Delegation.UnityAdapter.Tests`, `ProjectAegis.Delegation.Demo.Tests`, `ProjectAegis.MissionEditor.Cli.Tests` |
| `src/ProjectAegis.Delegation/` | `ProjectAegis.Delegation.Tests`, `ProjectAegis.Ai.Host.Tests`, `ProjectAegis.Delegation.UnityAdapter.Tests`, `ProjectAegis.Delegation.Demo.Tests`, `ProjectAegis.MissionEditor.Cli.Tests` |
| `src/ProjectAegis.Data.Excel/` | `ProjectAegis.Data.Excel.Tests`, `ProjectAegis.Data.Tests`, `ProjectAegis.MissionEditor.Cli.Tests` |
| `src/ProjectAegis.Delegation.UnityAdapter/` | `ProjectAegis.Delegation.UnityAdapter.Tests`, `ProjectAegis.Delegation.Demo.Tests`, `ProjectAegis.MissionEditor.Cli.Tests` |
| `src/ProjectAegis.Ai.Host/` | `ProjectAegis.Ai.Host.Tests`, `ProjectAegis.MissionEditor.Cli.Tests` |
| `src/ProjectAegis.MissionEditor.Cli/` | `ProjectAegis.MissionEditor.Cli.Tests` |
| `src/ProjectAegis.Delegation.Demo/` | `ProjectAegis.Delegation.Demo.Tests` |
| `docs/` | `ProjectAegis.Data.Tests` (pins: `KillChainTraceabilityMatrixPinsTests`, `Wave4RtmIndexHonestyPinsTests`, `BaselineRegressionSelectionPolicyTests`), `ProjectAegis.Delegation.Tests` (`EditorVerbCatalogTests`) |
| `Game-Requirements/` | `ProjectAegis.Data.Tests` (`Wave4RtmIndexHonestyPinsTests`) |

Whenever `ProjectAegis.Delegation.UnityAdapter.Tests` is selected, `ReplayGolden` and `PlayModeSmokeHarnessTests` are mandatory.

---

## 4. Fail-Closed Conservative Fallback Rules

`GitNexusTestSelectionEngine.BuildPlan` **must fail closed** to `RunFullSolution = true` (all 9 solution test assemblies + `ReplayGolden` + `PlayModeSmoke` + `31/31` architecture fences) on **any** of the following conditions:

1. **Non-PR tier:** `RequestedTier` is `BlockingMergeBaseline` or `ReleaseVerification`.
2. **Unhealthy or stale GitNexus index (`DRG-323`):** `HealthStatus != Healthy`, `IndexedCommitSha != TargetCommitSha`, `NodeCount <= 0`, `EdgeCount <= 0`, `EmbeddingCount <= 0`, `HasGarbageIdsOrEmptyNames == true`, or `VectorSearchStatus` not in `{"vector-index", "exact-scan"}`.
3. **High / unknown impact risk:** `ImpactRisk` is `Unknown`, `High`, or `Critical`.
4. **Empty or unmapped diff:** `ChangedPaths` is empty, contains blank entries, or contains any path outside the mapped assembly/doc roots.
5. **Global / Hard-No / scenario / catalog / CI / architecture paths:** Any modification to `ProjectAegis.sln`, `Directory.Build.props`, `global.json`, `production/test-floor.json`, `.buildkite/pipeline.yml`, `.github/workflows/dotnet-ci.yml`, `tools/buildkite/dotnet-ci.sh`, `data/scenarios/` (`data/scenarios/baltic-patrol.policy.json`), `assets/data/catalog/`, `tests/regression/README.md`, `*.csproj`, `Architecture/`, `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`, `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs`, or `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs`.

---

## 5. Explicit `DRG-323` GitNexus Reindex Policy

- **NEVER** run live `gitnexus analyze` (incremental or forced) on a dirty working tree or on a feature branch. Incremental `analyze` or concurrent open `gitnexus mcp` handles corrupt `.gitnexus/lbug` (`DRG-323`; see `docs/engineering/gitnexus-index-health.md`).
- Reindex **only** on a clean `main` checkout with all `gitnexus mcp` handles stopped and no `.gitnexus/store.json` shared store pointer, strictly via `tools/gitnexus-safe-reindex.sh`, which enforces:
  - `analyze --force --embeddings --skip-agents-md --skip-skills`
  - Post-check integrity assertions on `.gitnexus/meta.json`, `.gitnexus/gitnexus.json`, embedding parity, vector search readiness, and zero empty symbol names.
- Programmatic enforcement: `GitNexusReindexPolicy.EvaluateStrictFeatureBranchPolicy` refuses non-`main` branches and dirty trees unconditionally.

---

## 6. Monotonic Test-Floor Governance

- `production/test-floor.json` stores the committed CI floor (`solutionMinPass`, `replayGoldenMin`, `playModeSmokeMin`, `balticV2Hash`, `baselineWarnings`).
- `BaselineRegressionPolicy.ValidateFloorMonotonicity` enforces that floors never regress below `HistoricalSolutionMinFloor` (`1638`) or the prior committed manifest, `baselineWarnings == 0`, and `balticV2Hash == "17144800277401907079"`.
- Sprint-level floors in `AGENTS.md` (e.g., Sprint 6 `>= 4,987`) compose monotonically via `Math.Max(floor.SolutionMinPass, sprintEffectiveSolutionFloor)`.
