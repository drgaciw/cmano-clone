---
name: profiler-enable-module
description: Toggle the wrapper's local 'enabled' flag for a named profiler module. Bookkeeping only — Unity's runtime API does not expose direct module control; for real module visibility use the Profiler window.
---

# Profiler / Enable Module

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.
<!-- PROJECT-AEGIS:END -->


Adds or removes the given module name from the wrapper's `EnabledModules` set. This is local bookkeeping consumed by `profiler-get-status` and `profiler-list-modules`; Unity's runtime API does not allow programmatic toggling of Profiler-window modules from a built-in namespace, so this tool intentionally does not pretend to.

## Inputs

- `moduleName` (required) — one of the names returned by `profiler-list-modules`.
- `enabled` (default `true`) — set to `false` to mark the module disabled.

## Errors

- Returns an `[Error]` string when `moduleName` is empty or unknown.

## How to Call

```bash
unity-mcp-cli run-tool profiler-enable-module --input '{
  "moduleName": "string_value",
  "enabled": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-enable-module --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-enable-module --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `moduleName` | `string` | Yes | Profiler module name (e.g. 'CPU', 'GPU', 'Memory'). |
| `enabled` | `boolean` | No | True to mark the module enabled in local bookkeeping; false to mark disabled. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "moduleName": {
      "type": "string"
    },
    "enabled": {
      "type": "boolean"
    }
  },
  "required": [
    "moduleName"
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

