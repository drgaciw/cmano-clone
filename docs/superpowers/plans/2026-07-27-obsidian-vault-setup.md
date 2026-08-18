# Obsidian Vault Setup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold a PARA-organized Obsidian vault structure for cmano-clone (folders, templates, nested-tag taxonomy, MOC, plugin recommendations) via mcpvault MCP tools, migrating the two existing test artifacts to the new conventions. No project data is imported.

**Architecture:** Every task is a sequence of `mcpvault` MCP tool calls (`write_note`, `read_note`, `get_frontmatter`, `delete_note`, `list_directory`) against the vault at `/home/username01/Documents/Obsidian Vault`. There is no application code and no test framework — each task's "test" is a verification read (`list_directory` / `get_frontmatter` / `read_note`) confirming the vault state matches the spec. There are no git commits: the vault is a separate directory outside the `cmano-clone` git repo, so nothing here touches repo version control.

**Tech Stack:** `mcpvault` MCP server (bitbonsai/mcpvault) tools; Obsidian markdown + YAML frontmatter conventions; Dataview query-block syntax (inert until the user installs the plugin — not installed by this plan).

## Global Constraints

- Vault root is `/home/username01/Documents/Obsidian Vault` (fixed — configured in `~/.claude.json`'s `mcpvault` server args, not something this plan changes).
- Naming convention: **Title Case with spaces** for every note filename (e.g. `ADR-011 Platform Editor Excel Roundtrip.md`).
- Tag taxonomy: **nested tags only** — `project/<name>`, `type/<doctype>`, `status/<status>`, `topic/<topic>`. No flat tags in any newly written or migrated note.
- No project data (real ADRs/GDDs/requirements/production docs) is imported. The only content migrated is the pre-existing `ADR-011` test mirror and `README.md` from the prior session — not new data.
- `mcpvault` cannot write `.obsidian/` — no task installs or enables a plugin. Plugin guidance is written as a doc for the user to act on manually.
- Every mirrored/created note carries frontmatter appropriate to its type: `tags` always; `project`, `source`, `status`, `date` where applicable (per the spec's per-note-type schema).

---

### Task 1: PARA root — Areas / Resources / Archives placeholders

**Files:**
- Create: `Areas/README.md`
- Create: `Resources/README.md`
- Create: `Archives/README.md`

**Interfaces:**
- Consumes: nothing (first task, no dependencies)
- Produces: vault-root folders `Areas/`, `Resources/`, `Archives/` exist and are visible in `list_directory("/")` — later tasks don't depend on these, but the final verification task (Task 7) checks for them

- [ ] **Step 1: Write `Areas/README.md`**

Call `mcp__mcpvault__write_note`:
```json
{
  "path": "Areas/README.md",
  "frontmatter": {"tags": ["meta/para"]},
  "content": "# Areas\n\nOngoing responsibilities with no end date — not tied to a single project. Empty until a non-project area of responsibility needs a home here.\n"
}
```
Expected: `Successfully wrote note: Areas/README.md (mode: overwrite)`

- [ ] **Step 2: Write `Resources/README.md`**

Call `mcp__mcpvault__write_note`:
```json
{
  "path": "Resources/README.md",
  "frontmatter": {"tags": ["meta/para"]},
  "content": "# Resources\n\nReference material useful across projects, or material that outlives any single project. Not for project-specific working notes — those live under Projects/<name>/. Empty until something qualifies.\n"
}
```
Expected: `Successfully wrote note: Resources/README.md (mode: overwrite)`

- [ ] **Step 3: Write `Archives/README.md`**

Call `mcp__mcpvault__write_note`:
```json
{
  "path": "Archives/README.md",
  "frontmatter": {"tags": ["meta/para"]},
  "content": "# Archives\n\nCompleted or inactive projects, moved here from Projects/ once no longer active. Empty until a project retires.\n"
}
```
Expected: `Successfully wrote note: Archives/README.md (mode: overwrite)`

- [ ] **Step 4: Verify the three folders exist at vault root**

Call `mcp__mcpvault__list_directory` with `{"path": "/", "prettyPrint": true}`.
Expected: `dirs` includes `"Areas"`, `"Resources"`, `"Archives"` (alongside the pre-existing `"cmano-clone"` — that gets retired in Task 4).

---

### Task 2: cmano-clone subfolder scaffold (mirrors repo layout)

**Files:**
- Create: `Projects/cmano-clone/architecture/About This Folder.md`
- Create: `Projects/cmano-clone/gdd/About This Folder.md`
- Create: `Projects/cmano-clone/requirements/About This Folder.md`
- Create: `Projects/cmano-clone/production/About This Folder.md`
- Create: `Projects/cmano-clone/research/About This Folder.md`

**Interfaces:**
- Consumes: nothing (independent of Task 1)
- Produces: `Projects/cmano-clone/{architecture,gdd,requirements,production,research}/` folders exist; Task 3 writes into `architecture/`, Task 4 writes `cmano-clone.md` as a sibling of these folders, Task 6 writes into `research/`

- [ ] **Step 1: Write `Projects/cmano-clone/architecture/About This Folder.md`**

```json
{
  "path": "Projects/cmano-clone/architecture/About This Folder.md",
  "frontmatter": {"tags": ["project/cmano-clone"]},
  "content": "# Architecture\n\nMirrors `docs/architecture/` in the cmano-clone git repo — ADRs and architecture reviews. Source of truth stays in git; notes here are mirrored copies for graph/backlink browsing, written on request (not automatically synced). See [[cmano-clone]] for the project home note and mirror conventions.\n"
}
```
Expected: `Successfully wrote note: Projects/cmano-clone/architecture/About This Folder.md (mode: overwrite)`

- [ ] **Step 2: Write `Projects/cmano-clone/gdd/About This Folder.md`**

```json
{
  "path": "Projects/cmano-clone/gdd/About This Folder.md",
  "frontmatter": {"tags": ["project/cmano-clone"]},
  "content": "# GDD\n\nMirrors `design/gdd/` in the cmano-clone git repo — game design documents, one per mechanic. Source of truth stays in git. See [[cmano-clone]] for mirror conventions.\n"
}
```
Expected: `Successfully wrote note: Projects/cmano-clone/gdd/About This Folder.md (mode: overwrite)`

- [ ] **Step 3: Write `Projects/cmano-clone/requirements/About This Folder.md`**

```json
{
  "path": "Projects/cmano-clone/requirements/About This Folder.md",
  "frontmatter": {"tags": ["project/cmano-clone"]},
  "content": "# Requirements\n\nMirrors `Game-Requirements/requirements/` in the cmano-clone git repo. Source of truth stays in git. See [[cmano-clone]] for mirror conventions.\n"
}
```
Expected: `Successfully wrote note: Projects/cmano-clone/requirements/About This Folder.md (mode: overwrite)`

- [ ] **Step 4: Write `Projects/cmano-clone/production/About This Folder.md`**

```json
{
  "path": "Projects/cmano-clone/production/About This Folder.md",
  "frontmatter": {"tags": ["project/cmano-clone"]},
  "content": "# Production\n\nMirrors `production/` in the cmano-clone git repo — sprint and session notes. Source of truth stays in git. See [[cmano-clone]] for mirror conventions.\n"
}
```
Expected: `Successfully wrote note: Projects/cmano-clone/production/About This Folder.md (mode: overwrite)`

- [ ] **Step 5: Write `Projects/cmano-clone/research/About This Folder.md`**

```json
{
  "path": "Projects/cmano-clone/research/About This Folder.md",
  "frontmatter": {"tags": ["project/cmano-clone"]},
  "content": "# Research\n\nScratch research notes with no repo equivalent — not yet formalized into a git-tracked doc. Also home to the Obsidian plugin recommendations doc. See [[cmano-clone]] for mirror conventions.\n"
}
```
Expected: `Successfully wrote note: Projects/cmano-clone/research/About This Folder.md (mode: overwrite)`

- [ ] **Step 6: Verify all five subfolders exist**

Call `mcp__mcpvault__list_directory` with `{"path": "Projects/cmano-clone", "prettyPrint": true}`.
Expected: `dirs` includes `"architecture"`, `"gdd"`, `"requirements"`, `"production"`, `"research"`.

---

### Task 3: Migrate ADR-011 to `architecture/` with nested tags

**Files:**
- Read: `cmano-clone/adr/ADR-011-Platform-Editor-Excel-Roundtrip.md` (old location, flat tags)
- Create: `Projects/cmano-clone/architecture/ADR-011 Platform Editor Excel Roundtrip.md` (new location, nested tags)
- Delete: `cmano-clone/adr/ADR-011-Platform-Editor-Excel-Roundtrip.md`

**Interfaces:**
- Consumes: `Projects/cmano-clone/architecture/` folder (from Task 2)
- Produces: `Projects/cmano-clone/architecture/ADR-011 Platform Editor Excel Roundtrip.md` — Task 4's MOC links to this note by its title

- [ ] **Step 1: Read the old note to confirm current content before migrating**

Call `mcp__mcpvault__read_note` with `{"path": "cmano-clone/adr/ADR-011-Platform-Editor-Excel-Roundtrip.md"}`.
Expected: returns the full ADR-011 body (Status/Date/Decision Makers/.../Related sections) — confirms nothing has changed since it was written.

- [ ] **Step 2: Write the note to its new path with nested tags**

Call `mcp__mcpvault__write_note` with the same body content read in Step 1, and this frontmatter:
```json
{
  "path": "Projects/cmano-clone/architecture/ADR-011 Platform Editor Excel Roundtrip.md",
  "frontmatter": {
    "tags": ["project/cmano-clone", "type/adr", "topic/platform-editor", "topic/data-layer"],
    "project": "cmano-clone",
    "source": "docs/architecture/adr-011-platform-editor-excel-roundtrip.md",
    "status": "Accepted",
    "date": "2026-06-17"
  },
  "content": "<body from Step 1, unchanged>"
}
```
Expected: `Successfully wrote note: Projects/cmano-clone/architecture/ADR-011 Platform Editor Excel Roundtrip.md (mode: overwrite)`

- [ ] **Step 3: Verify the new note's frontmatter**

Call `mcp__mcpvault__get_frontmatter` with `{"path": "Projects/cmano-clone/architecture/ADR-011 Platform Editor Excel Roundtrip.md", "prettyPrint": true}`.
Expected:
```json
{
  "tags": ["project/cmano-clone", "type/adr", "topic/platform-editor", "topic/data-layer"],
  "project": "cmano-clone",
  "source": "docs/architecture/adr-011-platform-editor-excel-roundtrip.md",
  "status": "Accepted",
  "date": "2026-06-17"
}
```

- [ ] **Step 4: Delete the old note**

Call `mcp__mcpvault__delete_note` with `{"path": "cmano-clone/adr/ADR-011-Platform-Editor-Excel-Roundtrip.md"}`.
Expected: a success response referencing the deleted path, no error.

- [ ] **Step 5: Verify the old path is gone**

Call `mcp__mcpvault__list_directory` with `{"path": "cmano-clone", "prettyPrint": true}`.
Expected: `adr` no longer appears under `dirs` (or the directory is empty/absent) and `ADR-011-Platform-Editor-Excel-Roundtrip.md` does not appear under `files` anywhere in that tree.

---

### Task 4: Build the MOC home note, retire old `README.md`

**Files:**
- Create: `Projects/cmano-clone/cmano-clone.md`
- Delete: `cmano-clone/README.md`

**Interfaces:**
- Consumes: `Projects/cmano-clone/architecture/ADR-011 Platform Editor Excel Roundtrip.md` (from Task 3) — referenced as an example row the Dataview query will surface once the plugin is installed
- Produces: `Projects/cmano-clone/cmano-clone.md` — the project's entry point; nothing later depends on its exact content, but Task 7's verification reads it

- [ ] **Step 1: Write the MOC note**

Call `mcp__mcpvault__write_note`:
```json
{
  "path": "Projects/cmano-clone/cmano-clone.md",
  "frontmatter": {"tags": ["project/cmano-clone", "type/moc"]},
  "content": "# cmano-clone\n\nProject Aegis — Unity milsim project. This is the home note for everything mirrored from the `cmano-clone` git repo into this vault.\n\n## Folders\n\n- [[About This Folder|architecture/]] — mirrors `docs/architecture/` (ADRs, architecture reviews)\n- [[About This Folder|gdd/]] — mirrors `design/gdd/`\n- [[About This Folder|requirements/]] — mirrors `Game-Requirements/requirements/`\n- [[About This Folder|production/]] — mirrors `production/`\n- [[About This Folder|research/]] — scratch notes, no repo equivalent\n\n## Tag convention\n\n- `project/cmano-clone` — every note from this project\n- `type/adr`, `type/gdd`, `type/research`, `type/requirement`, `type/production` — doc-type tag\n- `status/<status>` — mirrors the source doc's status field where applicable\n- `topic/<topic>` — free-form topical tag(s), as many as apply\n\n## Frontmatter convention\n\nMirrored docs carry:\n\n```yaml\ntags: [project/cmano-clone, type/<doctype>, topic/<topic>]\nproject: cmano-clone\nsource: <path relative to repo root>\nstatus: <Accepted|Proposed|...>\ndate: <doc date>\n```\n\n`source` is the authoritative link back to the git-tracked file — always check there for the current version; this vault copy can drift. Mirroring is manual/on-request, not automatic.\n\n## ADRs\n\n```dataview\nTABLE status, date\nFROM \"Projects/cmano-clone/architecture\"\nWHERE contains(tags, \"type/adr\")\nSORT date DESC\n```\n\n## GDDs\n\n```dataview\nTABLE status, date\nFROM \"Projects/cmano-clone/gdd\"\nWHERE contains(tags, \"type/gdd\")\nSORT date DESC\n```\n\n## Research notes\n\n```dataview\nTABLE date\nFROM \"Projects/cmano-clone/research\"\nWHERE contains(tags, \"type/research\")\nSORT date DESC\n```\n\n*The `dataview` blocks above render as plain code until the Dataview community plugin is installed and enabled — see [[Obsidian Plugin Recommendations]]. Until then, browse via the folder links above.*\n"
}
```
Expected: `Successfully wrote note: Projects/cmano-clone/cmano-clone.md (mode: overwrite)`

- [ ] **Step 2: Verify the MOC content**

Call `mcp__mcpvault__read_note` with `{"path": "Projects/cmano-clone/cmano-clone.md"}`.
Expected: returns the content written in Step 1 verbatim.

- [ ] **Step 3: Delete the old `README.md`**

Call `mcp__mcpvault__delete_note` with `{"path": "cmano-clone/README.md"}`.
Expected: a success response referencing the deleted path, no error.

- [ ] **Step 4: Verify the old flat `cmano-clone/` folder is fully retired**

Call `mcp__mcpvault__list_directory` with `{"path": "/", "prettyPrint": true}`.
Expected: `dirs` no longer includes a top-level `"cmano-clone"` entry (it was only ever `README.md` + `adr/`, both now migrated/deleted); `dirs` instead includes `"Projects"` (containing `cmano-clone` one level deeper, per Task 1/2's PARA structure).

---

### Task 5: Templates

**Files:**
- Create: `Templates/Template - ADR.md`
- Create: `Templates/Template - GDD.md`
- Create: `Templates/Template - Research Note.md`

**Interfaces:**
- Consumes: nothing (independent of other tasks)
- Produces: three template notes under `Templates/`; no later task depends on these programmatically — they're for the user's manual/Templater-driven note creation going forward

- [ ] **Step 1: Write the ADR template**

```json
{
  "path": "Templates/Template - ADR.md",
  "frontmatter": {
    "tags": ["project/{{project}}", "type/adr", "status/proposed", "topic/{{topic}}"],
    "project": "{{project}}",
    "source": "{{source}}",
    "status": "Proposed",
    "date": "{{date}}"
  },
  "content": "<!-- Manual fields: replace {{number}}, {{title}}, {{project}}, {{source}}, {{topic}} by hand. Core Templates plugin only auto-fills {{date}}/{{title}}/{{time}} — upgrading to Templater lets the rest prompt automatically; see Templates/../Obsidian Plugin Recommendations.md -->\n\n# ADR-{{number}}: {{title}}\n\n## Status\n\nProposed\n\n## Date\n\n{{date}}\n\n## Decision Makers\n\n\n\n## Context\n\n\n\n## Decision\n\n\n\n## Consequences\n\n**Positive**\n\n\n**Negative / risks**\n\n\n## Compliance / Verification\n\n\n\n## Related\n\n"
}
```
Expected: `Successfully wrote note: Templates/Template - ADR.md (mode: overwrite)`

- [ ] **Step 2: Write the GDD template**

```json
{
  "path": "Templates/Template - GDD.md",
  "frontmatter": {
    "tags": ["project/{{project}}", "type/gdd", "topic/{{topic}}"],
    "project": "{{project}}",
    "source": "{{source}}",
    "date": "{{date}}"
  },
  "content": "<!-- Manual fields: replace {{title}}, {{project}}, {{source}}, {{topic}} by hand, or via Templater prompts once installed -->\n\n# {{title}}\n\n## Overview\n\n\n\n## Player Fantasy\n\n\n\n## Detailed Rules\n\n\n\n## Formulas\n\n\n\n## Edge Cases\n\n\n\n## Dependencies\n\n\n\n## Tuning Knobs\n\n\n\n## Acceptance Criteria\n\n"
}
```
Expected: `Successfully wrote note: Templates/Template - GDD.md (mode: overwrite)`

- [ ] **Step 3: Write the Research Note template**

```json
{
  "path": "Templates/Template - Research Note.md",
  "frontmatter": {
    "tags": ["project/{{project}}", "type/research", "topic/{{topic}}"],
    "project": "{{project}}",
    "date": "{{date}}"
  },
  "content": "<!-- Manual fields: replace {{title}}, {{project}}, {{topic}} by hand, or via Templater prompts once installed -->\n\n# {{title}}\n\n## Topic\n\n\n\n## Source links\n\n\n\n## Findings\n\n\n\n## Open questions\n\n"
}
```
Expected: `Successfully wrote note: Templates/Template - Research Note.md (mode: overwrite)`

- [ ] **Step 4: Verify all three templates exist**

Call `mcp__mcpvault__list_directory` with `{"path": "Templates", "prettyPrint": true}`.
Expected: `files` includes `"Template - ADR.md"`, `"Template - GDD.md"`, `"Template - Research Note.md"`.

---

### Task 6: Plugin recommendations doc

**Files:**
- Create: `Projects/cmano-clone/research/Obsidian Plugin Recommendations.md`

**Interfaces:**
- Consumes: `Projects/cmano-clone/research/` folder (from Task 2)
- Produces: `Obsidian Plugin Recommendations.md` — linked from the MOC (Task 4, Step 1) as `[[Obsidian Plugin Recommendations]]`

- [ ] **Step 1: Write the plugin recommendations doc**

```json
{
  "path": "Projects/cmano-clone/research/Obsidian Plugin Recommendations.md",
  "frontmatter": {"tags": ["project/cmano-clone", "type/research", "topic/tooling"]},
  "content": "# Obsidian Plugin Recommendations\n\nWritten guidance only — Claude cannot install or enable plugins (mcpvault has no access to `.obsidian/`). Enable these yourself in Settings.\n\n## Templates (core, built-in)\n\nEnable first, regardless of anything else below: **Settings → Core plugins → Templates → on**. Set its template folder location to `Templates/`. This alone lets you insert `Template - ADR.md` / `Template - GDD.md` / `Template - Research Note.md` via the command palette (`Insert template`).\n\n## Dataview (community)\n\n**Settings → Community plugins → Browse → \"Dataview\" → Install → Enable.** Powers the live query tables on the [[cmano-clone]] MOC (`TABLE ... FROM ... WHERE contains(tags, ...)`). Without it those blocks just render as inert code — install this to make the MOC dashboard live.\n\n## Templater (community)\n\n**Settings → Community plugins → Browse → \"Templater\" → Install → Enable.** Upgrades the `{{title}}`/`{{project}}`/`{{topic}}`/`{{source}}`/`{{number}}` placeholders in the three templates from manual fill-in-the-blank text into auto-filled or prompted values. Point its template folder at `Templates/` as well (same folder core Templates uses).\n\n## Tag Wrangler (community)\n\n**Settings → Community plugins → Browse → \"Tag Wrangler\" → Install → Enable.** Lets you rename or merge nested tags (e.g. `type/adr` → `type/architecture-decision`) across every note at once from the tag pane, instead of hand-editing frontmatter in each note. Becomes valuable once more notes accumulate under the `project/`, `type/`, `status/`, `topic/` taxonomy.\n"
}
```
Expected: `Successfully wrote note: Projects/cmano-clone/research/Obsidian Plugin Recommendations.md (mode: overwrite)`

- [ ] **Step 2: Verify frontmatter**

Call `mcp__mcpvault__get_frontmatter` with `{"path": "Projects/cmano-clone/research/Obsidian Plugin Recommendations.md", "prettyPrint": true}`.
Expected:
```json
{"tags": ["project/cmano-clone", "type/research", "topic/tooling"]}
```

---

### Task 7: Final verification pass

**Files:** none created — read-only confirmation of Tasks 1–6.

**Interfaces:**
- Consumes: the full vault state produced by Tasks 1–6
- Produces: a pass/fail confirmation that the vault matches the spec; nothing downstream depends on this task

- [ ] **Step 1: Vault-root shape**

Call `mcp__mcpvault__list_directory` with `{"path": "/", "prettyPrint": true}`.
Expected: `dirs` contains exactly `"Areas"`, `"Resources"`, `"Archives"`, `"Projects"`, `"Templates"` (plus any that already existed before this plan, e.g. the stray `Untitled.base` file is harmless and unrelated); **no** top-level `"cmano-clone"` directory remains.

- [ ] **Step 2: cmano-clone project shape**

Call `mcp__mcpvault__list_directory` with `{"path": "Projects/cmano-clone", "prettyPrint": true}`.
Expected: `dirs` contains `"architecture"`, `"gdd"`, `"requirements"`, `"production"`, `"research"`; `files` contains `"cmano-clone.md"`.

- [ ] **Step 3: Migrated ADR-011 in place with correct tags**

Call `mcp__mcpvault__list_directory` with `{"path": "Projects/cmano-clone/architecture", "prettyPrint": true}`, then `mcp__mcpvault__get_frontmatter` with `{"path": "Projects/cmano-clone/architecture/ADR-011 Platform Editor Excel Roundtrip.md", "prettyPrint": true}`.
Expected: the file is listed; frontmatter tags are `["project/cmano-clone", "type/adr", "topic/platform-editor", "topic/data-layer"]` (nested, not the old flat `["cmano-clone", "adr", "platform-editor", "data-layer"]`).

- [ ] **Step 4: Templates present**

Call `mcp__mcpvault__list_directory` with `{"path": "Templates", "prettyPrint": true}`.
Expected: `files` contains all three `Template - *.md` files from Task 5.

- [ ] **Step 5: Plugin recommendations doc readable**

Call `mcp__mcpvault__read_note` with `{"path": "Projects/cmano-clone/research/Obsidian Plugin Recommendations.md"}`.
Expected: returns the content written in Task 6, Step 1.

- [ ] **Step 6: Report results to the user**

Summarize which of Steps 1–5 passed/failed. If all passed, the vault setup is complete and matches `docs/superpowers/specs/2026-07-27-obsidian-vault-setup-design.md`.
