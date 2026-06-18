---
id: S28-11
status: Not Started
type: Config
priority: nice-to-have
graphite_branch: stack/sprint28/tl-branch-spike
estimate_days: 1
dependencies:
  - S28-02 complete
owner: team-data
sprint: 28
req_trace: Req 06 TL-0–TL-5 branch databases (spike only)
---

# Story 028-11 — TL Branching Spike (Export-Only)

> **Epic:** sprint-28-cmo-corpus-v2  
> **ADR:** ADR-006 (database branching release train — spike)

## Summary

Document TL-gated branch DB workflow for export-only evaluation. Spike doc with PROCEED/DEFER verdict. **No production branching**; no runtime branch binding.

## Acceptance Criteria

- [ ] Spike doc authored with TL-0–TL-5 branch workflow outline
- [ ] Export-only workflow documented (diffable drops, snapshot hashes)
- [ ] PROCEED or DEFER verdict recorded with rationale
- [ ] No runtime branch binding in code
- [ ] No TL-0–TL-5 production branch databases shipped
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Spike doc completeness
  - Given: draft spike doc
  - When: reviewed against Req 06 branching requirements
  - Then: workflow, rollback, and CI/nightly separation documented
  - Edge cases: parallel track overload; GHA billing constraints

- **AC-2**: No runtime binding
  - Given: codebase after spike
  - When: grep for TL branch runtime binding
  - Then: zero production bindings; doc-only artifact
  - Edge cases: accidental scenario DB branch switch

## Verify Commands

```bash
# Doc-only story — no code gate required
# Optional: confirm no new runtime branch bindings
rg -l "TlBranch|BranchDatabase" src/ --glob "*.cs" || true
ls production/agentic/sprint-28-tl-branching-spike-*.md 2>/dev/null || echo "spike doc pending"
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `OsintCatalogMapper` | LOW — TL routing reference only |
| `DelegationBridge.cs` | ZERO touch |

## References

- S22-07 pattern: `production/sprint-status.yaml` (OsintCatalogMapper TL routing)
- Skill: `database-branching-release-train`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-11)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*