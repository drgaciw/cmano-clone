# Unity Accelerator — ProjectAegis (cmano-clone)

**Change:** `ProjectSettings/EditorSettings.asset` → `m_CacheServerMode: 0` → `1`, plus an endpoint.
**Why this file is the leverage point:** `m_CacheServerMode` is a *project* setting, committed to git.
Flip it once and every clone, every `.worktrees/*` checkout, and every CI agent inherits it — no
per-machine Editor Preferences drift, no onboarding step. (`0` = defer to per-user global prefs,
`1` = enabled at project level, `2` = force-disabled.)

---

## 1. What it removes, specifically here

Accelerator is a shared read/write artifact cache in front of Unity's Asset Pipeline v2
(`m_AssetPipelineMode: 1` — already correct). On import, the Editor hashes each asset plus its
importer version and platform, asks Accelerator for the resulting artifact, and downloads it
instead of re-running the importer.

The cost being paid today:

- `Library/ArtifactDB` is **41 MB** and `Library/SourceAssetDB` is **33 MB**. Those are *index*
  sizes, not payload — they are the receipt for a large, fully-materialised import graph. Every
  entry in them was produced by CPU work on this machine.
- `Library/` is gitignored, so it does **not** travel. Consequences with this repo's model:
  - Each `.worktrees/<sprint>` checkout is a distinct `Library/`. Opening a worktree in the Editor
    for the first time reimports the project from zero.
  - Graphite stacks mean frequent branch motion. Any restack that touches importer-relevant files
    (or any `gt sync` that moves an asset/`.meta`) invalidates artifacts and triggers reimport on
    the *next* Editor focus.
  - Platform switching reimports per-platform artifacts, per worktree.
  - CI agents on ephemeral workspaces reimport 100% of assets on every job.
- With `m_CacheServerEnableDownload: 1` and `m_CacheServerEnableUpload: 1` already set, the only
  thing standing between this project and cache reuse is `m_CacheServerMode: 0`. The plumbing is
  wired; the valve is shut.

Expected shape of the win: the *first* import after enabling is unchanged (it populates the cache
via upload). The second worktree, the second branch, and every CI agent thereafter pull artifacts
over the LAN instead of recomputing them. Biggest wins are texture/audio/model importers and shader
variant artifacts; scripts and `.asmdef` compilation are **not** cached by Accelerator (that is a
separate concern — see §8).

**Honest caveat:** I have not measured this project's import time. Do not present a speedup number
to anyone until you have run the A/B in §6.

---

## 2. Prerequisites

- Docker Engine + Compose v2 on the Ubuntu host. `docker compose version` must report v2.x.
- A static address or stable DNS name for the Accelerator host. The endpoint gets **committed to
  git**, so it must be resolvable from every developer machine, every worktree, and every CI agent.
  If CI runs off-box, a DHCP LAN IP is not acceptable — use DNS or a reserved lease.
- Free disk on a fast volume (§4). Do not co-locate the cache with the repo. The compose file's
  default named volume does **not** satisfy this — see §4/§5.
- Editor `6000.3.22f1` on all clients. Artifacts are keyed by importer version, so a mixed
  leftover `6000.3.14f1` / `6000.5.1f1` fleet vs the pinned `6000.3.22f1` will simply produce
  disjoint artifact sets in the same cache — correct, but it doubles storage and halves your hit
  rate. Keep leftover 14f1/5.1 editors out of this project after the pin lands.
- **Sequencing vs the editor upgrade.** The sibling `UPGRADE-RUNBOOK.md` moves this project
  `6000.3.14f1` → `6000.3.22f1`. Artifacts are keyed by importer version, so a cache populated
  before that upgrade is 100% miss after it. Either stand Accelerator up *after* the upgrade
  lands, or accept that the first post-upgrade import on every machine pays full price and the
  pre-upgrade artifacts sit in the cache until LRU evicts them (double-count them in §4 sizing if
  you do both in the same window).
- Decide the exposure model **before** bring-up: who must reach the cache port (this host only /
  LAN developers / off-box CI), because auth and TLS are off in this project's
  `EditorSettings.asset` and the port default in the compose file is loopback-only on purpose
  (§5, §9).

---

## 3. Unverified: image + ports  ← read this

I could not verify from this environment:

1. **The current Unity Accelerator distribution channel and container image name/tag.** Unity has
   historically shipped Accelerator as a native installer (Linux/macOS/Windows) obtained through the
   Unity ID / download portal, and has at times published a container image. I am **not confident**
   which is current for the 2026 release line, and I will not fabricate an image tag — a wrong tag
   is worse than a documented gap. `accelerator-docker-compose.yml` therefore has **no default
   image**: it reads `ACCELERATOR_IMAGE` from `.env` and Compose refuses to start until you set it.
   Resolve this by checking Unity's Accelerator download/docs page under your Unity ID, or
   `docker search`-ing the publisher you find there. If Unity only ships a native installer for
   your release, **use the installer and skip the compose file** — the rest of this runbook (ports,
   sizing, endpoint, verification, rollback, CI wiring) applies unchanged.

2. **The exact default ports.** Best-effort values used in the compose file and in the placeholder
   endpoint: `10080` for the Cache Server protocol, `10079` for the HTTP dashboard, `10443`/`10078`
   for the TLS variants. Treat all four as **unconfirmed**. Confirm from the dashboard's own
   Cache Server panel (or the Accelerator config CLI / `config.yaml` inside the container) *before*
   opening any firewall rule and before committing the endpoint. If the real cache port differs,
   `m_CacheServerEndpoint` must be corrected to match or the Editor will silently fall back to
   local import.

3. **Whether cache size is settable via environment variable** in the container build, versus only
   via dashboard/CLI post-install. The compose file passes `ACCELERATOR_CACHE_SIZE_GB` and flags
   this in a comment; if the container ignores it, set the limit in the dashboard after first boot.

Everything else in this document is derived from the repo's own files and is not guesswork.

---

## 4. Disk sizing

Rough model, per artifact "flavour":

```
cache_bytes ≈ per_platform_Library_artifact_bytes
              × active_build_targets
              × concurrent_branches_in_flight (Graphite stack depth + .worktrees count)
              × churn_multiplier (1.5–3, since old artifacts linger until LRU eviction)
```

Practical guidance for this repo:

- Measure the real base first, on the existing full import:
  `du -sh /home/username01/projects/active/cmano-clone/cmano-clone/unity/ProjectAegis/Library/Artifacts`
  That number, not the 41 MB ArtifactDB, is the unit of scale.
- Start at **250 GB** and watch the dashboard's eviction counter. Sustained eviction with a low hit
  rate means the cache is too small and you are paying network cost for nothing — raise it.
- Put it on NVMe. Accelerator is random-read dominated under concurrent worktree opens.
- **The compose file's default named volume lands under `/var/lib/docker/volumes`, i.e. on `/`.**
  That is not a deliberate placement and a 250 GB high-water mark there will fill the host's root
  filesystem. Check before first `up`:

  ```bash
  docker info --format '{{.DockerRootDir}}'
  df -h "$(docker info --format '{{.DockerRootDir}}')"
  ```

  If that filesystem cannot hold the configured size plus headroom, switch the volume to the bind
  form documented in `accelerator-docker-compose.yml` (`driver_opts: o: bind, device:
  /srv/unity-accelerator`) with `device` on the dedicated disk, and create the directory first.
  Compounding risk: `ACCELERATOR_CACHE_SIZE_GB` is unverified (§3) — if the image ignores it there
  is no limit at all, so confirm the effective cap in the dashboard before clients start
  populating.
- Cache loss is not data loss. It is safe to nuke the volume and repopulate; the only cost is one
  cold import cycle.

---

## 5. Bring-up

```bash
# 1. Stage the compose file + env on the Accelerator host.
#    /srv needs root; `sudo` or pick a path you own. This directory holds the
#    compose file only — the cache itself goes wherever the volume points (§4).
sudo mkdir -p /srv/unity-accelerator && sudo chown "$USER" /srv/unity-accelerator
cd /srv/unity-accelerator
cp <copied>/accelerator-docker-compose.yml ./docker-compose.yml

# 2. Fill in the values you verified in §3
#    Cache-size-via-env is unverified (§3). Set BOTH names so a 250 GB cap is not a
#    no-op if the image only honors one of ACCELERATOR_CACHE_SIZE_GB / ACCELERATOR_CACHE_GB.
cat > .env <<'EOF'
ACCELERATOR_IMAGE=<VERIFIED publisher/image:tag>
ACCELERATOR_DATA_PATH=/agent
ACCELERATOR_CACHE_SIZE_GB=250
ACCELERATOR_CACHE_GB=250
EOF

# 3. Start
docker compose up -d
docker compose logs -f accelerator      # watch for a clean listen/ready line
```

Then confirm the listening ports actually match §3's assumptions:

```bash
docker compose exec accelerator sh -c 'ss -lntp || netstat -lntp'
```

Adjust the `ports:` block and the endpoint below to whatever that prints.

**Exposure — do this deliberately, not by default.** The compose file publishes the cache port on
`127.0.0.1` only. That is intentional: with `m_CacheServerEnableAuth: 0` and
`m_CacheServerEnableTls: 0` the cache is an unauthenticated read/**write** artifact store, so
anything that can reach the port can plant artifacts that every Editor and CI agent will load
without question. To serve other machines, change the bind to the host's LAN address
(`"192.168.x.y:10080:10080"`) — not `0.0.0.0`.

Note that **Docker's published ports bypass UFW**: the DNAT rules live in Docker's own iptables
chains, which are evaluated ahead of UFW's `INPUT` rules, so `ufw deny 10080` gives you nothing.
Filter in the `DOCKER-USER` chain, or rely on the bind address. Verify from a *second* host, not
by reading `ufw status`:

```bash
# from another machine on the LAN — expect refused/timeout until you widen the bind
nc -vz <accelerator-host> 10080
```

---

## 6. Point the project at it

`EditorSettings.asset.NEW` in this drop is a byte-for-byte copy of the current
`ProjectSettings/EditorSettings.asset` with **exactly two lines changed** (verified by diff):

```
  m_CacheServerMode: 1
  m_CacheServerEndpoint: CHANGE-ME.accelerator.invalid:10080
```

`CHANGE-ME.accelerator.invalid:10080` is a **deliberate placeholder**. `.invalid` is a reserved TLD,
so if you commit it unedited the Editor fails to connect loudly instead of appearing to work. You
must replace it with the real `host:port` — I do not know your intended Accelerator host.

Untouched and intentionally so: `m_CacheServerNamespacePrefix: default`,
`m_CacheServerEnableDownload: 1`, `m_CacheServerEnableUpload: 1`, `m_CacheServerEnableAuth: 0`,
`m_CacheServerEnableTls: 0`, `m_CacheServerValidationMode: 2`,
`m_CacheServerDownloadBatchSize: 128`, and everything outside the cache-server block
(`m_SerializationMode: 2`, `m_AssetPipelineMode: 1`, `m_EnterPlayModeOptionsEnabled: 1`, …).

**Apply it yourself — I am not authoring a commit.** Suggested sequence, on the repo host:

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone

# 0. Sanity: confirm the drop matches your current file except the two lines
diff unity/ProjectAegis/ProjectSettings/EditorSettings.asset <copied>/EditorSettings.asset.NEW
#   Expect exactly: m_CacheServerMode and m_CacheServerEndpoint. Anything else -> STOP.

# 1. Copy in, then EDIT THE ENDPOINT to your real host:port
cp <copied>/EditorSettings.asset.NEW unity/ProjectAegis/ProjectSettings/EditorSettings.asset
$EDITOR unity/ProjectAegis/ProjectSettings/EditorSettings.asset

# 2. Re-diff — confirm still only two lines, and that no CHANGE-ME survives
git diff -- unity/ProjectAegis/ProjectSettings/EditorSettings.asset
grep -n 'CHANGE-ME' unity/ProjectAegis/ProjectSettings/EditorSettings.asset && echo "STOP: placeholder still present"

# 3. Only now create the Graphite branch (per CLAUDE.md: gt, not raw git push).
#    Order matters: gt create commits at branch-creation time, so the edit must
#    exist first. -a stages the tracked change; drop -a if you prefer to
#    `git add` yourself first.
gt create aegis/editorsettings-cacheserver -a \
  -m "build: enable Unity Accelerator cache server for ProjectAegis"
```

Branch name matches the one reserved in `CHANGESET.md`. Submit at your discretion
(`gt submit --stack --no-interactive`). Do not merge until §7 shows a real hit.

---

## 7. Verify a cache HIT actually happened

Three independent checks. Do all three; the first two can both look fine while the third proves
nothing is being reused.

**a) Editor log — connection.** Log path on this host:
`~/.config/unity3d/Editor.log` (previous run: `Editor-prev.log`).

```bash
grep -inE 'cache server|accelerator|cacheserver' ~/.config/unity3d/Editor.log | tail -40
```

You are looking for a connect/ready line naming your endpoint, and the **absence** of
"Unable to connect to Cache Server" / "Cache Server connection failed". A failed connect is
non-fatal — Unity falls back to local import silently — which is exactly why this grep matters.
I am not quoting exact log strings: the wording varies by Editor patch and I have not run
`6000.3.14f1` here. Match on `cache server` case-insensitively rather than on an exact phrase.

**b) Accelerator dashboard.** Open the dashboard port (see §3) and watch the Cache Server
hit / miss / upload counters while a client imports. Non-zero uploads on the first import and
non-zero **hits** on the second client are the signal.

**c) The only test that actually proves value — cold-worktree A/B.** This is the one to trust.

It takes **three** runs, not two. An empty cache cannot produce a hit, so a straight
before/after A/B measures nothing: run 2 exists purely to populate. Skip it and the "after" run is
100% miss, looks identical to baseline, and you will wrongly conclude the endpoint is broken.

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
BR=<branch-with-the-EditorSettings-change>

# --- Run 1: BASELINE, cache forced OFF -------------------------------------
git worktree add .worktrees/accel-r1 "$BR"
time <editor> -batchmode -quit -logFile /tmp/import-r1.log \
  -projectPath .worktrees/accel-r1/unity/ProjectAegis \
  -cacheServerEndpoint ""            # override the committed endpoint; pure local import
du -sh .worktrees/accel-r1/unity/ProjectAegis/Library/Artifacts

# --- Run 2: POPULATE (not a measurement) -----------------------------------
# Cold Library + cache ON + upload enabled. Expect ~baseline duration and
# non-zero UPLOAD counters in the dashboard. Discard the timing.
git worktree add .worktrees/accel-r2 "$BR"
<editor> -batchmode -quit -logFile /tmp/import-r2.log \
  -projectPath .worktrees/accel-r2/unity/ProjectAegis
#   -> confirm uploads climbed in the dashboard before continuing. If they did
#      not, stop here: nothing is being written and run 3 cannot hit.

# --- Run 3: THE MEASUREMENT, cold Library + warm cache ---------------------
git worktree add .worktrees/accel-r3 "$BR"
time <editor> -batchmode -quit -logFile /tmp/import-r3.log \
  -projectPath .worktrees/accel-r3/unity/ProjectAegis
```

Compare run 3 against run 1 for wall-clock, and compare the logs' import counts. Run 3 should show
a large jump in the dashboard's **hit** counter. If run 3 is not materially faster than run 1 while
run 2 showed uploads, the download side is the problem — go back to (a) and to
`m_CacheServerEnableDownload`.

Notes: the same editor build must be used for all three runs, or nothing keys the same. Do not use
`gt sync` to set up these worktrees — it syncs and restacks branches (and offers to delete merged
ones), which is not worktree creation and not something to trigger mid-benchmark. Remove the
scratch worktrees with `git worktree remove .worktrees/accel-r1` (etc.) when done; **do not**
`rm -rf` them.

---

## 8. Wiring worktrees and CI

**Worktrees:** nothing to do. `ProjectSettings/` is tracked, so every `.worktrees/*` checkout of a
branch containing this change already has `m_CacheServerMode: 1` and the endpoint. That is the whole
point of doing it project-level rather than in Editor Preferences. Each worktree still keeps its own
`Library/`; it just fills that `Library/` from the network instead of from the CPU.

**CI agents:** the committed setting is inherited, but pin it explicitly on the command line anyway
so a CI job never depends on which branch's `ProjectSettings` it happens to have. Batchmode flags
(names per Unity's Asset Pipeline v2 CLI — **verify against `-help` for `6000.3.14f1`**, I have not
executed this editor):

```
-cacheServerEndpoint <host:port>
-cacheServerNamespacePrefix default
-cacheServerEnableDownload true
-cacheServerEnableUpload true
-cacheServerWaitForConnection <ms>     # fail fast rather than silently importing locally
```

CI-specific notes:

- Set `-cacheServerWaitForConnection` to a small non-zero value. Without it, an unreachable
  Accelerator degrades into a full local import and your build minutes quietly triple.
- Consider `-cacheServerEnableUpload false` on PR/stack validation jobs and `true` only on the
  trunk job. Otherwise short-lived Graphite stack branches fill the cache with artifacts nobody
  will ever request again, accelerating LRU eviction of the artifacts that matter.
- Use the **same namespace prefix** (`default`) everywhere, or agents will not share artifacts.
- Accelerator caches *imported assets*, not script compilation. CI script-compile time is unchanged;
  if that is the bottleneck, that is a separate piece of work.
- Do **not** add the cache volume path anywhere near the repo, and do not add new `.gitignore`
  entries for it — nothing cache-related lands inside the working tree.

---

## 9. Rollback

Cheapest first; all are non-destructive.

1. **Per-machine, immediate:** Editor → Preferences → Asset Pipeline, uncheck the cache server, or
   launch with `-cacheServerEndpoint ""`. Affects that Editor only.
2. **Project-wide:** set `m_CacheServerMode` back to `0` (defer to user prefs) or `2`
   (force-disabled for everyone). Revert the branch from §6, or `git revert` the commit if merged.
   One-line change, no side effects.
3. **Stop the service:** `docker compose down` on the Accelerator host. Clients then fail to
   connect and fall back to local import — degraded, not broken. Combine with (2) to silence the
   log noise.
4. **Reclaim the disk:** `docker compose down -v` removes the named volume and the cache with it.
   Only do this once (2) is in place, and understand that the next cold import on every machine
   pays full price again. If you switched the volume to the bind form (§4), `-v` does **not** clear
   it — the data stays in `device:` and you remove it yourself, deliberately.

Nothing here mutates any client's existing `Library/`. Rolling back does not invalidate artifacts
already on disk, so a rollback costs zero reimport on machines that are already warm.

**Not covered / deliberately out of scope:** auth (`m_CacheServerEnableAuth: 0`) and TLS
(`m_CacheServerEnableTls: 0`) are left off. That is defensible on a trusted LAN and indefensible if
the endpoint is ever routable beyond it. If CI is off-premises, turn both on before exposing the
port, and note that flipping `m_CacheServerEnableTls` to `1` changes the port (§3, unverified) and
is a second commit to `EditorSettings.asset`.
