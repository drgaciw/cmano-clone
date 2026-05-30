<#
.SYNOPSIS
    Comprehensive Git workflow script for commit, push, merge, and cleanup.

.DESCRIPTION
    This script performs the following sequence:
    1. Creates a descriptive and standardized commit message based on current changes
    2. Pushes local commits to the remote repository
    3. Merges all active feature branches into the main branch
    4. Deletes all merged local and remote branches for repository cleanup

.PARAMETER CommitMessage
    Optional custom commit message. If not provided, generates one based on changes.

.PARAMETER MainBranch
    The name of the main branch (default: "main").

.PARAMETER DryRun
    If specified, performs a dry run without making actual changes.

.EXAMPLE
    .\git-workflow.ps1
    Runs the full workflow with auto-generated commit message.

.EXAMPLE
    .\git-workflow.ps1 -CommitMessage "feat: add new feature"
    Runs the workflow with custom commit message.

.EXAMPLE
    .\git-workflow.ps1 -DryRun
    Performs a dry run to preview what would happen.
#>

param(
    [string]$CommitMessage,
    [string]$MainBranch = "main",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "  $Message" -ForegroundColor White
}

function Invoke-Git {
    param(
        [string]$Command,
        [string]$Description,
        [switch]$SuppressOutput
    )
    
    Write-Info "Running: git $Command"
    
    if ($DryRun) {
        Write-Info "[DRY RUN] Would run: git $Command"
        return $null
    }
    
    if ($SuppressOutput) {
        $result = & git $Command.Split(" ") 2>&1 | Out-String
        return $result.Trim()
    } else {
        & git $Command.Split(" ")
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "Git command failed: git $Command (exit code: $exitCode)"
        }
        return $null
    }
}

function Get-StagedChangesSummary {
    $stagedFiles = Invoke-Git -Command "diff --cached --name-only" -SuppressOutput -Description "Get staged files"
    
    if ([string]::IsNullOrWhiteSpace($stagedFiles)) {
        return $null
    }
    
    $fileList = $stagedFiles -split "`n" | Where-Object { $_ -ne "" }
    
    $stats = @{
        Total = $fileList.Count
        Added = 0
        Modified = 0
        Deleted = 0
        Renamed = 0
    }
    
    foreach ($file in $fileList) {
        $diffStatus = Invoke-Git -Command "diff --cached --name-status '$file'" -SuppressOutput -Description "Get file status"
        $status = $diffStatus.Split(" ")[0]
        
        switch ($status) {
            "A" { $stats.Added++ }
            "M" { $stats.Modified++ }
            "D" { $stats.Deleted++ }
            "R" { $stats.Renamed++ }
        }
    }
    
    return $stats
}

function Get-CommitMessagePrefix {
    param([hashtable]$Stats)
    
    $type = "chore"
    
    if ($Stats.Added -gt $Stats.Modified -and $Stats.Added -gt $Stats.Deleted) {
        $type = "feat"
    } elseif ($Stats.Deleted -gt $Stats.Added -and $Stats.Deleted -gt $Stats.Modified) {
        $type = "refactor"
    } elseif ($Stats.Modified -gt 0) {
        $type = "update"
    }
    
    return $type
}

function Generate-CommitMessage {
    $stats = Get-StagedChangesSummary
    
    if (-not $stats) {
        Write-Warning "No staged changes found"
        return $null
    }
    
    $prefix = Get-CommitMessagePrefix -Stats $stats
    $summary = "$prefix($env:USERNAME.ToLower()): "
    
    if ($stats.Total -eq 1) {
        $summary += "change 1 file"
    } else {
        $summary += "change $($stats.Total) files"
    }
    
    $details = @()
    if ($stats.Added -gt 0) { $details += "$($stats.Added) added" }
    if ($stats.Modified -gt 0) { $details += "$($stats.Modified) modified" }
    if ($stats.Deleted -gt 0) { $details += "$($stats.Deleted) deleted" }
    if ($stats.Renamed -gt 0) { $details += "$($stats.Renamed) renamed" }
    
    if ($details.Count -gt 0) {
        $summary += " [$($details -join ', ')]"
    }
    
    $body = "`n`nChanges:"
    $stagedFiles = Invoke-Git -Command "diff --cached --name-only" -SuppressOutput -Description "Get staged files for message"
    $stagedFiles -split "`n" | Where-Object { $_ -ne "" } | ForEach-Object {
        $body += "`n  - $_"
    }
    
    return $summary + $body
}

function Stage-Changes {
    Write-Section "Staging Changes"
    
    $unstagedFiles = Invoke-Git -Command "diff --name-only" -SuppressOutput -Description "Get unstaged files"
    $untrackedFiles = Invoke-Git -Command "ls-files --others --exclude-standard" -SuppressOutput -Description "Get untracked files"
    
    $allChanges = @()
    if (-not [string]::IsNullOrWhiteSpace($unstagedFiles)) {
        $allChanges += $unstagedFiles -split "`n" | Where-Object { $_ -ne "" }
    }
    if (-not [string]::IsNullOrWhiteSpace($untrackedFiles)) {
        $allChanges += $untrackedFiles -split "`n" | Where-Object { $_ -ne "" }
    }
    
    if ($allChanges.Count -eq 0) {
        Write-Info "No changes to stage"
        return $false
    }
    
    Write-Info "Found $($allChanges.Count) files to stage"
    
    foreach ($file in $allChanges) {
        Write-Info "  + $file"
    }
    
    if (-not $DryRun) {
        Invoke-Git -Command "add -A" -Description "Stage all changes"
    }
    
    Write-Success "All changes staged"
    return $true
}

function Create-Commit {
    param([string]$Message)
    
    Write-Section "Creating Commit"
    
    if ([string]::IsNullOrWhiteSpace($Message)) {
        $Message = Generate-CommitMessage
    }
    
    if (-not $Message) {
        Write-Warning "No changes to commit"
        return $false
    }
    
    Write-Info "Commit message:"
    Write-Host $Message -ForegroundColor Gray
    
    if (-not $DryRun) {
        $tempFile = [System.IO.Path]::GetTempFileName()
        $Message | Set-Content $tempFile
        Invoke-Git -Command "commit -F $tempFile" -Description "Create commit"
        Remove-Item $tempFile -Force
    }
    
    Write-Success "Commit created"
    return $true
}

function Push-Commits {
    Write-Section "Pushing Commits"
    
    $currentBranch = Invoke-Git -Command "branch --show-current" -SuppressOutput -Description "Get current branch"
    
    Invoke-Git -Command "push origin $currentBranch" -Description "Push current branch"
    
    Write-Success "Commits pushed to remote"
}

function Switch-To-Main {
    Write-Section "Switching to Main Branch"
    
    $currentBranch = Invoke-Git -Command "branch --show-current" -SuppressOutput -Description "Get current branch"
    
    if ($currentBranch -eq $MainBranch) {
        Write-Info "Already on main branch"
        return
    }
    
    if (-not $DryRun) {
        Invoke-Git -Command "checkout $MainBranch" -Description "Switch to main branch"
        Invoke-Git -Command "pull origin $MainBranch" -Description "Update main branch"
    } else {
        Write-Info "[DRY RUN] Would checkout and pull $MainBranch"
    }
    
    Write-Success "Switched to main branch"
}

function Get-FeatureBranches {
    $localBranches = Invoke-Git -Command "branch --format='%(refname:short)'" -SuppressOutput -Description "Get local branches"
    $remoteBranches = Invoke-Git -Command "branch -r --format='%(refname:short)'" -SuppressOutput -Description "Get remote branches"
    
    $featureBranches = @()
    
    foreach ($branch in $localBranches -split "`n") {
        if ($branch -ne $MainBranch -and $branch -ne "") {
            $featureBranches += [PSCustomObject]@{
                Name = $branch
                Type = "local"
                Remote = $null
            }
        }
    }
    
    foreach ($branch in $remoteBranches -split "`n") {
        if ($branch -match "^origin/(.+)$") {
            $branchName = $Matches[1]
            if ($branchName -ne $MainBranch -and $branchName -ne "HEAD") {
                $existing = $featureBranches | Where-Object { $_.Name -eq $branchName }
                if ($existing) {
                    $existing.Remote = "origin/$branchName"
                } else {
                    $featureBranches += [PSCustomObject]@{
                        Name = $branchName
                        Type = "remote-only"
                        Remote = "origin/$branchName"
                    }
                }
            }
        }
    }
    
    return $featureBranches
}

function Merge-FeatureBranches {
    Write-Section "Merging Feature Branches"
    
    $featureBranches = Get-FeatureBranches
    
    if ($featureBranches.Count -eq 0) {
        Write-Info "No feature branches found"
        return
    }
    
    Write-Info "Found $($featureBranches.Count) feature branch(es):"
    foreach ($branch in $featureBranches) {
        Write-Info "  - $($branch.Name) [$($branch.Type)]"
    }
    
    $mergedCount = 0
    foreach ($branch in $featureBranches) {
        Write-Info "`nMerging branch: $($branch.Name)"
        
        try {
            if ($branch.Type -eq "remote-only") {
                if (-not $DryRun) {
                    Invoke-Git -Command "fetch origin $($branch.Name)" -Description "Fetch remote branch" -SuppressOutput
                    Invoke-Git -Command "merge origin/$($branch.Name) --no-ff -m 'Merge $($branch.Name) into $MainBranch'" -Description "Merge remote branch"
                } else {
                    Write-Info "[DRY RUN] Would fetch and merge origin/$($branch.Name)"
                }
            } else {
                if (-not $DryRun) {
                    Invoke-Git -Command "merge $($branch.Name) --no-ff -m 'Merge $($branch.Name) into $MainBranch'" -Description "Merge local branch"
                } else {
                    Write-Info "[DRY RUN] Would merge $($branch.Name)"
                }
            }
            
            Write-Success "Merged $($branch.Name)"
            $mergedCount++
        } catch {
            Write-Warning "Failed to merge $($branch.Name): $_"
            Write-Info "Skipping this branch and continuing..."
        }
    }
    
    Write-Success "Merged $mergedCount / $($featureBranches.Count) feature branch(es)"
}

function Delete-MergedBranches {
    Write-Section "Cleaning Up Merged Branches"
    
    $featureBranches = Get-FeatureBranches
    
    if ($featureBranches.Count -eq 0) {
        Write-Info "No feature branches to clean up"
        return
    }
    
    $deletedLocal = 0
    $deletedRemote = 0
    
    foreach ($branch in $featureBranches) {
        $mergedIntoMain = $false
        
        if ($branch.Type -ne "remote-only") {
            $mergeBase = Invoke-Git -Command "merge-base $MainBranch $($branch.Name)" -SuppressOutput -Description "Get merge base" | Select-Object -First 1
            $mainCommit = Invoke-Git -Command "rev-parse $MainBranch" -SuppressOutput -Description "Get main commit" | Select-Object -First 1
            $branchTip = Invoke-Git -Command "rev-parse $($branch.Name)" -SuppressOutput -Description "Get branch tip" | Select-Object -First 1
            
            if ($mergeBase -eq $branchTip) {
                $mergedIntoMain = $true
            }
        } elseif ($branch.Remote) {
            $mergeBase = Invoke-Git -Command "merge-base $MainBranch origin/$($branch.Name)" -SuppressOutput -Description "Get merge base" | Select-Object -First 1
            $mainCommit = Invoke-Git -Command "rev-parse $MainBranch" -SuppressOutput -Description "Get main commit" | Select-Object -First 1
            $branchTip = Invoke-Git -Command "rev-parse origin/$($branch.Name)" -SuppressOutput -Description "Get branch tip" | Select-Object -First 1
            
            if ($mergeBase -eq $branchTip) {
                $mergedIntoMain = $true
            }
        }
        
        if ($mergedIntoMain) {
            Write-Info "`nCleaning up: $($branch.Name)"
            
            if ($branch.Type -ne "remote-only") {
                Write-Info "  - Deleting local branch..."
                if (-not $DryRun) {
                    Invoke-Git -Command "branch -d $($branch.Name)" -Description "Delete local merged branch" -SuppressOutput
                } else {
                    Write-Info "  [DRY RUN] Would delete local branch: $($branch.Name)"
                }
                $deletedLocal++
            }
            
            if ($branch.Remote) {
                Write-Info "  - Deleting remote branch..."
                if (-not $DryRun) {
                    Invoke-Git -Command "push origin --delete $($branch.Name)" -Description "Delete remote merged branch" -SuppressOutput
                } else {
                    Write-Info "  [DRY RUN] Would delete remote branch: origin/$($branch.Name)"
                }
                $deletedRemote++
            }
            
            Write-Success "Cleaned up $($branch.Name)"
        } else {
            Write-Info "`nSkipping $($branch.Name) - not merged into main"
        }
    }
    
    Write-Success "Cleanup complete: $deletedLocal local, $deletedRemote remote branches deleted"
}

function Push-Main {
    Write-Section "Pushing Main Branch"
    
    if (-not $DryRun) {
        Invoke-Git -Command "push origin $MainBranch" -Description "Push main branch"
    } else {
        Write-Info "[DRY RUN] Would push $MainBranch"
    }
    
    Write-Success "Main branch pushed to remote"
}

try {
    Write-Host "`nGit Workflow Script" -ForegroundColor Magenta
    Write-Host "====================`n" -ForegroundColor Magenta
    
    if ($DryRun) {
        Write-Warning "DRY RUN MODE - No actual changes will be made"
        Write-Host ""
    }
    
    $hasChanges = Stage-Changes
    
    if ($hasChanges -or -not [string]::IsNullOrWhiteSpace($CommitMessage)) {
        Create-Commit -Message $CommitMessage
    }
    
    Push-Commits
    
    $currentBranch = Invoke-Git -Command "branch --show-current" -SuppressOutput -Description "Get current branch"
    Switch-To-Main
    
    Merge-FeatureBranches
    Delete-MergedBranches
    
    Push-Main
    
    Write-Section "Workflow Complete"
    Write-Success "All operations completed successfully"
    
} catch {
    Write-Host "`n`n✗ Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nStack trace:" -ForegroundColor DarkGray
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    exit 1
}