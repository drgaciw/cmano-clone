# Story 004 — Content + Platform Editor honesty (docs 09, 10, 21)

**Epic:** requirements-corpus-maturity  
**Wave:** 3  
**Status:** Complete  
**Type:** Documentation + traceability  
**Design:** docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md §5 Wave 3  
**Depth:** Standard honesty  
**Tracks:** W3-a (09), W3-b (10 + tracker 10/10b), W3-c (21)

## Context

Waves 0–2 complete. Docs **09 / 10 / 21** remain research-integrated or Draft with missing FR reverse-refs, thin/stale Implementation Mapping, and tracker **10b** overclaiming S54 Kessler/DEW as Implemented while types are absent from `src/` on trunk.

## Acceptance

1. Docs **09, 10, 21** have Last Updated ≥ wave date + tracker grade footer  
2. Each has Implementation Mapping (≥4 evidence rows)  
3. **09/10** reverse-ref hub **FR-08**; **21** reverse-refs **FR-19**  
4. Doc **09** does not claim full tech matrix as shipped; headless spine mapped  
5. Doc **10** does not claim orbital DEW/Kessler/ladder as on-main shipped  
6. Doc **21** mapping not all “New”; FR-19 present  
7. Tracker **10b** status = **Phase N / not on main**; row **10** evidence no longer claims S54 DEW/Kessler on trunk  
8. `git diff` has **no** `.cs` / goldens / DelegationBridge  
9. S56 grades for 09 / 10 / 21 base rows not re-litigated (10b demotion is fact fix only)  
10. Design-review memo APPROVED or APPROVED WITH NOTES  

## Out of scope

- Re-land S54 DEW/Kessler code  
- ADR-011 / hub Related Index rewrite  
- Doc 11 AME residual  
- Adversarial W2 test merge (separate worktree)  
- Production sim hotpath / hash / CatalogWriteGate write-path changes  

## Completion Notes

- **Completed:** 2026-07-08  
- **Evidence:** Mapping 09=11 / 10=8 / 21=21 rows; FR-08/FR-19; tracker 10b **Phase N / not on main**; design-review **APPROVED**; docs-only verify battery  
- **Code Review:** N/A (docs)  
