# Wave 2 Plan — Template B honesty (13–20)

**Story:** 003 · **Depth:** Standard · **Tracks:** W2-a…e parallel · **Docs-only**

## Shared footer

```markdown
---
**Implementation grade:** <Partial|Partial+> — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row NN.
Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.
```

## Prefixes

ROE- (13), ENG- (14), SEN- (15), LOG- (16), RPL- (17), DOM- (18), CYB-/COM- (19), CMD- (20)

## Exit verify

```bash
rg -l '## Implementation Mapping' Game-Requirements/requirements/1{3,4,5,6,7,8,9}-*.md Game-Requirements/requirements/20-*.md
rg -n 'FR-1[1-8]|re-honesty: Wave 2' Game-Requirements/requirements/1{3,4,5,6,7,8,9}-*.md Game-Requirements/requirements/20-*.md
git diff --name-only | rg '\.cs$|tests/regression|DelegationBridge' || true
```

See approved session plan for full track specs.
