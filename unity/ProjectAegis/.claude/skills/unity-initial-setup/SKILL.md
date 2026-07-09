---
name: unity-initial-setup
description: |-
  Project Aegis bootstrap for Unity-MCP: Node/`unity-mcp-cli`, Editor 6000.3.14f1 at
  unity/ProjectAegis, MCP on localhost:8080, skill regen caveats, and headless-vs-Editor
  verification. Use at session start when Editor MCP is needed — not for inventing packages.
---

# AI Game Developer — Initial Setup (Project Aegis)

Bootstrap **Unity-MCP** for this repo’s Unity project. Full agent conventions:
[`../../README.md`](../../README.md).

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- **Project path:** `unity/ProjectAegis/` (no spaces). **Editor:** Unity **6.3 LTS** `6000.3.14f1`.
- **Stack:** [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md) · activate: [`Claude-Agent-Setup.md`](../../../../../Game-Requirements/Claude-Agent-Setup.md).
- **MCP:** `.cursor/mcp.json` / `.mcp.json` → `http://localhost:8080` (`ai-game-developer`). Plugin may still be **pending** until Editor open + `login`.
- **Packages (do not invent):** Entities 1.4.6, Burst 1.8.29, Entities.Graphics 1.4.20, UI Toolkit 2.0.0, Addressables 2.3.16. **No URP/HDRP/Input System** — Built-in Forward + legacy Input Manager.
- **Dual toolchain:** headless `net8.0` + Unity plugins `netstandard2.1` via `./tools/copy-delegation-assemblies.ps1`.
- **Zero-touch:** `DelegationBridge` hotpath. Prefer headless `dotnet test` / PlayModeSmokeHarness for gates.
- **When to use this skill:** First-time or broken MCP/Editor agent setup.
- **When not:** Pure headless sim/delegation work with no Editor — skip MCP and use `AGENTS.md` verify commands.
<!-- PROJECT-AEGIS:END -->

---

## Prerequisites

### Install Node.js

`unity-mcp-cli` requires **Node.js ^20.19.0 || >=22.12.0** (Node 21.x is not supported).

```bash
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt-get install -y nodejs
node --version && npm --version
```

Or download from https://nodejs.org/.

### Unity Editor

Install **6000.3.14f1** via Unity Hub. Open `unity/ProjectAegis` only with that editor version.

### .NET (headless — always available)

SDK **8.0.400** (`global.json`). Headless verification does **not** require MCP:

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

---

## Install `unity-mcp-cli`

Repo already documents **v0.77.0**. Prefer `npx` (no global install required):

```bash
npx --yes unity-mcp-cli --version
```

Optional global install:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli --version
```

> Permission errors: `npm config set prefix ~/.npm-global` and add `~/.npm-global/bin` to `PATH`, or use `npx`.

---

## Project Aegis — MCP activation (from repo root)

Paths below assume cwd = `cmano-clone/` (repo root).

### 1. Install / refresh the Unity-MCP plugin

```bash
npx --yes unity-mcp-cli install-plugin ./unity/ProjectAegis
```

Scoped registry for `com.ivanmurzak` is already in `Packages/manifest.json`. Package may remain **pending** until the Editor resolves it on first open — do not claim it is active without `package-list` / a live `:8080`.

### 2. Open Unity 6.3 LTS and let UPM resolve

```bash
npx --yes unity-mcp-cli open ./unity/ProjectAegis
```

Or open via Unity Hub → editor `6000.3.14f1`.

### 3. Authenticate (interactive)

```bash
npx --yes unity-mcp-cli login ./unity/ProjectAegis
```

### 4. MCP config for agents (usually already present)

```bash
npx --yes unity-mcp-cli setup-mcp cursor ./unity/ProjectAegis
# or: setup-mcp claude-code ./unity/ProjectAegis
npx --yes unity-mcp-cli setup-mcp --list
```

Expected: `ai-game-developer` → `http://localhost:8080` in `.cursor/mcp.json` / `.mcp.json`.

### 5. Generate / refresh AI skills (optional, destructive)

```bash
npx --yes unity-mcp-cli setup-skills cursor ./unity/ProjectAegis
# Editor must be running with plugin connected
```

Or from Editor MCP: `unity-skill-generate`.

> **Regen wipes** custom text in `skills/*/SKILL.md`. Keep [`../../README.md`](../../README.md) and restore every `<!-- PROJECT-AEGIS:BEGIN -->` … `<!-- PROJECT-AEGIS:END -->` block afterward (or restore from git).

### 6. Verify MCP HTTP

With Editor running and plugin logged in:

```bash
curl -sS -o /dev/null -w "%{http_code}\n" --max-time 5 http://localhost:8080
# or: unity-mcp-cli run-tool ping --input '{}'
```

Then use skill `ping` / `unity-tool-list`.

### 7. Plugin assemblies (delegation bridge)

If Play Mode / compile complains about missing plugin DLLs:

```powershell
./tools/Test-UnityPluginAssemblies.ps1
./tools/copy-delegation-assemblies.ps1
```

Then `assets-refresh` in Editor if needed. See [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).

---

## Common Commands Reference

| Command | Description |
|---|---|
| `npx unity-mcp-cli install-plugin ./unity/ProjectAegis` | Install/refresh Unity-MCP plugin |
| `npx unity-mcp-cli remove-plugin ./unity/ProjectAegis` | Remove plugin (avoid unless approved) |
| `npx unity-mcp-cli configure ./unity/ProjectAegis --list` | List MCP configuration |
| `npx unity-mcp-cli setup-mcp <agent> ./unity/ProjectAegis` | Write MCP config for an AI agent |
| `npx unity-mcp-cli setup-skills <agent> ./unity/ProjectAegis` | Generate skill files (regen risk) |
| `npx unity-mcp-cli open ./unity/ProjectAegis` | Open project in Editor |
| `npx unity-mcp-cli run-tool <tool> …` | Execute an MCP tool via HTTP API |
| `npx unity-mcp-cli login ./unity/ProjectAegis` | Interactive plugin auth |

Add `--verbose` for diagnostics. Do **not** use `create-project` against this repo — the Unity project already exists.

---

## Troubleshooting

- **`npm` not found**: Install Node.js and restart the shell.
- **`:8080` down**: Editor not running, plugin not resolved, or `login` incomplete — see Claude-Agent-Setup.
- **Plugin not appearing**: After `install-plugin`, open the project once so UPM resolves; check `package-list`.
- **Skills generation fails**: Editor must be running with MCP connected before `setup-skills` / `unity-skill-generate`.
- **Delegation / C2 failures**: Prefer headless PlayModeSmokeHarness before deep Editor debugging; never “fix” via `DelegationBridge` edits.
- **Wrong render/input assumptions**: This project is **not** URP and **not** the new Input System.
