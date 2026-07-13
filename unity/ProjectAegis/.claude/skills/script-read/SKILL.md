---
name: script-read
description: Read a `.cs` script file and return its content as a string. Supports a 1-based `lineFrom`/`lineTo` slice for partial reads. Pair with 'script-update-or-create' to write back.
---

# Script / Read

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.

- **When to use:** Inspect scripts under `Assets/` via Editor.
- Prefer repo `Read` tool for files outside the open Unity project or when Editor is unavailable.
<!-- PROJECT-AEGIS:END -->


Reads the content of a script file and returns it as a string. Use 'script-update-or-create' tool to update or create script files.

## Inputs

- `filePath` — required `.cs` path. Throws if missing on disk.
- `lineFrom` (default 1, 1-based) — start line; clamped into valid range.
- `lineTo` (default -1 = end-of-file, 1-based) — inclusive end line; clamped into valid range.

## Behavior

Reads the file with `File.ReadAllLines` and slices `[lineFrom..lineTo]` (inclusive). The slice indices are clamped — passing out-of-range `lineFrom`/`lineTo` is forgiving (read returns at-most the whole file).

## How to Call

```bash
unity-mcp-cli run-tool script-read --input '{
  "filePath": "string_value",
  "lineFrom": 0,
  "lineTo": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool script-read --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool script-read --input-file - <<'EOF'
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
| `lineFrom` | `integer` | No | The line number to start reading from (1-based). |
| `lineTo` | `integer` | No | The line number to stop reading at (1-based, -1 for all lines). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filePath": {
      "type": "string"
    },
    "lineFrom": {
      "type": "integer"
    },
    "lineTo": {
      "type": "integer"
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

