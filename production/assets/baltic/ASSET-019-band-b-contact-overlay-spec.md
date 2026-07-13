# ASSET-019 — Band B Datalink Contact Presentation Spec Sheet

**Asset ID:** ASSET-019  
**Sprint:** S93 (2026-07-09)  
**Authority:** [`design/assets/specs/baltic-patrol-assets.md`](../../../design/assets/specs/baltic-patrol-assets.md), art bible §2–§3, `baltic-v2-patrol-band-b` policy family

---

## Purpose

Specification sheet for band-B datalink contact presentation — live contacts at full opacity with datalink-active styling. Feeds store screenshot #1 (ASSET-027) when Editor capture available.

## Visual Rules

| Element | Token / value | Notes |
|---------|---------------|-------|
| Map field | `#080E16` (`surface-deep`) | Full canvas backdrop |
| Map canvas | `#101824` (`surface-map`) | Theater board region |
| Friendly contact | `#4A9EFF` square frame | APP-6 friendly |
| Hostile contact | `#E85D5D` diamond frame | APP-6 hostile |
| Datalink active | 100% opacity | No stale/ghost modifiers |
| Theater label | `#8C9BAA` (`text-muted`) | Upper-left muted label |
| Top bar comms | `#64C88C` (`comms-nominal`) | Band B nominal datalink |

## Symbol Placement (reference)

```
┌─────────────────────────────────────────────┐
│ THEATER: BALTIC PATROL — BAND B             │
│                                             │
│     ◆ hostile-1    ■ u1                     │
│              ◆ ucav-red                     │
│     ■ ucav-blue                             │
│                                             │
│  [datalink: NOMINAL — live contacts 100%]   │
└─────────────────────────────────────────────┘
```

## Policy Binding

- Primary: `baltic-v2-patrol-band-b.policy.json`
- v3 equivalent: `baltic-v3-*` band B family (read-only; no hash change)

## Production Files

| File | Role |
|------|------|
| `baltic-theater-framing-v1.png` | ASSET-018 theater board placeholder |
| `ASSET-019-band-b-contact-overlay-spec.md` | This sheet |
| `App6FrameAtlas.png` | Symbol frames (ASSET-004) |

## Verification

- [ ] Colors match `AegisTokens.uss`
- [ ] No 3D ocean VFX; map placeholder only
- [ ] ReplayGolden **6/6** unchanged
- [ ] Capture deferred to Phase C Editor host (ASSET-027)

**Status:** **Done** @ S93 — spec sheet + reference framing PNG on disk.
