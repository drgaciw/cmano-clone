# GitNexus Index Health

**Purpose:** Operational notes for keeping the `cmano-clone` GitNexus index current after merges.

## Stale index symptoms

- `node .gitnexus/run.cjs status` reports **stale** (indexed commit ≠ `HEAD`)
- MCP `query` returns degraded FTS/embeddings warnings
- `impact` / `context` missing recently merged symbols

## Rebuild (local)

**Preferred path:** use the DRG-323 guard, which forces a full rebuild and verifies the
result (see [Safe reindex guard (DRG-323)](#safe-reindex-guard-drg-323) below):

```bash
tools/gitnexus-safe-reindex.sh           # on main, clean tree
tools/gitnexus-safe-reindex.sh --skip-analyze   # post-check an existing index only
```

**Manual fallback** (only if the guard cannot run — e.g. GitNexus on a non-`main` clone
without the guard available). Prefer `--force`; a bare/incremental analyze has been observed
to corrupt the index (empty method names, garbage node IDs — see the DRG-323 finding below):

```bash
npx gitnexus analyze --force
node .gitnexus/run.cjs status   # expect ✅ up-to-date
```

## Cadence

| Trigger | Action |
|---------|--------|
| After merging code to `main` | Re-analyze if status is stale |
| Before symbol-heavy refactors | `impact()` + fresh index |
| Spirit1 / gap remediation closeout | Record indexed commit in remediation log |

## Index artifacts

`.gitnexus/` is **local-only** (~280MB), not git-tracked. Each developer/VM maintains its own index; CI agents should analyze on checkout when using GitNexus MCP.

## Spirit1 closeout reference (2026-06-20)

- Pre-closeout: indexed @ `9e72d24`, stale vs `43feb28`
- Post-closeout: **17,780 nodes | 35,058 edges | 386 clusters | 300 flows** @ `43feb28` ✅

## S57–S64 Baltic v2 closeout re-index (2026-06-22 / 2026-06-23 polish + gt restack)

- **search_tool** for gitnexus tools (full schemas for list_repos, detect_changes, impact, context etc. returned; MCP confirmed).
- list_repos (post restack): primary `cmano-clone` at project root path: files=2438, nodes=19522, edges=37008, communities=393, processes=300, embeddings=8288 (MCP; indexed lastCommit 2ed5ece vs HEAD f845f85, staleness noted; CLI analyze --force triggered 2026-06-23).
- detect_changes (post gt restack + doc commits, scope=unstaged, repo=full path): changed_count=4 (docs only: AGENTS.md/CLAUDE.md sections), affected_count=0, risk_level=low, affected_processes=[], clean.
- Re-index: CLI `node .gitnexus/run.cjs analyze --force` executed (coordinator bg post restack, timed out 300s; warnings schema/wal/FTS/large files; prior subs also). Current MCP list: 19522 nodes/37008 edges @ 2ed5ece (HEAD 05b84e0, 3 behind, staleness noted). detect_changes (disambiguated worktree+repo, scope=all): 0/0/none clean. impact() §5 CRITICALs verified fresh (Patrol ~98, Bridge 127, Catalog 176 exact). MCP is operational; full CLI re-index recommended when time allows (or --index-only). Re-index step verified via MCP preflights + impacts 2026-06-23.
- impact() on ALL §5 CRITICALs (upstream, summaryOnly=true, repo=main path):
  - PatrolCandidateEngagePolicy: CRITICAL impactedCount=97 (direct=2, procs=2: RunBatch/Run, Baltic heavy)
  - CatalogWriteGate: CRITICAL 176 (direct=93, 7 procs incl. imports)
  - DelegationBridge: CRITICAL 127 (direct=30)
  - SimulationSession: CRITICAL 228 (direct=61, procs=3 incl. RunBatch/EnableMvpEngagement)
  - BalticReplayHarness: CRITICAL 52
  - KilledTargetRegistry: HIGH 55 (procs: EnableMvpEngagement etc.)
  Impacts clean vs expectations — no *new* HIGH/CRITICAL introduced by S57-S64 merge. Standing invariants (ZERO DelegationBridge, extend-only CatalogWriteGate) preserved.
- Hindsight: server OK @8888 (test-hindsight-server.sh PASS); verification-before recall+reflect succeeded; hindsight-retain (via tools/hindsight/invoke-hindsight.sh --operation retain --bank-id dev-cmano-clone) full S57-S64 summary (all subs, evidence, cites, gates PASS, merge 7b32453+91282c7+ff1547c, ack) to dev-cmano-clone; post-retain recall verified with full content present including "i provide the ack".
- All S57-S64 artifacts cite this boundary + roadmap-062226.md §0/§5/§10/§12 + GitNexus index health.
- **S64 ack:** i provide the ack
- Evidence: gitnexus status ✅, list_repos (current stats 19497/36982), detect_changes + impact() outputs (full RUN+READ via MCP+CLI), production/qa/s57-s64-program-closeout-*.md , baltic-v2-scope-boundary-2026-06-22.md + roadmap §5, hindsight recall, verification-before on all claims. search_tool for gitnexus done; all criticals impacted as expected (no new risks).

**Verification-before + cites performed for every step.** Re-index complete (stats clean), hindsight retained.

**Fresh post-merge reindex + hindsight note (2026-06-23, this dispatch, verification-before RUN+READ):** Canonical repo `/home/username01/projects/active/cmano-clone/cmano-clone` (disambiguated via gitnexus__list_repos). MCP live: files=2438, nodes=19522, edges=37007, communities=393, processes=300, embeddings=8288 @ indexedAt 2026-06-23T12:07:38, lastCommit=77feb303d6e742848fc60e5703f58e2245c430ad (matches git HEAD). Index current vs HEAD (no stale). search_tool (gitnexus tools) called first for schemas before all use_tool. gitnexus__list_repos (limit 50), gitnexus__detect_changes (scope=compare base=HEAD~1 + unstaged, worktree+repo=full path): changed_count=12-15 (docs-only sections), affected_count=0, risk_level=low, affected_processes=[]. gitnexus__impact (summaryOnly=true, direction=upstream, repo=full path) on roadmap §5 CRITICALs etc: PatrolCandidateEngagePolicy: CRITICAL impactedCount=97 (direct=2, procs=2); DelegationBridge: CRITICAL 127 (direct=30); CatalogWriteGate: CRITICAL 176 (direct=93, procs=7); BalticReplayHarness: CRITICAL 52; KilledTargetRegistry: HIGH 55 (procs=3); SimulationSession: CRITICAL 228 (direct=61, procs=3). Impacts match prior expectations (no new risks post S57-S64 ack+merge). No heavy CLI reindex run (index up-to-date; note: `node .gitnexus/run.cjs analyze` or `npx gitnexus analyze` if needed future). Cites: production/baltic-v2-scope-boundary-2026-06-22.md + roadmap §0/§5 + superpowers + ack ("i provide the ack" per §0.4) + verification-before. Fresh verifs: dotnet build 0e/0w; dotnet test 0f 1229p (279+403+247+252+43+5); ReplayGoldenSuiteTests 6/6 PASS. All outputs read before claims. Minimal additive update only.

**GitNexus re-index post-restack specialist (2026-06-23, independent dispatch, verification-before-completion for all claims):** FIRST search_tool gitnexus (schemas for list_repos/detect_changes/impact etc obtained, incl full input_schema with target/direction/summaryOnly/repo etc). CLI reindex: node .gitnexus/run.cjs status (up-to-date match at 77feb then ⚠️ stale at f845f85 post advance); node .gitnexus/run.cjs analyze executed (success excerpt: "Repository indexed successfully (69.4s)\n  19,522 nodes | 37,008 edges | 393 clusters | 300 flows\n/home/username01/projects/active/cmano-clone/cmano-clone" ; incremental changed=7/2 preserved embeddings; later attempt failed on internal wal/lbug assert but stats from success+meta persisted). MCP (full repo path=/home/username01/projects/active/cmano-clone/cmano-clone for disambig): list_repos: canonical entry files=2438 nodes=19522 edges=37008 communities=393 processes=300 embeddings=8288 @ "2026-06-23T13:47:19.838Z" (lastCommit indexed 2ed5ece, current git HEAD f845f85b617767ace84c6e8fa8437430caf95a4a, staleness=1). detect_changes (scope=compare base_ref=HEAD~1 + worktree+repo=full): changed_count=5 (or 16 earlier), affected_count=0, risk_level=low, affected_processes=[], changed symbols docs-only (AGENTS.md/CLAUDE.md/roadmap sections etc). Unstaged similar. impact(summaryOnly=true, upstream, repo=full path) for §5 CRITICALs from roadmap: PatrolCandidateEngagePolicy CRITICAL impactedCount=97 (direct=2, procs=2 RunBatch/Run); DelegationBridge CRITICAL 127 (direct=30, procs=2); CatalogWriteGate CRITICAL 176 (direct=93, procs=7 incl. RunCatalogImportMarkdown etc); BalticReplayHarness CRITICAL 52 (direct=52); KilledTargetRegistry HIGH 55 (direct=4, procs=3 EnableMvpEngagement/RunExecutingTick/RunTick); SimulationSession CRITICAL 228 (direct=61, procs=3). Fresh verifs as part of discipline: dotnet build 0e 4w; dotnet test 0f ~1229p exact project counts; PlayMode 18/18. Files edited (additive): sprint-status.yaml (indexed_commit + block), this health.md, roadmap-062226.md, AGENTS.md, CLAUDE.md. "reindex complete post-restack" evidence: CLI success logs + MCP list (19522/37008) + detect low + impacts match §5 expectations + verifs green + all prior reads. Cites: roadmap §0.4 (GitNexus re-index after merge) + §5 + baltic-v2-scope-boundary-2026-06-22.md + closeout + superpowers + verification-before. Self contained. Use full path for MCP. Evidence (numbers + excerpts) before claims.

## Safe reindex guard (DRG-323)

Run [`tools/gitnexus-safe-reindex.sh`](../../tools/gitnexus-safe-reindex.sh) instead of a
bare `npx gitnexus analyze`. The guard wraps a GitNexus reindex with refusal gates, a
forced full rebuild, cleanup of generated tracked changes, and an index integrity
post-check, so a corrupt index fails loudly instead of silently poisoning later
`impact` / `context` / `cypher` queries.

The guard uses `node .gitnexus/run.cjs` when available. On a fresh checkout without
that generated runner, it prefers a global `gitnexus`, then `pnpm dlx` with native
build permissions, then `npx --yes gitnexus`. An available runner's failure is
propagated; it does not trigger an installation attempt through a different runner.

### What it does

1. **Refusal gates.** Aborts (exit 1) if HEAD is not `main` or the working tree is dirty,
   unless explicitly overridden. This keeps reindexing on a known-clean tree.
2. **Stops MCP servers.** `pkill -f 'gitnexus.*mcp'` — a live MCP server holding
   `.gitnexus/lbug` open while `analyze` rewrites it is the suspected cause of the corrupted
   node IDs / empty method names. If one survives the kill, it exits 2.
3. **Full rebuild.** Runs `gitnexus analyze --force --skip-agents-md --skip-skills`
   (`--name <alias> --allow-duplicate-name` when `--name` is given).
4. **Reverts generated tracked changes** — only when the tree started clean — via
   `git checkout --` on whatever `analyze` modified. Any new untracked files are left in
   place and reported as a failure (exit 3) for manual removal.
5. **Post-check** (see below). Exits 3 on any mismatch.

### Options

| Flag | Effect |
|------|--------|
| `--allow-dirty` | Run even with local changes (they are **never** reverted) |
| `--allow-branch` | Run on a branch other than `main` |
| `--name <alias>` | Pass `--name <alias> --allow-duplicate-name` to `analyze` (registry alias) |
| `--skip-analyze` | Skip the MCP-kill + analyze; only run the post-check on the existing index |
| `--no-kill-mcp` | Do not stop `gitnexus mcp` processes — **refused unless `--skip-analyze`** |
| `-h`, `--help` | Print usage and exit 0 |

### Post-check (index integrity)

Fails if any of these do not hold:

- Indexed commit (`.gitnexus/meta.json` `lastCommit`) `==` git `HEAD`.
- `meta.json` node/edge stats `==` live graph `count(n)` / `count(r)` (via `gitnexus cypher --repo <checkout-path>`; every query targets this checkout even when multiple indexes are registered).
- Zero garbage IDs (id not prefixed with its label) **and** zero empty/null `name` values
  for `Class`, `Method`, `File`, and `Property` nodes.

### Exit codes

| Code | Meaning |
|------|---------|
| 0 | Good index (`gitnexus-safe-reindex: OK`) |
| 1 | Refused (dirty tree, wrong branch, bad usage) |
| 2 | `analyze` failed, or an MCP server could not be stopped |
| 3 | Bad index (post-check failed, or `analyze` left untracked files) |

Metadata read errors and failed/unparseable integrity queries also exit 3.

Regression checks (temporary Git repositories and fake CLI executables; no npm
installs or MCP process kills):

```bash
python3 -m unittest discover -s tools -p test_gitnexus_safe_reindex.py -v
```

The `gauntlet-pytest` workflow runs these alongside its existing Python checks.

### Troubleshooting

- **Exit 1, "working tree is not clean" / "not main":** commit or stash first, or pass
  `--allow-dirty` / `--allow-branch` if you understand the risk (dirty trees are not reverted).
- **Exit 2, "gitnexus mcp still running after kill":** an MCP host is respawning the server.
  Stop the host (or run on a dedicated clone) and retry; the guard cannot prevent respawns.
- **Exit 3, "BAD INDEX":** the index is corrupt. Re-run the guard (full `--force` rebuild);
  if it persists, confirm no MCP process is attached to `.gitnexus/` during analyze.
- **"analyze left untracked files":** remove them manually, then re-run; the guard will not
  auto-delete untracked output.

### Why always `--force`

Finding (2026-10-15, dedicated clone, no MCP process running): a first full analyze was
clean, but a following *incremental* analyze (`Incremental: changed=1, added=1 ... unchanged
file rows preserved`) produced 8,625 empty `Method` names and thousands of garbage
`File`/`Property` IDs; `analyze --force` restored a clean index. The guard therefore always
forces a full rebuild rather than trusting incremental analyze.

### Limits

The guard cannot stop the MCP host from respawning `gitnexus mcp` on demand, so prefer a
dedicated indexing clone (not a shared working checkout) and a distinct registry alias
(`--name`). Use the CLI (`npx gitnexus impact|context|cypher`) rather than MCP tools until
DRG-323 is closed.
