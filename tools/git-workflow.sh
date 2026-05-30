#!/bin/bash
# Git Workflow Script
# Performs: commit, push, merge feature branches, cleanup

set -e

# Configuration
MAIN_BRANCH="${MAIN_BRANCH:-main}"
DRY_RUN="${DRY_RUN:-false}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[0;37m'
GRAY='\033[0;90m'
NC='\033[0m'

# Functions
write_section() {
    echo ""
    echo -e "${CYAN}=== $1 ===${NC}"
}

write_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

write_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

write_info() {
    echo -e "  ${WHITE}$1${NC}"
}

run_git() {
    local cmd="$1"
    local description="$2"
    local suppress_output="${3:-false}"
    
    write_info "Running: git $cmd"
    
    if [ "$DRY_RUN" = "true" ]; then
        write_info "[DRY RUN] Would run: git $cmd"
        return 0
    fi
    
    if [ "$suppress_output" = "true" ]; then
        git $cmd 2>&1
    else
        git $cmd
    fi
}

get_staged_changes_summary() {
    local staged_files=$(git diff --cached --name-only 2>/dev/null)
    
    if [ -z "$staged_files" ]; then
        return 1
    fi
    
    local total=$(echo "$staged_files" | wc -l | tr -d ' ')
    local added=0 modified=0 deleted=0 renamed=0
    
    while IFS= read -r file; do
        [ -z "$file" ] && continue
        local status=$(git diff --cached --name-status "$file" 2>/dev/null | cut -d' ' -f1)
        case $status in
            A) ((added++)) ;;
            M) ((modified++)) ;;
            D) ((deleted++)) ;;
            R) ((renamed++)) ;;
        esac
    done <<< "$staged_files"
    
    echo "total:$total added:$added modified:$modified deleted:$deleted renamed:$renamed"
}

get_commit_message_prefix() {
    local stats="$1"
    local added=$(echo "$stats" | grep -o 'added:[0-9]*' | cut -d':' -f2)
    local modified=$(echo "$stats" | grep -o 'modified:[0-9]*' | cut -d':' -f2)
    local deleted=$(echo "$stats" | grep -o 'deleted:[0-9]*' | cut -d':' -f2)
    
    if [ "$added" -gt "$modified" ] && [ "$added" -gt "$deleted" ]; then
        echo "feat"
    elif [ "$deleted" -gt "$added" ] && [ "$deleted" -gt "$modified" ]; then
        echo "refactor"
    elif [ "$modified" -gt 0 ]; then
        echo "update"
    else
        echo "chore"
    fi
}

generate_commit_message() {
    local stats
    if ! stats=$(get_staged_changes_summary); then
        write_warning "No staged changes found"
        return 1
    fi
    
    local total=$(echo "$stats" | grep -o 'total:[0-9]*' | cut -d':' -f2)
    local added=$(echo "$stats" | grep -o 'added:[0-9]*' | cut -d':' -f2)
    local modified=$(echo "$stats" | grep -o 'modified:[0-9]*' | cut -d':' -f2)
    local deleted=$(echo "$stats" | grep -o 'deleted:[0-9]*' | cut -d':' -f2)
    local renamed=$(echo "$stats" | grep -o 'renamed:[0-9]*' | cut -d':' -f2)
    
    local prefix=$(get_commit_message_prefix "$stats")
    local username=$(whoami | tr '[:upper:]' '[:lower:]')
    local summary="$prefix($username): "
    
    if [ "$total" -eq 1 ]; then
        summary+="change 1 file"
    else
        summary+="change $total files"
    fi
    
    local details=()
    [ "$added" -gt 0 ] && details+=("$added added")
    [ "$modified" -gt 0 ] && details+=("$modified modified")
    [ "$deleted" -gt 0 ] && details+=("$deleted deleted")
    [ "$renamed" -gt 0 ] && details+=("$renamed renamed")
    
    if [ ${#details[@]} -gt 0 ]; then
        summary+=" [$(IFS=', '; echo "${details[*]}")]"
    fi
    
    echo -e "$summary\n\nChanges:"
    git diff --cached --name-only 2>/dev/null | while read -r file; do
        [ -z "$file" ] && continue
        echo "  - $file"
    done
}

stage_changes() {
    write_section "Staging Changes"
    
    local unstaged_files=$(git diff --name-only 2>/dev/null)
    local untracked_files=$(git ls-files --others --exclude-standard 2>/dev/null)
    
    local all_changes=""
    [ -n "$unstaged_files" ] && all_changes+="$unstaged_files"$'\n'
    [ -n "$untracked_files" ] && all_changes+="$untracked_files"
    
    if [ -z "$all_changes" ]; then
        write_info "No changes to stage"
        return 1
    fi
    
    local count=$(echo "$all_changes" | grep -c '.' || echo 0)
    write_info "Found $count files to stage"
    echo "$all_changes" | grep '.' | while read -r file; do
        write_info "  + $file"
    done
    
    if [ "$DRY_RUN" = "false" ]; then
        run_git "add -A" "Stage all changes" true
    fi
    
    write_success "All changes staged"
    return 0
}

create_commit() {
    local message="$1"
    
    write_section "Creating Commit"
    
    if [ -z "$message" ]; then
        message=$(generate_commit_message)
    fi
    
    if [ -z "$message" ]; then
        write_warning "No changes to commit"
        return 1
    fi
    
    write_info "Commit message:"
    echo -e "${GRAY}$message${NC}"
    
    if [ "$DRY_RUN" = "false" ]; then
        echo "$message" | run_git "commit -F -" "Create commit" true
    fi
    
    write_success "Commit created"
    return 0
}

push_commits() {
    write_section "Pushing Commits"
    
    local current_branch=$(git branch --show-current 2>/dev/null)
    
    run_git "push origin $current_branch" "Push current branch"
    
    write_success "Commits pushed to remote"
}

switch_to_main() {
    write_section "Switching to Main Branch"
    
    local current_branch=$(git branch --show-current 2>/dev/null)
    
    if [ "$current_branch" = "$MAIN_BRANCH" ]; then
        write_info "Already on main branch"
        return
    fi
    
    if [ "$DRY_RUN" = "false" ]; then
        run_git "checkout $MAIN_BRANCH" "Switch to main branch"
        run_git "pull origin $MAIN_BRANCH" "Update main branch"
    else
        write_info "[DRY RUN] Would checkout and pull $MAIN_BRANCH"
    fi
    
    write_success "Switched to main branch"
}

get_feature_branches() {
    local output=""
    
    git branch --format='%(refname:short)' 2>/dev/null | while read -r branch; do
        [ -z "$branch" ] && continue
        [ "$branch" = "$MAIN_BRANCH" ] && continue
        echo "local:$branch"
    done
    
    git branch -r --format='%(refname:short)' 2>/dev/null | while read -r branch; do
        [ -z "$branch" ] && continue
        [[ "$branch" =~ ^origin/(.+)$ ]] || continue
        local branch_name="${BASH_REMATCH[1]}"
        [ "$branch_name" = "$MAIN_BRANCH" ] && continue
        [ "$branch_name" = "HEAD" ] && continue
        echo "remote:$branch_name"
    done
}

merge_feature_branches() {
    write_section "Merging Feature Branches"
    
    local branches=$(get_feature_branches)
    
    if [ -z "$branches" ]; then
        write_info "No feature branches found"
        return
    fi
    
    local count=$(echo "$branches" | grep -c '.' || echo 0)
    write_info "Found $count feature branch(es):"
    echo "$branches" | while read -r entry; do
        [ -z "$entry" ] && continue
        local type="${entry%%:*}"
        local name="${entry##*:}"
        write_info "  - $name [$type]"
    done
    
    local merged=0
    echo "$branches" | while read -r entry; do
        [ -z "$entry" ] && continue
        local type="${entry%%:*}"
        local name="${entry##*:}"
        
        write_info ""
        write_info "Merging branch: $name"
        
        if [ "$DRY_RUN" = "false" ]; then
            if [ "$type" = "remote" ]; then
                run_git "fetch origin $name" "Fetch remote branch" true
                run_git "merge origin/$name --no-ff -m 'Merge $name into $MAIN_BRANCH'" "Merge remote branch"
            else
                run_git "merge $name --no-ff -m 'Merge $name into $MAIN_BRANCH'" "Merge local branch"
            fi
            write_success "Merged $name"
        else
            write_info "[DRY RUN] Would merge $name"
        fi
        ((merged++)) || true
    done
    
    write_success "Merged feature branch(es)"
}

delete_merged_branches() {
    write_section "Cleaning Up Merged Branches"
    
    local branches=$(get_feature_branches)
    
    if [ -z "$branches" ]; then
        write_info "No feature branches to clean up"
        return
    fi
    
    local deleted_local=0 deleted_remote=0
    
    echo "$branches" | while read -r entry; do
        [ -z "$entry" ] && continue
        local type="${entry%%:*}"
        local name="${entry##*:}"
        
        local merged=false
        
        if [ "$type" != "remote" ]; then
            local merge_base=$(git merge-base $MAIN_BRANCH $name 2>/dev/null | head -1)
            local branch_tip=$(git rev-parse $name 2>/dev/null)
            
            [ "$merge_base" = "$branch_tip" ] && merged=true
        else
            local merge_base=$(git merge-base $MAIN_BRANCH origin/$name 2>/dev/null | head -1)
            local branch_tip=$(git rev-parse origin/$name 2>/dev/null)
            
            [ "$merge_base" = "$branch_tip" ] && merged=true
        fi
        
        if [ "$merged" = "true" ]; then
            write_info ""
            write_info "Cleaning up: $name"
            
            if [ "$type" != "remote" ]; then
                write_info "  - Deleting local branch..."
                if [ "$DRY_RUN" = "false" ]; then
                    run_git "branch -d $name" "Delete local merged branch" true
                else
                    write_info "  [DRY RUN] Would delete local branch: $name"
                fi
                ((deleted_local++)) || true
            fi
            
            write_info "  - Deleting remote branch..."
            if [ "$DRY_RUN" = "false" ]; then
                run_git "push origin --delete $name" "Delete remote merged branch" true
            else
                write_info "  [DRY RUN] Would delete remote branch: origin/$name"
            fi
            ((deleted_remote++)) || true
            
            write_success "Cleaned up $name"
        else
            write_info ""
            write_info "Skipping $name - not merged into main"
        fi
    done
    
    write_success "Cleanup complete"
}

push_main() {
    write_section "Pushing Main Branch"
    
    if [ "$DRY_RUN" = "false" ]; then
        run_git "push origin $MAIN_BRANCH" "Push main branch"
    else
        write_info "[DRY RUN] Would push $MAIN_BRANCH"
    fi
    
    write_success "Main branch pushed to remote"
}

# Main execution
main() {
    echo ""
    echo -e "${MAGENTA}Git Workflow Script${NC}"
    echo -e "${MAGENTA}====================${NC}"
    echo ""
    
    if [ "$DRY_RUN" = "true" ]; then
        write_warning "DRY RUN MODE - No actual changes will be made"
        echo ""
    fi
    
    local custom_message="$1"
    local has_changes=false
    
    if stage_changes; then
        has_changes=true
    fi
    
    if [ "$has_changes" = "true" ] || [ -n "$custom_message" ]; then
        create_commit "$custom_message"
    fi
    
    push_commits
    
    switch_to_main
    merge_feature_branches
    delete_merged_branches
    push_main
    
    write_section "Workflow Complete"
    write_success "All operations completed successfully"
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -m|--message)
            CUSTOM_MESSAGE="$2"
            shift 2
            ;;
        --dry-run)
            DRY_RUN="true"
            shift
            ;;
        --main-branch)
            MAIN_BRANCH="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -m, --message MSG    Custom commit message"
            echo "  --dry-run            Preview without making changes"
            echo "  --main-branch BRANCH Specify main branch name (default: main)"
            echo "  -h, --help           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

main "$CUSTOM_MESSAGE"