# Epic: Sprint 33 — Kill-Chain Intelligence (DBI P1)

> **Status:** Ready  
> **Sprint:** 33  
> **GDD:** Req 06 Database Intelligence §2–3 (DBI-1.5, DBI-3.5)

## Goal

Materialize **weapon→mount→sensor dependency graph index** and **kill-chain impossibility rule pack** in `CatalogRulesValidationAgent`, wired through orchestrator and curator CLI.

## Stories

| # | Story | ID | Priority | Est. | Status |
|---|-------|-----|----------|------|--------|
| 02 | [Dependency graph index](story-033-02-dependency-graph-index.md) | S33-02 | must-have | 2.5d | Complete |
| 03 | [Kill-chain rule pack](story-033-03-kill-chain-rule-pack.md) | S33-03 | must-have | 2.5d | Complete |
| 05 | [Orchestrator kill-chain gate](story-033-05-orchestrator-kill-chain-gate.md) | S33-05 | should-have | 1.5d | Complete |
| 08 | [Kill-chain CLI verbs](story-033-08-kill-chain-cli.md) | S33-08 | should-have | 1d | Complete |

## Sprint gate

**S33-02** — dependency graph index operational and commit-invalidated on `ApproveBatch`.

## GitNexus

- **CRITICAL extend-only:** `CatalogWriteGate`
- **HIGH:** `CatalogRulesValidationAgent`, `DatabaseIntelligenceOrchestrator`, `ICatalogReader`