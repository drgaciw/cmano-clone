---
name: package-remove
description: "Uninstall a UPM package from the Unity project. Modifies `manifest.json` and may trigger a domain reload — the final result is delivered after the reload via the request's `requestId`. Built-in packages and packages that are dependencies of others cannot be removed. Use 'package-list' to list installed packages first. Project Aegis: human approval required; do not remove pinned Entities/UI/Addressables/MCP."
---

# Package Manager / Remove

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.

- **When to use:** Explicitly approved removals only.
- **When not:** Do not remove Entities, Burst, Entities.Graphics, UI Toolkit, Addressables, or Unity-MCP without approval.
<!-- PROJECT-AEGIS:END -->


Remove (uninstall) a package from the Unity project. This removes the package from the project's manifest.json and triggers package resolution. Note: Built-in packages and packages that are dependencies of other installed packages cannot be removed. Note: Package removal may trigger a domain reload. The result will be sent after the reload completes. Use 'package-list' tool to list installed packages first.

## Inputs

- `packageId` — package name (e.g. `com.unity.textmeshpro`). A trailing `@<version>` is stripped automatically before being passed to `Client.Remove`, so accidental versioned IDs still work.

## Behavior

First verifies the package is installed via an offline `Client.List` — returns a clear `PackageNotFound` error if not. On removal failure, surfaces Unity's error message. On success, schedules a post-domain-reload notification so the response is delivered after Unity finishes the resolution.

## How to Call

```bash
unity-mcp-cli run-tool package-remove --input '{
  "packageId": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool package-remove --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool package-remove --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `packageId` | `string` | Yes | The ID of the package to remove. Example: 'com.unity.textmeshpro'. Do not include version number. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "packageId": {
      "type": "string"
    }
  },
  "required": [
    "packageId"
  ]
}
```

## Output

This tool does not return structured output.

