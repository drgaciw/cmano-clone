# Off-CI enterprise public corpus load (Windows orchestrator).
param(
    [string]$RunDate = (Get-Date).ToUniversalTime().ToString("yyyyMMdd"),
    [int]$ChunkSize = 500,
    [switch]$DryRun,
    [switch]$ApproveOnly,
    [switch]$ImportOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$env:AEGIS_PUBLIC_CORPUS = "1"
$scratch = Join-Path $repoRoot "scratch\nightly-cmo-$RunDate"
$db = Join-Path $scratch "catalog-proposed.db"
$cli = "src\ProjectAegis.MissionEditor.Cli\ProjectAegis.MissionEditor.Cli.csproj"
$corpus = Join-Path $repoRoot "docs\reference\cmano-db"
$weaponMd = Join-Path $corpus "weapon.md"

New-Item -ItemType Directory -Force -Path $scratch | Out-Null

$entities = @(
    @{ Name = "sensor";      Md = "sensor.md";      Weapon = $false },
    @{ Name = "weapon";      Md = "weapon.md";      Weapon = $false },
    @{ Name = "platform";    Md = "ship.md";        Weapon = $true },
    @{ Name = "aircraft";    Md = "aircraft.md";    Weapon = $true },
    @{ Name = "submarine";   Md = "submarine.md";   Weapon = $true },
    @{ Name = "facility";    Md = "facility.md";    Weapon = $true },
    @{ Name = "ground-unit"; Md = "ground-units.md"; Weapon = $true }
)

function Invoke-ImportEntity($entity) {
    $md = Join-Path $corpus $entity.Md
    $quarantine = Join-Path $scratch "$($entity.Name)-quarantine.json"
    $propose = Join-Path $scratch "$($entity.Name)-propose.json"
    Write-Host "==> Import $($entity.Name)"
    if ($DryRun) {
        Write-Host "    Markdown: $md"
        return
    }
    $args = @(
        "run", "--project", $cli, "--",
        "catalog_import_markdown",
        "--db", $db,
        "--markdown", $md,
        "--entity", $entity.Name,
        "--chunk-size", $ChunkSize,
        "--report-out", $quarantine
    )
    if ($entity.Weapon) {
        $args += @("--weapon", $weaponMd)
    }
    dotnet @args | Tee-Object -FilePath $propose
}

function Get-SortedBatchIds {
    param(
        [string]$EntityName,
        [string[]]$BatchIds
    )
    $unique = $BatchIds | Select-Object -Unique
    if ($EntityName -in @('platform', 'aircraft', 'submarine', 'facility', 'ground-unit')) {
        $prefixOrder = @(
            'batch-platform-',
            'batch-mount-',
            'batch-loadout-',
            'batch-magazine-'
        )
        return ,@($unique | ForEach-Object {
            $id = $_
            $rank = 99
            for ($i = 0; $i -lt $prefixOrder.Count; $i++) {
                if ($id -like "$($prefixOrder[$i])*") {
                    $rank = $i
                    break
                }
            }
            [pscustomobject]@{ Rank = $rank; Id = $id }
        } | Sort-Object Rank, Id | ForEach-Object { $_.Id })
    }
    return $unique
}

function Invoke-ApproveEntity($entityName) {
    $propose = Join-Path $scratch "$entityName-propose.json"
    if (-not (Test-Path $propose)) {
        Write-Warning "Missing propose json: $propose"
        return
    }
    $json = Get-Content $propose -Raw | ConvertFrom-Json
    $batchIds = Get-SortedBatchIds -EntityName $entityName -BatchIds @(
        $json.batches | ForEach-Object { $_.batchId }
    )
    foreach ($batchId in $batchIds) {
        Write-Host "==> Approve $entityName batch $batchId"
        if ($DryRun) { continue }
        dotnet run --project $cli -- catalog_write_approve `
            --db $db `
            --batch $batchId `
            --snapshot-id aegis_public_corpus `
            --release-version "corpus-full-$RunDate-$entityName-$batchId"
        if ($LASTEXITCODE -ne 0) {
            throw "Approve failed for $entityName batch $batchId (exit $LASTEXITCODE)"
        }
    }
}

Write-Host "=== cmo-enterprise-corpus-load ==="
Write-Host "RunDate: $RunDate"
Write-Host "Scratch: $scratch"
Write-Host "AEGIS_PUBLIC_CORPUS=1"

if (-not $ApproveOnly) {
    if (-not $DryRun) {
        dotnet build $cli -v minimal -nologo
    }
    foreach ($e in $entities) {
        Invoke-ImportEntity $e
    }
}

if (-not $ImportOnly) {
    foreach ($e in $entities) {
        Invoke-ApproveEntity $e.Name
    }
}

Write-Host "=== Done ==="
