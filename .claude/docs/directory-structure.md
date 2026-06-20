# Directory Structure

```text
/
├── CLAUDE.md                    # Master configuration
├── .claude/                     # Agent definitions, skills, hooks, rules, docs
│   └── docs/agentic/            # Parallel agent playbooks (PI todos, skill matrix)
├── src/                         # Game source code (core, gameplay, ai, networking, ui, tools)
├── assets/                      # Game assets (art, audio, vfx, shaders, data)
├── design/                      # Game design documents (gdd, narrative, levels, balance)
├── docs/                        # Technical documentation (architecture, api, postmortems)
│   └── engine-reference/        # Curated engine API snapshots (version-pinned)
├── tests/                       # Test suites (unit, integration, performance, playtest)
│   # S36-09 (QA/DevOps/Hygiene): hybrid layout accepted — co-located src/*/*.Tests/ (dotnet unit/int) + tests/regression/ (golden fixtures). 
│   # Flat tests/unit/ + tests/integration/ is aspirational reference in GDDs/ADRs (e.g. mission-editor) but not enforced for .sln structure.
│   # Decision: retain co-location for build/CI simplicity; audit hygiene only, no migration in Polish. (c-sharp-devops-engineer, 2026-07)
│   # S37-11 advance (QA/Process/Hygiene Track, c-sharp-devops-engineer isolated): Progress confirmed — CI verification on ≥1215 baseline (no breakage); no partial moves executed (preserves sln + determinism gates per polish-boundary). Signed decision note: retain hybrid; aspirational flat deferred post-Polish or via dedicated ADR if revisited. See sprint-37 + qa-plan. No regression. (2026-07, W4 track)
├── tools/                       # Build and pipeline tools (ci, build, asset-pipeline)
├── prototypes/                  # Throwaway prototypes (isolated from src/)
└── production/                  # Production management (sprints, milestones, releases)
    ├── session-state/           # Ephemeral session state (active.md — gitignored)
    └── session-logs/            # Session audit trail (gitignored)
```
