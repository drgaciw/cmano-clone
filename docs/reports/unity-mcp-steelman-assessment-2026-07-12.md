# Unity-MCP Steelman Assessment — Project Aegis

| Field | Value |
|-------|--------|
| **Assessment date** | 2026-07-12 (original framing) |
| **Evidence refresh** | 2026-07-23 |
| **Subject** | [IvanMurzak Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) / `com.ivanmurzak.unity.mcp` |
| **Question** | What is the strongest honest case for keeping and activating Unity-MCP in this repo? |
| **Companion HTML** | [unity-mcp-steelman-assessment-2026-07-12.html](./unity-mcp-steelman-assessment-2026-07-12.html) |

---

## Executive verdict

**Keep and activate Unity-MCP for local Editor / C2 presentation work.** The package and client wiring are already on disk; the remaining gap is an interactive Editor session (`login` + `:8080`), not architecture.

**Do not** make Unity-MCP a CI or Cloud merge dependency. Headless .NET on Linux Buildkite remains merge authority. MCP is the local multiplier for scenes, UI Toolkit, assets, and PNG evidence that Cloud Agents cannot produce.

```text
Ready today          Package 0.82.4 in manifest · mcp.json → :8080 · 77 Editor skills
Blocked on           Editor 6000.3.14f1 open · unity-mcp-cli login · :8080 live ping
Never MCP-gated      Buildkite · ReplayGolden · DelegationBridge hotpath · Cloud default path
```

---

## 1. Current factual state (2026-07-23)

| Fact | Evidence |
|------|----------|
| Package **is** a direct dependency | `unity/ProjectAegis/Packages/manifest.json`: `"com.ivanmurzak.unity.mcp": "0.82.4"` |
| Tech-Stack aligned | `Tech-Stack.md` pins `0.82.4`; Editor session / `:8080` still pending |
| Client MCP configs | `.cursor/mcp.json` and `.mcp.json` → `ai-game-developer` → `http://localhost:8080` |
| Editor skills tree | **77** skill directories under `unity/ProjectAegis/.claude/skills/` |
| CLI via npx | `unity-mcp-cli` available (docs still cite older **0.77.0**) |
| `:8080` / Editor session | **Pending** until Editor open + login |
| Cloud / CI policy | Live Editor PNG = **local only** (`production/agentic/local-cloud-agent-routing.md`) |

### Doc drift (P0)

| Source | Claims about package |
|--------|----------------------|
| `manifest.json` / `Tech-Stack.md` | Installed at **0.82.4** |
| `Game-Requirements/Claude-Agent-Setup.md` | **Not** a direct dependency — run `install-plugin` (stale) |
| `unity/ProjectAegis/.claude/README.md` | Soft hedge: package “may not yet appear” (stale vs disk) |
| `tech-stack-agent-skill-recommendations-2026-07-08.md` | Historical snapshot inverted vs current disk |

Both sides still correctly agree: **`:8080` pending** until Editor + login.

---

## 2. Steelman FOR Unity-MCP

1. **Closes the agentic gap Unity actually has here.** Core sim/delegation is already headless-complete (`dotnet test`, ReplayGolden, PlayModeSmokeHarness). Remaining hard work is Editor-native: UXML/USS, PanelHosts, scenes, prefabs, Addressables, Built-in materials/shaders, Game View visual smoke. Unity-MCP is the wired path for Cursor/Claude to *drive* that layer.

2. **Matches the integration review’s highest-leverage Unity pain.** [`unity-integration-review-2026-07-07.md`](./unity-integration-review-2026-07-07.md) §4.2–4.4: asmdefs, UI rebuild cost, human smoke checklist. MCP skills already cover `scene-*`, `gameobject-*`, `assets-*`, `screenshot-*`, `editor-application-set-state`, `tests-run`, `profiler-*` — the loop for C2 hosts and PNG evidence Cloud cannot produce.

3. **Skill surface is already paid for.** 77 generated skills + Project Aegis README safety rules + dual-tree routing in `Tech-Stack.md` means adoption is mostly **activate Editor session**, not invent tooling. Rejecting a dedicated `unity-mcp-operator` agent remains correct — this tree already owns the job.

4. **Low risk to Release invariants if bounded.** Package is Editor/dev tooling. Headless gates stay authoritative. DelegationBridge zero-touch is encoded in skill preambles. MCP need not touch sim tick paths to deliver value.

5. **Mil-sim C2 needs visual truth.** Dense UI Toolkit C2 is where “agent wrote C# but broke layout” happens. Screenshots + console + hierarchy reads are the strongest honest case for keeping IvanMurzak MCP rather than treating Unity as markdown-only.

---

## 3. Boundaries — when NOT to use

| Prefer headless / avoid MCP | Why |
|-----------------------------|-----|
| Buildkite / CI merge gates | No Editor on hosted Linux agents |
| ReplayGolden / Baltic hash `17144800277401907079` | Determinism |
| `DelegationBridge` hotpath / ROE / policy | Zero-touch invariant |
| Cloud Agent default path | VM typically lacks Unity 6.3 |
| Sim `Tick()` / Hindsight in sim | Determinism rules |
| Unapproved `package-add` / URP / Input System | Project invariants |
| Claiming gates green from Editor UTF alone | Prefer headless PlayModeSmoke |

Canonical split: ProjectAegis `.claude/README.md` — **Headless** for CI/determinism/delegation; **Editor MCP** for scenes/UI/assets/visual smoke/profiler.

See also: [`linux-vs-windows-build-gap-analysis-2026-07-20.md`](./linux-vs-windows-build-gap-analysis-2026-07-20.md) — Linux headless is merge authority; Editor automation is still Windows-path friction-prone.

---

## 4. Strongest objections and rebuttals

| Objection | Rebuttal |
|-----------|----------|
| “Headless-first means MCP is optional theater.” | True for merge authority. False for the presentation gap the integration review calls high leverage. Optional ≠ useless. |
| “`:8080` has never been verified; sunk cost.” | Package + 77 skills + mcp.json are already sunk; remaining cost is one interactive login. |
| “MCP can mutate DelegationBridge / packages.” | Real if agents ignore README. Mitigate with safety rules + headless gates — process control, not uninstall. |
| “CLI/package version skew.” | Hygiene issue. Pin docs to manifest; verify CLI after Editor session. |
| “Linux Cloud / Buildkite can’t use it.” | Routing already expects local Editor evidence. Invest in the local lane; never sell MCP as a CI substitute. |
| “Skill regen wipes PROJECT-AEGIS blocks.” | Maintenance tax with a documented restore procedure — keep hygiene, don’t abandon the bridge. |

---

## 5. Readiness checklist

**Ready today**

- [x] OpenUPM scopes + direct dep `0.82.4` in `manifest.json`
- [x] `.cursor/mcp.json` / `.mcp.json` → `:8080`
- [x] 77 Editor skills + `unity/ProjectAegis/.claude/README.md`
- [x] Headless verification path without MCP

**Not ready**

- [ ] Unity Editor `6000.3.14f1` open on an interactive machine
- [ ] `unity-mcp-cli login` completed
- [ ] `http://localhost:8080` responds; Cursor `ai-game-developer` connected
- [ ] Live `ping` / `unity-tool-list` / `package-list`
- [ ] Doc sync so agents stop running obsolete `install-plugin` instructions

---

## 6. Recommended next actions

1. **P0 docs:** Rewrite `Claude-Agent-Setup.md` status table + checklist to match `manifest.json` / `Tech-Stack.md` (package installed; remaining = Editor + login + `:8080`). Refresh CLI version notes.
2. **P0 soften hedges:** ProjectAegis README + `unity-initial-setup` — stop “may not appear in dependencies”; remove stale Entities pins from setup skill preamble.
3. **Local activation session:** open Editor → resolve packages → login → verify `:8080` → `ping` → restore PROJECT-AEGIS blocks if skills regenerated.
4. **Keep CI policy:** Buildkite/Cloud never depend on Unity-MCP.
5. **Use MCP where the steelman is strongest:** C2 UXML/USS/scene hierarchy, Game View screenshots for local evidence — always re-verify with headless PlayModeSmoke before merge.

---

## 7. References

| Artifact | Path |
|----------|------|
| Package manifest | `unity/ProjectAegis/Packages/manifest.json` |
| Tech stack | `Tech-Stack.md` |
| Agent setup (needs sync) | `Game-Requirements/Claude-Agent-Setup.md` |
| Editor MCP conventions | `unity/ProjectAegis/.claude/README.md` |
| Unity integration review | `docs/reports/unity-integration-review-2026-07-07.md` |
| Skill recommendations | `docs/reports/tech-stack-agent-skill-recommendations-2026-07-08.md` |
| Linux vs Windows build gaps | `docs/reports/linux-vs-windows-build-gap-analysis-2026-07-20.md` |
| Local/cloud routing | `production/agentic/local-cloud-agent-routing.md` |

---

*Steelman framing 2026-07-12; evidence refreshed 2026-07-23 against disk (`manifest.json` 0.82.4, 77 Editor skills, `:8080` still pending).*
