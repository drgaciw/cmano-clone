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
- **MCP:** `.cursor/mcp.json` / `.mcp.json` → `"type": "http"`, `http://localhost:8080` (`ai-game-developer`). Package **0.86.0** is installed; still must **pin Custom + `:8080`** (`./tools/pin-unity-mcp-8080.sh` or **Project Aegis → MCP → Pin Local Host :8080**) then open Editor.
- **Packages (do not invent):** Burst 1.8.29, UI Toolkit 2.0.0, Addressables 2.3.16, Unity-MCP 0.86.0. **No URP/HDRP/Input System** — Built-in Forward + legacy Input Manager. (Entities packages removed — managed/headless-first world state.)
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

Prefer a CLI that matches the package (**0.86.x**). Use `npx` (no global install required):

```bash
npx --yes unity-mcp-cli@0.86.0 --version
```

Optional global install:

```bash
npm install -g unity-mcp-cli@0.86.0
unity-mcp-cli --version
```

> Permission errors: `npm config set prefix ~/.npm-global` and add `~/.npm-global/bin` to `PATH`, or use `npx`.

---

## Project Aegis — MCP activation (from repo root)

Paths below assume cwd = `cmano-clone/` (repo root).

### 1. Pin local Custom + `:8080` (required every fresh clone)

Unity-MCP ≥0.86 defaults to **Cloud** + a path-hashed port in **20000–29999**. Project clients stay on `:8080`:

```bash
./tools/pin-unity-mcp-8080.sh
# Windows: .\tools\pin-unity-mcp-8080.ps1
# Or Editor menu: Project Aegis → MCP → Pin Local Host :8080
```

Writes gitignored `UserSettings/AI-Game-Developer-Config.json` (`connectionMode=Custom`, `host=http://localhost:8080`, keep flags true, `authOption=none`).

### 2. Open Unity 6.3 LTS and let UPM resolve

```bash
npx --yes unity-mcp-cli@0.86.0 open ./unity/ProjectAegis
```

Or open via Unity Hub → editor `6000.3.14f1`. Confirm **Window → AI Game Developer** shows Custom / `:8080`.

Package `com.ivanmurzak.unity.mcp` **0.86.0** is already a direct dependency — do **not** re-run `install-plugin` unless refreshing deliberately.

### 3. Authenticate (optional — Cloud / ai-game.dev relay only)

```bash
npx --yes unity-mcp-cli@0.86.0 login ./unity/ProjectAegis
```

Local `:8080` pin uses `authOption=none`; login is not required for that path.

### 4. MCP config for agents (usually already present)

Committed configs already point at `:8080` with `"type": "http"`. Prefer **not** regenerating with bare `setup-mcp` (0.86 may emit hashed `/mcp/p/<pin>` URLs and clobber sibling servers). If you must:

```bash
npx --yes unity-mcp-cli@0.86.0 setup-mcp cursor ./unity/ProjectAegis \
  --transport http --url http://localhost:8080 --no-pin
npx --yes unity-mcp-cli@0.86.0 setup-mcp --list
```

Expected: `ai-game-developer` → `{ "type": "http", "url": "http://localhost:8080" }` in `.cursor/mcp.json` / `.mcp.json`.

### 5. Generate / refresh AI skills (optional, destructive)

```bash
npx --yes unity-mcp-cli@0.86.0 setup-skills cursor ./unity/ProjectAegis
# Editor must be running with plugin connected
```

Or from Editor MCP: `unity-skill-generate`.

> **Regen wipes** custom text in `skills/*/SKILL.md`. Keep [`../../README.md`](../../README.md) and restore every `<!-- PROJECT-AEGIS:BEGIN -->` … `<!-- PROJECT-AEGIS:END -->` block afterward (or restore from git).

### 6. Verify MCP HTTP

With Editor running and plugin connected after the pin:

```bash
curl -sS -o /dev/null -w "%{http_code}\n" --max-time 5 http://localhost:8080
# or: npx unity-mcp-cli@0.86.0 status ./unity/ProjectAegis
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
| `./tools/pin-unity-mcp-8080.sh` | Pin Custom + `http://localhost:8080` into UserSettings |
| `npx unity-mcp-cli@0.86.0 open ./unity/ProjectAegis` | Open project in Editor |
| `npx unity-mcp-cli@0.86.0 status ./unity/ProjectAegis` | Check Editor + MCP connection |
| `npx unity-mcp-cli@0.86.0 setup-mcp cursor … --url http://localhost:8080 --no-pin` | Rewrite agent MCP config (avoid bare setup-mcp) |
| `npx unity-mcp-cli@0.86.0 setup-skills cursor ./unity/ProjectAegis` | Generate skill files (regen risk) |
| `npx unity-mcp-cli@0.86.0 run-tool <tool> …` | Execute an MCP tool via HTTP API |
| `npx unity-mcp-cli@0.86.0 login ./unity/ProjectAegis` | Optional Cloud / ai-game.dev auth |
| `npx unity-mcp-cli@0.86.0 install-plugin ./unity/ProjectAegis` | Install/refresh plugin (already at 0.86.0) |
| `npx unity-mcp-cli@0.86.0 remove-plugin ./unity/ProjectAegis` | Remove plugin (avoid unless approved) |

Add `--verbose` for diagnostics. Do **not** use `create-project` against this repo — the Unity project already exists.

---

## Troubleshooting

- **`npm` not found**: Install Node.js and restart the shell.
- **`:8080` down / connection refused**: (1) run `./tools/pin-unity-mcp-8080.sh`, (2) open Editor, (3) confirm Window → AI Game Developer is Custom/`http://localhost:8080` with keep-server on. Without the pin, 0.86 binds a hashed 20000–29999 port instead.
- **Cloud mode / wrong port**: package default — re-run the pin script or Editor menu; do not rewrite committed mcp.json to a machine-specific hashed port.
- **Bare `setup-mcp cursor` clobbered servers**: restore `.cursor/mcp.json` from git; re-apply with `--url http://localhost:8080 --no-pin` only if needed.
- **Skills generation fails**: Editor must be running with MCP connected before `setup-skills` / `unity-skill-generate`.
- **Delegation / C2 failures**: Prefer headless PlayModeSmokeHarness before deep Editor debugging; never “fix” via `DelegationBridge` edits.
- **Wrong render/input assumptions**: This project is **not** URP and **not** the new Input System.
