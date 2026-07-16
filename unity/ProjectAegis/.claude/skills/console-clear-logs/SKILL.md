---
name: console-clear-logs
description: Clear the MCP log cache (used by 'console-get-logs') and the Unity Editor Console window. Useful for isolating logs to a specific action by clearing the slate first.
---

# Console / Clear Logs

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.
<!-- PROJECT-AEGIS:END -->

Clears the MCP log cache (used by console-get-logs) and the Unity Editor Console window. Useful for isolating errors related to a specific action by clearing logs before performing the action.

## Behavior

Calls `Debug.ClearDeveloperConsole()` to wipe the Editor Console, then clears the MCP-side `LogCollector` cache so subsequent 'console-get-logs' calls only see new entries.

## How to Call

```bash
unity-mcp-cli run-tool console-clear-logs --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool console-clear-logs --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool console-clear-logs --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `nothing` | `string` | No |  |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "nothing": {
      "type": "string"
    }
  }
}
```

## Output

This tool does not return structured output.

