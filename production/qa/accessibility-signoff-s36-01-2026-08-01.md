# Accessibility Sign-off — S36-01 — 2026-08-01

**Story:** `production/epics/sprint-36-ux-foundation/story-036-01-accessibility-signoff.md`
**Reviewer:** accessibility-specialist (lean doc review per S36-01)
**Scope:** Doc-only lean review and sign-off of `design/accessibility-requirements.md`. No code, no C#, no Unity USS edits.
**Related sources read:** `design/art/art-bible.md`, `design/ux/interaction-patterns.md`, `design/ux/c2-command-post.md`, `design/ux/c2-map-placeholder.md`, `production/qa/c2-manual-signoff-2026-06-02.md`, `docs/architecture/adr-010-headless-first-command-driven-ui.md` (Decision + Compliance sections)

---

## Verdict

**APPROVED** (one CONCERNS item found during review, resolved in this pass — see Findings).

---

## Findings

| Finding | WCAG Criterion / Basis | Severity | Recommendation / Resolution |
|---------|------------------------|----------|------------------------------|
| Broken cross-link anchor: `design/accessibility-requirements.md` §7 Platform Editor pointed to `ux/interaction-patterns.md#platform-import-staging`, but the actual heading is `## 7. Platform Import Staging (P-PE-01)` (slug `#7-platform-import-staging-p-pe-01`) | Doc-integrity gate (story AC "no broken anchors"); not a WCAG SC itself, but blocks the story's cross-link validation requirement | BLOCKING (for this story's AC) | **RESOLVED** — corrected the anchor and link text in `design/accessibility-requirements.md` line 204 to point at the correct slug. |
| No other broken anchors found | — | — | 16 of 17 anchored cross-links in the doc were verified to resolve correctly against actual headings in `art-bible.md`, `c2-command-post.md`, and `c2-map-placeholder.md` (GitHub-style slug computation checked by hand for em-dash/parenthesis/ampersand-bearing headings, e.g. `#semantic--selection--focus`, `#semantic--platform-import-staging-diff-target-uss--phase-e`, `#comms-degradation-p1--implemented`). |
| Contrast tables (text, semantic/affiliation, staging diff, focus) all cite hex values that match `art-bible.md` §3 canonical tokens exactly | SC 1.4.3 Contrast (Minimum), SC 1.4.11 Non-text Contrast | INFO | No action — internally consistent, no drift between docs. |
| Non-color cues documented for affiliation, comms state, selection, and staging diff (shape/text/opacity-step pairing) | SC 1.4.1 Use of Color | INFO | No action — every color-bearing state in §5 has a documented non-color secondary cue. |
| Keyboard focus order documented for C2 (top bar → drawer → log → right panel, read-only) and Platform Editor (path field → actions → diff list → acknowledge → approve) | SC 2.4.3 Focus Order, SC 2.1.1 Keyboard | INFO | No action — map symbol keyboard picking correctly flagged as a P2 stub with OOB row selection as the accessible interim path (§6.1); this is documented, not silently missing. |
| Reduced-motion table (§4) covers all interactive/decorative motion sources identified in `art-bible.md` §2 and §7 (mode transitions, ghost drift, PAUSE state, diff refresh, approve gate) | SC 2.3.3 Animation from Interactions (AAA, cited here as project practice, not required at AA) | INFO | No action — table is exhaustive against the motion sources actually described in art-bible §2/§7. |
| Input remapping (§6.3) is explicitly documented as a stub only, correctly scoped out of Polish Phase 1 implementation per the story's Out of Scope boundary | SC 2.1.1 Keyboard (deferred implementation, not deferred documentation) | ADVISORY | No action required this story — stub table present and consistent with `interaction-patterns.md` hotkeys (`Space`, `1–4`, `Esc`, `F`, `Ctrl+1/2/3`). |

---

## Cross-Link Integrity Check (detail)

All anchored links in `design/accessibility-requirements.md` verified against actual target headings:

| Link (post-fix) | Target file | Target heading | Result |
|---|---|---|---|
| `#colorblind-safety` (×2) | art-bible.md | `### Colorblind safety` | Resolves |
| `#3-color-palette` | art-bible.md | `## 3. Color Palette` | Resolves |
| `#semantic--platform-import-staging-diff-target-uss--phase-e` | art-bible.md | `### Semantic — Platform Import staging diff (target USS — Phase E+)` | Resolves |
| `#semantic--selection--focus` | art-bible.md | `### Semantic — selection & focus` | Resolves |
| `#hierarchy-rules` | art-bible.md | `### Hierarchy rules` | Resolves |
| `#supporting-principles` | art-bible.md | `### Supporting principles` | Resolves |
| `#platform-catalog-vs-import-panels` | art-bible.md | `### Platform Catalog vs Import panels` | Resolves |
| `#iconography` | art-bible.md | `### Iconography` | Resolves |
| `#9-accessibility-screen-level` | c2-command-post.md | `## 9. Accessibility (screen-level)` | Resolves |
| `#6-interaction-map-mvp` (×2) | c2-command-post.md | `## 6. Interaction Map (MVP)` | Resolves |
| `#acceptance` | c2-map-placeholder.md | `## Acceptance` | Resolves |
| `#7-platform-import-staging-p-pe-01` | interaction-patterns.md | `## 7. Platform Import Staging (P-PE-01)` | **Resolves (fixed this pass — was `#platform-import-staging`)** |

Reverse direction spot-checked: `interaction-patterns.md` and `art-bible.md` both link back into `accessibility-requirements.md#4-reduced-motion` and `#24-non-text-ui-focus-selection` — both resolve against actual headings `## 4. Reduced Motion` and `### 2.4 Non-text UI (focus, selection)`.

---

## Standard Tier Surface Coverage Check

Story AC requires C2 surfaces (drawer, OOB, topbar, staging diff) + Platform Editor hosts to be listed as Standard tier in §1.

| Named surface | Coverage in §1 tier table | Basis |
|---|---|---|
| Drawer | `C2LeftDrawerPanelHost` — Standard | Direct |
| OOB | Covered via `C2LeftDrawerPanelHost` (OOB is a tab within the drawer host) | `interaction-patterns.md` P-C2-01, `c2-command-post.md` §4.2 |
| Topbar | `C2TopBarHost` — Standard | Direct |
| Staging diff | Covered via `PlatformImportPanelHost` (diff list is the hero content of the Import host) | `interaction-patterns.md` §8 (P-PE-02), art-bible.md §6 Platform Catalog vs Import panels |
| Platform Editor hosts | `PlatformCatalogViewerHost`, `PlatformImportPanelHost` — Standard | Direct |

All required surfaces confirmed Standard tier, either directly named or as a documented sub-component of a named host.

---

## ADR-010 Alignment Check

Read ADR-010 §"Decision" (points 1–3) and §"Compliance". The Standard-tier hosts named in `accessibility-requirements.md` §1 (`C2LeftDrawerPanelHost`, `MapPanelHost`, `UnitDetailPanelHost`, `MessageLogHost`, `C2TopBarHost`, `PlatformCatalogViewerHost`, `PlatformImportPanelHost`) are exactly the class of "Unity Presentation layer... decoupled client" surfaces ADR-010 describes — they render read-only projections and hold only presentation-only state (selection, tabs, filters). No accessibility requirement in this doc asks any UI host to become authoritative over sim/scenario/catalog state, so there is no conflict with ADR-010's headless-first model.

---

## C2 Manual Sign-off Cross-Check

Cross-checked against `production/qa/c2-manual-signoff-2026-06-02.md` (checks 1–18, PASS WITH NOTES 18/18 @ `8de98b1`). Every host granted Standard tier in the accessibility doc corresponds to a surface already exercised in that sign-off (checks 2–13 for C2 drawer/OOB/map/topbar/log; checks 14–18 for Platform Editor import staging/catalog/doctrine/begin-execution). No accessibility tier commitment is being made for an untested or unimplemented surface.

---

## Doc Changes Made This Pass

| File | Change |
|------|--------|
| `design/accessibility-requirements.md` (line 204) | Fixed broken anchor: `[interaction-patterns.md §Platform Import](ux/interaction-patterns.md#platform-import-staging)` → `[interaction-patterns.md §7 Platform Import Staging](ux/interaction-patterns.md#7-platform-import-staging-p-pe-01)` |

No other files were modified. No code, no C#, no USS changes (out of scope per story).

---

## Gate Trace

Closes S36-01 acceptance criteria in full. This sign-off is additive to the existing `## Lean Review (2026-06-19)` section already committed in `design/accessibility-requirements.md` (S35-03 gate r2 residual #2) — that prior APPROVED WITH NOTES verdict stands; this pass adds a fresh cross-link integrity pass and closes the one anchor defect found.
