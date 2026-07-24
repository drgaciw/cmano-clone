# Linear ↔ Parallel Agent Dispatch Playbook

**Date:** 2026-07-24
**Linear project:** [cmano-clone](https://linear.app/drgamtd-workspace/project/cmano-clone-7f6a00e4c1c9) (workspace `drgamtd-workspace`, team `Drgamtd-workspace`)
**Companions:** [`local-cloud-agent-routing.md`](local-cloud-agent-routing.md) · [`s39-s48-worktree-manifest.md`](s39-s48-worktree-manifest.md) · [`s39-s48-program-execution-guide.md`](s39-s48-program-execution-guide.md)
**Superpowers skill:** `dispatching-parallel-agents`

---

## 1. Reconciliation result (2026-07-24)

The headline: **Linear is not lagging behind the repo — it is an unstarted greenfield template.**

### Linear side

| Fact | Value |
|------|-------|
| Issues | **12** (`DRG-6` … `DRG-17`) — exactly 2 per milestone |
| Milestones | 6 (M1 Core Sim, M2 Entity/Data, M3 Sensors, M4 Weapons, M5 Map/UI, M6 Scenario Editor) |
| Assignees set | **0** |
| Estimates set | **0** |
| Project start / target date | **null** |
| Issues referencing a repo path, PR, or sprint | **0** |
| Git commits referencing a `DRG-` identifier | **0** |

Every issue carries an auto-generated `branchName` — that is Linear's default for all issues, **not** evidence of linkage.

### Repo side

| Fact | Value |
|------|-------|
| `.md` files under `production/epics/` | 336 |
| …of which carry story frontmatter (`id:`) | **142** (S23–S36 only) |
| …the remainder | 194 `EPIC.md` / index / narrative files |
| Story status | 128 Complete · 6 Ready · 4 In Progress · 3 Not Started · 1 `complete` (casing bug) |
| AC checkboxes across stories | 882 |
| Parallel-kickoff files for **S37+** | 45 |
| Distinct story IDs ≥ S37 (kickoff lane tables only) | **209** |
| Union of identifiable work items | **≈ 350** |
| Not-Complete work items | **14** |
| Open PRs (non-dependabot) | 8 (4 ready, 4 draft) |
| Open dependabot PRs | 8 |
| Remote branches | 176 |
| Open GitHub Issues | 0 |

**Structured story files stopped at S36.** Sprints S37–S107 exist only as kickoff lane tables (`| Lane | Story | Surface |`) plus prose in the 455 KB `sprint-status.yaml`. That decay is the real tracking gap — not Linear.

### Do the 12 Linear issues describe unbuilt work?

Verified by direct code search on 2026-07-24:

| Issue | Title | Verdict |
|-------|-------|---------|
| DRG-15 | Define tick/update loop architecture | **Shipped** — `src/ProjectAegis.Sim/Core/SimTickPipeline.cs` |
| DRG-11 | Implement radar detection model | **Shipped** — `Sim/Sensors/DeterministicDetectionLoop.cs` |
| DRG-9 | Damage/kill probability model | **Shipped** — `Sim/Engage/CombatOutcomeResolver.cs` |
| DRG-17 | Weapon-target engagement resolution | **Shipped** — Baltic engage / kill-chain path |
| DRG-8 | Unit/platform base schema | **Shipped** — platform catalog + Excel round-trip |
| DRG-13 | Reference database loader | **Shipped** — catalog snapshot/import path |
| DRG-12 | Scenario file format | **Shipped** — scenario publish/authoring surface |
| DRG-6 | Basic scenario editor UI | **Shipped** — `ScenarioMapAuthoringPanel` |
| DRG-16 | Map rendering baseline | **Shipped** — `Delegation/Projection/MapPanelState.cs` |
| DRG-7 | Unit icons & selection UI | **Shipped** — `C2PresentationController` |
| DRG-14 | Time acceleration & pause | **Partial / unclear** — `Paused`/`Resume` exist but for agent suspension, not sim time scaling |
| DRG-10 | IR/visual detection model | **Appears genuinely unbuilt** — radar + EMCON only; no IR/EO sensor model found |

**10 of 12 are already delivered.** Only DRG-10 is real remaining work.

---

## 2. Decision: start Linear at the waterline, do not backfill history

Backfilling ~350 mostly-Complete items would produce a tracker that is 96% archive and 4% signal, and would cost more than it returns.

**Do this instead:**

1. **Close DRG-6, -7, -8, -9, -11, -12, -13, -15, -16, -17** as already-delivered, each with a one-line pointer to the shipping file or PR. Ten issues, ten comments.
2. **Re-scope DRG-14** to the actual gap (sim-clock time acceleration + pause), or close it if the headless batch runner already covers the need.
3. **Keep DRG-10** as the one genuine open feature.
4. **Create ~22 new issues** covering the live waterline: the **14** not-Complete stories + the **8** open PRs.
5. **Leave S1–S107 history in Git.** `production/epics/` and the kickoff files stay the historical record. Linear starts at S108.

Net result: a Linear project with roughly **23 live issues**, every one actionable — instead of 350 tombstones.

---

## 3. Field mapping (story frontmatter → Linear)

| Story frontmatter | Linear field |
|---|---|
| `id: S34-01` | Title prefix — keep for traceability |
| `status` | Workflow state |
| `owner: c-sharp-devops-engineer` | **Label** `agent/<name>` — assignee stays a human |
| `dependencies` | Issue relation **blocked-by** (this drives wave planning) |
| `estimate_days` | Estimate |
| `graphite_branch` | Linked branch (use Linear's branch name — see §5) |
| `type: Config\|Logic\|UI\|Integration` | Label — selects the test-evidence gate |
| `req_trace` | Link to the Notion design page under **Resources** |
| AC checklist | Description checklist, or sub-issues when > 6 items |

Honour the sync contract already written in the Notion *Architecture & Design Decisions* page: **design in Notion, status in Linear, files in Git.** Linear issues reference specs; they never duplicate them.

---

## 4. The `Surface` field — the parallelism-safety predicate

**`Surface`** = the files/symbols an issue will touch. Your kickoff tables already carry it (S107 lane C = `MapPanelApplyState + ASSET-009/010 USS`).

**Dispatch rule: never dispatch two issues whose `Surface` values intersect.**

> **Implementation.** Linear has no arbitrary per-issue custom field, so `Surface` is carried as a **`Surface:` line in the issue description**. All 21 backfilled issues were populated on 2026-07-24 with real values — derived from `gh pr view --json files` for the PR issues, and from the repo paths and type symbols named in each story file for the story issues. Treat any issue whose `Surface` is `TBD` as **not dispatchable**.

### Known collisions (do not co-schedule)

Computed from the populated surfaces, restricted to `src/` and `unity/` paths:

| Pair | Shared surface |
|---|---|
| DRG-31 × DRG-36 × DRG-37 | `src/ProjectAegis.Delegation/Projection`, `…Delegation.Tests/Projection`, `unity/ProjectAegis/Assets` |
| DRG-25 × DRG-31 / DRG-36 / DRG-37 | `unity/ProjectAegis/Assets` |
| DRG-18 × DRG-30 | `…UnityAdapter.Tests/Baltic`, `src/ProjectAegis.Sim.Tests/Sensors` |
| DRG-18 × DRG-27 | `…UnityAdapter.Tests/Baltic` |
| DRG-18 × DRG-29 | `src/ProjectAegis.Delegation.Tests/Projection` |
| DRG-27 × DRG-30 | `…UnityAdapter.Tests/Baltic` |

**The three live C2 PRs (#338 → DRG-31, #323 → DRG-36, #321 → DRG-37) all mutate the same projection layer and Unity assets.** They are already open concurrently. That is the exact hazard this rule exists to catch, and it is worth resolving by merge order before opening any further work on that surface.

### A valid first wave (surface-disjoint, within the 6-track cap)

`DRG-38` (`.github/workflows`) · `DRG-33` (`ProjectAegis.Data/*`) · `DRG-29` (`Delegation/Decision`) · `DRG-24` (playtests/design) · `DRG-28` (design/gdd) · `DRG-19` (`tools/*`)

Add a `single-owner` label capping concurrency at 1 for the known high-blast-radius clusters named in [`local-cloud-agent-routing.md`](local-cloud-agent-routing.md):

- the Catalog symbol cluster
- `DelegationBridge.cs`
- `SimulationSession`

---

## 5. Branch and PR linkage

Use **Linear's generated branch name** for the worktree branch so PRs auto-link and issues auto-close on merge. This composes with Graphite — the stack branch takes the Linear-derived name instead of `stack/sprintNN/<slug>`.

```bash
git worktree add /home/username01/cmano-clone/.worktrees/<issue-id> -b <linear-branch-name> main
```

Today **zero** commits reference a Linear ID, so this convention starts clean with the ~23 waterline issues.

---

## 6. WIP limits (from the existing routing matrix)

| Environment | Concurrent cap |
|---|---|
| Local | 6 |
| Cloud | 5 |
| **Effective tracks** | **4–6** |

Enforce as a board column WIP limit, not a convention. The binding constraint is **integration and review bandwidth**, not agent availability — the `dispatching-parallel-agents` skill optimises for task independence and is silent on merge cost.

---

## 7. Definition of Done — issue template

Apply to every code issue so no dispatched agent can self-certify:

- [ ] `dotnet build ProjectAegis.sln` — 0 errors
- [ ] `dotnet test ProjectAegis.sln` — ≥ prior baseline, 0 failures
- [ ] `ReplayGoldenSuiteTests` — 6/6
- [ ] `PlayModeSmokeHarnessTests` — 18/18
- [ ] GitNexus `impact()` run **before** symbol edits; `detect_changes()` **before** commit
- [ ] Evidence written under `production/qa/`
- [ ] Touched nothing outside the issue's `Surface`

---

## 8. Dispatch loop

1. Query Linear: state = ready, no unresolved **blocked-by**.
2. Filter to a `Surface`-disjoint set; cap at the WIP limit; `single-owner` surfaces take at most one slot.
3. `git worktree add` per issue, using the Linear branch name.
4. **Dispatch all agents in one message** (multiple tool calls = parallel; one per message = sequential). Each prompt carries: the issue body, its ACs, its `Surface` as an explicit do-not-touch boundary, and the §7 gate block.
5. On return: read each summary, check for cross-track conflicts, run the full suite once over the integrated result, merge baseline → code → closeout.
6. Closeout regenerates `sprint-status.yaml` from Linear.

**Write access:** start read-only — agents query Linear, humans and the closeout lane write. An agent closing an issue it did not finish is worse than no tracker. Grant write to the closeout lane only once the loop is proven.

---

## 9. Hygiene backlog (unblocks the above)

| Item | Action | Status |
|---|---|---|
| 189 remote branches | Delete branches verifiably merged into `main`, protecting every open-PR branch | **Done 2026-07-24** — 98 deleted, 189 → 91; all 18 open PRs intact |
| 8 dependabot PRs | Group into one scheduled batch | **Done** — `dotnet-all` + `actions-all` groups in `.github/dependabot.yml` |
| GitHub Issues (0 open) | Keep enabled for inbound reports; point planning at Linear | **Done** — pointer added to `README.md` |
| Story-status casing bug | `status: complete` → `Complete` (S31-11 was misread as open) | **Done** |
| `sprint-status.yaml` (455 KB) | Demote to a generated artifact — see §11 | **Planned** — design only, no code change |
| Story files stopped at S36 | Linear is SoT from S108; files freeze as history | **Decided 2026-07-24** |
| `shadcn-ui` MCP server | Rotate the plaintext GitHub PAT in `~/.claude.json`; move to an env var | **Open — user action** |
| 9 Dependabot vulnerability alerts on `main` | 2 high, 6 moderate, 1 low (surfaced during branch cleanup) | **Open — untriaged** |

---

## 10. Source-of-truth decision (2026-07-24)

**Linear is the source of truth for work items from S108 onward.**

- New work is created in Linear only. Do **not** author new `production/epics/story-*.md` files.
- `production/epics/` and `production/agentic/sprint-*-parallel-kickoff-*.md` freeze as the **S1–S107 historical record**. They stay in Git, unedited.
- The 13 still-open pre-S108 stories were backfilled into Linear (§2); their markdown files remain as the spec text those issues reference.
- Running both systems is what produced the current drift — story files silently stopped at S36 while sprints continued to S107. Do not restart the second ledger.

Consequence for tooling: `/create-stories` should no longer be run for new epics. `/story-done` and `/dev-story` still write `sprint-status.yaml` — that rewiring is §11 and is deliberately *not* bundled with this pass.

---

## 11. `sprint-status.yaml` demotion — migration plan (not yet implemented)

**Problem.** 455 KB, single file, mutated by `/story-done` and `/dev-story`. Every parallel track touches it, so it conflicts precisely when agent count peaks. It also mixes machine-readable status with hand-written prose narrative, which is why it grew to this size.

**Target.** Status lives in Linear; the YAML becomes a read-only snapshot regenerated at closeout.

**Staged path (each stage independently shippable):**

1. **Freeze prose.** Add a `# GENERATED — do not hand-edit` banner. New narrative goes in the sprint closeout doc, not here.
2. **Split.** Separate machine fields (`sprint`, `tests_passed`, per-story status) from the narrative blob. The narrative moves to `production/agentic/sprint-NNN-closeout-*.md`, where it already belongs.
3. **Invert the write.** `/story-done` updates the Linear issue state instead of the YAML.
4. **Regenerate.** A closeout step renders `sprint-status.yaml` from Linear as a build artifact.
5. **Delete the write path.** Remove YAML mutation from `/dev-story`.

**Why staged:** stages 3–5 touch core sprint tooling. Landing them alongside a tracker migration would make a regression impossible to attribute. Run them as their own reviewed pass.

---

---

## 12. Remaining manual steps (Linear UI — not doable over MCP)

| Step | Why it matters |
|---|---|
| Set a **WIP limit of 6** on the In Progress board column | Enforces the §6 cap instead of relying on convention (board setting — UI only) |
| Save the §7 gate block as a reusable **issue template** | Already appended to all 21 live issues; a template keeps *future* issues covered |
| Attach Notion design pages under issue **Resources** | Completes the traceability contract in §3 |
| Resolve the **DRG-31 / DRG-36 / DRG-37** projection-layer collision | Three open PRs mutating the same surface — decide merge order (§4) |
| Rotate the plaintext GitHub PAT in `~/.claude.json` (`shadcn-ui` server) | Credential currently readable in `claude mcp list` output |
| Triage 9 Dependabot alerts on `main` (2 high, 6 moderate, 1 low) | Surfaced during branch cleanup; untriaged |

---

## 13. Implementation log

**2026-07-24 — applied**

*Linear (verified by independent read-back: 33 issues, 10 Done, 23 open):*
- Closed 10 already-delivered issues (DRG-6, 7, 8, 9, 11, 12, 13, 15, 16, 17), each with an evidence comment naming the shipping file
- Re-scoped DRG-14 → "Implement sim-clock time acceleration & pause"
- Kept DRG-10 open (the one genuine gap) with a verification comment
- Created 21 issues: DRG-18…DRG-30 (13 open stories), DRG-31…DRG-38 (8 live PRs)
- Created 6 labels (`agent/*`, `draft`)
- 4 fractional `estimate_days` values were rejected by Linear's integer point scale; retried without `estimate` and preserved in the description — 0 unresolved failures
- **Populated `Surface:` on all 21 issues** (21/21 confirmed) from `gh pr view --json files` and story-file path/symbol extraction; appended the §7 gate block to each. Spot-verified by read-back on DRG-18, DRG-25, DRG-31, DRG-38.
- Computed the §4 collision matrix — 10 colliding pairs found, including three concurrently-open PRs on the same projection surface

*Repo:*
- Deleted 98 merged remote branches (189 → 91), guarded against every open-PR branch; all 18 open PRs intact
- `.github/dependabot.yml`: consolidated to `dotnet-all` + `actions-all` groups
- `README.md`: added the work-tracking pointer
- Fixed `status: complete` → `Complete` in S31-11

*Not changed by design:* `sprint-status.yaml` (§11 is design-only), `/story-done` and `/dev-story` tooling, repo settings.

---

---

## 14. Roadmap layer (added 2026-07-24)

### Why sprint-numbered roadmaps keep going stale

`future-sprint-roadmap-07142026.md` (authored **2026-07-14**) proposed **S94–S97** as the next program. Kickoffs show **S94 through S107 all ran between 2026-07-14 and 2026-07-18** — execution passed the entire proposed program within four days.

At roughly **three sprints per day**, a four-sprint plan is about a day of work. Any roadmap keyed to sprint numbers is obsolete before it is committed. This is the same drift that stranded story files at S36 and left a chain of 10 dated `future-sprint-roadpmap-*.md` snapshots.

**Rule: roadmap milestones are keyed to outcomes and trigger conditions, never to sprint numbers.** Sprints are an execution detail; they move too fast to plan against.

### Milestone structure

`M1`–`M6` are the original foundations and are **retrospective** — all describe shipped work. They stay for history. Forward planning uses the `H`-series, each carrying an explicit trigger:

| Milestone | State | Trigger |
|---|---|---|
| **H1 — C2 Runtime Depth** | In flight | Epic A panels bound to runtime state, suite floor held |
| **H2 — Asset Approved Path** | In flight | Formal Approved criteria defined + applied to umbrella 001–003 |
| **H3 — Launch / Commercial Execution** | **Gated** | Human Launch ack + open `commercial-launch-execution-gate-TBD.md` |
| **H4 — Scenario Editor Phase 2 GUI** | Out of scope | Product prioritization + new scope boundary |
| **H5 — Content Pipeline / Addressables** | Out of scope | Content pipeline ADR accepted |
| **H6 — Multiplayer / Save-Load** | Out of scope | Product confirmation; needs ADR for the `DelegationBridge` ZERO-touch constraint |

Every H-milestone cites the repo document it was derived from. None were invented.

### What is deliberately NOT set

**Target dates.** No milestone carries one, because no source document commits to a date. Adding invented dates would make the roadmap look authoritative while being fiction. Dates are the owner's call.

**Cycles.** `list_cycles` returns `[]` — cycles are off, and enabling them is a team setting not reachable over MCP. Given the sprint cadence, cycles may be the wrong instrument anyway; milestone progress is the better signal here.

**Priorities are a mechanical first pass:** live PRs High, in-progress engine work Medium, docs/cleanup Low. Re-rank as product judgment dictates.

### `pr-mirror` label

The 8 PR-shadow issues now carry `pr-mirror` and should be **excluded from roadmap views**. They are transient and close on merge; they do not represent product intent. Real feature issues should own the intent, with PRs attaching to them — Linear's GitHub integration already links PRs automatically.

---

---

## 15. Requirement traceability (added 2026-07-24)

### The missing edge

Three systems each hold part of the picture, and none of them connected:

| System | Holds | Missing |
|---|---|---|
| **Notion** | 22 requirements (REQ-01…REQ-21) + 25 specs | **No relational edges** — every requirement has `Specs: null`, every spec has `Requirements: null` |
| **Repo** | `architecture-traceability-index.md` — 47 TR-IDs mapped GDD → System → ADR | Stale at 2026-07-08; nothing links it to Linear |
| **Linear** | Delivery board, 7 forward milestones | **No requirement linkage at all** |

Notion's databases are **inventory mirrors of repo files**, not live registers — all 22 requirements read `Status: Done` (meaning *inventoried*, not *delivered*), with no Priority or Owner set, all synced 2026-07-24. Reading them as a delivery signal would be a mistake.

### H7 — Requirement Coverage Gaps

The repo traceability index holds the coverage data Linear lacked: **15 Covered / 20 Partial / 12 Gap** across 47 TR-IDs. None of the Gap rows had any roadmap representation.

`H7` now carries them, one issue per requirement cluster, each linking its Notion requirement page under **Resources** — which satisfies the linking convention in Notion's own *Architecture & Design Decisions* sync-hygiene page.

| Issue | Requirement | Gap |
|---|---|---|
| DRG-43 | REQ-15 | `TR-sensor-004` side picture / datalink |
| DRG-44 | REQ-16 | `TR-logistics-003` deterministic fuel burn |
| DRG-45 | REQ-07 / REQ-17 | `TR-agentic-002/003` AAR infrastructure |
| DRG-46 | REQ-14 | `TR-engage-003` swarm slot ordering |
| DRG-47 | REQ-09 / REQ-10 | Near-Future + Speculative — **no GDD, no ADR** |
| DRG-48 | REQ-02 | Mission runtime — shipped but undocumented |

**Deliberately excluded:** the 20 Partial rows. They need the deferred re-assessment (DRG-41 item 4c) before they can become actionable — turning stale Partial statuses into issues would manufacture false precision.

### Two findings worth carrying forward

**DRG-47 is a product decision, not engineering work.** REQ-09 (Near-Future Technologies) and REQ-10 (Speculative Systems) are among the largest requirement documents in the corpus and carry the game's differentiating fiction — yet neither has a GDD or ADR, so neither can be scheduled. The corpus reads as 21 committed requirements; two of the biggest have no delivery path. **This should be settled before Launch**, because store copy draws on exactly that framing.

**DRG-48 is the inverse gap.** The mission runtime ships and works, but has no GDD and no ADR. Undocumented shipped behaviour is the hardest kind to change safely — there is no stated contract to check a change against.

### Notion write-back

`MS-commercial-launch-execution-gate-TBD` was still `Planned` and pointed at `commercial-launch-execution-gate-TBD.md`, described as *"DRAFT/TBD: incomplete gate checklist."* Updated to `Active`, re-pointed at the real gate opened 2026-07-24, with the four exit-criteria states recorded.

---

*Reconciliation performed 2026-07-24 against Linear project `7f6a00e4c1c9` and repo HEAD on `feat/s107-epic-a-panel-runtime-depth`. Roadmap and requirement-traceability layers added the same day.*
