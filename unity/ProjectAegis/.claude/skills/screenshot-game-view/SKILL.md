---
name: screenshot-game-view
description: Capture a screenshot of the Unity Editor's Game View by reading its internal render texture directly. Image size matches the current Game View resolution; the tool corrects Y-flip on DirectX / Metal so the output is always upright. Requires an open Game View window.
---

# Screenshot / Game View

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.

- Useful for C2 visual evidence during Play Mode smoke; not a substitute for headless gates.
<!-- PROJECT-AEGIS:END -->


Captures a screenshot from the Unity Editor Game View and returns it as an image. Reads the Game View's own render texture directly via the Unity Editor API. The image size matches the current Game View resolution. Returns the image directly for visual inspection by the LLM.

## Behavior

Locates `UnityEditor.GameView`, repaints it, then reflects the `m_RenderTexture` field and reads back via `Texture2D.ReadPixels`. On graphics APIs whose UV origin is top-left (`SystemInfo.graphicsUVStartsAtTop`), the read-back pixels are vertically flipped before encoding so the orientation matches what the user sees. Returns a PNG image with the resolution baked into the caption.

## How to Call

```bash
unity-mcp-cli run-tool screenshot-game-view --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool screenshot-game-view --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool screenshot-game-view --input-file - <<'EOF'
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

