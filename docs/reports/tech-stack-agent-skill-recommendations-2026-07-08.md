# Tech Stack / Agent / Skill Recommendations — 2026-07-08

**Scope:** Recommendations only (no large implementations).  
**Primary sources:** [`Tech-Stack.md`](../../Tech-Stack.md), `.claude/agents/`, `.claude/skills/`, `unity/ProjectAegis/.claude/`, [`Packages/manifest.json`](../../unity/ProjectAegis/Packages/manifest.json), known doc-sync gaps.

**Inventory snapshot**

| Area | Count / note |
|------|----------------|
| `.claude/agents/*.md` | **67** agent files |
| Unity specialists | `unity-specialist`, `unity-dots-specialist`, `unity-ui-specialist`, `unity-addressables-specialist`, `unity-shader-specialist` |
| Unity Editor MCP skills | **77** under `unity/ProjectAegis/.claude/skills/` — all currently have `<!-- PROJECT-AEGIS -->` blocks + [`README.md`](../../unity/ProjectAegis/.claude/README.md) invariants |
| Studio skill trees | `team-unity`, `team-ui`, `team-data`, `gitnexus/*`, `hindsight/*`, `buildkite-*`, `scenario-audit`, etc. |

**Pinned packages (cite only — from Tech-Stack / manifest):**  
Entities `1.4.6`, Burst `1.8.29`, Entities Graphics `1.4.20`, UI Toolkit `2.0.0`, Addressables `2.3.16`, AI Assistant `2.13.0-pre.2`, AI Inference `2.6.1`.  
**Not a direct dependency yet:** `com.ivanmurzak.unity.mcp` (OpenUPM scopes present; install via CLI).  
**Explicit non-stack:** Built-in RP (not URP/HDRP); legacy Input Manager (not new Input System) — per `unity/ProjectAegis/.claude/README.md`.

---

## A. Improve existing agents (priority-ordered)

### P0

| # | Agent | Weak / stale | Concrete improvement | Related skills |
|---|--------|--------------|----------------------|----------------|
| A1 | **`unity-ui-specialist`** | Input guidance contradicts project truth: body says “Prefer Input System package for new work”; ProjectAegis README forbids new Input System without approval (legacy Input Manager). | Replace with: **default = legacy Input Manager**; only propose `com.unity.inputsystem` after ProjectSettings/`activeInputHandler` proof + human approval. Align gamepad/keyboard guidance to UI Toolkit + legacy Input. | `team-unity` (ui-toolkit mode), `team-ui`, ProjectAegis `screenshot-*` / `tests-run` |
| A2 | **`unity-specialist`** | YAML `description` still lists “Input System” as a normal subsystem; stack table omits AI Assistant/Inference pins and MCP pending status. | Update description to “legacy Input Manager (Input System only if approved)”; add one-line MCP routing: Editor skills under `unity/ProjectAegis/.claude/skills/` + README; cite Tech-Stack package table including AI packages as **present but not agent-owned**. | `team-unity`, `unity-initial-setup` |
| A3 | *(docs, not agent)* see **E1** | Agents that read Claude-Agent-Setup will install/assume wrong MCP package state. | Fix setup doc first so agents stop citing false checklist items. | — |

### P1

| # | Agent | Weak / stale | Concrete improvement | Related skills |
|---|--------|--------------|----------------------|----------------|
| A4 | **`ui-experience-lead`** | Strong on C2/command boundaries; weak link to headless PlayModeSmoke vs Editor MCP and to Tech-Stack routing table. | Add short “Verification & MCP” section: prefer `PlayModeSmokeHarnessTests` 18/18; Editor MCP only for UXML/scene evidence; point at Tech-Stack C2 routing + ProjectAegis README. | `team-ui`, `team-unity` |
| A5 | **`user-experience-military-analyst`** | Still generic “indie game” framing; no Project Aegis C2 / Baltic / headless-proxy pointers. | Add Aegis context: C2 density, `C2PresentationController`, coordinate with `ui-experience-lead` / `unity-ui-specialist`; no sim mutation. | `ux-military-pattern-research`, `team-ui` |
| A6 | **`ai-programmer`** | Generic NPC/BT/pathfinding agent; **does not** cover pinned `com.unity.ai.assistant` / `com.unity.ai.inference`. Risk: agents invent “Unity AI” ownership or confuse with Delegation AI. | Add **When not to use**: Unity package AI Assistant/Inference → escalate to `unity-specialist` / TD; Project Aegis delegation/traits → `military-simulation-architect` / Delegation specialists — not this agent unless designing classic game AI. Optional: one paragraph “Unity AI packages are in manifest; do not implement without TD approval.” | `team-unity` (build-profile), future thin skill note under engine-reference (see D) |
| A7 | **`team-unity` skill’s agent (`unity-specialist`)** | Orchestration skill body barely mentions ProjectAegis `.claude/README.md` dual-tree / regen wipe risk. | In agent + skill: Phase 1 must load `unity/ProjectAegis/.claude/README.md` before any MCP mutation; note `unity-skill-generate` wipes custom blocks. | `team-unity`, `unity-skill-generate` |

### P2

| # | Agent | Weak / stale | Concrete improvement | Related skills |
|---|--------|--------------|----------------------|----------------|
| A8 | **`unity-dots-specialist` / `unity-addressables-specialist` / `unity-shader-specialist`** | Recently aligned (good). Minor: ensure descriptions don’t imply URP; Addressables already correct. | Light pass: cross-link Tech-Stack; shader agent already Built-in-first — keep. | `team-unity` modes `dots` / `addressables` / `shaders` |
| A9 | **`ui-programmer`** | Generic UI C#; may overlap `unity-ui-specialist` without clear “headless adapter vs Editor assets” split. | Clarify: net8.0 presentation/controllers + tests → this agent; UXML/USS/PanelSettings/Editor → `unity-ui-specialist`. | `team-csharp`, `team-ui` |
| A10 | **Buildkite / GitNexus / Hindsight / Data leads** | Generally healthy; not Unity-stack blockers. | Optional: Buildkite agents mention headless Unity gates only (no Editor MCP in CI); Hindsight agents already forbid Tick() — keep. | `buildkite-*`, `gitnexus/*`, `hindsight/*`, `team-data` |
| A11 | **Scenario / catalog adjacency** (`database-intelligence-lead`, `sim-data-specialist`, MissionEditor via skills) | Forward roadmap is scenario editor (req 11); no dedicated editor agent — **acceptable** if `scenario-audit` + data team own content gates. | Document spawn matrix in Tech-Stack or team-data: content audit → `scenario-audit`; schema/write-gate → data team; Unity scene packaging → `unity-addressables-specialist`. | `scenario-audit`, `team-data` |

---

## B. Improve existing skills (priority-ordered)

### P0

| # | Skill / tree | Missing | Concrete improvement |
|---|--------------|---------|------------------------|
| B1 | **`Game-Requirements/Claude-Agent-Setup.md`** (consumed as setup skill-adjacent) | Claims `com.ivanmurzak.unity.mcp` **in** `manifest.json` dependencies (false — only OpenUPM scopes). Checklist item checked. | Align with Tech-Stack: scopes yes; package **not** direct dep until `npx unity-mcp-cli install-plugin ./unity/ProjectAegis`; uncheck checklist until installed. |
| B2 | **`docs/engine-reference/unity/VERSION.md`** | Core packages table stops at Entities/Burst/Graphics — omits UI Toolkit, Addressables, AI packages from Tech-Stack. | Extend table from Tech-Stack/manifest; note Built-in RP + legacy Input; last-verified date. |
| B3 | **`unity-ui-specialist` + ProjectAegis skill preambles** | Input System contradiction (agent) vs README (legacy). Skills already say no Input System — agent is the outlier. | Fix agent (A1); optionally strengthen high-traffic skills (`editor-application-set-state`, `tests-run`) “when-not: use headless PlayModeSmoke for C2 gates.” |

### P1

| # | Skill / tree | Missing | Concrete improvement |
|---|--------------|---------|------------------------|
| B4 | **`.claude/skills/team-unity/SKILL.md`** | Phase 1 lists Tech-Stack/VERSION but not ProjectAegis `.claude/README.md`; shaders mode still says “render-pipeline customization” without Built-in default. | Add README as required context; MCP mode: ping → tool-list → mutate; when-not-to-use Editor MCP (CI/headless); shaders mode: Built-in RP default. |
| B5 | **`.claude/skills/team-ui/SKILL.md`** | Likely thin on headless-vs-Editor and Tech-Stack C2 agent table. | Link Tech-Stack routing; spawn `unity-ui-specialist` for Editor assets, `ui-programmer` for testable C#; PlayModeSmoke as gate. |
| B6 | **ProjectAegis MCP skills (regen hygiene)** | All 77 have PROJECT-AEGIS blocks **today**; regen still wipes them. | Keep README as source of truth; add a tiny repo-root note in `team-unity` mcp mode: “after `unity-skill-generate`, restore blocks / git checkout.” Optional script later (P2) — not a new skill. |
| B7 | **`unity-initial-setup`** | Bootstrap path must stay pinned to `./unity/ProjectAegis` + `6000.3.14f1`. | Re-verify body matches Tech-Stack MCP pending language; do not claim plugin already in dependencies. |

### P2

| # | Skill / tree | Missing | Concrete improvement |
|---|--------------|---------|------------------------|
| B8 | **`gitnexus/*` / `hindsight/*`** | Adequate; ensure Unity Editor work still runs GitNexus impact before symbol edits in adapter code. | One cross-link from `team-unity` Phase 3 → gitnexus-impact-analysis. |
| B9 | **`scenario-audit`** | Exists for content gates; may not be listed in Tech-Stack. | Optional Tech-Stack row under skill locations or C2/content routing. |
| B10 | **`buildkite-*`** | Fine for CI; ensure no skill implies Unity-MCP `:8080` in Buildkite agents. | Explicit when-not-to-use Editor MCP in CI docs if missing. |

---

## C. Propose NEW agents (only if justified)

**Preference: do not add agents.** Coverage is sufficient if A/B/E land.

| Proposal | Verdict | Rationale |
|----------|---------|-----------|
| `unity-mcp-operator` | **Reject** | `unity-specialist` + `team-unity` mcp mode + ProjectAegis skills already own this. |
| `unity-ai-packages-specialist` (Assistant/Inference) | **Reject for now** | Packages pinned but no product ownership story; steal work from `unity-specialist` / TD. Revisit only if Inference becomes a shipped feature. |
| `scenario-editor-lead` | **Defer** | Req 11 forward work; today `scenario-audit` + `database-intelligence-lead` + MissionEditor CLI cover content. Spawn a lead **when** editor UX/Unity tooling has a multi-sprint program — not for stack sync. |
| `c2-presentation-specialist` | **Reject** | Overlaps `ui-experience-lead` + `unity-ui-specialist`. |

**If forced to add one later:** `scenario-editor-lead` (orchestrates MissionEditor CLI, `scenario-audit`, data write-gate, optional Unity packaging) — steals coordination from producer/database-intelligence-lead, not Unity specialists.

---

## D. Propose NEW skills (only if justified)

| Proposal | Verdict | Prefer instead |
|----------|---------|----------------|
| `project-aegis-unity-invariants` skill | **Reject as separate skill** | Already exists as `unity/ProjectAegis/.claude/README.md` + per-skill PROJECT-AEGIS blocks. |
| `headless-vs-editor-mcp-routing` | **Reject as new skill** | Fold into `team-unity` + ProjectAegis README (B4). |
| `unity-ai-assistant-inference` | **Defer** | Thin note in `VERSION.md` / Tech-Stack “present, no workflow yet”; extend `team-unity` build-profile when productized. |
| `restore-project-aegis-skill-blocks` (post-regen) | **Optional P2 tooling** | Shell/script under `tools/` or checklist in README — not a Claude skill proliferation. |
| Extend **`scenario-audit`** | **Yes (extend)** | Add Tech-Stack / agent spawn pointers; keep single skill. |

---

## E. Doc sync debt

| Doc | Disagreement with Tech-Stack.md | Recommended fix owner |
|-----|----------------------------------|------------------------|
| **`Game-Requirements/Claude-Agent-Setup.md`** | Status table + activation prose + checklist claim plugin **in** `manifest.json` dependencies; Tech-Stack correctly says scopes only / install-plugin pending. | Docs / agent-setup owner (or whoever owns Claude integrations) — **P0** |
| **`docs/engine-reference/unity/VERSION.md`** | Omits `com.unity.ui` 2.0.0, Addressables 2.3.16, AI Assistant/Inference; no Built-in RP / Input Manager note. | Engine-reference owner / `unity-specialist` — **P0** |
| **`CLAUDE.md`** | “49 coordinated” agents; on-disk **67**. Note still mentions Godot/Unreal engine sets. | Studio config owner — **P1** (count + “Unity engine set only”) |
| **`unity-ui-specialist.md`** | Prefers new Input System vs ProjectAegis README legacy Input Manager. | Unity UI owner — **P0** (agent fix A1) |
| **`unity-specialist.md` description** | “Input System” as default subsystem language. | Unity specialist owner — **P1** (A2) |
| **Tech-Stack.md** | Generally authoritative after recent edits. Optional: add `scenario-audit` / Built-in RP + legacy Input one-liner for parity with ProjectAegis README. | Stack doc owner — **P2** |
| **`AGENTS.md`** | Already headless-first; no major conflict. Optional cross-link to ProjectAegis `.claude/README.md`. | — **P2** |

---

## F. Suggested implementation order

### P0 — Stop agents lying about the stack (1 short PR)

1. Fix **Claude-Agent-Setup.md** MCP package/checklist language to match Tech-Stack + actual `manifest.json`.
2. Extend **VERSION.md** package table from Tech-Stack (UI, Addressables, AI packages) + Built-in RP / legacy Input.
3. Fix **`unity-ui-specialist`** Input guidance; light **`unity-specialist`** description tweak.

### P1 — Routing clarity (second PR)

4. Update **`team-unity`** (+ **`team-ui`**) with ProjectAegis README, headless-vs-Editor MCP, Built-in shader default, regen wipe note.
5. Refresh **`ui-experience-lead`** / **`user-experience-military-analyst`** / **`ai-programmer`** when-not-to-use / Aegis context.
6. Correct **CLAUDE.md** agent count and engine-set wording.

### P2 — Hygiene & forward work

7. Optional Tech-Stack rows: scenario-audit, explicit Built-in/Input one-liner.
8. Regen-restore checklist or tiny `tools/` helper (not a new agent).
9. Revisit **scenario-editor-lead** only when req 11 editor program starts.
10. Revisit Unity AI package workflow only if Inference/Assistant become product scope.

---

## Appendix — Adjacent agent map (no change required unless noted)

| Concern | Existing agent(s) |
|---------|-------------------|
| C2 presentation orchestration | `ui-experience-lead` |
| Mil-sim UX density | `user-experience-military-analyst` |
| UI Toolkit implementation | `unity-ui-specialist` |
| UI C# / screen flow | `ui-programmer` |
| DOTS / Addressables / Shaders | matching `unity-*-specialist` |
| Data / catalog / write-gate | `database-intelligence-lead` + team-data |
| Scenario content audit | `scenario-audit` skill |
| Buildkite CI | `buildkite-ci-lead` (+ pipelines/ops/migration) |
| GitNexus / Hindsight | skills + `hindsight-dev-memory-lead` / `hindsight-aar-analyst` |
| Determinism / bridge | `determinism-engineer`; DelegationBridge zero-touch in Unity agents |

---

*End of recommendations. Approve P0 before any multi-file agent/skill rewrite.*
