# Notion MCP sync playbook (Project Aegis)

## Purpose

Keep the six Aegis Notion databases aligned with the Git monorepo **without** relying on Free-plan data-source SQL quotas.

## Prerequisites

1. Notion MCP configured (`https://mcp.notion.com/mcp`) and **authenticated** (OAuth).
2. All six databases + Hub shared with the MCP connection.
3. Repo checkout with inventory roots present.
4. Optional: `docs/engineering/notion/path-page-map.json` after Phase 2.

## Identity rules

| Field | Rule |
|-------|------|
| `Source path` | Exact repo-relative path using `/` (never absolute) |
| `UID` | Deterministic from path (see conventions below) |
| GitHub URL fields | Prefer `https://github.com/drgaciw/cmano-clone/blob/main/{Source path}` in addition to `Source path` |

### UID conventions

| Category | Pattern |
|----------|---------|
| Requirements | `REQ-{nn}` from `NN-Name.md` |
| ADRs | `ADR-{n}` from `adr-0NN-…` |
| Specs | `SPEC-{stem}` |
| Milestones | `MS-{stem}` |
| Replays | `RPLY-{stem without replay-golden-}` |
| Runbooks | `RB-{stem}` |

## Sync algorithm (idempotent)

```
for each inventory artifact:
  key = Source path
  if key in path-page-map:
    page_id = map[key]
    fetch page (optional spot-check)
    if UID/Source path/status stale:
      notion-update-page (properties only)
    set Last synced = today
  else:
    search data source by Source path or UID
    if found:
      update map; update properties
    else:
      notion-create-pages (parent = database_id, properties + minimal body)
      append map
never delete
```

## Free-plan constraints

| Action | Supported? |
|--------|------------|
| create / update / fetch / search | Yes |
| Full-table SQL query | Often **no** (entitlement/quota) |
| Reconciliation gate | **Must** work with SQL disabled |

## Rate limits

- Target **1–2 requests/second**.
- Batch creates ≤ 40 pages (prefer 20).
- On 429: exponential backoff (1s, 2s, 4s…), max 5 retries.

## Status mapping (inventory → Notion)

| Inventory | Requirements Status | Specs | ADRs | Milestones | Replays | Runbooks |
|-----------|---------------------|-------|------|------------|---------|----------|
| complete | Done | Approved | Accepted | Complete | Green | Current |
| draft | Draft | Draft | Proposed | Planned | Needs update | Draft |
| incomplete | In review | Draft | Proposed | Planned | Investigate | Draft |

Set `Sync note` when inventory is draft/incomplete (what is missing).

## Disaster recovery

If `path-page-map.json` is lost:

1. For each category data source, `notion-search` with queries covering stems / “Source path”.
2. `notion-fetch` each hit; extract `Source path` / UID from properties or body.
3. Rebuild map; commit.
4. Run dry-run sync; expect 0 creates if coverage complete.

## Verification checklist

- [ ] Every inventory path has exactly one mapped page_id  
- [ ] Second dry-run: 0 creates  
- [ ] Spot-check 3 `notion-fetch` pages show Source path + UID  
- [ ] Draft rows have Draft/Planned + Sync note  
- [ ] SQL probe may fail; sync still passes  

## CLI (Phase 3 — planned)

```bash
# Planned entrypoint (not implemented until Phase 3)
node tools/notion/sync-inventory.mjs --dry-run
node tools/notion/sync-inventory.mjs --category requirements
node tools/notion/sync-inventory.mjs --limit 10
```

## Related

- [docs/engineering/notion/](./notion/) — baselines and maps  
- [docs/engineering/notion/phase0-baseline.md](./notion/phase0-baseline.md)
