# SWARM-A5 — C2 projection + map integrity readout (DRG-90)

**Date:** 2026-08-09  
**Linear:** [DRG-90](https://linear.app/drgamtd-workspace/issue/DRG-90)  
**Requirements:** SWARM-05, SWARM-09  
**Surface:** `src/ProjectAegis.Delegation/Projection/**` · Delegation.Tests · this note  
**Verdict:** PASS (headless projection; Unity Assets not required for Phase A AC)

## Delivered

| Type | Role |
|------|------|
| `SwarmIntegrityReadout` | count/max + panel/map text |
| `SwarmMapSymbolProjection` | one symbol, swarm glyph ☷, integrity label |
| `SwarmUnitPanelProjection` | single selection id + INTEGRITY line + density |
| `MapSymbolEntry.IsSwarm` / `IntegrityLabel` | additive optional fields |

**CMD-12:** integrity is textual (`24/40`), not color-only.  
**No DelegationBridge edits.** Unity USS frame id `map-app6-frame--friendly-swarm` is declared for atlas follow-on.

## AC

| AC | Test | Verdict |
|----|------|---------|
| Single unit selection | `Selection_selects_single_swarm_unit_id` | **PASS** |
| Panel integrity | `Panel_shows_textual_integrity_not_color_only` | **PASS** |
| Not color-only | CountLabel / IntegrityLine asserted | **PASS** |
| Distinct map symbol | `Map_symbol_is_distinct_from_single_light_aircraft_surface_unit` | **PASS** |

## Verify

```bash
dotnet test src/ProjectAegis.Delegation.Tests --filter "FullyQualifiedName~SwarmC2Projection" -v minimal
```
