# Verify DRG-196 AGC-01..AGC-04 skill contract (TEST-SPEC.md).
# Contract docs plus headless ProjectAegis.Delegation.Skills types. No sim, no Unity, no forbidden worker files.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$repo = (Resolve-Path (Join-Path $root '..\..\..\..')).Path
Set-Location $repo

function Assert-True([bool]$ok, [string]$name) {
    if ($ok) {
        Write-Host "PASS  $name"
        return 0
    }
    Write-Host "FAIL  $name"
    return 1
}

$fail = 0
$catPath = Join-Path $root 'catalog.json'
$contract = Get-Content -Raw (Join-Path $root 'CONTRACT.md')
$schema = Get-Content -Raw (Join-Path $root 'envelopes\skill-envelope.schema.json')
$cat = Get-Content -Raw $catPath | ConvertFrom-Json
$track = Get-Content -Raw (Join-Path $repo 'production\docs\skills\c2-track-assessment\SKILL.md')
$link = Get-Content -Raw (Join-Path $repo 'production\docs\skills\c2-datalink-reasoning\SKILL.md')
$pair = Get-Content -Raw (Join-Path $repo 'production\docs\skills\c2-sensor-to-shooter-pairing\SKILL.md')
$explain = Get-Content -Raw (Join-Path $repo 'production\docs\skills\c2-explanation\SKILL.md')
$impl = Get-Content -Raw (Join-Path $repo '.claude\skills\agent-c2-skill-contract\SKILL.md')

$ids = @($cat.skills.skillId)
$fail += Assert-True ($ids.Count -eq 4) 'catalog has four Slice A skills'
$fail += Assert-True ($ids -contains 'c2.track.assess') 'catalog includes c2.track.assess'
$fail += Assert-True ($ids -contains 'c2.datalink.reason') 'catalog includes c2.datalink.reason'
$fail += Assert-True ($ids -contains 'c2.pairing.recommend') 'catalog includes c2.pairing.recommend'
$fail += Assert-True ($ids -contains 'c2.explain') 'catalog includes c2.explain'
$submitOnSlice = $false
foreach ($s in $cat.skills) {
    if ($s.lanes -contains 'submit') { $submitOnSlice = $true }
}
$fail += Assert-True (-not $submitOnSlice) 'no Slice A skill lists submit'
$fail += Assert-True ($cat.submitVerb.skillId -eq 'c2.skill.submit') 'submit verb is c2.skill.submit'
$fail += Assert-True ($schema -match 'authorityBasis') 'schema defines authorityBasis'
$fail += Assert-True ($schema -match 'playerOverride') 'schema defines playerOverride'
$fail += Assert-True ($schema -match 'replayProvenance') 'schema defines replayProvenance'
$fail += Assert-True ($schema -match '"engagementAuthorizationImplied": \{ "const": false \}') 'propose forces engagementAuthorizationImplied false'
$fail += Assert-True ($schema -match 'ttlTicks') 'schema defines ttlTicks'
$fail += Assert-True ($contract -match 'C2CommandIssuance') 'CONTRACT names C2CommandIssuance'
$fail += Assert-True ($contract -match 'C2PlayerCommandBridge') 'CONTRACT names C2PlayerCommandBridge'
$fail += Assert-True ($contract -match 'IPolicyEvaluator') 'CONTRACT names IPolicyEvaluator'
$fail += Assert-True ($contract -match 'ADR-018') 'CONTRACT names ADR-018'
$fail += Assert-True ($contract -match 'state `approved`') 'submit requires approved'
$fail += Assert-True ($contract -match 'PROPOSAL_NOT_APPROVED') 'CONTRACT lists PROPOSAL_NOT_APPROVED'

$fail += Assert-True ($track -match 'ContactPictureProjection') 'track skill reads ContactPictureProjection'
$fail += Assert-True ($track -match 'SensorC2Projection') 'track skill reads SensorC2Projection'
$fail += Assert-True ($track -match 'Not `engage`') 'track propose does not allow engage'
$fail += Assert-True ($track -match 'Do not append') 'track read forbids order-log append'
$fail += Assert-True ($pair -match 'trackSource: "organic"') 'pairing engage requires organic'
$fail += Assert-True ($pair -match 'fireControlSatisfied: true') 'pairing engage requires FC'
$fail += Assert-True ($link -match 'Never `engage`') 'datalink never engage'
$fail += Assert-True ($explain -match '\*\*Lanes:\*\* `read` only') 'explain is read only'
$fail += Assert-True ($explain -match 'Do not paraphrase a known abort') 'explain does not rewrite abort codes'
$fail += Assert-True ($impl -match 'allowed-tools: Read, Grep, Glob') 'implementer skill is read-only tools'
$fail += Assert-True ($impl -match 'DelegationBridge') 'implementer names DelegationBridge'
$fail += Assert-True ($impl -match 'CatalogWriteGate') 'implementer names CatalogWriteGate'
$fail += Assert-True ($impl -match 'SimulationSession') 'implementer names SimulationSession'
$fail += Assert-True ($impl -match 'BalticReplayHarness') 'implementer names BalticReplayHarness'
$fail += Assert-True ($impl -match 'MissionContactTargetClass') 'implementer names MissionContactTargetClass'
$fail += Assert-True ($impl -match 'DRG-179') 'implementer names DRG-179'
$fail += Assert-True ($impl -match '## Next step') 'implementer has next-step handoff'

$examples = @(
    'envelopes\examples\read-track.json',
    'envelopes\examples\propose-pairing.json',
    'envelopes\examples\submit-engage.json',
    'envelopes\examples\fail-shared-track-engage.json'
)
foreach ($rel in $examples) {
    $p = Join-Path $root $rel
    try {
        $obj = Get-Content -Raw $p | ConvertFrom-Json
        $fail += Assert-True ($true) "json parses $rel"
    } catch {
        $fail += Assert-True ($false) "json parses $rel"
        continue
    }
}

$read = Get-Content -Raw (Join-Path $root 'envelopes\examples\read-track.json') | ConvertFrom-Json
$fail += Assert-True ($read.lane -eq 'read') 'read example lane=read'
$fail += Assert-True ($null -eq $read.commandId) 'read example commandId is null'
$fail += Assert-True (-not $read.replayProvenance.submitted) 'read example not submitted'

$prop = Get-Content -Raw (Join-Path $root 'envelopes\examples\propose-pairing.json') | ConvertFrom-Json
$fail += Assert-True ($prop.lane -eq 'propose') 'propose example lane=propose'
$fail += Assert-True ($prop.authorityBasis.engagementAuthorizationImplied -eq $false) 'propose example does not imply authorization'
$fail += Assert-True ($prop.authorityBasis.trackSource -eq 'organic') 'propose example organic track'
$fail += Assert-True ($prop.ttlTicks -eq 30) 'propose example ttlTicks=30'

$sub = Get-Content -Raw (Join-Path $root 'envelopes\examples\submit-engage.json') | ConvertFrom-Json
$fail += Assert-True ($sub.skillId -eq 'c2.skill.submit') 'submit example skillId'
$fail += Assert-True ($sub.replayProvenance.submitted -eq $true) 'submit example submitted=true'

$bad = Get-Content -Raw (Join-Path $root 'envelopes\examples\fail-shared-track-engage.json') | ConvertFrom-Json
$illegal = ($bad.commandId -eq 'engage') -and ($bad.authorityBasis.trackSource -eq 'datalinkShared')
$fail += Assert-True $illegal 'fail fixture is shared-track engage (must be rejected by host)'
$fail += Assert-True ($schema -match 'datalinkShared') 'schema mentions datalinkShared reject rule'

$forbiddenTouched = @(
    git diff --name-only origin/main HEAD
) | Where-Object {
    git diff --quiet origin/main HEAD -- $_
    $LASTEXITCODE -ne 0
} | Where-Object {
    $_ -match 'DelegationBridge\.cs|CatalogWriteGate|SimulationSession|BalticReplayHarness|MissionContactTargetClass|qa-gauntlet|t2-escort|KillChainContact'
}
$fail += Assert-True ($null -eq $forbiddenTouched -or @($forbiddenTouched).Count -eq 0) 'committed diff avoids forbidden worker files'

$committed = @(git diff --name-only origin/main HEAD)
$deltaFromMain = @()
foreach ($f in $committed) {
    git diff --quiet origin/main HEAD -- $f
    if ($LASTEXITCODE -ne 0) {
        $deltaFromMain += $f
    }
}
$allowedPath = '^(production/docs/skills/|\.claude/skills/agent-c2-skill-contract/|src/ProjectAegis\.Delegation/Skills/|src/ProjectAegis\.Delegation\.Tests/Skills/)'
$onlyAllowed = $true
foreach ($f in $deltaFromMain) {
    if ($f -notmatch $allowedPath) {
        $onlyAllowed = $false
        Write-Host "FAIL  unexpected committed path $f"
        $fail++
    }
}
if ($onlyAllowed) {
    Write-Host 'PASS  committed paths are contract docs and Skills types only'
}

$skillsValidator = Join-Path $repo 'src\ProjectAegis.Delegation\Skills\SkillEnvelopeValidator.cs'
$fail += Assert-True (Test-Path $skillsValidator) 'SkillEnvelopeValidator.cs present'
$validatorSource = Get-Content -Raw $skillsValidator
$fail += Assert-True ($validatorSource -match 'ReasonNoFireControl') 'validator enforces NO_FIRE_CONTROL on engage'
$fail += Assert-True ($validatorSource -match 'ReasonWeaponsReleaseRequired') 'validator enforces weaponsRelease on engage'
$fail += Assert-True ($validatorSource -match 'ReasonCommandNotAllowed') 'validator enforces skill command allowlist'

Write-Output ''
Write-Output "HEAD=$(git rev-parse HEAD)"
Write-Output "origin/main=$(git rev-parse origin/main)"
Write-Output "merge-base=$(git merge-base HEAD origin/main)"
Write-Output "ahead=$(git rev-list --count origin/main..HEAD)"
Write-Output "behind=$(git rev-list --count HEAD..origin/main)"
if ($fail -eq 0) {
    Write-Output 'VERDICT=PASS'
    exit 0
}
Write-Output "VERDICT=FAIL count=$fail"
exit 1
