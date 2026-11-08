# Notion optimization — Phase 0 baseline

**Date:** 2026-07-24  
**Plan:** Optimize Project Aegis Notion for Agent/MCP Reliability  
**Workspace plan:** Free / Plus (no Business AI data-source query entitlement)  
**Source of truth:** Git-primary for all six databases  
**Schema policy:** Additive only

## Locked decisions

| Decision | Choice |
|----------|--------|
| Primary goal | Agent/MCP reliability |
| Notion plan | Free / Plus |
| SoT | Git-primary for all six DBs |
| Schema changes | Additive only |

## Target databases

| Database | ID |
|----------|-----|
| Requirements | `cbdc1d28057147c8a66bff9495946913` |
| Specs | `8050c95c5dc54fecbf70d44d9ecd8f8a` |
| ADRs | `16f16964138245b99f2c58526a029ff7` |
| Milestones | `1dc99c0ef1bc4a5e8bd9d9d849db07d5` |
| Replays & Golden Fixtures | `9bc84746d02a41ef97c6a9221cf5962b` |
| Engineering Runbooks | `38717d7993a0468dbea65655d39579aa` |

Hub page (prior session): `362f7cb4e4df80e0a587eb0ae15d5c9c` (Project Aegis Notion Hub).

## Known property sets (from prior live fetch; re-verify)

### Requirements
`Name` (title), `Status` (Draft / In review / Approved / Implementing / Done), `Type` (multi), `Priority`, `Owner`, `Source (legacy)` (url), `Notes / Open questions`, `Specs` (relation), Created/Last updated, Last reviewed.

### Specs
`Name`, `Spec status` (Draft / In review / Approved / Implementing / Shipped), `Subsystem`, `Determinism-impacting`, `GitHub Issue/PR` (url), `Requirements` (relation), Owner, Target, Created/Last updated.

### ADRs
`Title`, `Status` (Proposed / Accepted / Superseded), `Decision type`, `GitHub PR/commit` (url), `Consequences (short)`, Date, Created/Last updated.

### Milestones
`Name`, `Status` (Planned / Active / Complete), `Scope (short)`, `Release tag/URL`, Target date, Created/Last updated.

### Replays
`Name`, `Status` (Green / Needs update / Investigate), `Scenario`, `Expected hash/artifact`, `Purpose`, Seed, CI link, Notes, Last verified, Created/Last updated.

### Runbooks
`Title`, `Area`, `Severity`, `Related systems` — **no Status property** (gap for agents).

## Additive properties required (Phase 1)

On **all six** DBs (if missing):

| Property | Type | Purpose |
|----------|------|---------|
| `Source path` | text | Canonical repo-relative path (idempotency key) |
| `UID` | text | Stable machine id (`REQ-01`, `ADR-011`, …) |
| `Last synced` | date | Last agent sync |
| `Sync note` | text | Errors / needs-review reason |

Runbooks only: add `Status` = Draft | Current | Deprecated.

## Capability probe results

| Probe | Result | Notes |
|-------|--------|-------|
| Notion MCP tools via Cursor `search_tool` | **Blocked** (session start 2026-07-24 post-approval) | Server configured; requires re-auth OAuth |
| Prior OAuth token in scratch | **Absent** | Prior session tokens not durable |
| `notion-query-data-sources` (prior session) | **ENTITLEMENT_FAIL / quota** | Free plan: do not hard-require SQL |
| Free-plan agent path | **search + fetch + local path map** | Confirmed viable in prior population |

### Re-run when MCP auth available

```text
T0.1  tools/list → confirm notion-fetch, notion-search, notion-create-pages, notion-update-page
T0.2  notion-fetch each database ID → save schema-baseline-*.json under this directory
T0.3  one notion-query-data-sources call → record PASS or ENTITLEMENT_FAIL
T0.4  seed path-page-map from search+fetch or prior live-after map
```

## Inventory roots (Git SoT)

| Category | Paths |
|----------|--------|
| Requirements | `Game-Requirements/requirements/*.md`, `docs/requirements/` |
| Specs | `design/gdd/*.md`, `docs/architecture/*.md` (non-ADR), `docs/superpowers/**` |
| ADRs | `docs/architecture/adr-*.md`, `docs/adr/*.md` |
| Milestones | `production/milestones/`, `production/gate-checks/`, roadmap aliases |
| Replays | `tests/regression/replay-golden-*.txt`, related scenario policies |
| Runbooks | `docs/engineering/*.md`, explicit `*runbook*` |

## Free-plan agent operating model

1. Inventory filesystem → list of `{path, title, status}`.
2. Load `path-page-map.json` if present.
3. Match by `Source path` / `UID` via map first; else `notion-search` + `notion-fetch`.
4. Missing → `notion-create-pages` (properties + minimal body).
5. Present + stale → `notion-update-page` (UID/Source path/Status/Last synced only).
6. **Never delete.**
7. Treat SQL query failures as expected; do not fail the sync job solely on SQL.

## Prior population scale (approx.)

~250 rows created in prior session (22 requirements, 78 specs, 19 ADRs, 50 milestones, 36 replays, 45 runbooks). Re-count after re-auth.

## Exit criteria for Phase 0

- [x] Decisions + DB IDs documented  
- [x] Free-plan constraints documented  
- [x] Additive schema target documented  
- [x] Live MCP tools/list after re-auth (2026-07-24 Phase 1 session)  
- [x] Live schema-baseline files under `schema-baseline-*.md`  
- [ ] Explicit SQL probe result recorded (optional; Free-plan may fail)

## Next phase

**Phase 1 COMPLETE** — see [phase1-complete.md](./phase1-complete.md). Proceed to **Phase 2** backfill.
