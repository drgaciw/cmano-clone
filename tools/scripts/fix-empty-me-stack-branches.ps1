# Add marker commits to empty Graphite stack branches so gt submit can proceed.
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '../..')

$empty = @(
    'me-005-reserved-ops-timeline',
    'me-006-reserved-events-slot',
    'me-007-reserved-legacy-adapter',
    'me-008-reserved-migration-hook',
    'me-010-event-dtos',
    'me-015-reserved-event-runtime',
    'me-016-reserved-mission-timeline',
    'me-017-reserved-variable-store',
    'me-018-reserved-side-posture',
    'me-019-reserved-score-model',
    'me-024-ac5-headless-sample',
    'me-026-reserved-mining-cargo',
    'me-027-reserved-briefing-fields',
    'me-028-reserved-realism-toggles',
    'me-029-reserved-time-compression',
    'me-032-scenario-export-brief',
    'me-033-ac10-editversion-conflict',
    'me-035-reserved-nl-authoring',
    'me-036-reserved-agent-provenance',
    'me-037-reserved-cmo-import',
    'me-038-reserved-lua-shim',
    'me-039-reserved-workshop',
    'me-041-map-orbat-tree',
    'me-042-patrol-zone-gesture',
    'me-043-invalid-overlay-undo',
    'me-045-reserved-cesium-map',
    'me-046-reserved-play-mode-lock',
    'me-047-reserved-headless-edit-cli',
    'me-048-reserved-validation-panel',
    'me-049-reserved-mission-list-panel',
    'me-051-replay-verify-golden',
    'me-053-qa-plan-exit-gate'
)

$markerDir = 'production/epics/agentic-mission-editor-v1/.stack-markers'
New-Item -ItemType Directory -Force -Path $markerDir | Out-Null

foreach ($slug in $empty) {
    $branch = "stack/mission-editor/$slug"
    Write-Host "Marker: $branch"
    gt checkout $branch --no-interactive | Out-Null
    $id = ($slug -split '-')[1]
    $markerPath = Join-Path $markerDir "$slug.md"
    @"
# Stack marker — $slug

Reserved / deferred story slice. Story spec lives in ``story-$slug.md``.
No implementation in this PR; branch preserves ME-$id stack ordering for Graphite review.
"@ | Set-Content -Encoding utf8 $markerPath
    git add $markerPath
    gt modify -m "chore(mission-editor): $slug stack marker (reserved scope)" --no-interactive
}

gt checkout stack/mission-editor/me-054-cli-program-wiring --no-interactive | Out-Null
Write-Host "Done. Tip: stack/mission-editor/me-054-cli-program-wiring"
