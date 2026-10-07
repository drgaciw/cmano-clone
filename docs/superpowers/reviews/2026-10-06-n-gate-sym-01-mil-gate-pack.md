# N-GATE-SYM-01 wiki gate pack — Military Tactical symbology subset (MIL-only) — 2026-10-06

**Sprint:** [S126 symbology subset](../../../production/sprints/sprint-126-symbology-subset.md) · **Task:** S126-02 · **Linear:** [DRG-231](https://linear.app/drgamtd-workspace/issue/DRG-231) SYM-MIL-01 · [DRG-232](https://linear.app/drgamtd-workspace/issue/DRG-232) SYM-CIV-01 (pending)
**Requirement source:** [SYM-MIL-01 Notion record](https://app.notion.com/p/3d2f7cb4e4df81bc8028f32673f31e32) (MIL-AC-01…07)
**Document maturity:** Draft, Git-side gate pack. Not published to the Notion design wiki; owner design review pending. No certification claim.
**Scope:** MIL-only. The CIV scope decision (DRG-232: all-units alternate profile vs civilian-vessel-only) has not been made, so this pack does **not** cover dual-profile behaviour.

## Gate verdict

```text
PASS    — MIL subset pinned: NATO APP-6(C) / MIL-STD-2525C 15-character SIDC + declared NTDS subset.
PASS    — Canonical keys, profile switch, ≥3 naval types, disclaimer exist headless (see evidence pack).
PASS    — Presentation wall ADR-010 §2–3 / ADR-007 / ADR-001 preserved; Baltic v2 hash 17144800277401907079 unchanged.
HOLD    — W2-SYM-05 affiliation expansion (W3-SYM-03 freeze).
PENDING — CIV profile (DRG-232 scope decision); dual-profile parity (MIL-AC-06 profile half).
PENDING — Unity visual atlas review, Game View screenshots, owner design approval, DRG-208.
```

Evidence: [`production/qa/evidence/s126-symbology-subset-evidence-2026-10-06.md`](../../../production/qa/evidence/s126-symbology-subset-evidence-2026-10-06.md).

## 1. Overview

A selectable **Military Tactical (subset)** map symbology profile alongside the existing affiliation-only APP-6 placeholder (`Legacy`, first-run default unchanged). The profile restyles projected map symbols only. It reads observer-side `MapSymbolEntry` rows plus an optional observer-known symbol key per symbol, and never reads or writes sim, order, ROE, contact-knowledge or replay state.

## 2. Player fantasy

A naval commander reads a familiar tactical picture: frame shape tells affiliation, a half frame marks a submerged track, and a short icon tells the known platform category. Where the observer does not know a category, the symbol stays generic.

## 3. Detailed rules

- **Profiles:** `Legacy` (default), `MilitaryTactical`, `CivilianFriendly` (reserved, not selectable; `SymbologyProfileSwitch.TrySelect` returns false with a DRG-232 reason).
- **Pinned editions:** NATO APP-6(C) and MIL-STD-2525C, 15-character letter SIDC. The proposed MIL-STD-2525E Change 1 baseline is recorded as a deviation and not adopted in this slice.
- **NTDS subset:** surface = full frame; subsurface = lower-half frame. No NTDS air, ESM or SSDS modifiers.
- **Affiliations (frozen):** Friendly, Hostile, Neutral, Unknown. Any other identity (Suspect, Pending, Assumed Friend, Joker, Faker, null) renders as Unknown (W2-SYM-05 HOLD).
- **Canonical keys (`aegis-sym-keys/v1`):**

| Key | Domain | Dim | Function | Icon | Fallback |
| --- | --- | --- | --- | --- | --- |
| `generic.unknown` | generic | Z | `------` | `?` | itself |
| `naval.surface.combatant` | naval | S | `C-----` | `CBT` | `naval.surface.unknown` |
| `naval.subsurface.submarine` | naval | U | `S-----` | `SUB` | `generic.unknown` |
| `naval.surface.unknown` | naval | S | `------` | `?` | `generic.unknown` |

- **Observer-safe fallback:** missing, empty, unrecognised or wrong-case keys resolve to `generic.unknown` (battle dimension Z). The display never invents a class, category or domain.
- **Destroyed:** only own units carry destroyed state from the projection; SIDC status becomes `X` and the frame id gains `--destroyed`. Contact damage is never disclosed.
- **Legend:** every selectable profile shows the disclaimer *"Symbology subset — not certified against NATO APP-6 or MIL-STD-2525. Presentation only."*, `CertificationClaimed = false`, the W2-SYM-05 HOLD line and the DRG-232 pending line.

## 4. Formulas

`SIDC = "S" + identity{F,H,N,U} + dimension{S,U,Z} + status{P | X if destroyed} + functionId(6) + "-----"` (15 characters).

`frameId = "map-sym-mil--" + lower(affiliation) + "-" + {surface|subsurface|unknown} + ("--destroyed" if destroyed)`.

Glyph by frame family: full frame Friendly `▣`, Hostile `◆`, Neutral `■`, Unknown `✤`; lower-half frame Friendly `◡`, Hostile `▽`, Neutral `⊔`, Unknown `⌣`. Affiliations differ by shape, not colour (MIL-AC-04).

## 5. Edge cases

- Null or empty symbol list → empty result; null list → `ArgumentNullException`.
- `CivilianFriendly` passed to projection or legend → `ArgumentException` naming DRG-232.
- Toggling Legacy → MIL → Legacy returns rows equal to the original `MapPictureProjection` output.
- A U.S. platform is not assumed friendly; affiliation comes from the projected row only.

## 6. Dependencies

`MapPictureProjection` / `MapPictureBridge` (inputs, unchanged), `App6Sidc` (Legacy path, unchanged). No dependency from `ProjectAegis.Sim`, `Delegation/Replay`, `Delegation/Decision`, `Delegation/Orchestration`, `BalticReplayHarness` or `DelegationBridge` onto symbology types (source-scan test).

## 7. Tuning knobs

Key table, glyph tables and icon text are code constants in `SymbolKeyRegistry` / `MilitarySymbology`. Moving them to a versioned data manifest is S126-09 W2-SYM-04 (Should), not delivered here.

## 8. Acceptance criteria mapping (DRG-231)

| Criterion | S126 status | Evidence |
| --- | --- | --- |
| MIL-AC-01 named profile + legend with editions/deviations | Headless model done; UI binding not done | `SymbologyLegendProjection`, legend tests |
| MIL-AC-02 versioned mapping with fallback | Partial: code registry v1, not a data manifest | registry tests |
| MIL-AC-03 naval atlas (carrier…auxiliary) | **Not met** — thin slice of 3 naval keys only; full atlas is SYM-04 (out of sprint) | — |
| MIL-AC-04 shape-distinct affiliations, observer affiliation | Done for frozen 4 identities | distinctness + degrade tests |
| MIL-AC-05 readability at zoom/UI scale | Pending Unity review | — |
| MIL-AC-06 selection/camera/time/orders preserved across switch | Orders, decision log and map identity preserved headless; camera/selection and CIV half pending | hash-safety tests |
| MIL-AC-07 headless tests + Unity atlas review | Headless done; Unity review pending | evidence pack |
