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
