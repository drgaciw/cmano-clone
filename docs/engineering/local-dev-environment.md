# Local developer environment — editor setup & troubleshooting

Practical setup notes for working on this repository in a local editor, focused on
issues that are specific to **this workspace's shape**: a large .NET 8 solution plus a
Unity project, plus many gitignored agent-tooling and session trees. Cloud/headless
setup lives in [`AGENTS.md`](../../AGENTS.md#cursor-cloud-specific-instructions); CI
parity lives in [`buildkite-ci.md`](buildkite-ci.md) and
[`ci-and-branch-protection.md`](ci-and-branch-protection.md).

---

## Why this workspace is "large"

The tree that an editor tries to index and watch is much bigger than the tracked
source. On top of `src/**` and `unity/ProjectAegis/Assets/**`, a working checkout
accumulates:

- **Build output** — `**/bin/`, `**/obj/` for every .NET project (many projects).
- **Git internals** — `.git/objects/` (thousands of loose/packed objects).
- **Unity caches** — `unity/ProjectAegis/Library/`, `Temp/`, `Logs/`,
  `MemoryCaptures/`, and `mono_crash.mem.*.blob` crash dumps.
- **Agent / session tooling** (all gitignored) — `.worktrees/`, `worktrees/`,
  `.hindsight-local/`, `.forgeflow/run/`, `.atl/`, `.grok/`, `agent-tools/`,
  `mcps/`, `terminals/`, `production/session-state/`, `production/session-logs/`.
- **Reference data & scratch** — `tools/cmano-db-crawler/_raw/`, `scratch/`,
  `docs/manual/assets/images/`.

Every one of these paths is generated or local-only (see `.gitignore`), so nothing is
lost by hiding it from the editor's watcher, search, and file tree.

---

## VS Code on Linux — file watcher exhaustion (ENOSPC)

### Symptom

Opening the workspace in VS Code on Linux shows:

> **Visual Studio Code is unable to watch for file changes in this large workspace (error ENOSPC)**

### Root cause

VS Code watches files with the Linux kernel's `inotify` subsystem. Each watched
directory consumes one entry from a per-user budget (`fs.inotify.max_user_watches`,
default commonly **8,192** on many distros). The generated trees above blow past that
budget, so the kernel returns `ENOSPC` ("no space left on device" — here it means "no
watch descriptors left", not disk space).

### The two-part fix (both are needed)

**1. Workspace watcher/search exclusions — already committed.**

[`.vscode/settings.json`](../../.vscode/settings.json) at the repo root ships shared
exclusions so a fresh clone is comfortable with no manual step:

- `files.watcherExclude` — stops `inotify` from watching generated/local trees. This
  is what actually reduces the watch count.
- `search.exclude` — keeps full-text search fast and results relevant (does not affect
  watches).
- `files.exclude` — hides `bin/`, `obj/`, and `mono_crash.mem.*.blob` from the
  Explorer tree entirely.

> Editing `.vscode/settings.json`? Keep `files.watcherExclude` and `search.exclude` in
> sync when you add a new generated/local path, and add the same path to `.gitignore`.
> Only add **generated or local-only** paths — never exclude tracked source.

**2. Raise the kernel watch budget on the host — one-time, system-wide.**

The workspace file reduces demand but cannot change a kernel limit. On the host:

```bash
echo 'fs.inotify.max_user_watches=524288' | sudo tee /etc/sysctl.d/99-vscode-inotify.conf
sudo sysctl --system
cat /proc/sys/fs/inotify/max_user_watches   # should print 524288
```

`524288` is VS Code's recommended value. **Memory constraint:** each watch consumes
~1,080 bytes of unswappable kernel memory, so 524,288 watches is a ~540 MiB upper
bound if fully used. In memory-constrained environments (small VMs/containers) pick a
smaller value (e.g. `262144`) and lean harder on `files.watcherExclude`.

### Verify / diagnose

```bash
# Current limit
cat /proc/sys/fs/inotify/max_user_watches

# Watches currently held per process (highest first) — spot a runaway watcher
find /proc/*/fd -lname anon_inode:inotify 2>/dev/null \
  | cut -d/ -f3 | sort | uniq -c | sort -nr | head

# Confirm a heavy tree is excluded, not watched: after opening the workspace,
# a `git status`-clean checkout should not report the editor holding watches on
# unity/ProjectAegis/Library or **/obj.
```

If ENOSPC persists after both fixes: reload the VS Code window (the watcher only picks
up `settings.json` changes on reload), and confirm you opened the **repo root** (the
committed `.vscode/settings.json` only applies to the workspace rooted there).

---

## Other editors / platforms

- **macOS / Windows** do not use `inotify` and won't hit this specific error, but the
  same `.vscode/settings.json` exclusions still make search and the file tree faster.
- **JetBrains Rider / other IDEs** — replicate the exclusion list from
  `.vscode/settings.json` in the IDE's "excluded folders" settings for the same
  responsiveness benefit; the kernel `max_user_watches` bump also helps file-watching
  IDEs on Linux.
- **Cursor** inherits `.vscode/settings.json`, so the same exclusions apply.

---

## Unity Editor project bootstrap (from the headless repo)

The simulation is a headless-first **.NET 8** solution; `dotnet build` / `dotnet test` need **no**
Unity. The `unity/ProjectAegis/` project is an **optional rendering layer** you only bootstrap when
you actually want the Editor / Play Mode. It ships as *scaffolding*: the Unity plugin DLLs and some
generated project files are **gitignored**, so a fresh clone has to be primed before it will open
cleanly in Unity Hub. This section is the operational pointer the engineering index otherwise lacks;
the canonical step list lives in [`unity/ProjectAegis/README.md`](../../unity/ProjectAegis/README.md)
and the scene checklist in [`unity/ProjectAegis/PLAYMODE-SMOKE.md`](../../unity/ProjectAegis/PLAYMODE-SMOKE.md).

### One-shot scaffold (Windows / PowerShell)

From the repo root, **after** a Release build so the plugin publish has inputs:

```powershell
dotnet build ProjectAegis.sln -c Release
./tools/init-unity-project.ps1
```

[`tools/init-unity-project.ps1`](../../tools/init-unity-project.ps1) is idempotent and does the whole
bring-up: it creates the folder layout (`Assets/Plugins/ProjectAegis`, `Assets/Scripts/Runtime`,
`Packages`, `ProjectSettings`), seeds `Packages/manifest.json` from
[`manifest.template.json`](../../unity/ProjectAegis/Packages/manifest.template.json) and
`ProjectSettings/ProjectVersion.txt` (pinned to **6000.3.14f1**) if absent, copies any `Runtime/*.cs`
into `Assets/Scripts/Runtime`, then chains
[`copy-delegation-assemblies.ps1`](../../tools/copy-delegation-assemblies.ps1) and
[`Test-UnityPluginAssemblies.ps1`](../../tools/Test-UnityPluginAssemblies.ps1). Open the resulting
`unity/ProjectAegis` folder in **Unity Hub 6.3 LTS**.

### Plugin assemblies — the copy step in detail

`copy-delegation-assemblies.ps1` runs `dotnet publish` on
`src/ProjectAegis.Delegation.UnityAdapter/…csproj` for **`netstandard2.1`** into a temp dir, copies
**all** publish-output `*.dll` into `Assets/Plugins/ProjectAegis/`, and cleans up.
`Test-UnityPluginAssemblies.ps1` then asserts the expected set is present and exits non-zero if not:

| Category | DLLs it checks |
|----------|----------------|
| Core | `ProjectAegis.Data.dll`, `ProjectAegis.Sim.dll`, `ProjectAegis.Delegation.dll`, `ProjectAegis.Delegation.UnityAdapter.dll` |
| Transitive | `Microsoft.Data.Sqlite.dll`, `System.Text.Json.dll`, `SQLitePCLRaw.core.dll` |

On success it prints the total DLL count; on a miss it exits `1` and tells you to re-run the copy
script. (After Epic A host work it also nudges you to run `dotnet test --filter UnityPluginEpicATypesTests`
as a type-export guard.)

### Linux / no-PowerShell hosts

`init-unity-project.ps1`, `copy-delegation-assemblies.ps1`, and `Test-UnityPluginAssemblies.ps1` are
**PowerShell-only**. On a Linux VM or container without `pwsh`, use the committed bash equivalent of
the load-bearing copy step instead:

```bash
dotnet build ProjectAegis.sln -c Release
./tools/copy-delegation-assemblies.sh   # same netstandard2.1 publish → Assets/Plugins/ProjectAegis, prints DLL count
```

[`copy-delegation-assemblies.sh`](../../tools/copy-delegation-assemblies.sh) exports
`PATH="$HOME/.dotnet:$PATH"` first (matching the [`AGENTS.md`](../../AGENTS.md#cursor-cloud-specific-instructions)
SDK install location) and mirrors the `.ps1` output. On a normal clone
`Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt` are **already tracked** — do
**not** recreate them from `manifest.template.json` (that template is the three-package seed used
only when those files are missing; overwriting the live manifest drops MCP/AI/Linux/OpenUPM
entries). Linux bring-up is: keep the tracked files, run the bash copy step. There is no bash
port of the folder-scaffold or `Test-UnityPluginAssemblies.ps1`; install `pwsh` only if you need
those.

### Common pitfalls

- **`netstandard2.1`, never `net8.0`.** Unity 6.3 loads only netstandard2.1 plugins (Player Settings
  `apiCompatibilityLevel: 6`). Copying `net8.0/bin` output into `Assets/Plugins` will fail to load —
  always let the copy script publish the correct TFM.
- **Plugins are gitignored → re-copy after every clone and after any .NET change.** A stale or empty
  `Assets/Plugins/ProjectAegis/` is the usual cause of missing-type / compile errors in the Editor;
  re-run the copy script and the verifier.
- **Build first.** Both the scaffold and copy scripts publish from source, so run
  `dotnet build ProjectAegis.sln -c Release` before them.
- **Don't re-add removed packages.** The manifest template pins only `com.unity.addressables`,
  `com.unity.burst`, and `com.unity.ui`; `com.unity.entities` was removed (world state is managed, not
  DOTS) and must not be re-added without human package approval — see the
  [Unity README §5](../../unity/ProjectAegis/README.md).
- **The Editor is optional.** CI-style gates (`dotnet test`, the headless Play Mode smoke harness) run
  without Unity; in Cloud VMs the Editor is usually not installed at all — prefer the headless path
  ([`AGENTS.md` › Cursor Cloud](../../AGENTS.md#cursor-cloud-specific-instructions)).

---

## Related setup docs

| Topic | Doc |
|-------|-----|
| Build / test / CI-parity commands | [`../../README.md`](../../README.md), [`../../AGENTS.md`](../../AGENTS.md) |
| Unity Editor project + plugin bootstrap | [`unity/ProjectAegis/README.md`](../../unity/ProjectAegis/README.md), [`PLAYMODE-SMOKE.md`](../../unity/ProjectAegis/PLAYMODE-SMOKE.md) |
| Cloud / headless agent environment | [`AGENTS.md` › Cursor Cloud](../../AGENTS.md#cursor-cloud-specific-instructions) |
| Buildkite CI pipeline | [`buildkite-ci.md`](buildkite-ci.md) |
| Branch protection / required checks | [`ci-and-branch-protection.md`](ci-and-branch-protection.md) |
| Agent methodology (Superpowers) | [`superpowers-setup.md`](superpowers-setup.md) |
