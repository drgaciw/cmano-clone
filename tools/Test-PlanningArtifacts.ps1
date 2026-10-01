<#
.SYNOPSIS
Checks the finite planning scope declared by a reconciliation manifest.
.DESCRIPTION
Read-only local checks: referenced files and Markdown heading anchors, selected
current roadmap pointers, requirement clause IDs, acceptance-row ownership, and
basic evidence provenance. Historical links outside the declared scope are not
crawled. External links are metadata only; no requests or writes are performed.
This is a planning consistency check, not complete VER-07 validation, product
execution proof, a test-suite substitute, or an owner acceptance decision.
Exit code 0 means the declared checks passed; 1 means validation or input failed.
.PARAMETER ManifestPath
JSON manifest path. Relative paths resolve against RepositoryRoot.
.PARAMETER RepositoryRoot
Root containing the declared artifacts. Defaults to this script's repository.
Every declared local path must remain inside this root.
.EXAMPLE
.\tools\Test-PlanningArtifacts.ps1
.EXAMPLE
.\tools\Test-PlanningArtifacts.ps1 -RepositoryRoot C:\Temp\fixture -ManifestPath manifest.json
#>
[CmdletBinding()]
param(
    [string]$ManifestPath = 'production/planning-reconciliation-2026-09-30.json',
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}
$failures = New-Object 'System.Collections.Generic.List[string]'
$checks = 0
$fileCache = @{}

function Get-Field {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -ne $property) { return $property.Value }
    return $null
}

function Test-RequiredText {
    param($Object, [string]$Field, [string]$Context)
    $value = Get-Field $Object $Field
    if ($value -isnot [string] -or [string]::IsNullOrWhiteSpace($value)) {
        $failures.Add("$Context requires nonempty '$Field'.")
        return $false
    }
    return $true
}

function Resolve-DeclaredPath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path) -or [IO.Path]::IsPathRooted($Path)) {
        throw "Declared path must be relative to RepositoryRoot: '$Path'."
    }
    $absolute = [IO.Path]::GetFullPath((Join-Path $root $Path))
    $prefix = $root.TrimEnd([char[]]@('\', '/')) + [IO.Path]::DirectorySeparatorChar
    if (-not $absolute.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Declared path escapes RepositoryRoot: '$Path'."
    }
    return $absolute
}

function Read-DeclaredFile {
    param([string]$Path)
    $absolute = Resolve-DeclaredPath $Path
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        $failures.Add("Missing referenced file: $Path")
        return $null
    }
    if (-not $fileCache.ContainsKey($absolute)) {
        $fileCache[$absolute] = (Get-Content -LiteralPath $absolute -Raw -Encoding UTF8).Replace("`r`n", "`n")
    }
    return $fileCache[$absolute]
}

function Get-MarkdownAnchors {
    param([string]$Text)
    # ATX headings with GitHub-style slugs; skip fenced code blocks.
    $seen = @{}
    $fence = $null
    foreach ($line in ($Text -split '\r?\n')) {
        if ($line -match '^\s{0,3}(`{3,}|~{3,})') {
            $marker = $Matches[1].Substring(0, 1)
            if ($null -eq $fence) { $fence = $marker }
            elseif ($fence -eq $marker) { $fence = $null }
            continue
        }
        if ($null -ne $fence -or $line -notmatch '^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$') { continue }
        $heading = $Matches[1]
        $heading = [regex]::Replace($heading, '\[([^\]]+)\]\([^)]*\)', '$1')
        $heading = [regex]::Replace($heading, '<[^>]+>', '')
        $slug = [regex]::Replace($heading.ToLowerInvariant(), '[^\p{L}\p{N}_ -]', '').Replace(' ', '-')
        if ($seen.ContainsKey($slug)) {
            $seen[$slug]++
            $slug + '-' + $seen[$slug]
        } else {
            $seen[$slug] = 0
            $slug
        }
    }
}

try {
    $root = [IO.Path]::GetFullPath($RepositoryRoot)
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { throw 'RepositoryRoot does not exist.' }
    $manifestFile = if ([IO.Path]::IsPathRooted($ManifestPath)) {
        [IO.Path]::GetFullPath($ManifestPath)
    } else { Resolve-DeclaredPath $ManifestPath }
    $manifest = Get-Content -LiteralPath $manifestFile -Raw -Encoding UTF8 | ConvertFrom-Json
    if ((Get-Field $manifest 'schemaVersion') -ne 1) { throw 'Unsupported or missing manifest schemaVersion.' }
    foreach ($field in @('scopeNotice', 'owner', 'currentRoadmap', 'acceptanceTarget', 'inspectedRevision')) {
        [void](Test-RequiredText $manifest $field 'Manifest')
    }
    foreach ($field in @('references', 'pointers', 'requirements', 'acceptanceRows', 'evidence')) {
        $items = Get-Field $manifest $field
        if ($null -eq $items -or @($items).Count -eq 0) { $failures.Add("Manifest requires nonempty '$field'.") }
    }

    foreach ($reference in @(Get-Field $manifest 'references')) {
        $checks++
        if (-not (Test-RequiredText $reference 'path' 'Reference')) { continue }
        $content = Read-DeclaredFile $reference.path
        $anchor = Get-Field $reference 'anchor'
        if ($null -ne $content -and -not [string]::IsNullOrWhiteSpace($anchor)) {
            if (@(Get-MarkdownAnchors $content) -cnotcontains $anchor) {
                $failures.Add("Missing Markdown heading anchor: $($reference.path)#$anchor")
            }
        }
    }

    $roadmap = Get-Field $manifest 'currentRoadmap'
    if (-not [string]::IsNullOrWhiteSpace($roadmap)) { [void](Read-DeclaredFile $roadmap) }
    foreach ($pointer in @(Get-Field $manifest 'pointers')) {
        $checks++
        if (-not (Test-RequiredText $pointer 'path' 'Pointer')) { continue }
        if (-not (Test-RequiredText $pointer 'selector' "Pointer $($pointer.path)")) { continue }
        $content = Read-DeclaredFile $pointer.path
        if ($null -eq $content) { continue }
        $matchesFound = [regex]::Matches($content, $pointer.selector, [Text.RegularExpressions.RegexOptions]::Multiline)
        if ($matchesFound.Count -ne 1 -or -not $matchesFound[0].Groups['target'].Success) {
            $failures.Add("Current pointer selector must select exactly one named target: $($pointer.path)")
            continue
        }
        $target = $matchesFound[0].Groups['target'].Value.Trim().Trim([char[]]@('"', "'"))
        $base = Get-Field $pointer 'relativeTo'
        $resolved = if ([string]::IsNullOrWhiteSpace($base)) { Resolve-DeclaredPath $target }
            else { Resolve-DeclaredPath (Join-Path $base $target) }
        if ($resolved -ne (Resolve-DeclaredPath $roadmap)) {
            $failures.Add("Contradictory current roadmap pointer in $($pointer.path): '$target' (expected '$roadmap').")
        }
    }

    foreach ($requirement in @(Get-Field $manifest 'requirements')) {
        if (-not (Test-RequiredText $requirement 'path' 'Requirement')) { continue }
        $content = Read-DeclaredFile $requirement.path
        $ids = @(Get-Field $requirement 'ids')
        if ($null -eq (Get-Field $requirement 'ids') -or $ids.Count -eq 0) {
            $failures.Add("Requirement $($requirement.path) requires clause IDs.")
        }
        foreach ($id in $ids) {
            $checks++
            if ([string]::IsNullOrWhiteSpace($id) -or ($null -ne $content -and $content -notmatch ([regex]::Escape($id) + '(?![\w.])'))) {
                $failures.Add("Missing requirement clause '$id' in $($requirement.path)")
            }
        }
    }

    $rowIds = @{}
    foreach ($row in @(Get-Field $manifest 'acceptanceRows')) {
        $checks++
        foreach ($field in @('id', 'requirement', 'owner', 'issue', 'status')) {
            [void](Test-RequiredText $row $field 'Acceptance row')
        }
        if ((Get-Field $row 'status') -notin @('pending', 'accepted', 'rejected')) {
            $failures.Add('Acceptance row status must be pending, accepted, or rejected.')
        }
        $id = Get-Field $row 'id'
        if (-not [string]::IsNullOrWhiteSpace($id)) {
            if ($rowIds.ContainsKey($id)) { $failures.Add("Duplicate acceptance row ID: $id") }
            $rowIds[$id] = $true
        }
        if ((Get-Field $row 'status') -eq 'accepted') {
            foreach ($field in @('decisionReference', 'decisionDate', 'acceptedRevision')) {
                [void](Test-RequiredText $row $field "Accepted row $id")
            }
        }
    }

    foreach ($record in @(Get-Field $manifest 'evidence')) {
        $checks++
        foreach ($field in @('criterion', 'revision', 'kind', 'result', 'localDelta', 'artifact')) {
            [void](Test-RequiredText $record $field 'Evidence record')
        }
        $criterion = Get-Field $record 'criterion'
        if (-not [string]::IsNullOrWhiteSpace($criterion) -and -not $rowIds.ContainsKey($criterion)) {
            $failures.Add("Evidence criterion has no owned acceptance row: $criterion")
        }
        $acceptance = Get-Field $record 'ownerAcceptance'
        if (-not (Test-RequiredText $acceptance 'status' 'Evidence ownerAcceptance') -or
            (Get-Field $acceptance 'status') -notin @('pending', 'accepted', 'rejected')) {
            $failures.Add('Evidence ownerAcceptance requires status pending, accepted, or rejected.')
        }
        if ((Get-Field $acceptance 'status') -eq 'accepted') {
            foreach ($field in @('owner', 'decisionDate', 'decisionReference', 'acceptedRevision')) {
                [void](Test-RequiredText $acceptance $field 'Accepted evidence')
            }
            if ((Get-Field $acceptance 'acceptedRevision') -ne (Get-Field $record 'revision')) {
                $failures.Add('Accepted evidence revision contradicts its decision revision.')
            }
        }
        $artifact = Get-Field $record 'artifact'
        if (-not [string]::IsNullOrWhiteSpace($artifact)) { [void](Read-DeclaredFile $artifact) }
    }
    foreach ($external in @(Get-Field $manifest 'externalLinks')) {
        if ($null -eq $external) { continue }
        foreach ($field in @('label', 'url', 'role')) { [void](Test-RequiredText $external $field 'External metadata') }
        $uri = $null
        if (-not [Uri]::TryCreate((Get-Field $external 'url'), [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne 'https') {
            $failures.Add('External metadata requires an HTTPS URL; it is not fetched.')
        }
    }
} catch {
    $failures.Add("Input/check failure: $($_.Exception.Message)")
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Output "[FAIL] $failure" }
    Write-Output "FAIL: $($failures.Count) planning consistency failure(s); $checks declared checks inspected."
    exit 1
}
Write-Output "PASS: $checks declared planning checks; external metadata not fetched; product/owner acceptance not asserted."
exit 0
