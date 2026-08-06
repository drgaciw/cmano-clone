# Compare public corpus H3 counts vs live catalog rows (enterprise >= 99% gate).
param(
    [Parameter(Mandatory = $true)]
    [string]$DbPath,
    [double]$MinCoveragePercent = 99.0,
    [string]$CorpusRoot = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$py = Join-Path $PSScriptRoot "cmo_verify_corpus_coverage.py"
$args = @("--db", (Resolve-Path $DbPath).Path, "--min-coverage", $MinCoveragePercent)
if ($CorpusRoot) {
    $args += @("--corpus-root", $CorpusRoot)
}
python $py @args
exit $LASTEXITCODE
