#!/usr/bin/env bash
# unity-housekeeping.sh — ProjectAegis / cmano-clone Unity housekeeping
#
# Safe by design:
#   * DRY RUN by default. Nothing changes until you pass --apply.
#   * Refuses to touch any git-TRACKED file. Only untracked/ignored cruft is removed.
#   * Ambiguous items are QUARANTINED to _to_delete/ (never rm'd), so you review then delete.
#   * Only provably regenerable Unity caches are hard-removed, and only with --caches.
#
# Usage:
#   ./unity-housekeeping.sh                  # dry run, default phases
#   ./unity-housekeeping.sh --apply          # execute default phases
#   ./unity-housekeeping.sh --caches --apply # ALSO nuke Library/ + Builds/ (slow reimport after)
#   ./unity-housekeeping.sh --report         # sizes only, change nothing
#
# Phases (default: dumps, logs, scratch, meta):
#   dumps    mono_crash.mem.*.blob   (~170 MB)   hard remove — Mono crash dumps
#   logs     unity/ProjectAegis/Logs/*           hard remove — gitignored, regenerated
#   scratch  repo-root debug leftovers           QUARANTINE  — you confirm, then delete
#   meta     report missing .meta / stray files  REPORT ONLY — never auto-edits Assets
#   caches   Library/ Builds/ (opt-in)           hard remove — full reimport on next open
#
set -euo pipefail

APPLY=0; DO_CACHES=0; REPORT_ONLY=0
for a in "$@"; do case "$a" in
  --apply)  APPLY=1 ;;
  --caches) DO_CACHES=1 ;;
  --report) REPORT_ONLY=1 ;;
  -h|--help) sed -n '2,26p' "$0"; exit 0 ;;
  *) echo "unknown flag: $a" >&2; exit 2 ;;
esac; done

ROOT="$(git rev-parse --show-toplevel 2>/dev/null || true)"
[[ -n "$ROOT" ]] || { echo "FATAL: not inside a git repository." >&2; exit 1; }
cd "$ROOT"
UP="unity/ProjectAegis"
[[ -d "$UP" ]] || { echo "FATAL: $UP not found — wrong repo? (cwd=$ROOT)" >&2; exit 1; }

QUAR="_to_delete/housekeeping-$(date +%Y%m%d-%H%M%S)"
FREED=0
say(){ printf '%s\n' "$*"; }
hr(){ printf '%s\n' "------------------------------------------------------------"; }
bytes_of(){ [[ -e "$1" ]] && du -sb "$1" 2>/dev/null | cut -f1 || echo 0; }
human(){ numfmt --to=iec --suffix=B "${1:-0}" 2>/dev/null || echo "${1}B"; }

# A file is protected if git tracks it. Never delete tracked content.
tracked(){ git ls-files --error-unmatch -- "$1" >/dev/null 2>&1; }

act_rm(){ # hard remove a regenerable, untracked path
  local p="$1"
  [[ -e "$p" ]] || return 0
  if tracked "$p"; then say "  SKIP (git-tracked!) $p"; return 0; fi
  local n; n="$(bytes_of "$p")"; FREED=$((FREED+n))
  if (( APPLY )); then rm -rf -- "$p"; say "  removed   $p  ($(human "$n"))"
  else say "  would rm  $p  ($(human "$n"))"; fi
}

act_quarantine(){ # move to _to_delete/ for human review
  local p="$1"
  [[ -e "$p" ]] || return 0
  if tracked "$p"; then say "  SKIP (git-tracked!) $p"; return 0; fi
  local n; n="$(bytes_of "$p")"; FREED=$((FREED+n))
  if (( APPLY )); then
    mkdir -p "$QUAR"; mv -n -- "$p" "$QUAR/"; say "  quarantined $p -> $QUAR/  ($(human "$n"))"
  else say "  would move  $p -> $QUAR/  ($(human "$n"))"; fi
}

hr; say "ProjectAegis housekeeping   repo=$ROOT"
say "mode: $( ((APPLY)) && echo APPLY || echo 'DRY RUN (pass --apply to execute)' )"; hr

# ---------------------------------------------------------------- report
say "== Current footprint =="
for p in "$UP/Library" "$UP/Builds" "$UP/Logs" "$UP/Assets" Builds scratch node_modules .git; do
  [[ -e "$p" ]] && printf '  %-34s %s\n' "$p" "$(human "$(bytes_of "$p")")"
done
dumps=( "$UP"/mono_crash.mem.*.blob )
[[ -e "${dumps[0]:-}" ]] && printf '  %-34s %s (%d files)\n' "$UP/mono_crash.mem.*.blob" \
  "$(human "$(du -cb "${dumps[@]}" 2>/dev/null | tail -1 | cut -f1)")" "${#dumps[@]}"
hr
(( REPORT_ONLY )) && exit 0

# ---------------------------------------------------------- phase: dumps
say "== Phase: Mono crash dumps =="
if [[ -e "${dumps[0]:-}" ]]; then for f in "${dumps[@]}"; do act_rm "$f"; done
else say "  none found"; fi

# ----------------------------------------------------------- phase: logs
say; say "== Phase: Unity editor logs (gitignored, regenerated on next run) =="
if [[ -d "$UP/Logs" ]]; then
  shopt -s nullglob
  found=0
  for f in "$UP"/Logs/*; do act_rm "$f"; found=1; done
  (( found )) || say "  Logs/ already empty"
  shopt -u nullglob
else say "  no Logs/ directory"; fi
# Stale ILPostProcessing pid left behind by a crashed editor
[[ -f "$UP/Library/ilpp.pid" ]] && act_rm "$UP/Library/ilpp.pid"

# -------------------------------------------------------- phase: scratch
say; say "== Phase: repo-root debug leftovers (quarantined, not deleted) =="
# Verified 2026-08-17:
#   re                      13.6 MB ImageMagick PostScript export of a 1920x1168 screenshot
#   panel_head_snapshot.cs  md5-identical to Assets/Scripts/Runtime/OsintStagingPanelHost.cs
#   panel_base_snapshot.cs  pre-S20 draft of the same panel
#   pre_cesium_bridge.cs    superseded draft of Assets/Scripts/Runtime/Cesium/CesiumGlobeBridge.cs
#   panel_full_diff.patch   git show of commit c38b4a6 — already in history
for f in re panel_head_snapshot.cs panel_base_snapshot.cs pre_cesium_bridge.cs panel_full_diff.patch; do
  act_quarantine "$f"
done

# ----------------------------------------------------------- phase: meta
say; say "== Phase: Unity .meta hygiene (REPORT ONLY — fix by opening the Editor) =="
missing=0
while IFS= read -r -d '' f; do
  case "$f" in */Library/*|*/Temp/*) continue ;; esac
  [[ -f "$f.meta" ]] || { say "  MISSING .meta: $f"; missing=$((missing+1)); }
done < <(find "$UP/Assets" -type f ! -name '*.meta' -print0)
orphan=0
while IFS= read -r -d '' m; do
  b="${m%.meta}"
  [[ -e "$b" ]] || { say "  ORPHAN .meta (asset gone): $m"; orphan=$((orphan+1)); }
done < <(find "$UP/Assets" -type f -name '*.meta' -print0)
say "  -> $missing missing, $orphan orphaned"
(( missing )) && say "  ACTION: open the Editor once so Unity mints the GUIDs, then COMMIT the new .meta files"
(( missing )) && say "          before any parallel branch imports the same asset (divergent GUIDs = broken refs)."

# --------------------------------------------------------- phase: caches
if (( DO_CACHES )); then
  say; say "== Phase: Unity caches (full reimport on next Editor open) =="
  for p in "$UP/Library" "$UP/Temp" "$UP/obj" "$UP/Builds" Builds; do act_rm "$p"; done
else
  say; say "== Phase: Unity caches — SKIPPED (pass --caches to include Library/ + Builds/) =="
fi

hr
say "Reclaimable/reclaimed: $(human "$FREED")"
(( APPLY )) || say "DRY RUN — nothing changed. Re-run with --apply."
if (( APPLY )) && [[ -d "$QUAR" ]]; then
  say "Quarantined files are in $QUAR — review, then: rm -rf '$QUAR'"
fi
say "Post-run sanity: git status --short   (should show no deletions of tracked files)"
hr
