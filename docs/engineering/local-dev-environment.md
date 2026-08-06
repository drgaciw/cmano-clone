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

## Related setup docs

| Topic | Doc |
|-------|-----|
| Build / test / CI-parity commands | [`../../README.md`](../../README.md), [`../../AGENTS.md`](../../AGENTS.md) |
| Cloud / headless agent environment | [`AGENTS.md` › Cursor Cloud](../../AGENTS.md#cursor-cloud-specific-instructions) |
| Buildkite CI pipeline | [`buildkite-ci.md`](buildkite-ci.md) |
| Branch protection / required checks | [`ci-and-branch-protection.md`](ci-and-branch-protection.md) |
| Agent methodology (Superpowers) | [`superpowers-setup.md`](superpowers-setup.md) |
