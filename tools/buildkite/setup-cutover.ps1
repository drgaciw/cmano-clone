# Buildkite CI cutover helper - docs/engineering/buildkite-ci.md
param(
    [switch]$SkipVerify,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'
# gh writes to stderr on 403; avoid terminating before branch-protection handling
$prevEap = $ErrorActionPreference
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$owner = 'drgaciw'
$repo = 'cmano-clone'
$pipelineSlug = 'cmano-clone'
$requiredCheck = "buildkite/$pipelineSlug"

Write-Host '=== Buildkite CI cutover ===' -ForegroundColor Cyan
Write-Host "Repo: $owner/$repo"
Write-Host "Required GitHub check: $requiredCheck"
Write-Host ''

if (-not $SkipVerify) {
    Write-Host '[1/6] Local CI parity...' -ForegroundColor Yellow
    & (Join-Path $repoRoot 'tools\verify-ci-local.ps1')
    Write-Host ''
} else {
    Write-Host '[1/6] Local CI parity skipped (-SkipVerify)' -ForegroundColor DarkYellow
    Write-Host ''
}

$pipelinePath = Join-Path $repoRoot '.buildkite\pipeline.yml'
if (-not (Test-Path $pipelinePath)) {
    Write-Error "Missing $pipelinePath"
}
Write-Host '[2/6] Pipeline file OK: .buildkite/pipeline.yml' -ForegroundColor Green
Write-Host ''

Write-Host '[3/6] Buildkite CLI status...' -ForegroundColor Yellow
$bk = Get-Command bk -ErrorAction SilentlyContinue
if (-not $bk) {
    Write-Host '  bk not on PATH. Install: winget install Buildkite.CLI'
} elseif ($env:BUILDKITE_API_TOKEN) {
    Write-Host "  bk found: $($bk.Source)"
    Write-Host '  BUILDKITE_API_TOKEN is set - listing pipelines...'
    bk pipelines list 2>&1
} else {
    Write-Host "  bk found: $($bk.Source)"
    Write-Host '  Set BUILDKITE_API_TOKEN for API automation (Buildkite Personal Settings -> API Access Tokens)'
}
Write-Host ''

Write-Host '[4/6] Graphite CI optimizer token...' -ForegroundColor Yellow
$envExample = Join-Path $repoRoot '.env.example'
if (Test-Path $envExample) {
    Write-Host "  Template: .env.example (copy to .env locally; gitignored)"
} else {
    Write-Host '  Missing .env.example — add GRAPHITE_CI_OPTIMIZER_TOKEN= placeholder'
}
gh secret list -R "$owner/$repo" 2>&1 | Select-String 'GRAPHITE_CI_OPTIMIZER_TOKEN' | Out-Null
Write-Host '  Paste the token into Buildkite pipeline Environment (Buildkite UI, not the repo).'
Write-Host ''

Write-Host '[5/6] Branch protection...' -ForegroundColor Yellow
$ErrorActionPreference = 'Continue'
$protection = gh api "repos/$owner/$repo/branches/main/protection" 2>&1
$protectionExit = $LASTEXITCODE
$ErrorActionPreference = $prevEap
if ($protectionExit -ne 0) {
    Write-Host '  API 403 - manual UI required (private free repo).'
    Write-Host "  GitHub Settings -> Branches -> main -> require: $requiredCheck"
    Write-Host '  Remove legacy checks: build_test, build'
} else {
    Write-Host $protection
}
Write-Host ''

if (-not $NoBrowser) {
    Write-Host '[6/6] Opening setup URLs in browser...' -ForegroundColor Yellow
    $urls = @(
        'https://buildkite.com/organizations/-/pipelines/new',
        'https://app.graphite.com/settings/ci',
        "https://github.com/$owner/$repo/settings/branches",
        "https://github.com/$owner/$repo/settings/secrets/actions"
    )
    foreach ($url in $urls) { Start-Process $url }
} else {
    Write-Host '[6/6] Browser launch skipped (-NoBrowser)' -ForegroundColor DarkYellow
}

Write-Host ''
Write-Host '=== Cutover checklist (Buildkite UI) ===' -ForegroundColor Cyan
Write-Host '  1. New pipeline slug: cmano-clone - connect drgaciw/cmano-clone'
Write-Host '  2. Read pipeline from repo: .buildkite/pipeline.yml'
Write-Host '  3. Build PRs + main; Skip builds with existing commits: ON'
Write-Host '  4. Environment: GRAPHITE_CI_OPTIMIZER_TOKEN (from .env / Graphite CI settings)'
Write-Host '  5. Graphite CI Optimizations -> Add new for this repo'
Write-Host "  6. Branch protection: require $requiredCheck"
Write-Host ''
Write-Host 'Done. See docs/engineering/buildkite-ci.md' -ForegroundColor Green
