param(
    [Parameter(Mandatory = $true)][string]$Action,
    [string]$OutName = 'uat-driver-out.txt'
)
$ErrorActionPreference = 'Stop'
$ev = 'c:\Users\dgorn\cmano-clone\cmano-clone\production\qa\evidence\uat-2026-09-21'
Set-Content -LiteralPath (Join-Path $ev 'uat-action.txt') -Value $Action -Encoding ascii -NoNewline
$code = Get-Content -LiteralPath (Join-Path $ev 'UatDriver.cs') -Raw
$escaped = $code.Replace('\','\\').Replace('"','\"').Replace("`r",'\r').Replace("`n",'\n').Replace("`t",'\t')
$payload = '{"csharpCode":"' + $escaped + '","className":"UatDriver","methodName":"Main"}'
& (Join-Path $ev 'Invoke-McpTool.ps1') -Name 'script-execute' -ArgumentsJson $payload -OutFile (Join-Path $ev $OutName)
Get-Content (Join-Path $ev $OutName) -Raw
