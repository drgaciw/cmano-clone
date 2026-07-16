# Project Aegis — Unity Editor MCP Skills

Agent conventions for skills under `unity/ProjectAegis/.claude/skills/`. These wrap **Unity-MCP** tools against a running Editor (`http://localhost:8080`).

> **Regen note:** `unity-skill-generate` rewrites each `skills/*/SKILL.md` from `[AiSkillDescription]` / `[AiSkillBody]`. Custom Project Aegis guidance lives here and in each skill’s `<!-- PROJECT-AEGIS:BEGIN -->` … `<!-- PROJECT-AEGIS:END -->` block. After regen, re-insert those blocks (or re-run the batch note from this README).

## Canonical references (repo root)

| Doc | Use |
|-----|-----|
| [`Tech-Stack.md`](../../../Tech-Stack.md) | Editor version, MCP status, dual toolchain |
| [`PLAYMODE-SMOKE.md`](../PLAYMODE-SMOKE.md) | Editor Play Mode smoke + headless gate |
| [`Game-Requirements/Claude-Agent-Setup.md`](../../../Game-Requirements/Claude-Agent-Setup.md) | Unity-MCP activate / login / `:8080` |
| [`AGENTS.md`](../../../AGENTS.md) | Headless verify, DelegationBridge zero-touch |

## Project constraints (do not invent)

- **Editor:** Unity **6.3 LTS** `6000.3.14f1` at `unity/ProjectAegis/`.
- **Packages (pinned):** Entities `1.4.6`, Burst `1.8.29`, Entities.Graphics `1.4.20`, UI Toolkit (`com.unity.ui`) `2.0.0`, Addressables `2.3.16`.
- **Not in this project:** URP, HDRP, new Input System — **Built-in Forward** + **legacy Input Manager**.
- **Dual toolchain:** headless `net8.0` + Unity plugins `netstandard2.1`. Copy plugin DLLs with `./tools/copy-delegation-assemblies.ps1` (guard: `./tools/Test-UnityPluginAssemblies.ps1`).
- **Seams:** `ISimWorldSnapshot` → `DelegationBridge.Tick` → `IOrderSink`. **`DelegationBridge` is zero-touch** through Release v1 — no hotpath edits.
- **Unity-MCP:** client → `localhost:8080`. Scoped registry for `com.ivanmurzak` is in `Packages/manifest.json`; **`com.ivanmurzak.unity.mcp` may not yet appear under `dependencies`** until `install-plugin` / Editor resolve. **`:8080` pending** until Editor open + `login`. Do not invent tools or claim packages are installed without `package-list`.

## When to use Editor MCP vs headless

| Prefer | When |
|--------|------|
| **Headless** (`dotnet test`, PlayModeSmokeHarness, ReplayGolden) | CI, determinism, delegation/sim logic, no Editor available |
| **Editor MCP** | Scene/UI Toolkit layout, prefabs, materials/shaders, visual smoke, AssetDatabase, Profiler in Editor |

Default verification (repo root):

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

## Safety rules for agents using these skills

1. **Do not** edit `DelegationBridge` / hotpath via `script-update-or-create`, `script-execute`, or reflection.
2. **Do not** add URP / HDRP / Input System / random packages without explicit human approval (`package-add` / `package-remove` are high-risk).
3. **Do not** invent MCP tools — discover with `unity-tool-list` / `ping` when Editor is up.
4. Prefer **EditMode** `tests-run` for fast Editor iteration; for C2/delegation gates prefer headless PlayModeSmoke (see `PLAYMODE-SMOKE.md`).
5. After external `.cs` / plugin DLL changes: `assets-refresh` and/or re-run `copy-delegation-assemblies.ps1`.
6. Collaboration: ask before multi-file Editor asset writes; no commits unless requested.

## Shared skill preamble (paste after H1)

Use this block inside each `SKILL.md` (survives manual review; wipe risk on `unity-skill-generate`):

```markdown
<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Read `unity/ProjectAegis/.claude/README.md` before Editor MCP work.
- Stack: [Tech-Stack.md](../../../../../Tech-Stack.md) · smoke: [PLAYMODE-SMOKE.md](../../../PLAYMODE-SMOKE.md).
- Prefer headless `dotnet test` for sim/delegation; Editor MCP for scenes/UI/assets.
- Zero-touch: `DelegationBridge`. Plugins: `netstandard2.1` via `./tools/copy-delegation-assemblies.ps1`.
- No URP / HDRP / Input System — Built-in Forward + legacy Input Manager.
<!-- PROJECT-AEGIS:END -->
```

Relative links assume `skills/<skill-name>/SKILL.md` → adjust only if skill depth changes.

## High-traffic skills (start here)

| Skill | Role |
|-------|------|
| `unity-initial-setup` | CLI + MCP bootstrap for this repo |
| `unity-tool-list` / `ping` | Discover tools / health |
| `tests-run` | Editor UTF — prefer headless for gates |
| `script-*` | C# under Assets — respect zero-touch + netstandard2.1 |
| `package-*` | Manifest changes — approval required |
| `scene-*` / `gameobject-*` | Scene hierarchy / C2 hosts |
| `editor-application-set-state` | Play Mode — pair with `PLAYMODE-SMOKE.md` |

## After `unity-skill-generate`

1. Keep this `README.md` (not overwritten by generate).
2. Re-apply `<!-- PROJECT-AEGIS:… -->` blocks to regenerated skills, or restore from git.
3. Re-check `unity-initial-setup` Project Aegis path (`./unity/ProjectAegis`, editor `6000.3.14f1`).
