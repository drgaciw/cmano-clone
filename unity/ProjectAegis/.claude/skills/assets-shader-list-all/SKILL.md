---
name: assets-shader-list-all
description: List all shaders available in the project assets and packages, sorted by name. Use this to discover a valid `shaderName` for 'assets-material-create'.
---

# Assets / List Shaders

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.

- Expect Built-in / project shaders; do not assume URP Lit.
<!-- PROJECT-AEGIS:END -->

List all available shaders in the project assets and packages. Returns their names. Use this to find a shader name for 'assets-material-create' tool.

## Behavior

Enumerates shaders via `ShaderUtils.GetAllShaders`, filters out nulls, and returns the names alphabetically sorted.

## How to Call

```bash
unity-mcp-cli run-tool assets-shader-list-all --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-shader-list-all --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-shader-list-all --input-file - <<'EOF'
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

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/System.String-1"
    }
  },
  "$defs": {
    "System.String-1": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

