# GitNexus Index Health

**Purpose:** Operational notes for keeping the `cmano-clone` GitNexus index current after merges.

## Stale index symptoms

- `node .gitnexus/run.cjs status` reports **stale** (indexed commit ≠ `HEAD`)
- MCP `query` returns degraded FTS/embeddings warnings
- `impact` / `context` missing recently merged symbols

## Rebuild (local)

From repo root:

```bash
node .gitnexus/run.cjs analyze
# or: npx gitnexus analyze --force
node .gitnexus/run.cjs status   # expect ✅ up-to-date
```

Incremental analyze preserves existing embeddings when possible. Full wipe is rarely needed.

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

## S57–S64 Baltic v2 closeout re-index (2026-06-22)

- **search_tool** for gitnexus tools (full schemas for list_repos, detect_changes, impact, context etc. returned; MCP confirmed).
- list_repos (pre/post + post-reindex): primary `cmano-clone` at project root path: files=2417, nodes=19497, edges=36982, communities=393, processes=300, embeddings=8288 @ commit ff1547c (MCP indexedAt 2026-06-22T15:47:44 post-CLI-analyze).
- detect_changes (post-merge on S57-S64/ack merge, scope=compare base HEAD~1): changed_count=21 (doc sections), affected_count=0, risk=low, affected_processes=[] . Clean.
- Re-index (final): CLI `node .gitnexus/run.cjs analyze` (incremental, 0 changed, 12.5s) → **19,497 nodes | 36,982 edges | 393 clusters | 300 flows** ; status ✅ up-to-date (HEAD ff1547c). MCP list_repos confirms. Post S64 ack merge re-index complete.
- impact() on ALL §5 CRITICALs (upstream, summaryOnly=true, repo=main path):
  - PatrolCandidateEngagePolicy: CRITICAL impactedCount=97 (direct=2, procs=2: RunBatch/Run)
  - CatalogWriteGate: CRITICAL 176 (direct=93, 7 procs incl. catalog imports)
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
