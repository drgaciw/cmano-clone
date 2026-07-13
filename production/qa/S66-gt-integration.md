# S66 Gt Integration Checklist (PREP)

**Date:** 2026-06-25  
**Status:** PREP (ready-to-run sequence; isolated Gt Integration Prep agent)  
**Cites:** production/release-train-scope-boundary-2026-06-24.md (mandatory for all S66; GitNexus impact+detect, invariants, E10 only, Catalog extend-only, hash 17144800277401907079, verification-before) + production/sprints/sprint-66-content-manifest-playtest.md (S66-05) + AGENTS.md + graphite-github-substitute-plan.md + production/qa/smoke-sprint-66-closeout.md (updated gt section) + sprint-status.yaml (prior restack examples)

**Current state:** main (ahead ~20); dirty from S66 edits (see files below). Prep only; no mutating gt run here.

## Pre: GitNexus (FIRST, per boundary + AGENTS)
- search_tool (query for gitnexus tools) then:
  - list_repos (canonical "cmano-clone" @ /home/username01/projects/active/cmano-clone/cmano-clone : 19665 nodes/37292 edges)
  - detect_changes(scope=unstaged, repo=canonical-path): changed=28, affected=1 (RecordUnifiedRelease intra), risk=medium (doc/manifest from S66 closeout dirty state)
  - impact(target=CatalogWriteGate, direction=upstream, summaryOnly=true): CRITICAL 178 impacted (exact boundary §5)
  - impact(target=UnifiedReleaseTrainManifest): LOW
- Report risk before any commit. Re-run fresh at exec.

## Exact Gt Sequence (interleaved verification-before)
1. cd /home/username01/cmano-clone/cmano-clone
   - verification-before: read boundary + sprint-66-*.md + smoke-sprint-66 + release-checklist-v2 + AGENTS + sprint-status + roadmap/execute; git status
2. git add [S66 payload files only]
3. gt sync || git pull --ff-only
   - verification-before: dotnet build 0e; dotnet test ... 0f (~1232); replay 6/6; C2 18/18; hash grep; ZERO grep; GitNexus detect/impact re-run
4. gt restack
   - (conflict note: resolve, gt add ., gt continue)
   - verification-before: full gates RUN+READ + gt status ; gt log short
5. gt submit --stack --no-interactive
6. Post: git checkout main; gt sync; full verification-before gates + detect; gt status clean; update status/closeout; optional re-index.

**verification-before points between EVERY step:** RUN commands above + full READ of outputs; GitNexus pre; boundary cite; no regression on invariants.

## S66 Payload Files (exact from current dirty dispatch: manifest source, tests, new index, checklist-v2, status, yaml updates)
- production/playtests/baltic-v2-scenario-manifest.yaml (M)
- production/sprint-status.yaml (M; s66_ blocks)
- production/qa/evidence/baltic-v2-playtest-index.md (?? new index)
- production/qa/smoke-sprint-66-closeout.md (??)
- production/release/release-checklist-v2.md (??)
- production/sprints/sprint-66-content-manifest-playtest.md (??)
- production/sprints/sprint-66-content-manifest-stub.md (??)
- production/sprints/sprint-66-evidence-packaging.md (??)
(Plus any src manifest tests if part of dispatch record, but S66 closeout focuses doc/yaml; see sprint-66 stub for 10+9 baltic-v2-*.json + goldens)

## Updates Made (in this prep)
- smoke-sprint-66-closeout.md: replaced gt notes with precise step-by-step sequence, interleave verif-before, exact commands, GitNexus pre details, payload stage list, submit howto.
- Created this S66-gt-integration.md (small checklist for exec).
- GitNexus pre executed + recorded.
- Boundary cited throughout.

**Status:** GT STAGED READY; USER SYNC NEEDED (prep complete; cite boundary; verification-before required). GitNexus pre: list_repos (cmano-clone /home/username01/projects/active/cmano-clone/cmano-clone: 19792/37427/2455); detect_changes(scope=staged): 137 changed /1 affected /medium risk; impact(CatalogWriteGate upstream summaryOnly): 178 CRITICAL exact per release-train-scope-boundary-2026-06-24.md §5. Payload staged. 

See smoke-sprint-66-closeout.md for GitNexus pre results, exact user resolution commands (gt sync or manual + restack + full verif + submit), staged payload list, and verification-before note for resolution. No push executed.

Next: user action on trunk. 
