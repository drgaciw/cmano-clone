# Aegis catalog SDLC orchestration

Maps **global agent methodology skills** to **local Project Aegis catalog skills** and defines six subagent roles for fixture-first catalog migration. Use this page when planning a multi-step catalog update or dispatching parallel agents.

> **Policy firewall.** Track A (local CMO `*.db3` in `cmo_db_inspector`) never feeds this repo directly. Track B (fixtures → write gate) is the only in-repo catalog path — see [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md).

---

## Local skills (`.cursor/skills/`)

| Skill | Covers |
|-------|--------|
| [aegis-catalog-curator](../../.cursor/skills/aegis-catalog-curator/SKILL.md) | Master weekly curator workflow: fixture-first propose → review → approve |
| [aegis-markdown-fixture-author](../../.cursor/skills/aegis-markdown-fixture-author/SKILL.md) | Markdown shape, entity types, slug rules, Baltic remap scope |
| [aegis-write-gate-approve](../../.cursor/skills/aegis-write-gate-approve/SKILL.md) | Extend-only gate, error codes, `catalog_write_approve` |
| [aegis-catalog-quality-gate](../../.cursor/skills/aegis-catalog-quality-gate/SKILL.md) | Pre-merge checklist + `scripts/verify-catalog-import.ps1` |

Engineering deep-dives: [cmo-markdown-import.md](cmo-markdown-import.md), [catalog-write-gate.md](catalog-write-gate.md).

---

## Global skills → catalog work

| Global skill | Catalog application |
|--------------|---------------------|
| **TDD** (`test-driven-development`) | Red → green for parser/proposer changes: add failing test in `ProjectAegis.Data.Tests/Import/CmoMarkdown*.cs` or `WriteGate/*.cs` before fixing importer/gate code |
| **Dispatching parallel agents** (`dispatching-parallel-agents`) | Split fixture authoring, propose JSON review, WriteGate approve prep, and quality gate across subagents with non-overlapping file ownership |
| **Verification before completion** (`verification-before-completion`) | Run `./scripts/verify-catalog-import.ps1` and optional kill-chain smoke before claiming a catalog task done; human still approves production batches |

Supporting globals: **systematic-debugging** for quarantine/kill-chain triage; **executing-plans** for multi-fixture waves; **finishing-a-development-branch** after quality gate passes.

---

## Six subagent roles

Dispatch one role per agent when work can run in parallel. **Human always approves production paths** — no subagent runs `catalog_write_approve` against committed production DBs without explicit user consent.

### 1. Curator

**Skill:** `aegis-catalog-curator`

**Owns:** workflow sequencing, scratch DB paths, nightly vs fixture-first choice, weekly checklist.

**Typical tasks:**
- Choose fixture vs full corpus refresh vs enterprise promote ([enterprise-public-corpus-catalog.md](enterprise-public-corpus-catalog.md))
- Run `catalog_import_markdown` propose commands from [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md)
- After enterprise load: run `cmo-promote-corpus-catalog` → `aegis_public_corpus.db` only; **never** overwrite `baltic_patrol.db`
- Coordinate maintainer **Git LFS commit** of the promoted enterprise DB (`.gitattributes` already tracks the path)
- Coordinate handoff to review and approve roles

### 2. Importer (Markdown Fixture Author)

**Skill:** `aegis-markdown-fixture-author`

**Owns:** `tools/cmano-db-crawler/fixtures/*.md` edits.

**Typical tasks:**
- Author H2/H3 records, `<sub>` URLs, field tables, **Weapons** / **Sensors** bullets
- Apply `--map-baltic-platform-ids` only on Baltic mini-fixtures
- Fix quarantine root causes (weapon name, nationality, domain)

### 3. WriteGate

**Skill:** `aegis-write-gate-approve`

**Owns:** batch review, approve CLI, error-code triage.

**Typical tasks:**
- Parse `*-propose.json` for `batchId` values
- Run `catalog_write_approve` on scratch DBs (human-gated for production)
- Resolve `quarantine:*`, `orphan_platform:*`, `kill_chain:*` errors

### 4. Quality

**Skill:** `aegis-catalog-quality-gate`

**Owns:** pre-merge verification, policy guards.

**Typical tasks:**
- Run `./scripts/verify-catalog-import.ps1` (**PR / CI gate** — importer tests + Baltic fixture path; isolate `AEGIS_PUBLIC_CORPUS` so enterprise env does not bleed into Baltic)
- Run `./tools/cmo-verify-corpus-coverage.ps1` for enterprise DB (**off-CI**, ≥99% gate) — ship/facility domain filters; submarine / ground-unit = markdown-ID overlap (not naive `domain=` tallies)
- Confirm zero quarantine before production approve (fixture path)
- Assert no `*.db3` in `git ls-files`
- Full corpus load / promote / kill-chain on `aegis_public_corpus.db` stays **off-CI** (maintainer machine or dedicated job)

### 5. Workbook

**Skill:** [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) (no dedicated `.cursor` skill yet)

**Owns:** post-import tuning of `BasePd`, `RcsBandDbsm`, signatures, Phase B sheets.

**Typical tasks:**
- Export → edit → diff → validate → stage via workbook pipeline
- Approve workbook batches through the same write gate (human approve)

### 6. Inspector (Track A bridge)

**Skill:** external `cmo-inspector-analysis` + `aegis-catalog-curator` (Track A section)

**Owns:** local `.db3` analysis only — **outside this repo**.

**Typical tasks:**
- Spot-check ranges/RCS in Gradio UI (including **Ship** selector)
- Emit citation-required templates via `python -m cmo_db_inspector.emit_fixture_notes` (no `.db3` reads; **never** write enterprise/Baltic Aegis DBs)
- Cite public cmano-db pages when translating findings into Track B fixtures

---

## PR gates vs off-CI full corpus

| Gate | Scope | When |
|------|-------|------|
| `scripts/verify-catalog-import.ps1` | CmoMarkdown importer tests + Baltic fixture path; `*.db3` policy | Every PR / `verify-ci-local` |
| `cmo-verify-corpus-coverage.ps1` (≥99%) | Enterprise `aegis_public_corpus.db` coverage semantics | Off-CI after corpus load/promote |
| Enterprise load + promote + kill-chain | Full `docs/reference/cmano-db/` → LFS artifact | Off-CI curator run; not a PR blocker |

---

## End-to-end flow (fixture-first)

```text
Inspector (Track A, optional)
        │ public cmano-db citation
        ▼
Importer ──► fixtures/*.md
        │
        ▼
Curator ──► catalog_import_markdown (propose)
        │
        ▼
WriteGate ◄── review *-propose.json / quarantine
        │ human catalog_write_approve
        ▼
Workbook (optional BasePd / signatures)
        │
        ▼
Quality ──► verify-catalog-import.ps1 + optional kill-chain
        │
        ▼
Commit markdown + catalog artifacts (never *.db3)
```

---

## Parallel dispatch example

| Agent | Role | Files / commands |
|-------|------|------------------|
| A | Importer | Edit `baltic-sweden-1990-sensors.md` |
| B | Curator | Propose sensor + platform fixtures to scratch DB |
| C | Quality | Run verify script on branch (read-only) |

After A and B complete, serial gate: WriteGate human approve → Quality final pass.

---

## CLI quick reference (repo root)

```bash
# Propose
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/dual-track-smoke/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/sensor-mini.md \
  --entity sensor \
  --report-out scratch/dual-track-smoke/sensor-propose.json

# Approve (human)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve \
  --db scratch/dual-track-smoke/catalog-proposed.db \
  --batch <batchId-from-report>

# Quality
./scripts/verify-catalog-import.ps1
```

---

## See also

| Topic | Doc |
|-------|-----|
| Dual-track firewall | [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md) |
| Markdown import | [cmo-markdown-import.md](cmo-markdown-import.md) |
| Write gate | [catalog-write-gate.md](catalog-write-gate.md) |
| Workbook round-trip | [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) |
| Mission Editor CLI | [mission-editor-cli.md](mission-editor-cli.md) |
| Superpowers / TDD setup | [superpowers-setup.md](superpowers-setup.md) |
