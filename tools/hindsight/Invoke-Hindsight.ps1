#requires -Version 5.1
<#
.SYNOPSIS
  Thin REST client for local Hindsight (retain / recall / reflect).

.EXAMPLE
  .\Invoke-Hindsight.ps1 -Operation retain -BankId dev-cmano-clone -Content "Fixed tests"
.EXAMPLE
  .\Invoke-Hindsight.ps1 -Operation recall -BankId dev-cmano-clone -Query "Hindsight integration"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('retain', 'recall', 'reflect')]
    [string] $Operation,

    [Parameter(Mandatory = $true)]
    [string] $BankId,

    [string] $Content,
    [string] $Query,
    [string] $BaseUrl = 'http://localhost:8888',
    [string] $ApiKey
)

$ErrorActionPreference = 'Stop'
$bankEscaped = [Uri]::EscapeDataString($BankId)

function Invoke-HindsightRequest {
    param([string] $Path, [object] $Body)
    $uri = ($BaseUrl.TrimEnd('/')) + $Path
    $headers = @{ Accept = 'application/json' }
    if ($ApiKey) { $headers['Authorization'] = "Bearer $ApiKey" }
    $json = $Body | ConvertTo-Json -Depth 6 -Compress
    return Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -Body $json -ContentType 'application/json'
}

switch ($Operation) {
    'retain' {
        if (-not $Content) { throw 'retain requires -Content' }
        $path = "/v1/default/banks/$bankEscaped/memories/retain"
        $body = @{
            items = @(@{ content = $Content; context = 'agentic-dev' })
            async = $true
        }
        Invoke-HindsightRequest -Path $path -Body $body | ConvertTo-Json -Depth 4
    }
    'recall' {
        if (-not $Query) { throw 'recall requires -Query' }
        $path = "/v1/default/banks/$bankEscaped/memories/recall"
        $body = @{ query = $Query; budget = 'mid'; maxTokens = 4096 }
        $resp = Invoke-HindsightRequest -Path $path -Body $body
        if ($resp.results) {
            $resp.results | ForEach-Object { $_.text }
        }
        else {
            $resp | ConvertTo-Json -Depth 6
        }
    }
    'reflect' {
        if (-not $Query) { throw 'reflect requires -Query' }
        $path = "/v1/default/banks/$bankEscaped/reflect"
        $body = @{ query = $Query; budget = 'mid'; includeFacts = $true }
        $resp = Invoke-HindsightRequest -Path $path -Body $body
        if ($resp.text) { $resp.text } else { $resp | ConvertTo-Json -Depth 6 }
    }
}
