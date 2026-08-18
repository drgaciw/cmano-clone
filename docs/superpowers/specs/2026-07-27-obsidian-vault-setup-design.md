# Obsidian Vault Setup — Design Spec

**Date:** 2026-07-27
**Status:** Approved (pending spec review)
**Author:** Claude (via brainstorming skill), decisions by user

## Overview

This spec defines the general-purpose Obsidian vault configuration for the cmano-clone
project, built via the `mcpvault` MCP server (bitbonsai/mcpvault). It is **scaffolding
only** — no project documents (ADRs, GDDs, requirements, production notes) are imported
as part of this work, aside from reconciling the two test artifacts (`README.md`,
`ADR-011...md`) already written to the vault in a prior session to the conventions
defined here.

The vault is shared across projects (its path is configured globally in
`~/.claude.json`, not per-project), so structure decisions here are vault-wide, not
cmano-clone-scoped, even though cmano-clone is currently the only content in it.

## Scope boundary

`mcpvault` deliberately excludes `.obsidian/` and `.git` from its reads/writes (per its
own README, under "Security Features"). `.obsidian/` is where Obsidian's actual app
configuration lives — enabled core/community plugins, hotkeys, themes, graph-view
settings, daily-notes config. **This setup cannot touch that file.** Two things follow:

1. Everything in this spec that is achievable is **vault content**: folders, notes,
   templates (as plain markdown, since the Templates/Templater community plugin reads
   template files from a folder — it doesn't require its own hidden config to *exist*
   as files), tags (stored in note frontmatter/inline, not `.obsidian/`).
2. Plugin choices are delivered as a **written recommendation document** the user
   installs/enables manually in the Obsidian app. Claude cannot install or enable
   plugins.

## Decisions

### 1. Vault root — PARA method

Adopting Tiago Forte's PARA (Projects / Areas / Resources / Archives) at the vault
root, as the industry-standard default organizational method for a personal
multi-project Obsidian vault:

```
/
├── Projects/cmano-clone/     ← active project workspace (this spec's focus)
├── Areas/                    ← reserved, empty — ongoing non-project responsibilities
├── Resources/                ← reserved, empty — reference material outliving a project
└── Archives/                 ← reserved, empty — completed/dead projects
```

`Areas/`, `Resources/`, `Archives/` are created empty (with a `.gitkeep`-equivalent —
in Obsidian, an empty folder with no notes is simply invisible in the file explorer, so
each gets a placeholder note explaining its purpose) so the top-level shape is visible
immediately, even before they're populated.

### 2. cmano-clone subfolders — mirror the git repo's doc layout

Chosen over a flattened category set so that:
- `source:` frontmatter paths (repo-relative) read naturally against the vault
  subfolder they're mirrored into
- future 1:1 mirroring of a given repo folder maps to a predictable vault folder

```
Projects/cmano-clone/
├── cmano-clone.md        ← MOC / home note
├── architecture/         ← mirrors docs/architecture (ADRs, architecture reviews)
├── gdd/                  ← mirrors design/gdd
├── requirements/         ← mirrors Game-Requirements/
├── production/           ← mirrors production/ (sprint/session notes)
└── research/             ← scratch notes with no repo equivalent (plugin
                             recommendations doc lives here too)
```

`ADR-011 Platform Editor Excel Roundtrip.md` moves from `cmano-clone/adr/` to
`Projects/cmano-clone/architecture/` as part of this setup. `README.md` is superseded
by `cmano-clone.md` (the MOC) and its content folded in; the old `cmano-clone/` folder
is removed once the move is verified.

### 3. Templates

A vault-root `Templates/` folder (shared across future projects, not cmano-clone-only),
containing:

| File | Purpose |
|---|---|
| `Template - ADR.md` | Status / Date / Decision Makers / Context / Decision / Consequences / Compliance / Related sections, matching the repo's ADR format, pre-filled with the mirror frontmatter schema (`tags`, `project`, `source`, `status`, `date`) |
| `Template - GDD.md` | The 8 required sections from `.claude/docs/coding-standards.md`: Overview, Player Fantasy, Detailed Rules, Formulas, Edge Cases, Dependencies, Tuning Knobs, Acceptance Criteria |
| `Template - Research Note.md` | Lightweight: Topic, Source links, Findings, Open questions |

Templates use Obsidian's core Templates plugin syntax (`{{date}}`, `{{title}}`) as a
baseline; the plugin recommendations doc notes which fields upgrade to Templater
syntax (e.g. cursor placement, prompts) if that plugin is enabled later.

### 4. Tag taxonomy — nested tags

```yaml
tags:
  - project/cmano-clone
  - type/adr            # or type/gdd, type/research, type/requirement, type/production
  - status/accepted      # mirrors the source doc's status field where applicable
  - topic/platform-editor # free-form topical tag(s), as many as apply
```

Nested tags (Obsidian's `parent/child` tag syntax) were chosen over flat tags so that
tag-pane and Dataview queries can filter by category (`tag:#type/adr`) rather than by
an unstructured flat list, and so the scheme scales cleanly as more projects/types are
added under the shared vault. `ADR-011`'s existing flat tags
(`cmano-clone, adr, platform-editor, data-layer`) migrate to
`project/cmano-clone, type/adr, topic/platform-editor, topic/data-layer`.

### 5. Naming convention — Title Case with spaces

`ADR-011 Platform Editor Excel Roundtrip.md`, `Combat Balance Formulas.md`,
`2026-07-27 Session Notes.md`. Matches what was already used for the ADR-011 test
mirror, and is idiomatic for Obsidian (file name doubles as the wiki-link display
text and page title).

### 6. Home note (MOC)

`Projects/cmano-clone/cmano-clone.md` — a Dataview-powered dashboard:
- Dataview queries per subfolder, e.g. a table of all notes tagged `type/adr` sorted by
  `date` descending, similarly for `type/gdd`, `type/research`
- A manual "Getting started" section (folder purpose, tag convention, frontmatter
  schema, the "source of truth stays in git" rule) carried over from the current
  `README.md`
- Until Dataview is installed, the query blocks render as plain code fences (harmless,
  just inert) — the note still works as a manually-maintained index in the meantime

### 7. Plugin recommendations doc

`Projects/cmano-clone/research/Obsidian Plugin Recommendations.md` — written guidance,
not an install action:

| Plugin | Type | Why |
|---|---|---|
| Templates | Core (built-in) | Baseline templating — enable this first regardless of anything else |
| Dataview | Community | Powers the MOC's live queries against tagged/frontmatter'd notes |
| Templater | Community | Adds variables, prompts, and cursor placement beyond core Templates |
| Tag Wrangler | Community | Safely rename/merge nested tags across all notes at once (important once the nested tag scheme has many notes using it) |

Each entry includes what it's for and why it was picked, so the user can decide
whether to enable it without needing to research it independently.

## Non-goals

- No project documents (ADRs beyond the ADR-011 test, GDDs, requirements, production
  notes) are mirrored into the vault as part of this spec.
- No `.obsidian/` app configuration (plugin enablement, hotkeys, themes, daily notes
  settings) is performed — that requires the user's action inside the Obsidian app.
- No automated/scheduled sync between the git repo and the vault — mirroring stays
  manual/on-request per the existing convention.

## Testing / verification

Since this is documentation/config scaffolding (not code), verification is:
- `list_directory` at vault root and at `Projects/cmano-clone/` confirms the folder
  tree matches this spec
- `get_frontmatter` on the migrated `ADR-011...md` confirms nested tags applied
  correctly and no data was lost in the move
- `read_note` on the new MOC confirms it renders the intended structure (Dataview
  blocks will be inert without the plugin, which is expected and noted above)
