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
    Write-Error "Hindsight unreachable at $BaseUrl — run tools/hindsight/start-hindsight-server.sh --detach or check firewall. $_"
    exit 1
}
