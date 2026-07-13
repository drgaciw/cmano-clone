---
name: profiler-save-data
description: Save a snapshot of profiler-derived stats (status + memory + rendering + script + frame capture) to a JSON file. Built-in Unity APIs only.
---

# Profiler / Save Data

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.
<!-- PROJECT-AEGIS:END -->


Composes the outputs of `profiler-get-status`, `profiler-get-memory-stats`, `profiler-get-rendering-stats`, `profiler-get-script-stats` and `profiler-capture-frame` into a single JSON document and writes it to `filePath`. Creates any missing parent directories.

## Inputs

- `filePath` (required) — absolute or workspace-relative path to write to.

## Errors

- Returns `[Error]` when `filePath` is empty or the write fails (message includes the underlying exception text).

## Behavior

Uses `System.Text.Json` (BCL) for serialization and `System.IO.File.WriteAllText` for the write. No external Unity package is required.

## How to Call

```bash
unity-mcp-cli run-tool profiler-save-data --input '{
  "filePath": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-save-data --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-save-data --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | Absolute or workspace-relative output file path. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filePath": {
      "type": "string"
    }
  },
  "required": [
    "filePath"
  ]
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

