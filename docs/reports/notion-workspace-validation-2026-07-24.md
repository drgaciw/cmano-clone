# Notion Workspace Validation — Project Aegis / cmano-clone

**Date:** 2026-07-24
**Scope:** Notion workspace (Aegis Hub + cmano-clone design wiki) validated against repo HEAD and GitHub delivery state
**Repo state at audit:** branch `feat/s107-epic-a-panel-runtime-depth`, HEAD `7283226`; `origin/main` at `a73f722` (2026-07-17)
**Method:** five parallel read-only audits (Hub databases, design wiki, repo inventory, GitHub delivery state, duplicate/orphan sweep), with headline claims independently re-verified
**Nothing in Notion was created, modified, moved, or deleted.**

---

## 1. Verdict

The Notion workspace is a **faithful, accurate, one-way mirror of the repository — imported once on 2026-07-24 and never edited since.**

It is not broken, and the sync harness works correctly. It is also not a project-management system: no row has an owner, the only relation is 0% populated, and no status has ever moved off its imported value.

The deeper finding is that **the mirror is pointed at the wrong layer**, and in doing so inverts the project's own written contract.

---

## 2. The contract, and how reality diverges

The contract is stated in [`production/agentic/linear-parallel-dispatch-playbook.md`](../../production/agentic/linear-parallel-dispatch-playbook.md) line 101:

> **design in Notion, status in Linear, files in Git.** Linear issues reference specs; they never duplicate them.

Actual state on both ends is the reverse:

| Layer | Contract says | Actual Notion contents |
|---|---|---|
| **Design** — what Notion is *for* | Notion | **~350 words total.** 6 of 8 design pages are byte-identical stubs; **0 of 64** required design sections present; Data Reference DB has **0 rows** |
| **Documents** — already versioned in Git | Git | **253 rows** mirrored across 6 databases (requirements, ADRs, specs, runbooks, fixtures, "milestones") |
| **Work items** — what tracking is *for* | Linear | **0 rows.** 258 stories, 71 epics, 91 sprint docs, 20 bugs — mirrored nowhere |

Notion duplicates the layer Git already versions, and mirrors none of the layer that would justify a tracker. Linear meanwhile holds 12 issues, **10 of which are already shipped** (per the same-day reconciliation in the playbook, §1).

---

## 3. Aegis Hub — six databases, 253 rows

Coverage is genuinely good. The mirror is accurate against the repo:

| Database | Rows | Repo counterpart | Coverage | Verdict |
|---|---:|---|---|---|
| 📌 Requirements | 22 | 21 numbered + glossary | Complete, 21/21 clean | **Skeleton** |
| 🧩 Specs | 78 | GDDs, design docs, reports | Broad | **Partial** |
| 🧠 ADRs | 19 | 18 (ADR-001…017 + addendum) | Complete | **Partial** |
| 🏁 Milestones | 50 | 4 real milestones | **46/50 are not milestones** | **Skeleton** |
| 🎞️ Replays & Golden Fixtures | 36 | 34 golden baselines | Complete +2 non-fixtures | **Partial** |
| 🛠️ Engineering Runbooks | 48 | 45 docs | Complete +3 sync artifacts | **Partial** |

### What is missing that would make it operational

1. **0 of 253 rows have an owner.** ADRs, Milestones, Replays, and Runbooks have **no owner property at all**; Requirements and Specs have one and it is 0% populated.
2. **The hub's only relation is 0% populated.** Requirements ↔ Specs exists in schema, 0/22 and 0/78 in data. ADRs, Milestones, Replays, and Runbooks have **no relations at all**.
3. **There are zero rollups** in any of the six schemas. The Milestones "Timeline" view groups by `Target date`, which is empty on all 50 rows — it renders nothing.
4. **No PR or commit traceability exists**, despite three properties named as if it did. All 147 values across `Specs.GitHub Issue/PR`, `ADRs.GitHub PR/commit`, and `Milestones.Release tag/URL` are `blob/main/<path>` file links. No PRs, no commit SHAs, no release tags.
5. **Five of six status fields are single-valued** — Requirements 22/22 "Done", ADRs 19/19 "Accepted", Replays 36/36 "Green", Runbooks 48/48 "Current", Milestones 48/50 "Complete" with **0 "Active"** on a project with an in-flight feature branch. Only `Specs.Spec status` shows variance (73 Approved / 5 Draft).
6. **Status vocabularies are incompatible across all six databases**, and three use `select` rather than `status`, so they get none of Notion's lifecycle grouping semantics. Cross-database rollup would be impossible even if relations were added.
7. **Nothing has been edited since import.** All 253 rows carry `Last synced` = 2026-07-24, and no row has a `Last updated` post-dating its import batch.

### Broken agent views

All six databases ship an "Agent - Draft" view; **none work**:

- ADRs and Replays filter on `Status = "Draft"`, which is not an option in either — permanently 0 rows.
- Requirements, Specs, and Milestones have an **empty filter array** — they return *every* row, not drafts.
- Only Runbooks has a valid filter, and it returns 0 because 48/48 are "Current".

The "Agent - Missing UID" views are correctly configured in all six and correctly return 0.

---

## 4. Actively misleading content

These rank above hygiene — a reader acting on them would be wrong.

| # | Defect | Evidence |
|---|---|---|
| 1 | **ADR-005 "DOTS/ECS for World State" is marked `Accepted` in Notion.** The repo superseded it 2026-07-07 (DOTS dropped, `com.unity.entities` removed, managed headless-first sim). The `Superseded` option is unused across all 19 ADR rows. | `docs/architecture/adr-005-dots-sim-core.md` (Superseded) vs Notion ADRs DB |
| 2 | **The test baseline is stated four different ways.** Current authoritative is **≥1638**. But `.claude/docs/directory-structure.md` — loaded into *every session* via CLAUDE.md — says ≥1215, and three `src/*/README.md` say 1232. | `AGENTS.md` + `production/sprints/sprint-107-*.md` (≥1638) |
| 3 | **Replays reports 36/36 "Green" with `Last verified` empty on all 36.** Nothing has been verified, yet everything is green. | Replays DB |
| 4 | **`Expected hash/artifact` mixes value types** — 25 rows hold a fingerprint, 11 hold a file path. Unusable as a determinism reference without cleanup. | Replays DB |
| 5 | **Three properties report 100% populated but contain placeholders.** `ADRs.Consequences (short)` is the literal string `Repo path: <path>` on 19/19; `Milestones.Scope (short)` and `Release tag/URL` restate the source path. Any fill-rate dashboard reads healthier than reality. | ADRs, Milestones DBs |
| 6 | **46 of 50 "Milestones" are not milestones** — they are gate checks, QA matrices, and roadmap snapshots. The repo has exactly 4 real milestones under `production/milestones/`. | Milestones DB |

> **Note on "1739 tests":** no such claim exists in the repo. Every `1739` occurrence is the timestamp suffix of gauntlet run `gauntlet-20260713-1739`.

---

## 5. Delivery pipeline is hard-down (outranks everything above)

Verified directly:

- `origin/main` HEAD is `a73f722`, **2026-07-17 — frozen for 7 days**
- **All 18 open PRs** are `BLOCKED` / `BEHIND` / `DIRTY`
- `buildkite/cmano-clone` is the **sole** required status check on `main`, and it is **`FAILURE`**
- `required_approving_review_count: 0` — one external CI check is the entire gate
- `required_conversation_resolution: false`; no merge queue

### CI health

| Workflow | State |
|---|---|
| `buildkite/cmano-clone` | **Primary gate — currently failing on every open PR** |
| gauntlet-oracle | 96% pass (107/112) — healthy |
| GitNexus Security Checks | **59/59 failures, then disabled** rather than fixed |
| GitNexus PR Analysis | `active` but 5/5 failures; dormant since 2026-06-13 |
| `.NET Tests` | `active` but **last ran 2026-05-29 and failed** |
| Unity CI | **never run** |

5 of 8 GitHub workflows are disabled. The project's stated `dotnet test` / PlayMode gates are **not enforced by GitHub Actions at all**.

`bk` CLI is not installed locally, so the Buildkite failure log requires the Buildkite web UI or a local `bk` install.

### Root-cause diagnosis (2026-07-26)

**Verdict: the Buildkite failure is infrastructure-side. No repo change can fix it.**

Evidence:

| Fact | Value |
|---|---|
| Last passing build | **#880, 2026-07-17 — passed in 1m39s** on `a73f722` |
| First failing build | **#885, 2026-07-18T22:43** |
| Every build since | Fails with `created_at == updated_at` — **0 seconds, nothing executed**, on every branch including `main` (#929) |
| `.buildkite/pipeline.yml` last changed | **2026-07-09** (`cbe3f0d`) — nine days *before* the break |
| GitHub Actions `gauntlet-oracle` | **Passing on the same commits**, including 2026-07-26 |
| The gate script run locally on current HEAD | **PASS** — 1792 tests / 0 failures, ReplayGolden 6/6, PlayModeSmoke 21/21 |

A build that fails in zero seconds ran no command, so neither the pipeline YAML nor `tools/buildkite/*.sh` can be the cause — and the same commit that now fails passed eight days earlier under the same config.

**Leading hypothesis (unverified — needs Buildkite access):** plan/quota exhaustion or a cluster/agent configuration change on the Buildkite side. This fits the timeline: week 2026-W29 saw a burst of **117 merged PRs**, and builds began failing instantly on 07-18. A related precedent is already documented in this repo — five GitHub Actions workflows were disabled on 2026-06-20 with the note "Actions billing blocked", and work was migrated *to* Buildkite. Actions is demonstrably working again now, so the constraint appears to have moved.

**Could not verify:** `buildkite.com/drgaciw/cmano-clone/builds/*` returns **HTTP 403** (private) and no Buildkite API token is available in this environment; `bk` CLI is not installed. Confirming the hypothesis requires signing in to Buildkite and checking **billing/usage** and **Agents → cluster**.

### Mitigation applied

`.github/workflows/dotnet-ci.yml` (new) runs `bash tools/buildkite/dotnet-ci.sh` — *the same script* Buildkite runs, deliberately not a re-implementation, so the two providers cannot drift. actionlint clean. This removes the single point of failure: before it, the full build+test gate existed **only** on Buildkite (the ".NET Tests" workflow `gh` still lists has no file — it was deleted during the Buildkite migration).

**Not changed — requires a human decision:** `main`'s required status check is still only `buildkite/cmano-clone`. Until that is repointed (or `dotnet-ci / Build and test` is added alongside it), the 18 open PRs stay blocked. Note also `required_approving_review_count: 0`, so whichever check is required is the *entire* gate.

### GitHub governance

| Signal | Value |
|---|---|
| Open issues | **0** (1 closed, ever) against 242 merged PRs |
| Milestones | **0** — none exist |
| Labels | 13, all GitHub defaults or dependabot-created; **no human PR is labeled** |
| Remote branches | **190**; 26 non-dependabot stale >30 days; merged branches essentially never pruned |
| Bot noise | 10 of 18 open PRs (56%) are dependabot/copilot |
| Commit cadence | ~115/week over the active period; **0 this week** |

---

## 6. Duplication and orphans — cleaner than assumed

An initial read suggested nine duplicated "Future Sprint Roadmap" pages. **That was incorrect.**

They are **10 distinct Milestones rows**, each with a unique UID sourced from a different dated snapshot covering a different sprint train (062026 = S39–S48, 062126 = S49–S56, 062226 = S57–S64, 062426 = S65–S68, 062526 = S69–S72, 062526.01 = S73–S80, 07042026 = S81–S88, 07092026 = S89–S92). They share a title only because the sync derives page titles from each file's H1.

**Workspace hygiene: ~0% orphans, <1% duplication.** Every page resolves through an `ancestor-path` terminating at the Aegis Hub. The 21 imported requirements pages map 21-for-21 with no strays.

### The three real defects all originate in Git, not Notion

Fixing them in Notion would be overwritten on the next sync. All three confirmed firsthand:

1. **`docs/reports/future-sprint-roadpmap.md` is a broken symlink** whose target string is the entire 2.4 KB markdown body rather than a path. It resolves to nothing.
2. **`docs/reports/future-sprint-roadpmap-062126.md` appears clobbered.** Its H1 reads `# Future Sprint Roadmap — Stable Alias`, though the alias table says it should hold S49–S56. Corroborating evidence: the file is **1.5 KB** where sibling snapshots are 16–40 KB.
3. **Two golden fixtures are byte-identical** — `replay-golden-baltic-destroyed-reengage-merged.txt` and `replay-golden-baltic-destroyed-reengage-2026-06-22.txt`, both md5 `bbeff2aafe6b6e1fcee9d280a8d013f1`.

Additional smaller items: the Notion page `Implementation Plan` has duplicated body lines, suggesting the sync **appends rather than replaces** on re-run — worth checking whether other re-synced pages accumulate the same way.

---

## 7. Design wiki — the layer Notion is supposed to own

| Page | Content | 8-section compliance |
|---|---|---|
| Architecture & Design Decisions | ~150 words of *process* rules; **zero architecture decisions** | 0/8 |
| Core Simulation Design | Stub | 0/8 |
| Entity & Unit Data Model | Stub | 0/8 |
| Sensor & Detection Model | Stub | 0/8 |
| Weapons & Engagement Resolution | Stub | 0/8 |
| Map / Rendering / UI | Stub | 0/8 |
| Scenario Editor Spec | Stub | 0/8 |
| Research Notes | Stub | 0/8 |

**0 of 64 required sections present.** All six stubs are identical text varying only by milestone label and one bolded phrase, and each asserts *"Full content is authored here first"* while containing none.

**Root cause is the parent page's template.** It mandates 4 metadata fields (`Status / Owner / Related Linear Milestone / Last updated`) and zero content sections. The project's own coding standard mandates 8 content sections. The two do not overlap at any point — every child page was cloned from a template that satisfies none of the project's documented requirements.

**Data Reference Database: 0 rows**, and it should probably stay that way. Its 5 flat columns have no schema overlap with the repo's canonical catalog (`assets/data/catalog/baltic_patrol.db`: 30 tables, 79 platforms, 463 sensors, 265 weapons, with `snapshot_id` provenance and staging gates). Hand-entering platform data there would create an ungoverned second source of truth for determinism-critical content. **Its emptiness is currently protective.**

---

## 8. Recommendations

### P0 — Unblock delivery
1. **Diagnose and fix the Buildkite failure.** 18 PRs and 7 days of throughput sit behind it. No PM restructuring matters while the pipeline is down.
2. **Set `required_approving_review_count: 1`.** A single external check with zero human review is a fragile gate that has now demonstrably halted everything.

### P1 — Decide what Notion is for, then commit
Two coherent options; the current state is neither.

- **(A) Honor the written contract** — freeze or remove the six document-mirror databases, and actually author design in the cmano-clone wiki. Notion becomes small and real.
- **(B) Keep the mirror** — but label every database "generated, read-only," strip the owner/status fields that imply a workflow, and stop presenting it as a tracker.

**Recommendation: (A).** The mirror's marginal value over `git grep` is near zero, and its unused status fields actively invite someone to mistake an archive for live state.

### P2 — Correct the misleading content
3. Fix **ADR-005** to `Superseded` and start using that status. Highest rework risk of anything in this report.
4. **Single-source the test baseline** at ≥1638 — starting with `.claude/docs/directory-structure.md`, which loads into every session.
5. Split `Expected hash/artifact` into distinct hash and artifact-path fields; populate `Last verified` or stop reporting 36/36 Green.
6. Replace the placeholder `Consequences` / `Scope` / `Release tag` values, or remove the properties.
7. Move the 46 non-milestone rows out of the Milestones database.

### P3 — Fix the Git-origin defects
8. Restore `docs/reports/future-sprint-roadpmap.md` as a real file or a correct symlink.
9. Recover the S49–S56 content in `future-sprint-roadpmap-062126.md` and fix its H1.
10. De-duplicate the identical golden fixture pair in `tests/regression/`.
11. Check whether the Notion sync appends rather than replaces on re-run.

### P4 — Design wiki
12. **Fix the parent page template first** — every child clones from it. Align it to the project's 8-section standard.
13. **Write the Scenario Editor Spec.** Code is actively shipping ahead of it on the current branch (`ScenarioMapAuthoringPanel`, `assets/data/scenarios/authoring/`, `Cli validate` round-trip).
14. Decide whether the Data Reference Database becomes a citation index for Research Notes, or is removed. Do not populate it as a platform catalog.

### P5 — GitHub governance
15. Disable GitHub Issues or redirect to Linear — do not leave it ambiguous (0 issues against 242 merged PRs).
16. Prune merged and stale branches (190 remote, 26 stale >30d).
17. Batch the 9 open dependabot PRs into one scheduled group.
18. Add a minimal label taxonomy, or accept that PR titles and branch names are the only traceability and document that.

### Security
19. **Rotate the plaintext GitHub PAT in `~/.claude.json`** (`shadcn-ui` MCP server) and move it to an environment variable. Carried forward from the playbook's own hygiene backlog.

---

## 9. Changes applied to Notion (2026-07-26)

The audit itself was read-only. The following writes were made afterwards, on request. **No row, page, or database was deleted**, and the six mirror databases were left structurally intact pending the P1 decision.

| # | Target | Change |
|---|---|---|
| 1 | cmano-clone parent page | Template block rewritten from 4 metadata fields to the mandated **8 content sections**, plus authoring rules (cite repo paths; use `> **OPEN QUESTION:**` instead of inventing). Highest leverage — all child pages clone from here. |
| 2 | Project Aegis (Notion Hub) | Added a "read this before trusting anything below" banner marking the six databases as a generated one-way mirror, with a per-database freshness table and the design/status/files contract. |
| 3 | ADRs → ADR-005 | `Status` **Accepted → Superseded**. `Consequences (short)` placeholder replaced with the actual reversal (DOTS dropped 2026-07-07, `com.unity.entities` removed, managed headless-first sim). `Sync note` records that the harness is not mapping ADR status. |
| 4 | ADRs → "Agent - Draft" view | Filtered on `Status = "Draft"`, which is not an option in that database (always 0 rows). Repointed to `Status = "Proposed"` and renamed **Agent - Proposed**. Now returns the 2 real Proposed ADRs. |
| 5 | Replays → "Agent - Draft" view | Same defect. Repointed to `Status != "Green"` and renamed **Agent - Needs attention**. |
| 6 | Scenario Editor Spec page | Stub replaced with a full 8-section spec reverse-engineered from the implementation, marked **DRAFT / unowned** with an explicit provenance warning and **4 `OPEN QUESTION` flags** where the GDD and the code disagree. |

### Corrected during execution

The audit's snapshot was already stale. Re-checked 2026-07-26: the **ADRs database has grown to 22 rows and was edited 2026-07-25** (now 20 Accepted / 2 Proposed) — it is being maintained by hand. The other five databases remain untouched since import. Workspace total is **256 rows, not 253**. The Hub banner reflects the corrected figures.

### Could not be fixed — needs manual action

The "Agent - Draft" views on **Requirements, Specs, and Milestones** have empty filters and therefore return *every* row while claiming to show drafts. These three use `status`-type properties, and the Notion MCP view DSL **cannot write filters against `status` properties** — verified across three syntax forms, each silently producing an empty filter group. The two views that were fixable (ADRs, Replays) use `select`.

The Requirements view has been renamed to `Agent - Draft (⚠ FILTER BROKEN — returns ALL rows, set Status=Draft by hand)` so it no longer misleads. **Specs and Milestones were left untouched and still silently return all rows** — fix both in the Notion UI.

### Open follow-ups from these changes

1. **Fix the sync harness ADR status mapping**, or change #3 regresses on the next import.
2. Assign an owner to the Scenario Editor Spec and resolve its 4 `OPEN QUESTION` flags — particularly the event `priority` field, which the GDD specifies and `ScenarioEventDto` does not implement.
3. Set the Specs and Milestones draft-view filters by hand in the Notion UI.
4. Make the P1 call (freeze/remove the mirror vs. keep it labelled). Nothing above forecloses either option.

---

## 10. Caveats on completeness

- Two audits hit Notion's **free-plan hourly SQL query cap** and fell back to view-mode row dumps. Row counts reconcile exactly with `COUNT(*)` in every case, but only the derivation method differs — a low-frequency duplicate outside the searched terms could remain unsurfaced. Notion workspace search also caps at 25 results per query.
- Page **bodies** in the six Hub databases were not read; findings there are based on property values and titles. A row with stub properties may still carry real body content.
- The **Linear MCP server requires authorization** and could not be queried directly. Linear state in this report derives from the repo's own same-day reconciliation ([playbook §1](../../production/agentic/linear-parallel-dispatch-playbook.md)), not direct observation.
- `bk` CLI is not installed locally; the Buildkite failure cause was not diagnosed, only confirmed as the blocking check.
