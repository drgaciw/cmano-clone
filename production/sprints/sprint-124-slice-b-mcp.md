# Sprint 124 — Slice B C2 bind + Unity-MCP evidence

**Dates:** 2026-09-15 to 2026-09-19  
**Predecessor:** S122 Slice A Unity-MCP (\`production/sprints/sprint-122-slice-a-unity-mcp.md\`) & S123 Backlog Expansion (\`production/sprints/sprint-123-backlog-expansion.md\`)  
**Epic:** [DRG-184](https://linear.app/drgamtd-workspace/issue/DRG-184) Slice B — Target, Engage, Assess  
**Stage:** **Release** · **Not Launch** · **Not Phase N**  
**Review mode:** lean (\`production/review-mode.txt\`)  
**Unity-MCP:** Custom pin \`http://localhost:8080\` · Editor **6000.3.22f1** · \`/team-unity\` MCP gate  
**Doctrine:** ADR-010 / 007 / 001 · unity-csharp-architect pr-finish on presentation

> **Invariants:** Do not rebuild CMD-32/34. Do not touch \`DelegationBridge\` hotpath. Do **not** implement Slice C UI ([DRG-185](https://linear.app/drgamtd-workspace/issue/DRG-185) stays Backlog). Max 29 UIDocument roots.

## Sprint Goal

Bind Slice B engagement chrome onto **existing** DelegationSmoke hosts (EngageExplain, ContactDetail BDA, Map VFX, CombatDomains) and prove it with Unity-MCP Play Mode screenshots — no new UIDocuments.

## Capacity

| Metric | Value |
|---|---|
| Total days | 5 (Mon–Fri) |
| Buffer (20%) | 1 day (Editor lock / \`:8080\`) |
| Available wall-clock | 4 days |
| Agent-days (max points) | **~12** (file-disjoint C# + 1 serial Editor MCP lane) |
| Must-have SP | 18 |
| Should-have SP | 5 |
| Nice-to-have SP | 2 (Slice C bind map, docs only) |

**Unity Editor lock:** one Play Mode / MCP mutation stream.

## Sources

| Source | Finding |
|---|---|
| Unity-MCP \`slice-rec-2\` (2026-09-04) | \`EngageExplain\` GO present; panel \`ENGAGE: —\` while Contact/Chain show \`EXPLAIN: engage/c1\`. \`CombatDomains\` **missing** from scene (\`gameobject-find\` 500). RootCount **29**. |
| Linear | Epic DRG-184 **In Progress**. Headless Done: DRG-211, 215, 216, 220, 224, 226, 230. S123 Governance: DRG-231..241 Done. UI DRG-165–170 active. |
| S122 | NET HUD live; contact/chain stacked; AUTH/ROE honesty landed in baseline \`3795e53d\`; CONTACTS list honesty landed. |

## Agent / subagent / skill routing

\`\`\`
Orchestrator
├── Wave 0  Lane 0 /team-unity     S124-00 baseline landed (3795e53d); MCP ping
├── Wave 1  Lane 1 /team-csharp    S124-01 EngageExplain fill (TDD, file-disjoint)
├── Wave 1b Lane 0 /team-unity     S124-02 CombatDomains Ensure + scene-save (serial Editor)
├── Wave 2  Lane 2 /team-csharp    S124-03 ContactDetail BDA/WRA bind
├── Wave 2b Lane 0 /team-unity     S124-04 Map VFX MCP shots (no DRAW rewrite)
└── Wave 3  Lane 3 /qa-gauntlet-combat-ui  S124-05/06 declutter + vertical-slice shot-list
\`\`\`

**Always:** GitNexus \`impact()\` before C#; \`detect_changes()\` before commit; MCP probe before \`.unity\` / \`.uxml\`.  
**Never:** \`DelegationBridge.Tick\`; CatalogWriteGate; overlay projection rewrite; two Editors on \`unity/ProjectAegis\`; Slice C UI.

## Tasks

### Must Have (Critical Path) — 18 SP

| ID | Task | Linear | Agent/Owner | Est. Days | SP | Dependencies | Acceptance Criteria |
|----|------|--------|-------------|-----------|----|--------------|-------------------|
| S124-00 | Land S122-12/13 baseline | DRG-180/182 | Orchestrator | 0.5 | 2 | Clean tree | Landed in \`3795e53d\`. Contacts list shows \`c1\`, unit details show honest ROE/AUTH. |
| S124-01 | EngageExplain live text | [DRG-168](https://linear.app/drgamtd-workspace/issue/DRG-168) | \`/team-csharp\` + \`/team-unity\` | 1.0 | 3 | S124-00 | Panel is not \`ENGAGE: —\` when Contact Detail shows \`EXPLAIN: engage/c1\`. TDD unit test. MCP screenshot. |
| S124-02 | CombatDomains on scene | ASSET-021 | \`/team-unity\` Ensure + **scene-save** | 0.5 | 2 | S124-00 | \`gameobject-find CombatDomains\` succeeds; \`CombatDomainsHotTickHost\` + existing UXML. MCP screenshot of hot-tick strip. |
| S124-03 | Combat card + BDA on ContactDetail | [DRG-169](https://linear.app/drgamtd-workspace/issue/DRG-169) | \`/team-csharp\` | 1.0 | 5 | Headless DRG-216 Done | BDA/WRA lines from projection; no fourth right panel. TDD tests pass. MCP shot. |
| S124-04 | Missile/gun/laser VFX evidence | [DRG-167](https://linear.app/drgamtd-workspace/issue/DRG-167) | \`/team-unity\` | 0.5 | 2 | S121 DRAW Done | MCP Game View of existing MapPlaceholder VFX. Do **not** rebuild DRAW. |
| S124-05 | Declutter / a11y | [DRG-170](https://linear.app/drgamtd-workspace/issue/DRG-170) | \`/team-unity\` + \`/qa-gauntlet-combat-ui\` | 1.0 | 3 | S124-01..03 | USS contrast; Orders vs MessageLog overlap fixed; ReplayGolden still green; Play shots. |
| S124-06 | Vertical-slice harness | [DRG-166](https://linear.app/drgamtd-workspace/issue/DRG-166) | \`/qa-gauntlet-combat-ui\` | 1.0 | 3 | S124-01..05 | One MCP shot-list: permit fire, refuse fire, BDA, explain on DelegationSmoke. 0 errors. |

### Should Have & Nice to Have — 7 SP

| ID | Task | Linear | Agent/Owner | Est. Days | SP | Dependencies | Acceptance Criteria |
|----|------|--------|-------------|-----------|----|--------------|-------------------|
| S124-07 | Hide unused Wave4/5 smoke hosts | heap | \`/team-unity\` | 0.5 | 2 | S124-00 | Selectively deactivate unused hosts when scenario doesn't need them; reduces idle heap. |
| S124-08 | Backlog manifest sync | tooling | Orchestrator | 0.5 | 3 | S124-01..06 | Linear YAML and Notion JSON manifests committed in \`production/backlog/\`. |
| S124-N1 | Slice C bind map (docs) | [DRG-185](https://linear.app/drgamtd-workspace/issue/DRG-185) parked | \`/se-technical-writer\` | 0.5 | 2 | none | \`production/qa/evidence/s124-slice-c-bind-map.md\` maps DRG-171–175/191 to existing hosts. **No scene mutation. No Slice C UI.** |
