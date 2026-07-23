---
name: aegis-write-gate-approve
description: >
  Project Aegis catalog write gate: extend-only IWriteGate (ADR-006), propose vs approve,
  error codes, catalog_write_approve CLI. Use when approving staged catalog batches or
  debugging WriteGateDecision failures. Never write SQLite catalog tables directly.
---

# Aegis Write Gate Approve

Operational skill for the **propose → review → approve** path that is the only audited way catalog data reaches live SQLite. Deep reference: [catalog-write-gate.md](../../docs/engineering/catalog-write-gate.md).

## When to use

- Approving batches after `catalog_import_markdown`, workbook import, or OSINT staging
- Debugging `{ ok: false, errors: [...] }` from `catalog_write_approve`
- User asks to "approve catalog batch", "commit staged rows", or "write gate approve"

## Hard invariants (ADR-006)

- **Extend-only:** add new `Propose*Batch` overloads; never alter or delete existing propose/approve paths.
- **Two-step by design:** import/workbook stages proposals; approve UPSERTs live rows + audit log + snapshot.
- **Never write SQLite directly** — no hand-edited UPSERTs into catalog tables, no bypassing `CatalogWriteGate`.
- **Deterministic clock:** `FixedCatalogClock`; never `DateTime.UtcNow` on the commit path.
- **Human approves production:** CLI approve uses `actorType=human` / `reviewer-mcp`.

## Lifecycle

```text
Propose*Batch  →  staging tables (approval_state = "proposed")
       │  ListPendingBatches() / review JSON report
       ▼
ApproveBatch   →  validate → UPSERT live → audit log → snapshot (sensors)
       │
RejectBatch    →  drain staging (no orphans)
```

`WriteGateDecision.Committed == true` is the only success signal.

## Propose vs approve

| Step | Who | What happens |
|------|-----|--------------|
| **Propose** | Agent / import / workbook | Rows sorted, staged under batch id; **nothing live yet** |
| **Review** | Curator | Read `*-propose.json`, quarantine, pending batches |
| **Approve** | **Human** | Validates guards, commits live rows, pins snapshot hash |

`catalog_import_markdown` and `platform_import_xlsx` are **propose-only**. If catalog reads look unchanged, you skipped approve.

## CLI: `catalog_write_approve`

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve \
  --db <catalog.db> \
  --batch <batchId-from-propose-report> \
  [--snapshot-id <id>] \
  [--release-version <ver>] \
  [--enable-balance-drift]
```

Success prints `{ ok, batchId, releaseVersion, snapshotId, contentHashSha256, … }`. Failure prints `{ ok: false, errors: [...] }`.

List batch ids from the propose JSON `batches[]` array or `nextStep` hint in `catalog_import_markdown` output.

## Error codes (match prefix before `:`)

| Code | Meaning / fix |
|------|---------------|
| `staging_batch_not_found` | Unknown or already drained batch — re-propose |
| `ambiguous_staging_batch` | Mixed row kinds under one batch id — one kind per propose call |
| `quarantine:{platform}/{sensor}:{reason}` | Sensor failed import floor — fix confidence/TRL/review state, re-propose |
| `orphan_platform:{platformId}` | Child row references missing platform — approve platform batch first |
| `kill_chain:{CODE}` | Kill-chain dependency error — resolve before commit (`KILL_CHAIN_*`) |

`Propose*Batch` throws `ArgumentException` on empty input (programming error, not review outcome).

## Approve guards by row kind

| Kind | Guards |
|------|--------|
| Sensor | Import partition (confidence / TRL / review-state floors) |
| Platform / Weapon / Mount | Kill-chain commit gate |
| Mount / Loadout / Mobility / Signature / EMCON / Damage | Platform must exist |
| Magazine / Comms / Link | Row-shape validated at propose/upsert |

## Related CLI verbs

| Verb | Role |
|------|------|
| `catalog_write_propose` | Single-row sensor staging (agent) |
| `catalog_import_markdown` | Parse markdown → propose batches |
| `platform_import_xlsx` | Workbook → propose (no auto-commit) |
| `osint_staging_review` | List / approve OSINT proposals |

Full verb table: [mission-editor-cli.md](../../docs/engineering/mission-editor-cli.md).

## What not to do

- Open `assets/data/catalog/*.db` in a SQLite GUI and INSERT/UPDATE catalog tables
- Auto-approve production batches from an agent without human review
- Hand-craft batch ids or mix row kinds in one staging batch
- Regenerate golden hashes silently after approve-path changes

## Further reading

- [catalog-write-gate.md](../../docs/engineering/catalog-write-gate.md)
- [ADR-006 data layer boundary](../../docs/architecture/adr-006-data-layer-boundary.md)
- [AGENTS.md hard invariants](../../AGENTS.md#hard-invariants--never-break-these)
- Tests: `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGate*.cs`
