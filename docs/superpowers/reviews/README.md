# UX preview mocks

Static HTML stand-ins for Unity UI Toolkit panels, used to critique layout and information
design without an Editor session.

**These are hand-authored. There is no generator.** No script, CI job, or skill writes to this
directory — `gitnexus wiki` emits `wiki/` plus `AGENTS.md`/`CLAUDE.md` and never touches it.

| File | Stands in for | Scope |
|------|---------------|-------|
| [`scenario-editor-uiux-preview.html`](scenario-editor-uiux-preview.html) | `ScenarioMapAuthoringWindow` + authoring presenters | Scenario Editor P2.1 / P2.2 |
| [`platform-editor-uiux-preview.html`](platform-editor-uiux-preview.html) | `PlatformEditorShellHost` + catalog / import panels | PE-UX W0–W2 |

## The rule

**Render structure from the shipped UXML, not from memory.**

Open the panel's `.uxml` and mirror its element tree before drawing anything. A mock that invents
structure generates review findings against a UI that does not exist, and the cost lands on
whoever reconciles them later.

This is not hypothetical. A UX review of a Platform Editor mock produced 21 findings; roughly half
were invalid — either already delivered by the approved
[PE-UX productization plan](../plans/2026-07-23-platform-editor-uiux-productization.md), or aimed
at structure the mock had invented.

### Worked example — the tab that does not exist

`unity/ProjectAegis/Assets/UI/PlatformEditor/PlatformEditorShell.uxml` declares a mode title and
**two** tabs:

```xml
<ui:Label name="platform-editor-shell-mode-title" text="BROWSE CATALOG" … />
…
<ui:VisualElement name="platform-editor-shell-tabs" focusable="true">
  <ui:Button name="platform-editor-shell-tab-catalog" text="Catalog" class="…--active" />
  <ui:Button name="platform-editor-shell-tab-import"  text="Import" />
</ui:VisualElement>
```

`PlatformEditorShellProjection` drives the title from the active tab:

```csharp
ModeTitle: isCatalog ? "BROWSE CATALOG" : "IMPORT STAGING"
```

A later mock rendered these as **three tabs** — `CATALOG | IMPORT | BROWSE CATALOG` — and styled
the mode title as the active one. That inverts the relationship: `BROWSE CATALOG` is the
*consequence* of the Catalog tab being active, not a sibling of it.

**Correct:** two tab buttons, plus one separate title label above them.

## Provenance lines must be true

Both current mocks carry a line resembling *"Generated from GitNexus query on cmano-clone."*
No such generation step exists. Either drop the claim or name what actually produced the file — a
false provenance line sends the next reader hunting for a generator that was never written.

## Trace chrome is document furniture, not proposed UI

The TRACE rows, symbol lists and pipeline footers describe *this document's* derivation. They are
not proposals for the product's chrome, and they should never be carried into a panel.

Reviewers have misread them as proposed interface copy more than once. If that keeps happening,
move the trace block into an HTML comment or a collapsed `<details>` element rather than styling
it as page header.

## Status indicators must reflect real behaviour

A mock may show a control that does not exist yet — that is the point of a mock. It must not show
a **status** that misreports the system.

A Scenario Editor mock displayed `VALIDATION ENGINE · DEBOUNCE 300MS`. `LiveFindingsPresenter`
stores `debounceMs` and never honours it; `ScheduleRefresh()` unconditionally calls
`RefreshImmediate()`, and the field is documented as reserved. The chip asserted a behaviour the
code does not have, which is worse than omitting it — someone debugging validation cost will
trust it.

## Before committing a new mock

- [ ] Every panel's element tree traced from its shipped `.uxml`, not recalled
- [ ] Tab counts, labels and active states match the UXML and its projection
- [ ] Any status text corresponds to behaviour that actually exists in code
- [ ] Provenance line names what really produced the file
- [ ] Header states which wave / scope the mock represents, so reviewers can date it
- [ ] Existing plan and gate docs under `../plans/` and `production/qa/` checked — a mock that
      contradicts an approved plan should say so deliberately, not accidentally

## Reviewing one of these

Ground findings against shipped code before filing them. Search the repo for the subsystem name
first — plans, gates, epics and ADRs frequently already answer the question. The reviews produced
from these mocks live in Notion under the **cmano-clone** design wiki, in a *Reviews* section that
is explicitly exempt from the 8-section design-page template.
