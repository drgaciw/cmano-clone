# SWARM-B2 / DRG-93 — Catalog Phase B + CEC flags

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Data/Catalog/**`, migration `013_swarm_phase_b_cec.sql`

## ACs

| AC | Evidence |
|----|----------|
| Generic non-CEC | `uas-swarm-generic.CecCapable=false` |
| ≥1 US/NATO CEC row | `usn-uas-swarm-cec.CecCapable=true` |
| Phase B fields | `DefaultMode`, `RequiresHost`, `AllowedHostClasses` |
| Migration 013 | Additive columns + idempotent skip |

## Tests

Extended `SwarmPlatformCatalogTests` (+3 facts).
