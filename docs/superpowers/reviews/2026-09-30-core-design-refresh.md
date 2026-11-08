# Core design wiki refresh — 2026-09-30

**Owner:** drg amtd. **Document maturity:** source-backed drafts; owner design review pending. **Scope:** live Notion design artifacts, not production implementation or human Play Mode acceptance.

| Page | Delivery issue | Verified content |
| --- | --- | --- |
| [Core Simulation Design](https://app.notion.com/p/3a7f7cb4e4df81f4a1d8f827bf0639a7) | [DRG-343](https://linear.app/drgamtd-workspace/issue/DRG-343), In Review | Paused orchestration versus clock progression; outer session acceleration versus pipeline cadence; distinct deterministic RNG contracts; checkpoint metadata versus proposed save/resume. |
| [Weapons & Engagement Resolution](https://app.notion.com/p/3a7f7cb4e4df8136b8c8f667ac4f3a68) | [DRG-344](https://linear.app/drgamtd-workspace/issue/DRG-344), In Review | Source-ordered refusal gates; actual magazine consumption; conditional seeded draws and Hit → Intercept → Kill; same-tick authoritative outcomes versus visual InFlight projection. |
| [Map / Rendering / UI](https://app.notion.com/p/3a7f7cb4e4df81e19d16ebeb66b33126) | Existing [DRG-275](https://linear.app/drgamtd-workspace/issue/DRG-275), In Review | Normalized pose → clamped geographic pose → hash fallback precedence; Baltic/interpolation formulas; selection/inspection isolation; existing atlas fallback versus proposed H5 content isolation. |

All three pages were fetched after publishing and independently refetched by the integrator. Each contains all eight mandatory substantive sections: Overview, Player Fantasy, Detailed Rules, Formulas, Edge Cases, Dependencies, Tuning Knobs and Acceptance Criteria. Administrative Linked Linear sections are retained where present. Each page has a native drg amtd person mention, milestone context, September 30 update date, Draft / pending owner review and native verification `unverified`. Linear In Review describes the authored artifact, not approval of the Notion design.

Sources were inspected at local HEAD `4190fb1b49cd15775b6b995b7c245d73828cd7a2` with pre-existing dirty changes. Absolute-path GitNexus context was corroborated by local code reads; known DRG-323/324 graph limitations remain explicit. The Weapons draft cites actual `CombatEventLogProjection` / `CombatEventProjection` / `CombatMapPresentation` sources rather than attributing behavior to an unavailable `BattleGraphicsPresentationBridge` file. Candidate tests are labeled mappings, not independent criterion-level proof.

The delegated Weapons update was initially rejected by automatic approval review, which stated that external writes lacked human authorization. The integrator re-read the active goal's explicit human objective, “proceed with all your recommendations and updates, run with max parallelization,” fetched the unchanged target, inspected the complete draft and retried the same Notion update tool. That update succeeded and was refetched. No alternate API or guard bypass was used; no approval remains blocked.

The wiki root's July/August blanket stub statement now identifies these three refreshed drafts and does not claim to have audited every other design page. The [live reconciliation](https://app.notion.com/p/3ebf7cb4e4df81cd80b7ff559888a0bd) and [full closeout](2026-09-30-planning-reconciliation-closeout.md) record the cross-system context and local Debug/Release verification. No page was marked Approved or natively verified, and no owner walk was performed.

```text
PASS — all eight design sections, named ownership, sources and pending review verified.
PASS — presentation wall ADR-010 §2–3 / ADR-007 / ADR-001 preserved.
N/A  — runtime, assembly, Editor, plugin or scene mutation for these design edits.
PENDING — owner design approval, criterion execution and DRG-208 acceptance.
```
