---
id: S32-03
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint32/mount-loadout-quarantine
estimate_days: 2.5
dependencies:
  - S32-02 unified manifest landed
owner: team-data
sprint: 32
req_trace: Req 06 catalog integrity; mount/loadout FK quarantine remediation; DBI-3.2
---

# Story 032-03 — Mount/Loadout Quarantine Triage

> **Epic:** sprint-32-release-train-ops  
> **ADR:** ADR-011 (write-gate governance; extend-only `CatalogWriteGate`)

## Summary

Curator triage script for **quarantined ship/facility/submarine child rows** (mount/loadout FK gaps). Bounded FK repair rules; quarantine count reduction evidenced. CI stays curated slices only — off-CI scratch DB for repair validation.

## Acceptance Criteria

- [x] Curator triage script identifies quarantined mount/loadout child rows by domain
- [x] Bounded FK repair rules applied — no scope creep beyond documented repair envelope
- [x] Quarantine count reduction evidenced per domain (before/after counts)
- [x] CI remains curated slices only — no full corpora in `dotnet test`
- [x] WriteGate regression PASS; no live-table mutation outside approved repair path
- [x] Evidence doc with per-domain repair summary
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Bounded FK repair
  - Given: quarantined ship/facility/submarine mount/loadout child rows on scratch DB
  - When: curator triage script runs with bounded repair rules
  - Then: FK violations resolved for in-envelope rows; out-of-envelope rows remain quarantined with documented reason
  - Edge cases: circular FK references; orphan mount rows; duplicate loadout keys

- **AC-2**: CI scope unchanged
  - Given: post-repair curated catalog slices
  - When: CI data test filters run
  - Then: tests PASS on curated slices only; quarantine count reduction evidenced
  - Edge cases: accidental full-corpus import in CI; repair script mutates live tables

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|CmoMarkdown|Platform|CatalogImport|Snapshot|Quarantine" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
npx gitnexus impact CatalogWriteGate
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — extend-only |
| `PlatformWorkbookWriteBridge` | HIGH |
| `ICatalogReader` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-03)
- Track plan: `production/agentic/sprint-32-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*