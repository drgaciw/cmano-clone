---
name: profiler-get-memory-stats
description: Return memory statistics snapshot from UnityEngine.Profiling.Profiler — reserved, allocated, mono heap, graphics, etc. (in MB).
---

# Profiler / Get Memory Stats

<!-- PROJECT-AEGIS:BEGIN -->
### Project Aegis notes

- Conventions: [`../../README.md`](../../README.md) · stack: [`Tech-Stack.md`](../../../../../Tech-Stack.md) · smoke: [`PLAYMODE-SMOKE.md`](../../../PLAYMODE-SMOKE.md).
- Prefer **headless** `dotnet test` / PlayModeSmokeHarness for sim/delegation gates; use this Editor MCP tool for Editor-only work.
- **Zero-touch:** do not modify `DelegationBridge` hotpath. Unity plugins target **netstandard2.1** (`./tools/copy-delegation-assemblies.ps1`).
- **Not in project:** URP, HDRP, new Input System — Built-in Forward + legacy Input Manager. Do not invent MCP tools or packages.
<!-- PROJECT-AEGIS:END -->


Reads UnityEngine.Profiling.Profiler scalar memory counters and converts each from bytes to megabytes (`/1048576f`).

## Fields

- `TotalReservedMemoryMB` — `GetTotalReservedMemoryLong()`.
- `TotalAllocatedMemoryMB` — `GetTotalAllocatedMemoryLong()`.
- `TotalUnusedReservedMemoryMB` — `GetTotalUnusedReservedMemoryLong()`.
- `MonoHeapSizeMB` — `GetMonoHeapSizeLong()`.
- `MonoUsedSizeMB` — `GetMonoUsedSizeLong()`.
- `TempAllocatorSizeMB` — `GetTempAllocatorSize()`.
- `GraphicsMemoryMB` — `GetAllocatedMemoryForGraphicsDriver()`.
- `MaxUsedMemoryMB` — `maxUsedMemory`.
- `UsedHeapSizeMB` — `usedHeapSizeLong`.

## Behavior

Uses only built-in Unity APIs (`UnityEngine.Profiling.Profiler`). No external Unity package is required.

## How to Call

```bash
unity-mcp-cli run-tool profiler-get-memory-stats --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-get-memory-stats --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-get-memory-stats --input-file - <<'EOF'
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-MemoryStatsData",
      "description": "Memory statistics from the Unity Profiler. All values in megabytes unless otherwise noted."
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-MemoryStatsData": {
      "type": "object",
      "properties": {
        "TotalReservedMemoryMB": {
          "type": "number",
          "description": "Total reserved memory (UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong / 1048576)."
        },
        "TotalAllocatedMemoryMB": {
          "type": "number",
          "description": "Total allocated memory (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong / 1048576)."
        },
        "TotalUnusedReservedMemoryMB": {
          "type": "number",
          "description": "Total unused reserved memory (UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong / 1048576)."
        },
        "MonoHeapSizeMB": {
          "type": "number",
          "description": "Mono heap size (UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong / 1048576)."
        },
        "MonoUsedSizeMB": {
          "type": "number",
          "description": "Mono used size (UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong / 1048576)."
        },
        "TempAllocatorSizeMB": {
          "type": "number",
          "description": "Temp allocator size (UnityEngine.Profiling.Profiler.GetTempAllocatorSize / 1048576)."
        },
        "GraphicsMemoryMB": {
          "type": "number",
          "description": "Graphics memory reserved by the driver (UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver / 1048576)."
        },
        "MaxUsedMemoryMB": {
          "type": "number",
          "description": "Maximum used memory observed (UnityEngine.Profiling.Profiler.maxUsedMemory / 1048576)."
        },
        "UsedHeapSizeMB": {
          "type": "number",
          "description": "Used heap size (UnityEngine.Profiling.Profiler.usedHeapSizeLong / 1048576)."
        }
      },
      "required": [
        "TotalReservedMemoryMB",
        "TotalAllocatedMemoryMB",
        "TotalUnusedReservedMemoryMB",
        "MonoHeapSizeMB",
        "MonoUsedSizeMB",
        "TempAllocatorSizeMB",
        "GraphicsMemoryMB",
        "MaxUsedMemoryMB",
        "UsedHeapSizeMB"
      ],
      "description": "Memory statistics from the Unity Profiler. All values in megabytes unless otherwise noted."
    }
  },
  "required": [
    "result"
  ]
}
```

