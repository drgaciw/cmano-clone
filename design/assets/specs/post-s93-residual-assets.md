# Asset Specs — Post-S93 Residual Needed (036 / 037 / 040 / 041)

> **Source:** `design/assets/asset-manifest.md`, S93 closeout, post-S93 gate Track B  
> **Art Bible:** `design/art/art-bible.md` (lean B2)  
> **Generated:** 2026-07-14  
> **Status:** Specced (production-ready lean specs)  
> **Authority:** post-s93-project-release-hold-gate-2026-07-14.md Track B; s93-asset-production-scope-boundary

---

## ASSET-036 — Main Menu shell

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | 1920×1080 layout |
| Format | UXML + USS (vector/USS-first) |
| Path (proposed) | `production/assets/ui/MainMenuShell.uss` (+ optional UXML later) |
| Naming | `MainMenu*` |

**Visual Description:**  
Cold-start shell: title lockup, primary CTA (New Baltic Patrol / Continue), secondary (Settings, Credits). Matte dark surfaces matching AegisTokens (`#080E16` base). No sim mutation — presentation only.

**Acceptance Criteria:**
- [ ] USS builds without error when imported to Unity UI Toolkit
- [ ] Uses tokens from ASSET-014 `AegisTokens.uss` where possible
- [ ] No UGUI mix; no hardcoded sim IDs

**Dependencies:** ASSET-014 (tokens), art bible §1–3  
**Status:** Specced → produce stub USS in Track B2

---

## ASSET-037 — Scenario Select

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | 1920×1080 layout |
| Format | UXML + USS |
| Path (proposed) | `production/assets/ui/ScenarioSelect.uss` |

**Visual Description:**  
List/grid of scenario cards (title, tier tag, catalog-backed ORBAT hint). Selected state uses single warm accent. Read-only roster preview; load action is controller-bound.

**Acceptance Criteria:**
- [ ] Card density legible at 1080p
- [ ] Focus/hover states for keyboard nav
- [ ] Compatible with scenario policy id strings from `data/scenarios/`

**Dependencies:** ASSET-014, ASSET-036  
**Status:** Specced → produce stub USS in Track B2

---

## ASSET-040 — Policy denial SFX

| Field | Value |
|-------|-------|
| Category | Audio |
| Duration | 80–200 ms one-shot |
| Format | WAV 48 kHz mono (or OGG) |
| Path (proposed) | `production/assets/audio/sfx_policy_denial.wav` |

**Sound Description:**  
Short dry “denied” click/thud — low mid, non-alarmist, readable under music. Not a weapon impact.

**Acceptance Criteria:**
- [ ] Peak normalized ≤ −1 dBFS
- [ ] Loop-free one-shot
- [ ] Distinct from ASSET-041

**Status:** Specced → produce placeholder tone in Track B2 if tooling available

---

## ASSET-041 — ROE change SFX

| Field | Value |
|-------|-------|
| Category | Audio |
| Duration | 120–300 ms one-shot |
| Format | WAV 48 kHz mono (or OGG) |
| Path (proposed) | `production/assets/audio/sfx_roe_change.wav` |

**Sound Description:**  
Soft ascending confirmation blip (ROE loosened) / neutral double-click (ROE tightened) — single file for default “ROE updated” unless split later.

**Acceptance Criteria:**
- [ ] Peak ≤ −1 dBFS
- [ ] Distinct timbre from ASSET-040
- [ ] No speech

**Status:** Specced → produce placeholder tone in Track B2

---

## Cross-links

- Manifest: `design/assets/asset-manifest.md`
- Gate: `production/gate-checks/post-s93-project-release-hold-gate-2026-07-14.md`
- S93 closeout: `production/qa/smoke-sprint-93-closeout-2026-07-09.md`
