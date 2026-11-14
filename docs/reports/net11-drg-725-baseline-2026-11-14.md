# DRG-725 — .NET 10.0.403 baseline capture (pre-.NET 11)

Captured: 2026-11-14 (Track A, read-only for product code)

## Environment

| Item | Value |
|------|-------|
| Host | `Ubuntu-Host` |
| `uname -a` | `Linux Ubuntu-Host 6.14.0-37-generic #37-Ubuntu SMP PREEMPT_DYNAMIC Fri Nov 14 22:10:32 UTC 2025 x86_64 x86_64 x86_64 GNU/Linux` |
| CPU | 13th Gen Intel Core i7-13850HX, 28 logical cores |
| x86-64-v2 check | `sse4_2` yes, `popcnt` yes (also `ssse3`, `cx16`) — **OK** |
| Branch | `net11/drg-725-baseline` |
| Commit | `d3a358ae3a1122f651c381e1e0d89bc8b84d7157` (= origin/main) |
| SDKs installed | 8.0.400, 8.0.422, **10.0.403**, **11.0.100** |
| SDK used for baseline | **10.0.403** (`dotnet --version` printed exactly `10.0.403` before every gate) |

### How 10.0.403 was forced

`global.json` was temporarily changed to `{ "version": "10.0.403", "rollForward": "disable" }`, then reverted with
`git checkout -- global.json`. Nothing from that change is committed. Verified after revert: `global.json` is back to
`latestMajor` and `dotnet --version` prints `11.0.100` again.

## Results

| # | Gate | Result |
|---|------|--------|
| 3 | `dotnet build ProjectAegis.sln -c Release` | **0 Warning(s), 0 Error(s)**. Same on a `--no-incremental` rebuild. |
| 4 | `dotnet test ProjectAegis.sln -v minimal -c Release` (after plugin DLL copy) | **4361 passed, 0 failed, 0 skipped, 4361 total** (floor ≥1638) |
| 5 | ReplayGolden filter (`--filter ReplayGolden`) | **17/17 pass** (name-substring match). CI's `ReplayGoldenSuiteTests` filter: **6/6 pass**. |
| 6 | `--filter PlayModeSmokeHarnessTests` | **25/25 pass** (floor ≥20) |
| 7 | Hash `17144800277401907079` in `tests/` + `data/` | **Present**, 18 files. Real production goldens (`WORLD_HASH=` or `REPLAY_CHECKPOINT=`): `replay-golden-baltic-engage-2026-06-02.txt`, `replay-golden-baltic-v2-patrol-band-b-2026-06-22.txt`, `replay-golden-baltic-kill-checkpoints-2026-06-02.txt`, `replay-golden-baltic-replay-checkpoints-2026-06-02.txt`. Other matches are isolation comments or README text. **OK** |
| 8 | `bash tools/copy-delegation-assemblies.sh` | **14 DLLs** in `unity/ProjectAegis/Assets/Plugins/ProjectAegis/` (list below) |
| 9 | `bash tools/buildkite/dotnet-ci.sh` | **Ran, exit 0, `=== PASS ===`** (see below) |
| 10 | `tools/resharper/inspect.ps1 -Check` | **Skipped**: no `pwsh` on host, and no `inspectcode.exe` or `jb` on PATH. The script also requires SDK 10.0.403 and ReSharper CLT 2026.2.1. |
| 11 | SDK selected under `rollForward: latestMajor` | See below. |

### Per-assembly test counts (Release, SDK 10.0.403)

| Assembly | Passed | Failed |
|----------|-------:|-------:|
| ProjectAegis.Sim.Tests | 652 | 0 |
| ProjectAegis.Delegation.Demo.Tests | 14 | 0 |
| ProjectAegis.Delegation.Tests | 1465 | 0 |
| ProjectAegis.Data.Excel.Tests | 24 | 0 |
| ProjectAegis.MissionEditor.Cli.Tests | 121 | 0 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 1227 | 0 |
| ProjectAegis.Data.Tests | 858 | 0 |
| **Total** | **4361** | **0** |

### Important caveat: plugin DLLs must be staged before the full suite

The first full-suite run, before step 8, gave **4360 passed / 1 failed / 4361 total**. The failure was
`UnityPluginEpicATypesTests.Unity_plugin_Delegation_dll_exports_Epic_A_types_used_by_Runtime_hosts`, with the message
"Missing plugin DLL". `unity/ProjectAegis/Assets/Plugins/**/*.dll` is gitignored (`.gitignore:33`), so a fresh worktree has
only `.meta` files. After `tools/copy-delegation-assemblies.sh` the suite was 4361/4361. `dotnet-ci.sh` already runs the
copy before testing. This is an environment-staging issue, not a regression.

### Plugin DLLs (14, netstandard2.1 publish)

```
Microsoft.Bcl.AsyncInterfaces.dll
Microsoft.Data.Sqlite.dll
ProjectAegis.Data.dll
ProjectAegis.Delegation.dll
ProjectAegis.Delegation.UnityAdapter.dll
ProjectAegis.Sim.dll
SQLitePCLRaw.batteries_v2.dll
SQLitePCLRaw.core.dll
SQLitePCLRaw.provider.e_sqlite3.dll
System.IO.Pipelines.dll
System.Net.Http.Json.dll
System.Runtime.CompilerServices.Unsafe.dll
System.Text.Encodings.Web.dll
System.Text.Json.dll
```

### `tools/buildkite/dotnet-ci.sh` (step 9)

The script was run as-is. The bootstrap reported `dotnet SDK resolved: 10.0.403`, with `global.json` forced as above.
It ran restore, the CmoMarkdown catalog gate (67 passed), Release build (0 warnings, 0 errors), plugin copy (14 DLLs), and a
serialized (`-m:1`) full test run. All assemblies passed with the same counts as above. The ReplayGoldenSuiteTests filter
gave 6/6 and PlayModeSmokeHarnessTests gave 25/25. The hash grep succeeded. `DelegationBridge` src refs: 244 (count only; no
edits made). Final output: `=== PASS ===`.

### Step 11: which SDK CI would select with `rollForward: latestMajor`

- **This host:** both 10.0.403 and 11.0.100 are installed, and the original `global.json` (`10.0.403` + `latestMajor`) makes
  `dotnet --version` print **11.0.100**. Any un-forced run on this host is therefore an 11.0.100 run, not a baseline.
- **GitHub:** `actions/setup-dotnet@v6` with `global-json-file: global.json` installs the version in the file, 10.0.403.
  The runner's pre-installed SDKs then decide what `dotnet` resolves to. If an 11.x SDK is pre-installed or present on PATH,
  `latestMajor` can select it. Hosted images normally have 10.x, and 11.x only when the image ships it.
- **Buildkite:** `tools/buildkite/agent-bootstrap-dotnet.sh` accepts any SDK that satisfies `global.json`. Its comments say
  `latestMajor` "intentionally selects a newer SDK when one is present". It only installs 10.0.403 when no SDK satisfies the pin.
  An agent with 11.0.100 installed will therefore build and test with 11.0.100 while the pin still reads 10.0.403.
- **Implication for DRG-725:** a green CI run does not prove 10.0.403 parity wherever 11.x is installed. Pin
  `rollForward` to `latestFeature` or `disable`, or run the net11 move deliberately, before treating CI as the 10.0.403 reference.
  Also note that `tools/resharper/inspect.ps1` hard-requires exactly 10.0.403.

## Worktree hygiene notes

- A full test run **modifies two tracked files**: `assets/data/catalog/aegis_public_corpus.db` and
  `assets/data/catalog/baltic_patrol.db`. They were restored with `git checkout --`.
- Copied plugin DLLs are gitignored and remain on disk (untracked and ignored).
- No source, csproj, `global.json`, or `production/test-floor.json` changes. Nothing pushed. Other worktrees untouched.
