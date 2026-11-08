# QA Plan — Sprint 96 Architecture Hygiene (2026-07-15)

**Sprint:** S96 only  
**Stage:** **Release** (no Launch)  
**Authority:** [`sprint-96-architecture-hygiene.md`](../sprints/sprint-96-architecture-hygiene.md)

## Tracks

| Track | Story | Check |
|-------|-------|-------|
| Arch freshness | S96-01 | architecture.md updated **or** re-matrix report dated post-S93/gauntlet |
| AGENTS hub | S96-02 | Playbook path + CRITICAL hubs discoverable |
| Closeout | S96-03 | Smoke; stage Release; ≥1638 cited |

## Test cases

| ID | Pass criterion |
|----|----------------|
| QA-96-01 | Architecture artifact exists and mentions post-S93 / gauntlet / editors |
| QA-96-02 | `rg critical-hub-merge-playbook AGENTS.md` hits |
| QA-96-03 | Stage starts with Release |
| QA-96-04 | Suite ≥1638 cited (docs-only) |

## Non-goals

S97, Launch, S95 re-implementation, full ADR re-audit.

---
*QA plan S96 — 2026-07-15*
