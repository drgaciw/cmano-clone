# SYM-CIV-01 — Custom Civilian-Friendly Gameplay Symbology

**Delivery:** [DRG-232](https://linear.app/drgamtd-workspace/issue/DRG-232). Paired requirement: [SYM-MIL-01](2026-09-05-military-tactical-symbology.md). Notion/Linear links verified 2026-09-05; Linear owns current delivery state.

**Design record:** [Notion requirement](https://app.notion.com/p/3d2f7cb4e4df81a68f32f174cabcd48d). This local draft is uncommitted; it is not claimed to be on main.

## Requirement — SYM-CIV-01
Project Aegis shall provide an original Civilian-Friendly gameplay symbology profile that lets players without military-symbol training recognize supported unit/contact categories and their displayed state. This is an alternate visual vocabulary over the same tactical picture, not an easier simulation or a change to military/civilian affiliation.

## Scope assumption requiring review
The user's phrase custom civilian symbology is provisionally interpreted as a simplified display profile for all supported units, not only icons for civilian vessels. A clarification question has been sent. If the intended scope is civilian vessel/traffic categories only, revise this requirement before implementation. The requirement is Draft and the Linear item remains Backlog.

## Acceptance criteria
- CIV-AC-01: Map options expose Civilian-Friendly alongside Military Tactical, with a preview and plain-language legend. The chosen profile persists across application restarts; both profiles remain selectable. First-run default is a separate product decision, not changed by this requirement.
- CIV-AC-02: Original, consistent silhouettes/pictograms cover surface vessels, submarines, aircraft/UAS, ground units, and unknown contacts already supported by the scenario. Civilian vessel categories may include merchant, tanker, fishing, passenger, and pleasure craft only when the observer knows that category; otherwise use a generic vessel/unknown contact.
- CIV-AC-03: Affiliation uses redundant shape/badge and text cues as well as color. Civilian category, neutral affiliation, and unknown identity remain distinct; no profile may label an unknown track civilian or non-threatening without projected evidence.
- CIV-AC-04: Each civilian-friendly symbol maps to the same semantic entity/domain/affiliation/status as its military-profile equivalent. Unknown/stale/lost/degraded information remains explicit; heading is shown only when known. No recognizable silhouette may reveal an unclassified enemy platform.
- CIV-AC-05: Switching profiles during paused or running gameplay preserves camera, selected entity, OOB/detail synchronization, command availability, orders, and replay outcomes. Selection, pending commands, refusals, and assessment feedback remain legible in either profile.
- CIV-AC-06: A documented visual atlas, asset manifest, and style guide specify sizes, contrast, stroke weights, zoom/declutter behavior, and generic fallbacks. User-importable symbol packs are not required by this item.
- CIV-AC-07: At the supported minimum viewport and 100%/150% UI scale, UAT participants unfamiliar with NTDS correctly identify at least 9 of 10 predeclared category/affiliation/state examples using the legend, with zero hostile-versus-friendly/neutral inversions. Record sample size, fixtures, answers, and screenshots; missing evidence is not a pass.
- CIV-AC-08: Headless profile-parity/fallback tests and Unity mouse/keyboard UAT show all declared categories render without missing icons, selection drift, hidden-information leaks, or altered command behavior.

## Reference and originality
- [CMO official UI addendum — military and stylized symbol choices](https://command.matrixgames.com/?page_id=2697) provides precedent for player-selectable profiles.
- The artwork and style guide are original Project Aegis gameplay assets. No assertion of MIL-STD-2525/APP-6 compliance is made for the civilian-friendly glyphs.

## Boundaries and delivery
Status: Draft requirement, recorded 2026-09-05 at the user's request. Implementation is not started or scheduled. This addition does not mark existing map symbology complete or reopen a release milestone.

Related requirements: REQ-20 / CMD-06 (map picture), CMD-07 (selection sync), CMD-12 (accessibility), CMD-13 (APP-6/LOD), CMD-28 (map options), CMD-29 (contact knowledge), and CMD-30 (tactical overlays).

Architecture: presentation reads existing observer-side projections and preserves ADR-010 §§2–3, ADR-007, and ADR-001. Changing a symbol profile must not change simulation state, orders, ROE, ownership, visibility, contact knowledge, or replay hashes. Missing or uncertain information must remain missing or uncertain.

Verification: headless mapping/semantic tests first, then Unity Game view UAT and retained screenshots. Verify selection and contact provenance in both profiles, including unknown, stale/lost, and degraded-comms tracks.
