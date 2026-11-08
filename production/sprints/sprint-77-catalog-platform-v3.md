# Sprint 77 — Catalog & platform content (E9)

**Dates:** After S76 (~2026-06-25; est. 8–10 days)
**Lead:** E9 + E5
**Goal:** Catalog unit/loadout slices matching S75 OOB for v3 scenarios (additive baltic-v3-*); Excel round-trip for authors. Single owner for CatalogWriteGate (extend-only). Platform editor path for catalog.
**Capacity:** Cloud for S77-01/02 catalog; local for platform editor + closeout.
**Model:** Per roadmap-execute-plan-062526.01.md §4/§5 + baltic-v3-scope-boundary-2026-06-25.md. Parallel after prior. Isolated worktrees. GitNexus pre mandatory. verification-before on all. team-data + unity pattern. Cite boundary + execute-plan + design + AGENTS.md + S76/S75 + v2 ref (no mutate).

Cites: production/baltic-v3-scope-boundary-2026-06-25.md + docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md §3/§5/§8 + docs/reports/roadmap-execute-plan-062526.01.md §4 + AGENTS.md + future-sprint-roadpmap-062526.01.md + S76/S75/S74/S73/S72 complete + v2 catalog (read ref).

## Tasks / Tracks (from execute-plan §4)

### S77-01/02 Catalog slices (Cloud, team-data)
- Create additive catalog unit/loadout slices for v3 matching S75 OOB (data/catalog/ baltic-v3-*.json or appropriate; extend-only).
- Align to S74/S75/ S76 policies (platforms, sensors, units from baltic-v3-*).
- Support Excel round-trip (via platform editor path).
- Update manifests/catalog if needed, but single owner CatalogWriteGate.
- GitNexus pre FIRST: list_repos canonical, detect (wt), impact on CatalogWriteGate (CRIT extend-only), other §5.
- verification-before: run+READ build 0e/0w, test 1232/0f, replay 6/6, C2 18/18, hash preserved, ZERO=0.

### S77-03 Platform Editor path (Local, unity-engineer)
- Extend platform editor for Excel roundtrip / catalog import/export for v3.
- Ensure additive, no break to WriteGate.

### Other tracks
- S77-04 Closeout (local, c-sharp-devops-engineer)

## Baseline @ Start (S76 COMPLETE)
- Tests 1232/0f
- ReplayGolden 6/6
- C2 18/18
- Build 0e/0w
- Hash 17144800277401907079 preserved (v2)
- ZERO DelegationBridge
- GitNexus: list_repos (cmano-clone: 20496/38203/2516), impacts §5 exact (Catalog 178 CRIT extend-only etc.)
- S73–S76 complete; manifest/catalog v3 ready for slices.

## Inputs
- v3 OOB/policies (read): from S75/S76 baltic-v3-*.policy.json , data/catalog examples from v2/v3
- baltic-v3-scenario-manifest.yaml
- design files, execute-plan §4 S77 table, platform editor code

## Definition of Done (S77 tracks)
- baltic-v3 catalog slices created (additive, matching S75).
- Platform editor path updated for Excel/catalog roundtrip.
- sprint-77-catalog-platform-v3.md + agentic kickoff created.
- sprint-status.yaml s77_status added (tracks, baseline, COMPLETE)
- All GitNexus pre + verification-before RUN+READ; no v2 mutation; hash/ZERO/Catalog extend-only preserved.
- "S77-01/02 COMPLETE", "S77-03 COMPLETE", "S77-04 COMPLETE. S77 COMPLETE"

## Verification (post)
- cd to respective worktrees; re-run verif gates.
- GitNexus detect low risk post.
- List new catalog files.
- Full closeout smoke + GT block.

**S77 catalog/platform COMPLETE** (tracks parallel). Low risk additive v3 only. Independent. Dispatching pattern followed. Cites mandatory.

## GT Notes (for user post closeout)
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"
# GitNexus pre + verif (RUN+READ)
gt sync || git pull --ff-only
gt restack
# gt submit --stack --no-interactive for stack/sprint77/*
# interleaved verif after
