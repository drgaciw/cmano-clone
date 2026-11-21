# C10 — Owner pixel sign-off package (UAT 2026-11-21)

**For:** project owner. **Rubric criterion:** C10 "Pixel-level visual acceptance (DRG-162) — agent supplies screenshots; owner signs off". The agent's part (supplying the evidence) is complete with this package; the sign-off column below is yours.

**Capture conditions:** Unity Editor 6000.3.27f2 Game View at 1477×721 (panel reference 2056×1004), scene `Assets/Scenes/DelegationSmoke.unity`, scenario `baltic-patrol-classify`, sim paused at T+2–3 s after ACK + RESUME. Earlier screenshots that show a defect *before* its fix are kept for comparison (`r3-*`); `r4-*` / `r5-*` show the fixed state.

| # | Screenshot | Defect(s) | What to look at | Expected (fixed) | Owner sign-off |
|---|-----------|-----------|-----------------|------------------|----------------|
| 1 | [`r3-d22-hostile-attack-disabled.png`](r3-d22-hostile-attack-disabled.png) | D22 (before D29/D12 fixes) | Right panel with `hostile-1` selected | Attack buttons disabled with `NOT_PLAYER_CONTROLLED` (labels clipped here — fixed in #3) | ☐ |
| 2 | [`r3-d23-d24-d25-contact-selected.png`](r3-d23-d24-d25-contact-selected.png) | D23, D24, D25 (before D30/D31 fixes) | Contact Detail open on `c1`; bottom-centre ORDERS | `ISSUED: hold → u1`; Contact Detail shown (translucency/overlap fixed in #4) | ☐ |
| 3 | [`r4-d29-d12-hostile.png`](r4-d29-d12-hostile.png) | D29, D12 | Right-panel attack buttons; top of left drawer | Button labels wrap in full; "Collapse Left Drawer" fully inside the drawer | ☐ |
| 4 | [`r4-d30b-contact-lines.png`](r4-d30b-contact-lines.png) | D30, D30b | Contact Detail panel | Opaque background, no Unit Detail text bleeding through; ends above the Message Log; "Engagement: UNKNOWN" / "Posture: UNKNOWN" legible | ☐ |
| 5 | [`r4-d31b-drawer.png`](r4-d31b-drawer.png) | D31, D31b | Left drawer, CONTACTS tab | SORT shows "Contact ID", FILTER shows "All", FIND is a dark input; contact row `c1 Classified → hostile-1` visible | ☐ |
| 6 | [`r5-d15-toolbar-visible.png`](r5-d15-toolbar-visible.png) | D15 subset, D25 | Bottom of the screen between the map controls and the Message Log | ORDERS toolbar visible, clear of the Tactical…Latest events row and the Message Log; `ISSUED: hold → u1` | ☐ |

Not covered by screenshots (non-visual or verified by measurement): D27 `TIME: PAUSED (2x)` label (text-verified), toolbar clearance (measured: toolbar x 1138–1616 vs control row x 274–952 and Message Log x ≥ 1628, panel units), D36 map-marker selection (behaviour-verified).

**Sign-off:** ☐ Accepted as shown ☐ Accepted with notes ☐ Rejected — notes:
