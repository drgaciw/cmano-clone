# Pin Unity-MCP (com.ivanmurzak.unity.mcp ≥0.86) to local Custom mode on :8080.
# Idempotent. Does not mint tokens. Matches Project Aegis client mcp.json convention.
[CmdletBinding()]
param(
    [string]$UnityProject = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($UnityProject)) {
    $UnityProject = Join-Path $Root "unity/ProjectAegis"
}

$UserSettings = Join-Path $UnityProject "UserSettings"
$Config = Join-Path $UserSettings "AI-Game-Developer-Config.json"
$HostUrl = "http://localhost:8080"

New-Item -ItemType Directory -Force -Path $UserSettings | Out-Null

$data = [ordered]@{}
if (Test-Path -LiteralPath $Config) {
    try {
        $parsed = Get-Content -LiteralPath $Config -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($null -ne $parsed) {
            foreach ($p in $parsed.PSObject.Properties) {
                $data[$p.Name] = $p.Value
            }
        }
    }
    catch {
        $data = [ordered]@{}
    }
}

$data["connectionMode"] = "Custom"
$data["host"] = $HostUrl
$data["keepServerRunning"] = $true
$data["keepConnected"] = $true
$data["authOption"] = "none"
if (-not $data.Contains("transportMethod")) { $data["transportMethod"] = "streamableHttp" }
if (-not $data.Contains("logLevel")) { $data["logLevel"] = "Warning" }
if (-not $data.Contains("timeoutMs")) { $data["timeoutMs"] = 10000 }

($data | ConvertTo-Json -Depth 8) + "`n" | Set-Content -LiteralPath $Config -Encoding UTF8 -NoNewline

Write-Host "Pinned Unity-MCP to Custom + $HostUrl"
Write-Host "Wrote $Config"
Write-Host ""
Write-Host "Next:"
Write-Host "  1. Open Unity Editor on $UnityProject (6000.3.22f1)"
Write-Host "  2. Confirm Window > AI Game Developer shows Custom / $HostUrl"
Write-Host "  3. Invoke-WebRequest -Uri $HostUrl -UseBasicParsing -TimeoutSec 5"
Write-Host "  4. Restart Cursor MCP if ai-game-developer was already loaded"
