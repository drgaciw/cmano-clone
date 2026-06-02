#requires -Version 5.1
<#
.SYNOPSIS
  Returns exit 0 if Hindsight API responds on localhost:8888.
#>
param(
    [string] $BaseUrl = 'http://localhost:8888'
)

$ErrorActionPreference = 'Stop'
try {
    $uri = $BaseUrl.TrimEnd('/') + '/v1/default/banks'
    Invoke-RestMethod -Method Get -Uri $uri -TimeoutSec 5 | Out-Null
    Write-Host "Hindsight OK: $BaseUrl"
    exit 0
}
catch {
    Write-Error "Hindsight unreachable at $BaseUrl — start Docker (see .claude/skills/hindsight/hindsight-local-setup/SKILL.md). $_"
    exit 1
}
