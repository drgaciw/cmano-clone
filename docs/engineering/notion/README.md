# Notion (Project Aegis) — agent index

Git is the **source of truth** for artifact files. Notion mirrors metadata for navigation, status, and agent workflows.

## Databases

| Database | ID |
|----------|-----|
| Requirements | `cbdc1d28057147c8a66bff9495946913` |
| Specs | `8050c95c5dc54fecbf70d44d9ecd8f8a` |
| ADRs | `16f16964138245b99f2c58526a029ff7` |
| Milestones | `1dc99c0ef1bc4a5e8bd9d9d849db07d5` |
| Replays & Golden Fixtures | `9bc84746d02a41ef97c6a9221cf5962b` |
| Engineering Runbooks | `38717d7993a0468dbea65655d39579aa` |

## Free / Plus plan constraints

- Official Notion MCP works on Free/Plus for create/update/fetch/search.
- **Data-source SQL query tools** may return entitlement/quota errors. Agents must reconcile via **search + fetch + committed path map**, not full-table SQL.
- Rate limit guidance: ~3 req/s per connection; throttle automation to 1–2 req/s; backoff on 429.

## Machine properties (target)

After Phase 1, every DB should have:

| Property | Role |
|----------|------|
| `Source path` | Repo-relative path (idempotency key) |
| `UID` | Stable id (`REQ-01`, `ADR-011`, …) |
| `Last synced` | Last agent sync date |
| `Sync note` | Needs-review / error text |

Runbooks additionally need `Status` (Draft / Current / Deprecated).

## Agent MCP tool order

1. `notion-fetch` — schema / page verify  
2. `notion-search` — find by UID or path  
3. `notion-create-pages` — only if path not mapped  
4. `notion-update-page` — identity/status only  
5. `notion-query-data-sources` — optional; never hard-fail Free-plan jobs  

## Docs in this folder

| File | Purpose |
|------|---------|
| [phase0-baseline.md](./phase0-baseline.md) | Capability probe + schema targets |
| [notion-mcp-sync.md](./notion-mcp-sync.md) | Sync playbook (expand as runner lands) |
| [path-page-map.json](./path-page-map.json) | Committed path→page_id map (**Phase 2 done**, 253 entries) |
| [phase2-complete.md](./phase2-complete.md) | Backfill results |
| `schema-baseline-*.json` | Live schema snapshots (Phase 0 live probe) |

## Related plan

Session plan: optimize Notion for Agent/MCP reliability (Git-primary, additive schema, Free-plan path).
