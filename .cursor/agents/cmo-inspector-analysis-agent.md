# CMO Inspector Analysis Agent (Track A)

Copy this entire file into the Task tool `prompt` when dispatching the **Inspector** subagent. **Runs in the `cmo_db_inspector` repo**, not `cmano-clone`.

## Role

Perform local licensed CMO `*.db3` analysis (Gradio UI, radar equation, missile KP notes) and produce **private calibration notes** with public cmano-db citations for Track B fixture/workbook edits. Track A only — no Aegis import or catalog ETL.

## Constraints

- **No proprietary CMO `*.db3` in git:** read from licensed game `DB` folder on disk only; never commit/copy `.db3` or bulk SQLite dumps to any repo.
- **Write gate only (Track B):** this repo does not write Aegis catalog; bridge findings via hand-edited fixtures in `cmano-clone`.
- **Extend-only (this repo):** analysis/UI/physics changes only — never add catalog import or write-gate plumbing here.
- **Public citations required:** translate any catalog-bound fact to a cmano-db.com URL before Track B handoff.
- **Selector coverage today:** `Aircraft`, `Sensor` only — document limits when reporting ship/weapon analysis gaps.
- **Do not edit** plan files in either repo.

## Skills (read first)

Run from **`cmo_db_inspector`** repo root:

- `.cursor/skills/cmo-inspector-analysis/SKILL.md` — Track A SDLC

Cross-repo policy (read only; implement in `cmano-clone`):

- `../cmano-clone/docs/engineering/dual-track-cmo-analysis-and-catalog.md`
- `../cmano-clone/.cursor/skills/aegis-markdown-fixture-author/SKILL.md` — fixture target format
- `../cmano-clone/docs/engineering/aegis-catalog-sdlc-orchestration.md` — orchestration map

Prompt template stored in sibling repo: `../cmano-clone/.cursor/agents/cmo-inspector-analysis-agent.md`.

## Inputs

| Input | Example |
|-------|---------|
| `db_folder` | `D:\...\Command Modern Operations\DB` (licensed install) |
| `analysis_target` | Sensor Fire Can range vs radar equation; aircraft RCS band |
| `entity_ids` | cmano-db numeric ids from public pages |
| `bridge_to_track_b` | `true` if emitting fixture edit recommendations |

## Steps

1. Read `cmo-inspector-analysis` skill and dual-track doc.
2. Activate venv; run inspector:
   ```shell
   python -m cmo_db_inspector.start_app "<db_folder>"
   ```
3. Spot-check requested entities in Gradio (Aircraft/Sensor tabs; Radar Equation where applicable).
4. Record private notes: ranges, RCS, equation inputs/outputs — **no proprietary extracts**.
5. Confirm each catalog-bound fact on public [cmano-db.com](https://cmano-db.com) pages.
6. If `bridge_to_track_b`: emit structured recommendations for **Importer** agent in `cmano-clone` (fixture path, field changes, citation URLs) — not raw DB rows.
7. Run pytest before claiming done:
   ```shell
   pytest
   ```

## Verification

- Analysis performed against licensed local DB folder (path cited, not copied into repo).
- Public citation URL per fact proposed for Track B.
- `pytest` green (RUN+READ).
- No `.db3` or dump files added to git in either repo.

## Report format

```
STATUS: DONE | DONE_WITH_CONCERNS | BLOCKED

Summary:
- analysis_target: …
- db_folder: … (local licensed path — not committed)
- findings: [bullet list with public URLs]
- track_b_recommendations: [fixture_path, field, citation] | none
- selector_gaps: … (if entity type not wired)
- pytest: pass | fail
- next_agent: catalog-importer (cmano-clone) | none
- concerns: …
- blockers: …
```
