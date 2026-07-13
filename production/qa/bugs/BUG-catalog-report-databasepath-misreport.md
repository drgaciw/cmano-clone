# Bug Report

## Summary
**Title**: Catalog read-only report commands report the wrong `databasePath` when the requested `--db` file does not exist (silent fallback misattributed to the requested path)
**ID**: BUG-catalog-report-databasepath-misreport
**Severity**: S3-Minor
**Priority**: P3-Backlog
**Status**: Open
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-loop-10-cli-integration)

## Classification
- **Category**: CLI / Tooling (catalog read-only report pipeline)
- **System**: `ProjectAegis.MissionEditor.Cli` catalog report commands (`catalog_kill_chain_report`, `catalog_link_report`, `catalog_dependency_graph`)
- **Frequency**: Always (deterministic, 100% reproducible whenever `--db` points at a path that does not exist)
- **Regression**: No — appears to have existed since these commands were introduced (S33-08/S34-08/S37-03); not something this loop's changes caused.

## Environment
- **Build**: commit `5eab203` (branch `qa-loop-10-cli-integration`, worktree of `cmano-clone` main)
- **Platform**: Linux sandbox, .NET 8 SDK, `dotnet test` (no Unity Editor available)
- **Scene/Level**: N/A — plain .NET CLI (`ProjectAegis.MissionEditor.Cli`), not Unity PlayMode
- **Game State**: N/A

## Reproduction Steps
**Preconditions**: A valid Baltic Patrol catalog database resolvable via `CatalogReaderFactory.ResolveBalticPatrolDatabasePath()` (present in-repo at `assets/data/catalog/baltic_patrol.db`).

1. Call `CatalogKillChainReportCommand.Run(missingPath, writer)` (or the equivalent CLI invocation `catalog_kill_chain_report --db <path-that-does-not-exist>`), where `missingPath` is a non-null path that does **not** exist on disk.
2. Observe the command still returns exit code `0` and prints a full JSON report (this silent-fallback-to-default-catalog behavior is intentional and shared by all three commands).
3. Inspect the `databasePath` field of the emitted JSON payload.

**Expected Result**: The reported `databasePath` should reflect the database that was *actually opened and read* — i.e., the resolved Baltic Patrol fallback path — since that's the data the rest of the payload (findings/links/edges/hashes) was computed from. This is exactly what the sibling command `CatalogIntelligenceRunCommand` already does correctly (it reassigns `databasePath = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();` in its fallback branch before using it in output).

**Actual Result**: The payload's `databasePath` field echoes back the caller-supplied (nonexistent) path verbatim, because the code computes it as `databasePath ?? CatalogReaderFactory.ResolveBalticPatrolDatabasePath()` — a null-coalesce that only fires when the input is literally `null`. When the input is a non-null but missing/typo'd path, the report claims data came from a database that was never opened, while the actual read silently happened against a completely different (default/fallback) database. For a "deterministic read-only report for curator review" this is misleading: a curator or CI pipeline consuming this JSON has no way to tell that their `--db` argument was silently ignored.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.MissionEditor.Cli/CatalogKillChainReportCommand.cs` (fixed this loop)
  - `src/ProjectAegis.MissionEditor.Cli/CatalogLinkReportCommand.cs` (fixed this loop)
  - `src/ProjectAegis.MissionEditor.Cli/CatalogDependencyGraphCommand.cs` (fixed this loop)
- **Related systems**: `CatalogReaderFactory.ResolveBalticPatrolDatabasePath()` (`src/ProjectAegis.Data/Catalog/CatalogReaderFactory.cs`); the correctly-behaving sibling `CatalogIntelligenceRunCommand.Run` (`src/ProjectAegis.MissionEditor.Cli/CatalogIntelligenceRunCommand.cs`) which already reassigns the fallback path before reporting it — used as the reference pattern for this fix.
- **Root cause**: `OpenReader(string? databasePath)` internally decides which database file to actually open (input path if it exists, else the resolved Baltic fallback, else throw), but the `Run` method separately recomputed the *reported* path using only `databasePath ?? ResolveBalticPatrolDatabasePath()` — duplicating the fallback decision with weaker logic (`??` instead of "does the file exist") instead of reusing the value `OpenReader` actually decided on.

## Evidence
- **New regression test**: `ProjectAegis.MissionEditor.Cli.Tests.CatalogKillChainReportCommandTests.KillChain_reported_database_path_reflects_actual_source_when_input_path_missing` (`src/ProjectAegis.MissionEditor.Cli.Tests/CatalogKillChainReportCommandTests.cs`)
  - Before fix (red): `Assert.NotEqual(missingPath, reportedPath)` failed — `Assert.NotEqual() Failure: Strings are equal` — both were `/tmp/aegis-cli-kc-missing-<guid>.db`.
  - After fix (green): test passes; `reportedPath` now equals `CatalogReaderFactory.ResolveBalticPatrolDatabasePath()`.
- **Fix**: `OpenReader` in all three commands now takes an `out string resolvedDatabasePath` parameter and the `Run` methods use that value (instead of re-deriving it) when building the JSON payload's `databasePath` field.

## Related Issues
- None on file (no prior `production/qa/bugs/BUG-*.md` entries existed in this repo before this one).

## Notes
- Impact/blast-radius check (GitNexus MCP not reachable from this isolated worktree sandbox; did a manual Grep-based caller check instead): the only callers of `CatalogKillChainReportCommand.Run`, `CatalogLinkReportCommand.Run`, and `CatalogDependencyGraphCommand.Run` are `Program.cs` (thin CLI dispatch, passes the same `db`/`Console.Out` args straight through) and each command's own test file. No other production code inspects or depends on the `databasePath` field's value, and no existing test asserted a specific `databasePath` value, so this fix is **LOW risk**.
- Fixed all three commands together (not just the one with the new failing test) because they share the exact same duplicated-fallback-logic bug, verified by manual code comparison against the correctly-implemented `CatalogIntelligenceRunCommand`; leaving two of three fixed and one broken would have been an inconsistent, confusing half-fix.
- `CatalogReleaseDiffCommand` does not have this bug — it requires an explicit non-empty `databasePath` (no fallback resolution) and echoes it as-is, so there's no fallback-vs-reported mismatch possible there.
