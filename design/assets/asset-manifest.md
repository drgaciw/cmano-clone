# Asset Manifest

> **Last updated:** 2026-07-09 (S93 asset production wave)  
> **Authority:** [`s93-asset-production-scope-boundary-2026-07-09.md`](../../production/s93-asset-production-scope-boundary-2026-07-09.md), S91 closeout, S93 closeout  
> **Inventory:** `design/assets/entity-inventory.md` (Phase 0b; user review pending)  
> **Art bible:** `design/art/art-bible.md` (lean B2 — C2 + Platform Editor)  
> **S91 specs:** [`design/assets/specs/c2-ui-assets.md`](specs/c2-ui-assets.md), [`baltic-patrol-assets.md`](specs/baltic-patrol-assets.md), [`store-capsule-assets.md`](specs/store-capsule-assets.md)

---

## Progress Summary

| Total | Needed | Specced | In Production | Done | Approved |
|-------|--------|---------|---------------|------|----------|
| 42 | 4 | **27** | **3** | **8** | 0 |

*S93 @ 2026-07-09: First binary wave — **8 Done** (004, 005, 014, 018, 019, 023, 024, 025); umbrellas **001–003 In Production**. **Needed** (4): ASSET-036, 037, 040, 041 (deferred).*

---

## Assets by Context

### Priority stubs (S91 production-ready)

| Asset ID | Name | Category | Status | Spec File |
|----------|------|----------|--------|-----------|
| ASSET-001 | C2 Command Post panel suite | UI | **In Production** | design/assets/specs/c2-ui-assets.md |
| ASSET-002 | Baltic patrol theater presentation | Environment / UI | **In Production** | design/assets/specs/baltic-patrol-assets.md |
| ASSET-003 | Store capsule + header art pack | Marketing | **In Production** | design/assets/specs/store-capsule-assets.md |

### System: Command & Control UI

| Asset ID | Name | Category | Status | Spec File |
|----------|------|----------|--------|-----------|
| ASSET-004 | APP-6 Frame Atlas | Sprite / 2D | **Done** | design/assets/specs/c2-ui-assets.md → `production/assets/c2/App6FrameAtlas.png` |
| ASSET-005 | C2 Top Bar Panel | UI | **Done** | design/assets/specs/c2-ui-assets.md → `production/assets/c2/C2TopBarPanel.uss` |
| ASSET-006 | Message Log Panel | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-007 | Left Drawer (OOB / missions / contacts) | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-008 | Right Unit Detail Panel | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-009 | Map Placeholder Canvas + symbols | UI / Environment | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-010 | Map comms degradation modifiers | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-011 | Delegation badge overlay | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-012 | Policy denial + EMCON HUD | HUD | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-013 | Replay scrubber / hash overlay | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-014 | AegisTokens.uss shared theme | UI tokens | **Done** | design/assets/specs/c2-ui-assets.md → `production/assets/c2/AegisTokens.uss` |

### System: Platform Editor

| Asset ID | Name | Category | Status | Spec File |
|----------|------|----------|--------|-----------|
| ASSET-015 | Platform Catalog Panel | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-016 | Platform Import Staging Panel | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-017 | Staging diff row styles | UI | Specced | design/assets/specs/c2-ui-assets.md |

### Scenario: Baltic patrol (v2 / v3 corpus)

| Asset ID | Name | Category | Status | Spec File |
|----------|------|----------|--------|-----------|
| ASSET-018 | Baltic patrol map framing | Environment | **Done** | design/assets/specs/baltic-patrol-assets.md → `production/assets/baltic/baltic-theater-framing-v1.png` |
| ASSET-019 | Band B datalink contact presentation | UI / Map | **Done** | design/assets/specs/baltic-patrol-assets.md → `production/assets/baltic/ASSET-019-band-b-contact-overlay-spec.md` |
| ASSET-020 | Comms-challenged / jammed capture set | UI / Map | Specced | design/assets/specs/baltic-patrol-assets.md |
| ASSET-021 | Combat domains hot-tick overlay | HUD | Specced | design/assets/specs/baltic-patrol-assets.md |
| ASSET-022 | Band C intercept / spoof stress layout | UI / Map | Specced | design/assets/specs/baltic-patrol-assets.md |

### Store / E7 prep (S70 asset-checklist)

| Asset ID | Name | Category | Status | Spec File |
|----------|------|----------|--------|-----------|
| ASSET-023 | Main capsule (616×353) | Marketing | **Done** | design/assets/specs/store-capsule-assets.md → `production/assets/store/ProjectAegis_BalticMainCapsule_v1.png` |
| ASSET-024 | Small capsule (231×87) | Marketing | **Done** | design/assets/specs/store-capsule-assets.md → `production/assets/store/ProjectAegis_SmallCapsule.png` |
| ASSET-025 | Logo / header variants | Marketing | **Done** | design/assets/specs/store-capsule-assets.md → `production/assets/store/ProjectAegis_Logo_Dark_v1.png`, `ProjectAegis_Icon_v1.png` |
| ASSET-026 | Press kit logo pack | Marketing | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-027 | Store screenshot 01 — C2 map patrol | Capture | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-028 | Store screenshot 02 — policy panel | Capture | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-029 | Store screenshot 03 — order log + replay | Capture | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-030 | Store screenshot 04 — combat domains | Capture | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-031 | Store screenshot 05 — sensor/EW jammed | Capture | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-032 | Store screenshot 06 — catalog panel | Capture | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-033 | Store screenshot 07 — band C intercept | Capture | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-034 | Store screenshot 08 — replay AAR | Capture | Specced | design/assets/specs/store-capsule-assets.md |
| ASSET-035 | Baltic trailer draft (script + placeholder) | Video | Specced | design/assets/specs/store-capsule-assets.md |

### Screens (deferred art — tracked only)

| Asset ID | Name | Category | Status | Spec File |
|----------|------|----------|--------|-----------|
| ASSET-036 | Main Menu shell | UI | Needed (deferred) | — |
| ASSET-037 | Scenario Select | UI | Needed (deferred) | — |
| ASSET-038 | Mission Planning RTwP | UI | Specced | design/assets/specs/c2-ui-assets.md |
| ASSET-039 | Onboarding Baltic overlay | UI | Specced | design/assets/specs/baltic-patrol-assets.md |

### Audio (descriptions only — no generation prompts)

| Asset ID | Name | Category | Status | Spec File |
|----------|------|----------|--------|-----------|
| ASSET-040 | Policy denial SFX | Audio | Needed (deferred) | — |
| ASSET-041 | ROE change SFX | Audio | Needed (deferred) | — |

### Shared / cross-context

| Asset ID | Name | Category | Status | Spec File | Referenced by |
|----------|------|----------|--------|-----------|---------------|
| ASSET-042 | Evidence capture protocol (1920×1080 PNG) | QA / Presentation | Specced | design/assets/specs/c2-ui-assets.md | All UI + store captures |

---

## Cross-links

| Artifact | Path |
|----------|------|
| Entity inventory (Phase 0b) | `design/assets/entity-inventory.md` |
| Art bible (lean B2) | `design/art/art-bible.md` |
| S70 store asset checklist | `production/release/store/asset-checklist.md` |
| S91 closeout | `production/qa/smoke-sprint-91-closeout-2026-07-09.md` |
| S93 closeout | `production/qa/smoke-sprint-93-closeout-2026-07-09.md` |
| Production assets (S93) | `production/assets/c2/`, `production/assets/store/`, `production/assets/baltic/` |
| Release checklist v3 (asset section) | `production/release/release-checklist-v3.md` |
| Evidence index | `production/release/launch/evidence-index.md` |
| QA evidence captures | `production/qa/evidence/` |

---

## GitNexus note (S93)

Assets + docs sprint — expect **low** `detect_changes` risk. No Addressables bulk import; no `DelegationBridge` edits. Manifest bumps only where files exist on disk.
