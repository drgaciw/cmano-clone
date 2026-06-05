#Requires -Version 5.1
<#
.SYNOPSIS
  Install or refresh obra/superpowers globally for Cursor, Grok, and Claude Code.

.DESCRIPTION
  - Claude Code: uses `claude plugin marketplace add` + `claude plugin install`
  - Cursor / .agents / Grok: junctions each skill under the Claude plugin cache
    into ~/.cursor/skills, ~/.agents/skills, ~/.grok/skills

  Re-run after `claude plugin update superpowers@superpowers-marketplace`.

  Source: https://github.com/obra/superpowers (MIT)
#>
param(
    [switch]$SkipClaudePlugin,
    [switch]$SkillsOnly,
    [switch]$SkipProjectSkills
)

$ErrorActionPreference = 'Stop'
$Version = '5.1.0'

function Get-SuperpowersRoot {
    $cache = Join-Path $env:USERPROFILE '.claude\plugins\cache\superpowers-marketplace\superpowers'
    $versioned = Join-Path $cache $Version
    if (Test-Path $versioned) { return $versioned }
    $dirs = @(Get-ChildItem $cache -Directory -ErrorAction SilentlyContinue)
    if ($dirs.Count -eq 1) { return $dirs[0].FullName }
    throw "Superpowers not found under $cache. Run without -SkillsOnly first."
}

function Install-SkillJunctions {
    param(
        [string]$Root,
        [string[]]$TargetBases
    )
    $skillRoot = Join-Path $Root 'skills'
    if (-not (Test-Path $skillRoot)) { throw "Missing skills folder: $skillRoot" }

    foreach ($base in $TargetBases) {
        if (-not (Test-Path $base)) {
            New-Item -ItemType Directory -Path $base -Force | Out-Null
        }
    }

    foreach ($skillDir in Get-ChildItem $skillRoot -Directory) {
        foreach ($base in $TargetBases) {
            $link = Join-Path $base $skillDir.Name
            if (Test-Path $link) {
                $item = Get-Item $link -Force
                if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                    Remove-Item $link -Force
                }
            }
            cmd /c "mklink /J `"$link`" `"$($skillDir.FullName)`"" | Out-Null
        }
    }
}

if (-not $SkillsOnly -and -not $SkipClaudePlugin) {
    if (-not (Get-Command claude -ErrorAction SilentlyContinue)) {
        Write-Warning 'claude CLI not found; skipping plugin install. Use -SkillsOnly after installing Claude Code.'
    }
    else {
        claude plugin marketplace add obra/superpowers-marketplace 2>$null
        claude plugin install superpowers@superpowers-marketplace
        claude plugin enable superpowers@superpowers-marketplace 2>$null
    }
}

$root = Get-SuperpowersRoot
$globalBases = @(
    (Join-Path $env:USERPROFILE '.cursor\skills'),
    (Join-Path $env:USERPROFILE '.agents\skills'),
    (Join-Path $env:USERPROFILE '.grok\skills')
)
Install-SkillJunctions -Root $root -TargetBases $globalBases

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectSkillRoot = Join-Path $repoRoot '.claude\skills\superpowers'
if (-not $SkipProjectSkills) {
    Install-SkillJunctions -Root $root -TargetBases @($projectSkillRoot)
}

Write-Host "Superpowers $Version linked from $root"
Write-Host "  Cursor:  $env:USERPROFILE\.cursor\skills\<skill>"
Write-Host "  Agents:  $env:USERPROFILE\.agents\skills\<skill>"
Write-Host "  Grok:    $env:USERPROFILE\.grok\skills\<skill>"
if (-not $SkipProjectSkills) {
    Write-Host "  Project: $projectSkillRoot\<skill> (junctions; gitignored)"
}
Write-Host "Cursor marketplace (optional): /add-plugin superpowers"