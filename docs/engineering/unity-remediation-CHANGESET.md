# ProjectAegis / cmano-clone — repo hygiene & build-throughput changeset

## 1. What this does

Fixes four structural defects in the ProjectAegis monorepo that are currently costing time on every
branch switch: Unity YAML assets have no merge driver (so any parallel edit to a scene or prefab is
a whole-file conflict), ~183 MB of crash dumps and debug leftovers are untracked-but-unignored and
permanently dirty `git status`, `Packages/manifest.template.json` has drifted to 3 of 10 live
dependencies while presenting itself as authoritative, and `m_CacheServerMode: 0` leaves the
already-wired Accelerator plumbing shut so every `.worktrees/` checkout and CI agent reimports the
project from zero.

Everything here is either a config/attribute file or a document; no runtime C# changes, no commits
authored, no deletions performed. The destructive work is gated behind a tested, dry-run-by-default
script that the user runs, reviews, and then decides on.

## 2. Files

Staging paths are in `/home/claude/ws/out/`. Copy to the destination path shown; the `.NEW` suffix
is a staging artifact only and must be dropped.

| Staging file | Destination (repo-relative) | Change | Risk | Reversible? | Needs dev coordination? |
|---|---|---|---|---|---|
| `unity-housekeeping.sh` | `tools/unity-housekeeping.sh` | New file. Dry-run default; refuses git-tracked paths; quarantines ambiguous files to `_to_delete/` rather than deleting; aborts if not in a repo with `unity/ProjectAegis/`; never writes into `Assets/`. | Low | Yes (delete the file) | No |
| `gitignore.NEW` | `.gitignore` | Additive rules for `mono_crash.*`, `*.ps`/`*.eps`, `_to_delete/`. Two `# PROPOSED CHANGE` comment blocks (repo-wide `*.pdf`; triplicated `.worktrees/`) are deliberately **not** enacted — decide by hand. | Low | Yes | No |
| `manifest.template.json.NEW` | `unity/ProjectAegis/Packages/manifest.template.json` | Regenerated from live `manifest.json`: 3 deps → 10, plus a `_guardrails` block (no `com.unity.entities` re-add; `com.unity.ai.assistant` is a prerelease; OpenUPM scoped registry caveat). Reference mirror only — `packages-lock.json` stays authoritative. | Low | Yes | No |
| `ScenarioEditorShellHost.cs.meta.NEW` | `unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioEditorShellHost.cs.meta` | New `.meta` with GUID pinned to `b1187aea29e54070a9c496bda01049a6`. Prevents two branches minting divergent GUIDs for the same script. | Low **now**, High if delayed | Yes before anyone imports; **no** after divergent GUIDs exist in other branches | **Yes — time-sensitive.** Run the §2a pre-flight gate, then commit. |
| `ACCELERATOR-SETUP.md` + `accelerator-docker-compose.yml` | `docs/engineering/` (or repo convention) | New docs/runbook + compose file for standing up the shared artifact cache. | Low | Yes | No |
| `UPGRADE-RUNBOOK.md` | `docs/engineering/` | New doc: 6000.3.14f1 → 6000.3.22f1 path, Hub 3.20.1, Addressables 2.3.16 → 2.8.1, Burst bump, and the `com.unity.ai.assistant` prerelease exit. Procedure only; no version is changed by this changeset. | Low | Yes | No |
| `EditorSettings.asset.NEW` | `unity/ProjectAegis/ProjectSettings/EditorSettings.asset` | **Exactly two lines change**: `m_CacheServerMode: 0` → `1`, and `m_CacheServerEndpoint:` empty → `CHANGE-ME.accelerator.invalid:10080`. `m_CacheServerNamespacePrefix`, `EnableDownload`, `EnableUpload`, `ValidationMode`, `DownloadBatchSize` already existed and are **untouched**. Verified byte-identical elsewhere (50 lines both sides, YAML header and `serializedVersion: 15` intact, identical md5 with those two lines stripped). **Ships with the placeholder — must be replaced before landing.** | Med | Yes (revert to `0`) | **Yes** — project-level setting, inherited by every clone, worktree, and CI agent |
| `gitattributes.NEW` | `.gitattributes` | Preserves existing rules verbatim; adds `merge=unityyamlmerge` + `eol=lf` across Unity YAML types, `diff=csharp`, and LFS for binary **media only**. No `*.db` pattern anywhere. | **High** | Yes as a revert, but the working-tree churn it causes is not undone by reverting | **Yes — forces every dev to touch their working tree** (see §4) |

### 2a. The `.meta` destination — verified, with a pre-flight gate

**Verified against the live filesystem on 2026-08-17** (`device_list_dir --recursive` over
`unity/ProjectAegis/Assets/Scripts/Runtime/`):

| Fact | Evidence |
|---|---|
| `ScenarioEditorShellHost.cs` **exists** | 20,378 bytes, mtime 2026-07-26 |
| It is the **only** `.cs` in that tree lacking a `.cs.meta` | 21 scripts in `Runtime/` + 2 in `Runtime/Cesium/`; all others have one |
| There are **no orphan `.meta` files** | every `.meta` has a matching asset |
| **Nothing references it yet** | all 10 `m_Script` GUIDs in `DelegationSmoke.unity` resolve against existing `.meta` files; the single unresolved GUID `0000000000000000e000000000000000` is Unity's built-in |

Why no GUID has ever existed for it: the file's mtime (2026-07-26) is **after** the last write to
`Library/` (2026-07-20). The Editor has not been opened since the file appeared, so it has never
been imported and no `.meta` was ever minted. That is precisely why pinning the GUID now is safe
and valuable — it lands the GUID in VCS *before* any branch imports and mints its own.

> An earlier automated review flagged this filename as unverifiable. That was a false positive:
> the reviewer's source mirror held only 3 of the 23 scripts (files staged for md5 comparison,
> not a full tree), so it inferred three missing `.meta` files and one orphan. Neither holds
> against the full listing. The facts in the table above supersede it.

**Pre-flight gate — still run this.** The snapshot above is a day old and cheap to reconfirm:

```
cd unity/ProjectAegis/Assets
find . -name '*.cs' | while read f; do [ -e "$f.meta" ] || echo "MISSING META: $f"; done
find . -name '*.cs.meta' | while read m; do [ -e "${m%.meta}" ] || echo "ORPHAN META: $m"; done
```

Expected: exactly one `MISSING META: ./Scripts/Runtime/ScenarioEditorShellHost.cs`, no orphans.

- **If that is what you see** — copy `ScenarioEditorShellHost.cs.meta.NEW` to
  `Assets/Scripts/Runtime/ScenarioEditorShellHost.cs.meta` and commit it.
- **If more files are listed** — the delivered `.meta` covers only the one. Its GUID
  (`b1187aea29e54070a9c496bda01049a6`) is a single value and **must not be reused** across
  multiple scripts; two assets sharing one GUID is worse than no `.meta`. Let the Editor mint
  the rest, then commit them all in the same branch.
- **If zero are listed** — someone opened the Editor since; the `.meta` already exists. Commit
  the existing one as-is and **discard the delivered file** rather than overwriting a GUID that
  scenes may already reference.
### Blocking prerequisites

- **`EditorSettings.asset`**: pick a real Accelerator host:port first. Landing `m_CacheServerMode: 1`
  against an unresolvable endpoint is not fatal (the Editor falls back to local import) but logs a
  connection failure on every import for every dev.
- **`.gitattributes`**: each dev must run the three `git config merge.unityyamlmerge.*` commands in
  the file header, with their own editor path. Without that, the merge driver lines are silently
  inert and git falls back to the default text merge. Use the **6000.3.22f1** install, never
  6000.3.14f1 and never the 6000.5.1f1 one on this host.
- **LFS is not retroactive.** Already-committed media stays as plain blobs and will show as modified
  until history is converted. Inventory with `git ls-files -- '*.png' '*.psd' '*.fbx' '*.wav'`
  before landing. `git lfs migrate import` is a separate, coordinated exercise — do not bundle it.

## 3. Deliberately out of scope

**The leaked ai-game.dev MCP relay bearer token.** `unity/ProjectAegis/.gitignore` states the token
is still present in git history. The fix is **rotation at the provider**, not a history rewrite:
rotating invalidates every copy including the ones in history, forks, clones, and CI caches, whereas
a rewrite invalidates every SHA in the repo, forces a re-clone by every dev, and would strand the
live Graphite stacks and `.worktrees/` checkouts mid-sprint. A rewrite also does not help if the
token was ever pulled — assume it was. Rotate now; treat scrubbing history as optional cleanup with
a much worse cost/benefit ratio. Ignoring `.mcp.json` going forward is already in the changeset.

**The actual file deletions.** `tools/unity-housekeeping.sh` ships as a script, not as a commit that
removes files. The user runs `./tools/unity-housekeeping.sh` (dry run), reads the plan, then
`--apply`. Quarantined items land in `_to_delete/` for review before the user deletes them. `--caches`
is opt-in and costs a full reimport.

**Editor and Hub upgrades.** 6000.3.14f1 → 6000.3.22f1 and the Hub → 3.20.1 both require the Hub GUI
and multi-GB downloads on the user's machine; neither can be expressed as a file. `UPGRADE-RUNBOOK.md`
documents the sequence and the ordering constraint (Hub 3.20.1 first — it fixes the Linux
checksum-mismatch failure on editor module installs). The installed Hub version is unknown; Hub is
not under `$HOME` and is likely at `/opt/unityhub`.

**Package version bumps.** Addressables 2.3.16 → 2.8.1 and Burst 1.8.29 → 1.8.30 are documented, not
applied. Package Manager gates availability per editor version, so these should be re-resolved in
the Editor after the editor bump, not hand-edited into a manifest.

**`git rm` of `Assets/Scripts/Runtime/TestFile.cs`.** This is tracked scaffolding, so the housekeeping
script will not touch it by design (it refuses tracked paths). Removing it is a one-line user action:

`TestFile.cs` has **no** `.meta` in the files reviewed, so do **not** pass both paths to one
`git rm` — a single unmatched pathspec aborts the whole command and removes *nothing*
(verified: `fatal: pathspec 'TestFile.cs.meta' did not match any files`, exit 128, `TestFile.cs`
still present and tracked). Use:

```
cd unity/ProjectAegis/Assets/Scripts/Runtime
git rm TestFile.cs
git rm --ignore-unmatch TestFile.cs.meta   # no-op if it does not exist; do not let it fail the first
```

Confirm nothing references it first. If a `.meta` does exist, remove it in the same commit — a
`.meta` orphaned from its asset is exactly the drift the script's `meta` phase reports.

## 4. Landing order (Graphite)

Repo is Graphite-initialized; `CLAUDE.md` mandates `gt` over `gh pr create` / raw `git push` on stack
branches. **Three** independent units — do **not** fold them together. Each `gt create` below
captures whatever is *staged*, so the copy + `git add` for that branch must precede it.

### Stack A — hygiene (bottom → top)

`gt create` commits **staged** changes, so each branch needs its own copy + `git add` before the
`gt create` that captures it. Running the six `gt create` calls back to back with an empty index
does not build this stack — it errors or produces empty branches, and `gt submit --stack` then
opens empty PRs. One copy/stage per branch:

```
gt sync
# 1. urgent, tiny — ONLY after §2a is resolved and the filename is confirmed
git add unity/ProjectAegis/Assets/Scripts/Runtime/<confirmed>.cs.meta
gt create aegis/meta-scenario-shell-host -m "meta: pin GUID for <confirmed>.cs"

# 2. cp gitignore.NEW -> .gitignore
git add .gitignore
gt create aegis/gitignore-untracked-cruft -m "gitignore: crash dumps, PS exports, _to_delete/"

# 3. cp unity-housekeeping.sh -> tools/ ; chmod +x
git add tools/unity-housekeeping.sh
gt create aegis/housekeeping-script -m "tools: dry-run housekeeping script"

# 4. cp manifest.template.json.NEW -> Packages/manifest.template.json
git add unity/ProjectAegis/Packages/manifest.template.json
gt create aegis/manifest-template-regen -m "packages: regen template mirror (3->10 deps)"

# 5. cp ACCELERATOR-SETUP.md, accelerator-docker-compose.yml, UPGRADE-RUNBOOK.md -> docs/engineering/
git add docs/engineering
gt create aegis/accelerator-and-upgrade-docs -m "docs: accelerator + 6000.3.22f1 upgrade runbook"

gt submit --stack --no-interactive
```

- **1 is first because it is time-sensitive**, not because anything depends on it. Every hour it is
  unlanded is another chance for a parallel branch to mint a different GUID for the same script.
  It is also the one branch gated on an unresolved question — if §2a is not settled, **drop it and
  start the stack at 2** rather than holding the whole stack.
- **2 before 3** only so `_to_delete/` is already ignored when the script first creates it.
- **4 and 5 are fully independent** — pull either out to its own branch if review stalls the stack.

### Stack B — EditorSettings (separate stack, NOT the top of Stack A)

`gt submit --stack` submits **every** branch in the stack. A held-back branch sitting at the top
of Stack A is still published by that command, so the placeholder
`CHANGE-ME.accelerator.invalid:10080` would go up as a reviewable — and mergeable — PR. Keep it
out of Stack A entirely and create it only once the endpoint is real:

```
gt sync
# edit m_CacheServerEndpoint to the real host:port FIRST, then:
git add unity/ProjectAegis/ProjectSettings/EditorSettings.asset
gt create aegis/editorsettings-cacheserver -m "editor: enable cache server against <real-endpoint>"
gt submit --no-interactive
```

Announce it — it changes import behaviour for everyone on next Editor focus. Med risk, and the
only file here that is not additive.

### Stack C — `.gitattributes` (standalone, disruptive)

```
gt create aegis/gitattributes-unity-yaml-merge
gt submit --no-interactive
```

**This is the single change that forces everyone to touch their working tree.** The `text eol=lf`
rules trigger renormalization: the first checkout after it lands can touch thousands of files,
every live Graphite stack rebased across it will show EOL-only diffs on each restack, and each
`.worktrees/` checkout renormalizes independently.

**Do NOT run bare `git add --renormalize .`.** `--renormalize` re-runs *clean filters*, not just
EOL conversion, so with the new `filter=lfs` media patterns in place it stages every
already-committed PNG/PSD/FBX/WAV as an LFS pointer in one command — the mass LFS conversion this
changeset explicitly defers. Verified: a 200 000-byte tracked PNG became a 131-byte pointer blob
from that single command, with no prompt. If renormalization must be forced, pathspec-limit it to
text types (`git add --renormalize -- '*.cs' '*.unity' '*.prefab' '*.asset' '*.meta' ...`), which
leaves media blobs intact. Otherwise let checkout do it and run nothing.

Prerequisite: `git lfs version` succeeds and `git lfs install` has been run on every machine, and
the remote has LFS quota for the widened scope (today exactly one file is LFS-tracked). If git-lfs
is missing, the `filter=lfs` lines are inert no-ops and media commits as plain blobs while
everyone believes LFS is on.

Land it on a quiet trunk with all stacks either merged or ready to re-sync, announce it, and have
every dev immediately run `gt sync && gt restack` plus the three `merge.unityyamlmerge` config
commands — after locating and `ls -l`-verifying the `UnityYAMLMerge` binary (the path in the
header is a template; a wrong path yields `UU` files containing no conflict markers, which is
silent loss of the incoming side, not a graceful fallback). Do not land it mid-sprint. Do not
stack anything on top of it.

## 5. How this was produced — verified vs asserted

**Verified by execution:**
- `tools/unity-housekeeping.sh` was run against a mock repo. Confirmed: dry-run default, refusal on
  git-tracked paths, quarantine-instead-of-delete, idempotency, abort outside a repo containing
  `unity/ProjectAegis/`, no writes into `Assets/`.
- `EditorSettings.asset.NEW` was diffed against the original: 50 lines both sides, only the two
  cache-server lines differ, md5 identical with those two lines stripped, YAML header and
  `serializedVersion: 15` intact.
- `gitattributes.NEW` contains **no** `*.db` pattern; the pre-existing
  `assets/data/catalog/aegis_public_corpus.db` LFS rule and both `text eol=lf` rules are preserved
  verbatim and no later pattern overrides them, so `baltic_patrol.db` stays a plain blob.
- `manifest.template.json.NEW` was diffed against live `manifest.json`: all 10 dependencies and the
  `scopedRegistries` block match exactly.
- `gitignore.NEW` is additive only — no rule matches `Assets/**` or any `*.cs` glob.
- `git rm <tracked> <absent>` aborts atomically (exit 128, nothing removed) — hence the split
  command in §3.

**Verified by reading the actual repo files** (read-only copies of `.gitignore`, `.gitattributes`,
`.mcp.json`, `EditorSettings.asset`, `manifest.json`, `packages-lock.json`, `CLAUDE.md`): the
existing LFS rule and its "do not LFS-track `baltic_patrol.db`" instruction, `m_SerializationMode: 2`
(ForceText — the precondition SmartMerge requires), `m_AssetPipelineMode: 1`, `m_CacheServerMode: 0`
with download and upload already enabled, the manifest template's 3-vs-10 drift, and the
`.mcp.json`-token-in-history note.

**Read from vendor docs / release notes, not tested here:** all version numbers and dates —
6000.3.22f1 (2026-08-13, supported to Dec 2027), Hub 3.20.1 (2026-08-10) and its Linux checksum fix,
Addressables 2.8.1 (2026-01-05) with no breaking changes flagged across 2.3→2.8, Burst 1.8.30,
`com.unity.ai.inference` 2.6.1 being current.

**Asserted or unverified — treat as open questions:**
- **VERIFIED (was previously listed as unverified): the existence of `Assets/Scripts/Runtime/ScenarioEditorShellHost.cs`, and the GUID
  collision / inbound-reference check for `b1187aea29e54070a9c496bda01049a6`.** The filename does
  not appear in any repo file reviewed. See §2a — resolve before copying that `.meta`.
- Installed Unity Hub version. Not determined; Hub is not under `$HOME`.
- `com.ivanmurzak.unity.mcp` 0.86.0 currency. Resolves from the OpenUPM scoped registry; the
  registry API was not reachable. Check in-editor Package Manager.
- Accelerator performance gain. Argued from `Library/ArtifactDB` (41 MB) and `SourceAssetDB` (33 MB)
  index sizes plus the worktree/CI reimport pattern — directionally sound, not benchmarked.
- The `TestFile.cs` / `TestFile.cs.meta` paths and the absence of inbound references — confirm
  locally before `git rm`.
- Whether `worktrees/` (no dot) is still in use alongside `.worktrees/`. Verify before keeping both
  spellings in `.gitignore`.

No file in this changeset has been written to the user's repo, and no commit has been authored. Per
`CLAUDE.md`'s collaborative write-gate, every path in §2 requires explicit approval before it lands.

### 1a. Decisions taken on your behalf

You delegated these; recording them so they are reviewable rather than buried.

| # | Decision | Rationale |
|---|---|---|
| 1 | **`m_LineEndingsForNewScripts: 2` → `1` (Unix).** One-line edit to `EditorSettings.asset`, landed as its own commit so the cache-server diff stays a verified two-liner. | The new `*.cs text eol=lf` rule declares LF, but the Editor is set to write CRLF into newly created scripts. Git masks the mismatch on commit, so it surfaces as scripts flipping line endings on checkout. Host is Linux, targets are Linux (`com.unity.sdk.linux-x86_64`, `com.unity.toolchain.linux-x86_64-linux`), and every existing `*.sh` rule is already `eol=lf`. There is no case for CRLF here. |
| 2 | **`*.svg` is NOT LFS-tracked** — normalized as text instead. | SVG is XML. LFS-tracking it would make icon and UI-asset changes undiffable and unreviewable for zero storage benefit. |
| 3 | **`*.blob` NOT added to `.gitignore`;** `mono_crash.*` alone covers the 17 dumps. | A repo-wide `*.blob` would silently mask a future legitimate tracked `.blob` asset. `unity-housekeeping.sh` targets `mono_crash.mem.*.blob` explicitly, so cleanup is unaffected. |
| 4 | **Editor `6000.5.1f1` recommended for removal.** | 6.5 is an Update release — supported only until the next release publishes — is 7 patches behind, is not this project's stream, and costs roughly 10 GB. Keep it only if a specific 6.5 spike depends on it. |
| 5 | **The `.meta` GUID is pinned now rather than deferred to the Editor.** | Verified safe (§2a): nothing references the script, and no GUID has ever been minted for it. Pinning before any branch imports is the whole point; letting each worktree mint its own is the failure being prevented. |
| 6 | **No git history rewrite for the leaked token.** | Rotation at the provider closes the exposure. `filter-repo` under live Graphite stacks forces every developer to re-clone and is not worth it as a first move. |
