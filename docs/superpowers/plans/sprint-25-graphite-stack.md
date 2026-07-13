# Sprint 25 — Graphite Stack Runbook

**Sprint:** `production/sprints/sprint-25-phase-b-damage-assurance.md`  
**Parallel kickoff:** `production/agentic/sprint-25-parallel-kickoff-2026-06-17.md`  
**Trunk:** `main` @ `9ecbf2c`  
**Teams:** `team-data`, `team-unity`, `team-csharp`, `team-simulation`

---

## Stack topology (bottom → top)

```
main @ 9ecbf2c
 └── stack/sprint25/full-sln-gate              (S25-01)  day-1 baseline ≥592
      └── stack/sprint25/damage-schema-009     (S25-02)  migration 009 + types
           └── stack/sprint25/damage-reader-export (S25-03)  ICatalogReader + export
                └── stack/sprint25/damage-write-gate (S25-04)  CatalogWriteGate — CRITICAL
                     └── stack/sprint25/damage-importer (S25-05)  importer E2E
                          └── stack/sprint25/damage-validator (S25-06)  validator pack
                               └── stack/sprint25/closedxml-phase-b-ux (S25-07)  S24-11 carryover
                                    └── stack/sprint25/doctrine-emcon-readonly (S25-10)  S24-10 carryover
                                         └── stack/sprint25/app6-atlas-phase-c (S25-08)  APP-6 atlas
                                              └── stack/sprint25/cesium-editor-evidence (S25-09)  Editor PNGs
                                                   └── stack/sprint25/c2-editor-tri-batch (S25-11)  tri-batch log
                                                        └── stack/sprint25/closeout-gitnexus (S25-12)  replay + index
```

**Parallel worktrees (after S25-01):** S25-07, S25-10 can start in dedicated worktrees while damage stack serializes S25-02→05.

**Out of stack:** S25-13 (`damage-sim-consumer`), S25-14 (`cesium-app6-glyphs`) — optional branches stacked after S25-05 or S25-08 if buffer allows.

---

## Day-1 commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
unset npm_config_prefix
GT() { npx --yes @withgraphite/graphite-cli@stable "$@"; }

cd /home/username01/cmano-clone/cmano-clone
git checkout main && git pull --ff-only

dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal

GT create -m "chore(qa): S25-01 full-solution baseline gate [story-done]" stack/sprint25/full-sln-gate
```

---

## Submit + merge

```bash
GT checkout stack/sprint25/closeout-gitnexus
GT restack --no-interactive
GT submit --stack --no-interactive
# After Buildkite green + review:
GT merge --no-interactive
```

---

## Critical symbols (GitNexus before edit)

| Symbol | Severity |
|--------|----------|
| `CatalogWriteGate` | CRITICAL — extend-only |
| `DelegationBridge` | ZERO touch |
| `ICatalogReader` | HIGH — additive only |
| `PlatformWorkbookImporter` | HIGH |