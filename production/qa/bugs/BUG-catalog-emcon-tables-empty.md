# Bug Report

## Summary
**Title**: Catalog EMCON tables are empty — no per-platform emissions profiles exist
**ID**: BUG-catalog-emcon-tables-empty
**Severity**: S3-Minor (data gap; worked around by an established scenario convention, nothing breaks)
**Priority**: P3 — backlog. Filing so the gap is recorded rather than silently worked around forever.
**Status**: Open
**Reported**: 2026-07-27
**Reporter**: QA Gauntlet run `gauntlet-20260727-1455`, Tier 2 roster build (Tier 2 is the first tier whose ladder row requires EMCON postures)

## Classification
- **Category**: Data / catalog content
- **System**: `ProjectAegis.Data` catalog (`assets/data/catalog/baltic_patrol.db`)
- **Frequency**: Always
- **Regression**: No — the tables appear never to have been populated.

## The gap

Both EMCON tables exist with correct schema but contain **zero rows**:

```
platform_emcon:         0 rows | cols = [platform_id, condition, emitter_id, posture, review_state]
catalog_staging_emcon:  0 rows | cols = [batch_id, platform_id, condition, emitter_id, posture, review_state]
```

(Catalog release `catalog-p0-2026-06-04`, schema version `005`, 79 platform rows — so platforms exist, their EMCON bindings do not.)

This matters because the `/qa-gauntlet` skill instructs the scenario architect to produce scenarios "with EMCON postures consistent with each platform's `CatalogEmcon` profile", and Phase A0 to query the catalog for "each platform's `CatalogEmcon` emissions profile". **That instruction is unsatisfiable as written** — there is no such data to be consistent with.

## Existing workaround (already the corpus convention)

The promoted scenario `data/scenarios/gauntlet-t2-escort-passive.policy.json` models passive EMCON through the **detection block** instead — reduced `basePd` / `envMask` — and is explicit about it in its own intent string:

> "Escort passive-EMCON **stand-in** (low Pd/env mask) [catalog ORBAT: Visby vs Sovremenny]"

So the corpus already solved this honestly, by naming it a stand-in rather than implying catalog-backed EMCON. Tier 2 scenarios generated in this run follow the same convention and likewise say so in their intent text.

## Why this is filed rather than fixed

Populating EMCON profiles is catalog **content** work, not a code fix: it needs real emitter/posture data per platform, entered through the `CatalogWriteGate` propose/approve path with proper provenance tiers (`catalog_staging_emcon` → review → `platform_emcon`). That is a data-authoring task for whoever owns catalog content, not something to invent during a QA run — fabricating emissions profiles would be exactly the kind of unsourced data the write gate exists to prevent.

## Impact if left as-is

Low but non-zero:
- EMCON-flavoured scenarios test the *detection-probability* pathway, not a real emissions-posture pathway. They are still useful tests, but they do not exercise `platform_emcon` at all.
- Any future feature that reads `platform_emcon` will find it empty and silently get default behaviour.
- The skill's A0 instruction will keep producing this same finding on every future run until either the data lands or the instruction is reworded to acknowledge the stand-in convention.

## Suggested resolution options

1. **Populate the tables** via the write-gate propose/approve path with sourced emitter/posture data (the real fix).
2. **Reword the skill's A0/A1 EMCON instruction** to reference the detection-block stand-in convention explicitly, so it stops asking for data that does not exist.
3. Both — 2 now (cheap, stops the recurring confusion), 1 when catalog content work is scheduled.

## Related Issues
- `production/qa/gauntlet/gauntlet-20260727-1455/tier-2/roster.json` — the Tier 2 roster records the same finding in its notes.
- `production/qa/bugs/BUG-scoring-penalises-roe-correct-refusals.md` — separate design question from the same run.
