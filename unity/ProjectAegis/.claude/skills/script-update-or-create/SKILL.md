---
name: script-update-or-create
description: "Write a `.cs` script file (create or overwrite) with the provided C# code. Validates syntax via Roslyn before write — invalid code is rejected with error details and the file is left untouched. Refreshes the AssetDatabase and delivers the final result via `requestId` after Unity finishes the triggered compilation. Use 'script-read' to inspect existing content first. Project Aegis: never edit DelegationBridge hotpath; Unity plugins netstandard2.1."
---

# Script / Update or Create

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.

- **When to use:** Create/update Editor/runtime scripts under `Assets/` (UI hosts, presentation).
- **When not:** Do **not** rewrite `DelegationBridge` or sim hotpath; do not target headless `net8.0` assemblies via this tool.
- New Unity plugin code must stay **netstandard2.1**-compatible; refresh plugin DLLs with `./tools/copy-delegation-assemblies.ps1` when bridging headless builds.
<!-- PROJECT-AEGIS:END -->


Updates or creates script file with the provided C# code. Does AssetDatabase.Refresh() at the end. Provides compilation error details if the code has syntax errors. Use 'script-read' tool to read existing script files first.

## Inputs

- `filePath` — required `.cs` path.
- `content` — C# source. MUST pass `ScriptUtils.IsValidCSharpSyntax`.
- `requestId` — required for the processing/delivered-later contract.

## Behavior

Creates any missing parent directories, writes the file, then calls `AssetDatabase.Refresh` and schedules a post-compilation notification so the final response is delivered after Unity finishes the recompile.

## How to Call

```bash
unity-mcp-cli run-tool script-update-or-create --input '{
  "filePath": "string_value",
  "content": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool script-update-or-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool script-update-or-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | The path to the file. Sample: "Assets/Scripts/MyScript.cs". |
| `content` | `string` | Yes | C# code - content of the file. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filePath": {
      "type": "string"
    },
    "content": {
      "type": "string"
    }
  },
  "required": [
    "filePath",
    "content"
  ]
}
```

## Output

This tool does not return structured output.

