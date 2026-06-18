# Sprint 25 — Damage Schema Sign-off (S25-02)

**Date:** 2026-06-18  
**Status:** PROVISIONAL (agent bootstrap; producer review required before S25-02 merge)  
**Owners:** team-data / producer / qa-lead

## Decision

Damage columns remain on the **Platforms sheet** (Req 21). No new workbook sheet.

## Approved column set (provisional)

| Workbook column | SQL column | C# property | Type | Notes |
|-----------------|------------|-------------|------|-------|
| `MaxHp` | `max_hp` | `MaxHp` | `REAL` | Default 100; must be > 0 |
| `WithdrawThresholdPct` | `withdraw_threshold_pct` | `WithdrawThresholdPct` | `REAL` | Default 0; validator ≤ MaxHp in S25-06 |
| `CriticalFlags` | `critical_flags` | `CriticalFlags` | `INTEGER` | Bitmask (default 0) |

## Table layout

- Live: `platform_damage` (PK `platform_id`)
- Staging: `catalog_staging_damage` (FK `catalog_staging_batch`)

Follows migration 008 pattern (separate domain tables, provenance columns).

## Baltic fixture

- Seed `u1`: `MaxHp=100`, `WithdrawThresholdPct=25`, `CriticalFlags=0`, `review_state=provisional`

## Evidence

- Migration: `assets/data/catalog/migrations/009_platform_editor_phase_b_damage.sql`
- Tests: `CatalogPhaseBDamageMigrationTests.cs`
- Idempotency: `SqliteCatalogReader.ShouldSkipMigration` for `009`

## Producer action required

Confirm column names and `CriticalFlags` encoding before marking **APPROVED** for merge gate.