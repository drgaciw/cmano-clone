# UPGRADE-RUNBOOK.md — ProjectAegis / cmano-clone

Ordered runbook for the Unity Hub + editor + package version work.
Authored 2026-08-17. Host: Ubuntu Linux, user `username01`.
Repo: `/home/username01/projects/active/cmano-clone/cmano-clone`
Unity project: `unity/ProjectAegis`

**This document is instructions for a human to run.** Nothing here was executed. No
commits were authored. Per `CLAUDE.md` write-gate: every file write and every commit
below is yours to approve and perform.

## Ordering (and why this order)

```
0. Pre-flight: quiesce stacks/worktrees, tag rollback point
1. Unity Hub          -> 3.20.1          (Linux checksum fix must precede any module install)
2. Editor 6000.3.22f1 install ALONGSIDE  (Hub must be good before this)
3. Cleanup --caches   WHILE EDITOR CLOSED (wipe once, so reimport happens once)
4. Resolve .meta coverage               (ENUMERATE FIRST — the delivered filename is unconfirmed)
5. FIRST OPEN of 6000.3.22f1             (single reimport; editor rewrites ProjectVersion.txt)
6. Move the version-pin sites            (or agents read a stale pin)
7. Packages                              (PM version gating is editor-version-dependent)
8. Go/no-go, then and only then remove 6000.3.14f1
9. Decide on the stray 6000.5.1f1 install
```

Steps 3→4→5 are one atomic window. Do not open the editor in the middle of it.

---

## Step 0 — Pre-flight

Prerequisites, all blocking:

1. **No editor process holding the project.** `pgrep -af 'Unity.*ProjectAegis'` must be
   empty. A live editor will rewrite `Library/` and `ProjectVersion.txt` underneath you.
2. **Worktrees quiesced.** `git worktree list`; `.worktrees/` sprint stacks each contain a
   `unity/ProjectAegis` tree. If any of them is opened by **6000.3.14f1** after step 5, that
   worktree's `ProjectVersion.txt` is rewritten back to the old version and the pin
   divergence reappears in a branch. Land or park the stacks first: `gt log short`.
   (Note: `AGENTS.md` records the worktree root as `/home/username01/cmano-clone/.worktrees/`,
   which does not match the current repo path — reconcile before trusting either.)
3. **Rollback point.** Tag the pre-upgrade trunk state **now**:
   `git tag pre-unity-6000.3.22 <trunk-sha>`.
   This tag is the *real* rollback for steps 5–7 — see "Why git is the rollback" below.
   Do **not** run `gt create` yet. `gt create` captures the **staged index**; with an empty
   index it either errors or produces an empty branch that `gt submit --stack` then publishes
   as an empty PR. Create the branch in step 6, after the files are staged.
4. **Working tree clean** except the deliverables you are about to place. `git status --short`.
5. **Disk.** New editor ≈ several GB; `Library/` rebuild ≈ 100 MB+ of DBs. `df -h /home`.

### Why git is the rollback, not the old editor

`m_SerializationMode: 2` (ForceText) means assets and `ProjectSettings/*.asset` are
YAML on disk. A newer editor may rewrite them at a newer serialized version on first
open. Reopening 6000.3.14f1 against those files is **not** a supported downgrade. So for
every step from 5 onward, rollback = `git checkout pre-unity-6000.3.22 -- unity/ProjectAegis/`
plus re-wiping `Library/`, *not* "just open the old editor". Keep the old editor installed
anyway (step 8) so a rolled-back tree has something to open.

---

## Step 1 — Unity Hub -> 3.20.1

### Why this is first, and why that is Linux-specific

Hub **3.20.1** (2026-08-10) fixes a **Linux** defect where installing editor *modules*
fails with a **checksum mismatch**. Step 2 installs 6000.3.22f1 with modules
(`linux-il2cpp`, toolchain/SDK support). Hitting that bug mid-install leaves a partially
installed editor that Hub still lists — you then get to distinguish "editor is broken" from
"project is broken" during the one step where you most need that distinction. Upgrading Hub
first costs minutes and removes an entire class of ambiguity from step 2.

Secondary reason: **3.20.0 reworked the Hub UI.** Take that churn now, on a day when
nothing else is moving, rather than while triaging a failed editor install.

### Determine the installed version

Hub is not under `$HOME` (editor installs are — `/home/username01/Unity/Hub/Editor/`), so it
is almost certainly the apt package at `/opt/unityhub`.

```bash
# authoritative for an apt install
dpkg-query -W -f='${Package} ${Version} ${Status}\n' unityhub
# what apt would install, and from which repo
apt-cache policy unityhub
ls -l /etc/apt/sources.list.d/ | grep -i unity
# confirm the install location and that it is the apt one
dpkg -L unityhub | grep -m5 '^/opt/unityhub'
# Hub's own reported version (Electron app metadata; asar may need --list)
/opt/unityhub/unityhub --headless help 2>&1 | head -5
# fallbacks if the above is not an apt install
ls -ld ~/Applications/*Unity*Hub* ~/.local/share/applications/*unityhub* 2>/dev/null
ls -l ~/.config/UnityHub/
```

If `dpkg-query` returns nothing, it is an AppImage or a manual `.deb` outside dpkg — resolve
that before upgrading, because two Hubs writing `~/.config/UnityHub/` is its own failure mode.

### Upgrade

```bash
sudo apt-get update
sudo apt-get install --only-upgrade unityhub
dpkg-query -W -f='${Version}\n' unityhub   # expect 3.20.1
```

If Unity's apt repo is absent (`apt-cache policy` shows no `packages.unity.com` candidate),
add the repo rather than side-loading a `.deb`, so future upgrades stay in apt's hands.

**Before you upgrade, keep the current package:** `cp /var/cache/apt/archives/unityhub_*.deb ~/`
(if apt-clean has already run, download the exact current version's `.deb` first —
otherwise you have no downgrade artifact).

### Rollback

`sudo apt-get install --allow-downgrades unityhub=<old-version>` or
`sudo dpkg -i ~/unityhub_<old>.deb`. Low risk: Hub version and editor installs are
independent — a Hub downgrade cannot corrupt an installed editor, and
`~/.config/UnityHub/` (install paths, license, project list) is preserved across both
directions. Worst realistic case is UI relearn plus re-signing in.

**Verify:** Hub launches; **Installs** tab lists `6000.3.14f1` and `6000.5.1f1`;
**Projects** tab still lists ProjectAegis at the right path.

---

## Step 2 — Editor 6000.3.14f1 -> 6000.3.22f1 (install alongside)

6000.3.22f1 (2026-08-13) is the current 6.3 LTS patch; the LTS stream is supported to
Dec 2027. **Install alongside. Do not uninstall 6000.3.14f1 in this step** — see step 8.

### Match the module set of the existing install

Installing a module later is a second chance to hit exactly the bug step 1 fixed, so get
the set right on the first pass:

```bash
jq -r '.[] | select(.selected==true) | .id' \
  /home/username01/Unity/Hub/Editor/6000.3.14f1/modules.json
```

Install via Hub GUI (**Installs > Install Editor > Official releases**) or headless:

```bash
/opt/unityhub/unityhub --headless install --version 6000.3.22f1 \
  -m linux-il2cpp   # plus every other id printed above
/opt/unityhub/unityhub --headless editors -i    # confirm both versions present
```

Headless install of a version Hub does not currently list requires `--changeset <hash>`;
prefer the GUI in that case rather than guessing a changeset.

### The version-pin sites — THREE today, FOUR once Stack C lands

`git grep '6000\.3\.14f1'` on the current tree returns exactly **two** files: `ProjectVersion.txt`
and `CLAUDE.md:9` (verified). Site 3 (`VERSION.md`) is `@`-included by `CLAUDE.md` and carries the
version in prose. Site 4 does **not exist yet** — it is created by `gitattributes.NEW`
(`CHANGESET.md` Stack C), which is *not* part of the step-6 commit. Move sites 1–3 **together** in
step 6, or agents read a stale pin; make sure Stack C never lands carrying the old string.

`AGENTS.md` says only "Unity 6.3 LTS" with no patch (verified, lines 5 and 369) — no change needed.

| # | Site | What changes | Notes |
|---|------|--------------|-------|
| 1 | `unity/ProjectAegis/ProjectSettings/ProjectVersion.txt` | `m_EditorVersion` **and** `m_EditorVersionWithRevision` | **Do not hand-edit.** The revision hash (`d68c3f99a318` today) is per-build; let the editor rewrite this file on first open (step 5), then commit the result. `.gitattributes` deliberately gives this file **no** merge driver so a version bump always conflicts loudly — that is working as designed, resolve it by hand. |
| 2 | `CLAUDE.md` line 9 | `- **Engine**: Unity 6.3 LTS (editor 6000.3.14f1)` | This is the string every agent reads as ground truth. |
| 3 | `docs/engine-reference/unity/VERSION.md` | version + supported-until | `@`-included by `CLAUDE.md`, so it is in every agent's context. |
| 4 | `.gitattributes` — **NOT in the step-6 commit; Stack C.** `6000.3.14f1` occurs **three** times in `gitattributes.NEW`: line 3 (banner), line 18 (the `ls -l <editor-root>/6000.3.14f1/…` verification line), line 25 (the driver-path template). Search the file, do not trust these line numbers after any edit. | All three → `6000.3.22f1` **before Stack C lands** | Comment only, **but** it is the copy-paste source for the per-dev `git config` below. Stale here = every dev configures or verifies a driver pointing at an editor you are about to delete. Updating only two of three is the trap: line 18 is what an onboarding dev runs. The editor root itself is **unverified** on this host (Hub is not under `$HOME`) — locate it with `find / -type f -name UnityYAMLMerge 2>/dev/null` before pasting. |

### The merge driver must move too

Once `gitattributes.NEW` lands (Stack C), `.gitattributes` routes ~30 Unity YAML types through
`merge=unityyamlmerge`. Today the current 5-line `.gitattributes` routes **none** of them — so if
Stack C has not landed, this config is harmless prep, not a live dependency. Either way the mapping
is **inert** unless each dev has the driver configured, and the configured path points at a
specific editor install. Re-run after step 8 (or now, since both editors exist):

The editor root below is a **template**. Hub on this host is not under `$HOME` and its
install directory is configurable, so resolve the real path first — `git config` validates
nothing:

```bash
find / -type f -name UnityYAMLMerge 2>/dev/null     # locate; pick the 6000.3.22f1 one
DRV=<paste the verified absolute path here>
ls -l "$DRV"                                        # MUST exist and be executable

git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "$DRV merge -p --force %O %B %A %A"
git config merge.unityyamlmerge.recursive binary

git config --get merge.unityyamlmerge.driver        # confirms the STRING only
ls -l "$(git config --get merge.unityyamlmerge.driver | awk '{print $1}')"   # confirms the BINARY
```

Worktrees share the common config (`$GIT_COMMON_DIR/config`, i.e. `.git/config` — *not* the
per-worktree `$GIT_DIR`), so one update covers `.worktrees/*` — verify with
`git -C .worktrees/<any> config --get merge.unityyamlmerge.driver`.
Do **not** point this at `6000.5.1f1`: its SmartMerge/mergespec is from the 6.5 stream.

**Failure mode if the path is wrong or you delete 6000.3.14f1 (step 8) without re-pointing
the driver — verified, and it is NOT a graceful fallback to plain text merge:** git prints a
single easily-missed `...UnityYAMLMerge: not found` line, reports
`CONFLICT (content): Merge conflict in <file>`, and marks the path `UU` — but the working-tree
file is left as **your side, byte-for-byte, containing no conflict markers at all**. A dev who
sees a clean-looking file and runs `git add <file>` silently discards the entire incoming side
of a scene, prefab or ProjectSettings asset. There is no warning at commit time. Run the
step-0b smoke test in `.gitattributes` after any editor move.

### Rollback

Nothing has touched the project yet. `unityhub --headless uninstall --version 6000.3.22f1`,
or just leave it installed and keep using 6000.3.14f1.

**Verify:** both editors listed by `unityhub --headless editors -i`; module sets match;
project **not yet opened** with the new editor.

---

## Step 3 — Cleanup with `--caches`, editor CLOSED (the ordering that saves a reimport)

Run the already-tested script — do not rewrite it.

**Install path: `tools/unity-housekeeping.sh`** (this is what `CHANGESET.md` §2/§4 lands; earlier
drafts of this runbook said repo root). The script self-locates with
`git rev-parse --show-toplevel`, so it runs correctly from anywhere — but the *invocation path*
below must match where you actually put it, or every command in this step and in gates 5 and 12
fails with `No such file or directory`.

**Prerequisite:** land `gitignore.NEW` (or at minimum add `_to_delete/`) **before** the first
`--apply`, otherwise the quarantine directory the script creates shows up as untracked noise in
the `git status` check on the next line.

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
tools/unity-housekeeping.sh --report             # footprint sizes ONLY — exits before the meta phase
tools/unity-housekeeping.sh                      # DRY RUN of all default phases, incl. the meta report
tools/unity-housekeeping.sh --apply              # dumps, logs, scratch, meta report
tools/unity-housekeeping.sh --caches --apply     # ADDITIONALLY wipes Library/ Temp/ obj/ Builds/
git status --short                               # MUST show no deletions of tracked files
```

`--report` returns at the footprint table (script line 81) and never reaches the `meta` phase
(lines 112–126). The `missing / orphaned` counts come from the **bare dry run** or from `--apply`.
Anywhere this runbook needs those counts, use the bare invocation.

### Why `--caches` goes before the first open, explicitly

`Library/` holds a **41 MB ArtifactDB** and a **33 MB SourceAssetDB**. Artifacts are keyed by
importer version, and an editor patch bump invalidates importer versions — so **6000.3.22f1
will reimport regardless**. If you open the new editor first and *then* wipe caches, you pay
the full-project import **twice**: once when the new editor migrates the old DBs, once after
the wipe. Wiping while the editor is closed means the first launch performs exactly **one**
cold import, and the resulting `Library/` is built entirely by 6000.3.22f1 with no migrated
6000.3.14f1 artifact residue.

This also disposes of ~183 MB of untracked cruft (17 `mono_crash.mem.*.blob`, the 13.6 MB
PostScript file named `re`, three duplicate `.cs` snapshots, one `.patch`). The script
quarantines ambiguous items to `_to_delete/` rather than deleting — review that directory,
then remove it yourself. It refuses to touch git-tracked files.

### Cache Server interaction — read before you wipe

`EditorSettings.asset` currently has `m_CacheServerMode: 0` (use global/Preferences) with
download **and** upload enabled. The delivered `EditorSettings.asset.NEW` flips this to
`m_CacheServerMode: 1` with endpoint `CHANGE-ME.accelerator.invalid:10080`.

**Do not apply `EditorSettings.asset.NEW` before this cold import** unless the Accelerator is
actually up and the endpoint is real. A configured-but-unreachable cache server makes the
import *slower*, not faster — the importer attempts the remote lookup per artifact. Two clean
options:

- Accelerator not yet stood up: leave `m_CacheServerMode: 0`, do the cold import locally,
  land `accelerator-docker-compose.yml` afterwards as separate work.
- Accelerator stood up first: set the real endpoint, then wipe and import. Note the
  Accelerator is keyed by importer version too, so it is **cold for 6000.3.22f1** — the first
  machine through gets no speedup and populates the cache for everyone else.

### Rollback

None needed — everything `--caches` removes is regenerable by definition. Cost of being wrong
is time (one full import), not data. Untracked scratch is in `_to_delete/`, not deleted.

**Verify:** `unity/ProjectAegis/Library` absent; `git status --short` shows zero tracked
deletions; `_to_delete/` reviewed.

---

## Step 4 — `.meta` coverage (confirm with 4a, then copy)

> **VERIFIED — an earlier automated review withdrew these claims in error; they are reinstated.**
> `ScenarioEditorShellHost.cs` **does exist**: 20,378 bytes, mtime 2026-07-26, confirmed by a
> recursive listing of the live filesystem. It is the **only** `.cs` in `Assets/Scripts/Runtime/`
> (21 scripts, +2 under `Cesium/`) without a `.cs.meta`, and there are **no orphan `.meta`
> files**. The GUID `b1187aea29e54070a9c496bda01049a6` was collision-checked against all 22
> project GUIDs, and all 10 `m_Script` GUIDs in `DelegationSmoke.unity` were scanned and resolve
> against existing `.meta` files — the one unresolved GUID, `0000000000000000e000000000000000`,
> is Unity's built-in. **Nothing references this script yet.**
>
> The reviewer that withdrew these claims held a partial source mirror (3 of 23 scripts, staged
> for md5 comparison rather than as a full tree) and inferred three missing `.meta` files plus an
> orphan `C2LeftDrawerPanelHost.cs.meta`. Neither survives the full listing. See `CHANGESET.md` §2a.
>
> **Still enumerate first (step 4a).** The snapshot is a day old, it costs one command, and the
> `.meta` phase of `tools/unity-housekeeping.sh` reports it for free. Blind-copying is only unsafe
> in the cases 4a is there to detect: a `.meta` that already exists (overwriting a GUID scenes may
> reference), or additional unmetaed scripts (the delivered GUID is one value and **must not** be
> reused across files — two assets sharing a GUID is worse than no `.meta`).
### 4a. Enumerate the real set (authoritative, run on the host)

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone/unity/ProjectAegis/Assets
# .cs with no .meta:
find . -name '*.cs' | while read -r f; do [ -e "$f.meta" ] || echo "MISSING META: $f"; done
# .meta with no asset:
find . -name '*.cs.meta' | while read -r m; do [ -e "${m%.meta}" ] || echo "ORPHAN META: $m"; done
cd - && git ls-files -- 'unity/ProjectAegis/Assets/**/*.cs' 'unity/ProjectAegis/Assets/**/*.cs.meta' | sort
```

`tools/unity-housekeeping.sh` (bare dry run) reports the same counts across all asset types, not
just `.cs`. Use both.

**4a is authority.** The 2026-08-17 live listing (and 4b.3 / 4b.4) expect *exactly one*
`MISSING META: ./Scripts/Runtime/ScenarioEditorShellHost.cs` and **no orphans**. Do not pin
GUIDs from any other missing-meta set.

> **Historical (withdrawn-reviewer inference — do not follow):** a partial source mirror once
> listed `TestFile.cs`, `OsintStagingPanelHost.cs`, and `Cesium/CesiumGlobeBridge.cs` as unmetaed
> plus orphan `C2LeftDrawerPanelHost.cs.meta`. That set does not survive the full listing.
> See `CHANGESET.md` §2a.

### 4b. What to actually do, per file

**Do not hand-author GUIDs.** The delivered `.meta` carries a single GUID and cannot be reused
across several scripts — two assets sharing one GUID is a worse failure than no `.meta`. And
hand-authoring buys nothing here: it is only worth pinning a *specific* GUID if that value is
already referenced somewhere, which is precisely what could not be verified.

1. **If an untracked `.meta` already exists on disk** for a listed `.cs` (the local editor minted
   one at some point and it was never committed): **commit that file as-is.** It holds the GUID
   this machine's `Library/` and any locally-saved scene or prefab already reference. Overwriting
   it with a different GUID is the data-loss path, not the fix.
2. **If no `.meta` exists at all:** let the editor mint one at step 5, then commit it immediately —
   one `.meta` per script, each with its own GUID. Verify at step 5 that the number of new
   `.meta` files equals the number 4a reported.
3. **Orphan `.meta` files:** none exist as of 2026-08-17. If 4a reports one, resolve it in the
   same branch — restore the missing `.cs` or `git rm` the `.meta`. An orphan means any scene
   still holding that `m_Script` GUID is already broken.
4. **Expected 4a result:** exactly one line,
   `MISSING META: ./Scripts/Runtime/ScenarioEditorShellHost.cs`. On that result, copy
   `ScenarioEditorShellHost.cs.meta.NEW` into place as
   `Assets/Scripts/Runtime/ScenarioEditorShellHost.cs.meta` and commit it.

### The failure mode being prevented

A `.cs` file with no committed `.meta` gets a **fresh random GUID from every editor that
imports it** — every worktree, every branch, every CI clone, every teammate. This repo runs
Graphite stacks and parallel `.worktrees/` sprints, so that is not hypothetical, it is the
default. Then:

1. Branch A imports the file, mints GUID_A, and a scene or prefab on branch A stores
   `m_Script: {guid: GUID_A}`.
2. Branch B independently mints GUID_B.
3. On merge, one side's scene references a GUID that does not exist in the merged tree.
   Unity resolves `m_Script` to **null** — "The referenced script is missing" — the
   MonoBehaviour loses **all serialized field values**, and the next time anything saves that
   scene or prefab, the nulls are written to disk and committed. Silent data loss, discovered
   later, in a scene nobody was editing.
4. `.gitattributes` puts `*.meta` through SmartMerge with an explicit warning that a GUID
   conflict is never mergeable. Best case you get a manual conflict; worse case a clean-looking
   auto-merge.

**Not verified — check before relying on it:** whether any scene or prefab already references the
scripts in 4a. Run this on the host, per GUID from an existing `.meta`, before you touch anything:

```bash
git grep -n 'guid: ae40bacbd5ae3d34ea0bdd09b7e52fc2' -- 'unity/ProjectAegis/Assets/**/*.unity' \
                                                        'unity/ProjectAegis/Assets/**/*.prefab'
```

`0000000000000000e000000000000000` in scene files is Unity's built-in sentinel, not a broken
reference — ignore it. A `.cs` with **no** `.meta` cannot be referenced by GUID at all yet, which
is exactly why fixing it costs one file per script today and costs a scene-repair session once a
branch starts referencing it.

**These `.meta` files must be committed before any branch merges.** Not "before release" — before
the next merge. Every merge window they stay uncommitted is another chance for two GUIDs to exist.

### Rollback

`git checkout -- unity/ProjectAegis/Assets/Scripts/Runtime/` (pre-commit) or delete the
untracked `.meta` files and let the editor mint them again. Low risk **provided you did not
overwrite an existing GUID** — that is the one variant of this step that is not cheaply reversible.

**Verify after the editor opens (step 5):** every `.meta` from 4a is present, each retains the GUID
it had before the editor ran (the editor must **not** have replaced any), a `MonoImporter` block has
been appended, and `tools/unity-housekeeping.sh` (bare dry run — **not** `--report`) shows
`0 missing, 0 orphaned`. If the orphan from 4a.3 is still unresolved, `0 orphaned` is unreachable —
resolve it or record it as a documented, accepted exception before treating gate 5 as green.

---

## Step 5 — First open with 6000.3.22f1

Open the project from Hub with 6000.3.22f1 explicitly selected. Expect one long cold import.

Watch:

- `unity/ProjectAegis/Logs/` and the editor console: **zero** compile errors. `csc.rsp` sets
  `-langversion:10 -nullable:enable`, so nullability regressions surface as warnings/errors here.
- `git diff -- unity/ProjectAegis/ProjectSettings/` — the editor rewrites `ProjectVersion.txt`
  (both lines, with the real 6000.3.22f1 revision hash). Other `ProjectSettings/*.asset`
  churn is expected and must be **read**, not blind-committed: `.gitattributes` warns that
  ProjectSettings merges are position-significant (layer/tag/axis ordering).
- No "script missing" warnings in any scene you open.

Then run the smoke pass: `unity/ProjectAegis/PLAYMODE-SMOKE.md`. Note
`m_EnterPlayModeOptionsEnabled: 1` with `m_EnterPlayModeOptions: 0` — domain **and** scene
reload are both disabled on enter-play-mode, so static state and scene state persist between
plays. That makes stale-static bugs look like upgrade bugs; if the smoke pass behaves oddly,
re-run it once from a fresh editor launch before blaming the version bump.

Headless assemblies: `dotnet build ProjectAegis.sln` (SDK pinned 8.0.400,
`rollForward: latestMajor`; `Directory.Build.props` sets `ProduceReferenceAssembly=false`).
The editor bump should not move this, so a failure here is a signal, not noise.

### Rollback

`git checkout pre-unity-6000.3.22 -- unity/ProjectAegis/` then
`tools/unity-housekeeping.sh --caches --apply` and reopen with 6000.3.14f1. Note `git checkout <tag>
-- <path>` restores tracked content only — it does **not** remove files added since the tag (new
`.meta`, regenerated `Library/`), so pair it with the cache wipe. This is the step where
the tag from step 0 earns its keep — asset files may have been rewritten at a newer serialized
version and there is no editor-level downgrade.

---

## Step 6 — Commit the pin move

Per write-gate, one reviewed changeset, and **ask before writing**:

- `unity/ProjectAegis/ProjectSettings/ProjectVersion.txt` (editor-generated in step 5)
- `CLAUDE.md` (line 9 — verified)
- `docs/engine-reference/unity/VERSION.md`
- the `.meta` file(s) resolved in step 4 (whatever 4a actually produced)

**`.gitattributes` is deliberately NOT in this commit.** `gitattributes.NEW` is `CHANGESET.md`
Stack C — standalone and disruptive. Folding it in here would:

- trigger repo-wide renormalization from the `text eol=lf` rules on everyone's next checkout, and
  EOL-only diffs on every `gt restack` of every live `.worktrees/` stack;
- put **every already-tracked** `*.png`/`*.psd`/`*.fbx`/`*.wav` into a permanently *modified* state,
  because LFS rules are not retroactive and the `git lfs migrate import` is explicitly deferred;
- add prerequisites this step does not own — `git lfs install` on every machine and LFS quota on the
  remote for a scope widening from **one** tracked file to all project media;
- make `git revert` of the version bump drag thousands of lines of unrelated churn with it.

The version bump therefore has **three** file pin sites, not four. The fourth site (the
`.gitattributes` banner + `UnityYAMLMerge` driver-path template) only comes into existence when
Stack C lands — and it must land carrying `6000.3.22f1`, not `6000.3.14f1`. The per-dev
`git config merge.unityyamlmerge.driver` update is **independent of that file** and must still be
done as part of this step (see step 2), because it points at a real editor install on disk.

Submit with Graphite, not `gh` / raw push: stage first, then `gt create`, then
`gt submit --stack --no-interactive`.

Rollback: `gt` stack revert or `git revert` of that commit; then re-run the step 5 rollback if
the tree was already reimported by the new editor.

---

## Step 7 — Packages (AFTER the editor bump)

### Why after, and why the registry is not authoritative

Package Manager **gates package versions per editor version**. The registry API will happily
report a version that PM under 6000.3.22f1 will not offer you (and that a fresh clone
therefore cannot resolve). Treat the targets below as intent and let **in-editor Package
Manager** be the authority: `Window > Package Manager > Unity Registry`. Doing this after the
editor bump also avoids resolving twice — a new editor can force minimum versions and rewrite
`packages-lock.json` on its own.

Do them **one at a time**, in this order, each with its own validation and its own commit:

| Order | Package | From | To | Assessment |
|-------|---------|------|-----|-----------|
| 1 | `com.unity.burst` | 1.8.29 | 1.8.30 | Trivial patch. Satisfies every declared floor in the lock (`ai.inference` wants ≥1.8.17, test-framework chain wants ≥1.8.27). Expect a longer first build as Burst recompiles jobs. Do this first: it proves the bump/re-resolve/validate loop on the cheapest possible change. |
| 2 | `com.unity.ai.assistant` | **2.13.0-pre.2** | highest **stable** 2.x PM offers | **The problem is the prerelease, not staleness.** See below. |
| 3 | `com.unity.ai.inference` | 2.6.1 | — | **Already current.** No action. Re-confirm in PM after the editor bump anyway — `com.unity.ai.*` is gated hard by editor version. |
| 4 | `com.unity.addressables` | 2.3.16 | 2.8.1 (2026-01-05) | **The real gap.** No breaking changes flagged across 2.3 -> 2.8. Biggest blast radius; needs a content rebuild. Do it last. |

Also present and unverified: `com.ivanmurzak.unity.mcp` 0.86.0 resolves from the **OpenUPM
scoped registry**, not Unity's. Currency could not be verified from the registry API — check
it in PM under the scoped registry, and treat a third-party editor-integration package on a
critical path as its own risk item.

### `ai.assistant` — get off the prerelease

`2.13.0-pre.2` is a prerelease. Prereleases can be **unpublished** from the registry, and
`packages-lock.json` records a version + URL, not a content hash — so an unpublish breaks a
cold resolve (fresh clone, CI, new worktree) with **zero change on your side** and no way to
reproduce the old build. Options, in preference order:

1. Move to the highest stable 2.x PM offers for 6000.3.22f1. Preferred.
2. No stable exists: **embed** it — copy the resolved package into `Packages/<name>/` as an
   embedded package so an upstream unpublish cannot reach you. You then own updating it.
3. **Remove it.** `ai.assistant` is an editor-authoring convenience with no runtime
   dependency. If nothing in the workflow needs it, deleting the dependency is the cheapest
   correct answer.

Whatever you choose, mirror it into `Packages/manifest.template.json` — the delivered
`manifest.template.json.NEW` already carries this as a guardrail note, and a stale mirror is
worse than no mirror.

### What `packages-lock.json` being committed does and does not protect

**Does:** pins resolution. A fresh clone or worktree resolves the exact same version set,
including transitives; PM will not silently float a dependency to a newer patch; a diff on
this file is a reviewable record of every version movement.

**Does not:** it is not integrity-protected — version + URL, **no per-package hash** — so it
cannot detect a republished-under-the-same-version tarball. It does not survive a package
being **unpublished or yanked** (the lock then points at a 404 and every cold resolve fails).
It does not stop the **editor bump itself** from re-resolving and rewriting the lock. It does
nothing for registry downtime, and it does not pin **builtin modules**, which move with the
editor version. So: review the lock diff after every step in this section, and never
regenerate it as an unreviewed side effect.

### Validation after each package change

1. Editor reopens with no compile errors; console clean.
2. `unity/ProjectAegis/PLAYMODE-SMOKE.md` — run the documented pass.
3. `dotnet build ProjectAegis.sln`.
4. `git diff -- unity/ProjectAegis/Packages/` — expect changes in **both** `manifest.json` and
   `packages-lock.json`; anything else is a surprise. Update `manifest.template.json` to match.

### Addressables-specific validation — call this out separately

An Addressables 2.3 -> 2.8 move is not just a package version. It touches
`AddressableAssetSettings` and group schema assets (possible serialized-version bump) and can
change **catalog format**. Bundles and the catalog built by 2.3.16 are **not** guaranteed to be
honored by the 2.8.1 runtime, and the failure presents at runtime as assets that fail to load
— not as a compile error. Required, not optional:

- `Window > Asset Management > Addressables > Groups` — confirm settings and every group/schema
  survived the migration; check the console for schema upgrade messages.
- **Rebuild catalog + bundles from scratch**: `Build > New Build > Default Build Script`. Do
  not attempt an *update* build across the version boundary.
- Run the Addressables **analyze / build validation** rules (Groups window `Tools > Window >
  Analyze`: check duplicate bundle dependencies, missing groups, resource-to-Addressable
  references) and clear findings before shipping.
- If any remote/player content is already published against the 2.3.16 catalog, treat it as
  incompatible and republish. **Content Update Restrictions / `addressables_content_state.bin`
  from 2.3.16 must not be reused across this bump.**
- Exercise actual load paths in play mode, not just the build — a green build with a broken
  catalog is the standard way this bites.

### Rollback (per package)

Revert that single package's `manifest.json` entry **and** its `packages-lock.json` entry
together, reopen the editor, let PM re-resolve, re-run validation. Because each package is its
own commit, `git revert <sha>` is sufficient. For Addressables specifically, a revert also
requires a **full catalog + bundle rebuild** on the old version — the built content is not
part of the revert.

---

## Step 8 — Go/no-go, then remove 6000.3.14f1

### Go/no-go checklist

| # | Gate | Pass criterion |
|---|------|----------------|
| 1 | Hub | `dpkg-query -W unityhub` = 3.20.1; Hub lists all installed editors |
| 2 | Editors | 6000.3.22f1 present with the same module set as 6000.3.14f1; **6000.3.14f1 still installed** |
| 3 | Cold import | Completed once, no importer errors in `Logs/`; `Library/` regenerated |
| 4 | Compile | Editor console zero errors; `dotnet build ProjectAegis.sln` succeeds |
| 5 | Meta | Every `.meta` from step 4a present, each GUID **unchanged** from before the editor ran; `tools/unity-housekeeping.sh` (bare dry run, **not** `--report`) shows `0 missing, 0 orphaned` — or the orphan is a documented, accepted exception |
| 6 | Pins | All **three** file sites read 6000.3.22f1 (`.gitattributes` is Stack C, not this changeset); `git grep -n '6000\.3\.14f1'` — quote the pattern — returns **only** intentional historical references |
| 7 | Merge driver | `git config --get merge.unityyamlmerge.driver` points at the **6000.3.22f1** path and that path exists on disk |
| 8 | Scenes | Every scene/prefab that references a step-4 script opens with **no** missing-script warnings. Enumerate them by GUID (`git grep 'guid: <g>' -- '*.unity' '*.prefab'`) rather than trusting a named scene |
| 9 | Smoke | `PLAYMODE-SMOKE.md` pass green, from a fresh editor launch |
| 10 | Packages | `manifest.json` + `packages-lock.json` diffs reviewed and consistent; `manifest.template.json` mirrors reality; **no prerelease pins remain** (or the embed/removal decision is documented) |
| 11 | Addressables | Catalog + bundles rebuilt from scratch; analyze rules clear; runtime load paths exercised in play mode |
| 12 | Hygiene | `git status --short` shows no unexplained tracked deletions; `_to_delete/` reviewed and removed |
| 13 | Stacks | Every `.worktrees/` stack rebased onto the new pin (`gt sync` / `gt restack`) so no branch reintroduces `6000.3.14f1` |

Any **no** on 5, 6, 7, 8, or 11 is a stop — those are the silent-corruption gates.

### Only now: uninstall 6000.3.14f1

Optional, and only after gates 1–13 are green **and** the branch has been merged and lived
through one real merge of a `.unity`/`.prefab`. Reclaims several GB. Before you do:
re-confirm gate 7 — deleting that editor deletes the `UnityYAMLMerge` binary the driver path
names, and the failure is **silent** (git falls back to plain text merge).

### Rollback summary

| Step | Rollback |
|------|----------|
| 1 Hub | `apt-get install --allow-downgrades unityhub=<old>` / reinstall saved `.deb`. Config and editors unaffected. |
| 2 Editor install | Uninstall 6000.3.22f1, or simply keep using 6000.3.14f1. Project untouched. |
| 3 Cleanup `--caches` | Nothing to roll back — all regenerable. Quarantined scratch is in `_to_delete/`. |
| 4 `.meta` | `git checkout --` / delete the untracked file. |
| 5 First open | `git checkout pre-unity-6000.3.22 -- unity/ProjectAegis/`, wipe `Library/`, reopen with 6000.3.14f1. **This is the only step with no editor-level undo.** |
| 6 Pin commit | `git revert` the changeset; re-apply step 5 rollback if already reimported. |
| 7 Packages | Per-package revert of `manifest.json` + `packages-lock.json` together, re-resolve, revalidate. Addressables additionally requires a full content rebuild on the old version. |
| 8 Old-editor removal | Reinstall 6000.3.14f1 from Hub (same version + changeset) and restore the driver path. |

---

## Step 9 — Decide on the `6000.5.1f1` install (do not silently keep it)

Facts: it is on the **6.5 Update stream**, not this project's LTS stream. Update streams are
supported only until the next release in the stream publishes — so `6000.5.1f1` is
**already unsupported**, sitting **7 patches behind** `6000.5.8f1`, occupying **~10 GB**.

The cost of leaving it is not the disk. It is the **one-way door**: a dev or an agent
double-clicking a scene, or Hub offering it as "recommended", opens ProjectAegis with a 6.5
editor and upgrades `ProjectSettings/` and asset serialization to 6.5. There is no downgrade
— only the git tag saves you. `.gitattributes` already warns that its SmartMerge/mergespec
must not be used for the merge driver, which is the same hazard wearing a different hat.

Pick one, explicitly, and record it in `docs/engine-reference/unity/VERSION.md`:

- **(a) Uninstall.** Default recommendation if nothing needs 6.5. Reclaims ~10 GB and closes
  the door. `unityhub --headless uninstall --version 6000.5.1f1`.
- **(b) Keep, but current.** Only if it exists to evaluate the eventual 6.5 migration. Then
  move it to `6000.5.8f1` (an unsupported patch is worse than no sandbox) and document the
  owner and the purpose. Never point the merge driver at it.
- **(c) It was an accident** (Hub's "recommended version" default). Uninstall — same as (a).

Until decided, mitigate: set Hub's preferred editor for ProjectAegis to **6000.3.22f1**
(Hub Projects list, per-project editor version) so no double-click can route to 6.5.

---

## Delivered files referenced by this runbook

| File | Destination / use |
|------|-------------------|
| `unity-housekeeping.sh` | `tools/unity-housekeeping.sh` (per `CHANGESET.md` §2/§4). Already tested — do not rewrite. Steps 3, 4-verify, gates 5 and 12. Invoke as `tools/unity-housekeeping.sh`. |
| `gitignore.NEW` | `.gitignore`. **Land before step 3** so `_to_delete/` is ignored when the script first creates it. Contains two `# PROPOSED CHANGE` blocks that are deliberately not enacted — decide by hand. |
| `ScenarioEditorShellHost.cs.meta.NEW` | Copy to `unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioEditorShellHost.cs.meta` once step 4a confirms it is still the only unmetaed script (verified 2026-08-17). See step 4 and `CHANGESET.md` §2a. |
| `gitattributes.NEW` | `.gitattributes` — **`CHANGESET.md` Stack C, NOT part of step 6.** Before it lands, update **all three** `6000.3.14f1` occurrences (lines 3, 18, 25: the banner, the `ls -l <editor-root>/…` verification line, and the driver-path template) to `6000.3.22f1`. Missing line 18 leaves the onboarding check pointing at the editor step 8 deletes — the silent-merge-loss path. |
| `manifest.template.json.NEW` | `unity/ProjectAegis/Packages/manifest.template.json`. Step 7 — re-mirror after each package change. |
| `EditorSettings.asset.NEW` | `unity/ProjectAegis/ProjectSettings/EditorSettings.asset`. **Not part of this runbook.** Do not apply before the step-3 cold import unless the Accelerator endpoint is real. |
| `accelerator-docker-compose.yml` | Separate work item; prerequisite for `EditorSettings.asset.NEW`. Image name and ports are unverified by design. |
