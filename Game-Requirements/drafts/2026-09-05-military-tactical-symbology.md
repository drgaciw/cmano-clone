# SYM-MIL-01 — Military Tactical Symbology

**Delivery:** [DRG-231](https://linear.app/drgamtd-workspace/issue/DRG-231). Paired requirement: [SYM-CIV-01](2026-09-05-civilian-gameplay-symbology.md). Notion/Linear links verified 2026-09-05; Linear owns current delivery state.

**Design record:** [Notion requirement](https://app.notion.com/p/3d2f7cb4e4df81bc8028f32673f31e32). This local draft is uncommitted; it is not claimed to be on main.

## Requirement — SYM-MIL-01
Project Aegis shall provide a selectable Military Tactical symbology profile for U.S. Navy surface ships and the other supported unit/contact domains. The profile shall use documented NTDS naval display conventions and an explicitly versioned mapping to NATO APP-6 and U.S. MIL-STD-2525, informed by the CMANO/CMO tactical-display reference experience.

## Standards and reference interpretation
CMO's official UI addendum names its military symbol option NTDS + NATO APP-6, alongside a Stylized option. MIL-STD-2525 is the U.S. joint military symbology standard; APP-6 is the NATO standard family. These names must not be treated as interchangeable or as evidence that CMO implements every provision of MIL-STD-2525.

The supplied NTIS link identifies the 2007 Common Navy Warfighting Display Symbology Implementation Guide (ADA484484), not the standard itself. Its abstract describes maritime implementation of MIL-STD-2525 and additional Navy modifiers from NTDS/SSDS. Use this as supporting naval guidance; document deviations from the selected standard.

DLA lists MIL-STD-2525 Revision E Change 1, dated 2025-03-02. Proposed implementation baseline: that revision for the U.S. mapping; the exact APP-6 edition and NTDS subset must be pinned in the mapping specification before artwork implementation. This is a documented subset requirement, not a claim of formal standards certification.

## Acceptance criteria
- MIL-AC-01: Map options expose a clearly named Military Tactical profile and an in-game legend identifying its selected standards editions and any custom deviations.
- MIL-AC-02: A versioned symbol mapping manifest records semantic key, domain, affiliation, known platform category, frame/icon/modifiers, source edition or guide section, and fallback for every supported symbol. Missing entries use a documented generic unknown symbol.
- MIL-AC-03: The naval test atlas covers at least carrier, destroyer, cruiser, frigate, amphibious, support/auxiliary, and unknown surface vessel categories when known to the observer. Any category unsupported by the chosen standard has an explicit generic fallback; symbols must not invent class-level identification.
- MIL-AC-04: Friendly, hostile, neutral, and unknown tracks are distinguishable by shape/frame and text/legend, not color alone. A U.S. Navy platform's nationality does not automatically make it friendly; displayed affiliation follows the observer's projected knowledge.
- MIL-AC-05: Domain, affiliation, heading when known, selection, and observed contact quality/status remain readable at supported zoom/UI scales. Selection and command/outcome overlays must not overwrite or contradict symbol identity. Unobserved damage and platform type stay undisclosed.
- MIL-AC-06: Map, OOB, and detail views identify the same selected entity, including contact-to-unit selection transitions. Switching to/from the civilian profile preserves selection, camera position, simulation time, and active orders.
- MIL-AC-07: Headless mapping/unknown-fallback tests plus a Unity visual atlas review prove conformance to the declared subset and document any deviations. Asset provenance is retained; CMANO/CMO are behavior references, not a source of copied proprietary artwork.

## Sources
- [CMO official UI addendum — Map Symbols](https://command.matrixgames.com/?page_id=2697)
- [DLA official MIL-STD-2525 record](https://quicksearch.dla.mil/qsDocDetails.aspx?ident_number=114934)
- [NTIS / Naval Surface Warfare Center naval symbology guide, ADA484484](https://ntrl.ntis.gov/NTRL/dashboard/searchResults/titleDetail/ADA484484.xhtml)
- [User-supplied NTDS discussion — contextual only, not a normative standard](https://forum.finescale.com/t/navy-command-symbols/132729)

## Boundaries and delivery
Status: Draft requirement, recorded 2026-09-05 at the user's request. Implementation is not started or scheduled. This addition does not mark existing map symbology complete or reopen a release milestone.

Related requirements: REQ-20 / CMD-06 (map picture), CMD-07 (selection sync), CMD-12 (accessibility), CMD-13 (APP-6/LOD), CMD-28 (map options), CMD-29 (contact knowledge), and CMD-30 (tactical overlays).

Architecture: presentation reads existing observer-side projections and preserves ADR-010 §§2–3, ADR-007, and ADR-001. Changing a symbol profile must not change simulation state, orders, ROE, ownership, visibility, contact knowledge, or replay hashes. Missing or uncertain information must remain missing or uncertain.

Verification: headless mapping/semantic tests first, then Unity Game view UAT and retained screenshots. Verify selection and contact provenance in both profiles, including unknown, stale/lost, and degraded-comms tracks.
