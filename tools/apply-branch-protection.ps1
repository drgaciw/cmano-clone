# Apply branch protection for main (GitHub Pro, Team, or public repo required).
# See docs/engineering/ci-and-branch-protection.md

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$config = Join-Path $repoRoot '.github\branch-protection.main.json'

if (-not (Test-Path $config)) {
    Write-Error "Missing $config"
}

$remote = git -C $repoRoot remote get-url origin 2>$null
if (-not $remote) {
    Write-Error 'No git remote origin'
}

if ($remote -match 'github\.com[:/](.+?)/(.+?)(?:\.git)?$') {
    $owner = $Matches[1]
    $repo = $Matches[2] -replace '\.git$', ''
}
else {
    Write-Error "Cannot parse GitHub owner/repo from: $remote"
}

Write-Host "Applying branch protection to ${owner}/${repo} branch main ..."
gh api "repos/$owner/$repo/branches/main/protection" -X PUT --input $config
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'Branch protection API failed (common on private free repos — HTTP 403).'
    Write-Host 'Enable checks manually: docs/engineering/ci-and-branch-protection.md'
    exit $LASTEXITCODE
}
Write-Host 'Done. Verify in GitHub → Settings → Branches → main.'