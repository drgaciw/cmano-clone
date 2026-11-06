<#
.SYNOPSIS
  Install the Jev-augmented Playwright MCP proxy into tools/mcp/jev-playwright-mcp/
  (gitignored local checkout). Idempotent: re-run to update to the pinned ref.

.EXAMPLE
  .\tools\mcp\install-jev-playwright-mcp.ps1
  .\tools\mcp\install-jev-playwright-mcp.ps1 -Ref main -SkipBrowser

.NOTES
  Registered as "jev-playwright" in .mcp.json and .cursor/mcp.json.
  Docs: docs/engineering/jev-playwright-mcp-setup.md
#>
[CmdletBinding()]
param(
    [string]$RepoUrl = $(if ($env:JEV_PLAYWRIGHT_MCP_REPO) { $env:JEV_PLAYWRIGHT_MCP_REPO } else { 'https://github.com/drgaciw/jev-playwright-mcp.git' }),
    [string]$Ref = $(if ($env:JEV_PLAYWRIGHT_MCP_REF) { $env:JEV_PLAYWRIGHT_MCP_REF } else { 'db7762fd2ef66af7b38362c98982692bc81f57b7' }),
    [switch]$SkipBrowser
)

$ErrorActionPreference = 'Stop'
$Dest = Join-Path $PSScriptRoot 'jev-playwright-mcp'

function Write-Step([string]$Message) { Write-Host "[install-jev-playwright-mcp] $Message" }
function Invoke-Checked([scriptblock]$Block, [string]$What) {
    & $Block
    if ($LASTEXITCODE -ne 0) { throw "$What failed (exit $LASTEXITCODE)" }
}

foreach ($tool in 'git', 'node', 'npm') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "$tool not found" }
}
$nodeMajor = [int](node -p 'process.versions.node.split(".")[0]')
if ($nodeMajor -lt 20) { throw "Node.js >= 20 required (found $(node --version))" }

if (Test-Path (Join-Path $Dest '.git')) {
    Write-Step "updating existing checkout at $Dest"
    Invoke-Checked { git -C $Dest fetch --quiet origin } 'git fetch'
} else {
    Write-Step "cloning $RepoUrl -> $Dest"
    Invoke-Checked { git clone --quiet $RepoUrl $Dest } 'git clone'
}

git -C $Dest rev-parse --verify --quiet "origin/$Ref" *> $null
$target = if ($LASTEXITCODE -eq 0) { "origin/$Ref" } else { $Ref }
Invoke-Checked { git -C $Dest checkout --quiet --detach $target } 'git checkout'
Write-Step "checked out $(git -C $Dest log -1 --format='%h %cs %s')"

Push-Location $Dest
try {
    Write-Step 'npm ci'
    Invoke-Checked { npm ci --no-audit --no-fund --loglevel=error } 'npm ci'
    Write-Step 'npm run build'
    Invoke-Checked { npm run --silent build } 'npm run build'
    if (-not $SkipBrowser) {
        Write-Step 'installing Playwright chromium (use -SkipBrowser to skip)'
        Invoke-Checked { npx --no-install playwright install chromium } 'playwright install chromium'
    }
} finally {
    Pop-Location
}

$envFile = Join-Path $Dest '.env'
if (-not (Test-Path $envFile)) {
    Copy-Item (Join-Path $Dest '.env.example') $envFile
    Write-Step "created $envFile (set TYPESAFE_API_KEY there to enable Jev; empty = passthrough)"
}

Write-Step 'done. Verify with: node tools/mcp/verify-jev-playwright-mcp.mjs'
