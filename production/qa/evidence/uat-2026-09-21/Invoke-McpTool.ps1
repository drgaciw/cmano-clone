param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$ArgumentsJson = '{}',
    [string]$OutFile = ''
)

$ErrorActionPreference = 'Stop'
$initFile = Join-Path $env:TEMP 'mcp-uat-init-req.json'
[System.IO.File]::WriteAllText($initFile, '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"uat","version":"1.0"}}}')
$headerFile = Join-Path $env:TEMP 'mcp-uat-headers.txt'
$bodyFile = Join-Path $env:TEMP 'mcp-uat-init.txt'
curl.exe -sS -D $headerFile -o $bodyFile -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' --max-time 20 --data-binary "@$initFile" 'http://localhost:8080/mcp' | Out-Null
$hdr = Get-Content $headerFile -Raw
$sid = ([regex]::Match($hdr, 'Mcp-Session-Id:\s*(\S+)')).Groups[1].Value
if (-not $sid) { throw 'No MCP session id' }

$sseOut = Join-Path $env:TEMP 'mcp-uat-sse.txt'
$sse = Start-Process -FilePath curl.exe -ArgumentList @(
    '-sS','-N','-H','Accept: text/event-stream','-H',"Mcp-Session-Id: $sid",'--max-time','180','http://localhost:8080/mcp'
) -PassThru -RedirectStandardOutput $sseOut -WindowStyle Hidden
Start-Sleep -Milliseconds 400
try {
    $noteFile = Join-Path $env:TEMP 'mcp-uat-note.json'
    [System.IO.File]::WriteAllText($noteFile, '{"jsonrpc":"2.0","method":"notifications/initialized"}')
    curl.exe -sS -o NUL -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' -H "Mcp-Session-Id: $sid" --max-time 15 --data-binary "@$noteFile" 'http://localhost:8080/mcp' | Out-Null
    if ($Name -notmatch '^[A-Za-z0-9-]+$') { throw "Unexpected tool name: $Name" }
    $callFile = Join-Path $env:TEMP 'mcp-uat-call.json'
    $callJson = '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"' + $Name + '","arguments":' + $ArgumentsJson + '}}'
    [System.IO.File]::WriteAllText($callFile, $callJson)
    $out = if ($OutFile) { $OutFile } else { Join-Path $env:TEMP 'mcp-uat-result.txt' }
    curl.exe -sS -o $out -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' -H "Mcp-Session-Id: $sid" --max-time 120 --data-binary "@$callFile" 'http://localhost:8080/mcp' | Out-Null
    if (-not $OutFile) { Get-Content $out -Raw }
}
finally {
    if ($sse -and -not $sse.HasExited) { Stop-Process -Id $sse.Id -Force -ErrorAction SilentlyContinue }
}
