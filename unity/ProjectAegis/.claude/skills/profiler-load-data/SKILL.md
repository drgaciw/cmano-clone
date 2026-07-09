---
name: profiler-load-data
description: Read back a previously-saved JSON snapshot from `profiler-save-data` and return its raw text.
---

# Profiler / Load Data

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.
<!-- PROJECT-AEGIS:END -->


Reads `filePath` as UTF-8 text and returns the file body unchanged. Caller is responsible for parsing.

## Inputs

- `filePath` (required) — path written by `profiler-save-data`.

## Errors

- Returns `[Error]` when `filePath` is empty, the file does not exist, exceeds the 10 MB size cap, or the read fails.

## Behavior

Uses `System.IO.File.ReadAllText` (BCL) — no external Unity package is required. Files larger than 10 MB are rejected up-front to avoid OOM on a stray call.

## How to Call

```bash
unity-mcp-cli run-tool profiler-load-data --input '{
  "filePath": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-load-data --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-load-data --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | Path to a profiler snapshot file previously written by 'profiler-save-data'. |

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

