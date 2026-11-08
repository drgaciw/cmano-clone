# Sprint 34 — Unity Track Plan

**Owner:** team-unity  
**Must-have:** S34-06 (~2d)  
**Should-have:** S34-10, S34-11

## Stories

| ID | Name | Est. | Priority | Deps |
|----|------|------|----------|------|
| S34-06 | Phase H LinkCatalog Unity (viewer + staging + headless) | 2d | must | S34-01, S34-02, S34-03 |
| S34-10 | Presentation evidence (`*-s34-*.png`) | 1.5d | should | S34-06 |
| S34-11 | C2 Check 18 + refresh 14–17 | 1d | should | S34-06, S34-10 |

## Hard boundary

Schema-only Unity — sim owns S34-04. **Do not** start S34-06 until S34-03 merges.

## Headless targets

| Story | Filter target |
|-------|---------------|
| S34-06 | ≥43/43 `PlatformImport\|PlatformCatalogViewer\|PlatformLinkCatalog` |
| S34-10 | ≥48/48 (+ `PlatformComms\|C2TopBar`) |
| S34-11 | ≥55/55 (+ `Doctrine`) |

## Cut line

1. S34-10 (lean placeholders OK) — never cut S34-06