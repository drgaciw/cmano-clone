# Sprint 82 Parallel Kickoff — Validation Tracks A+C

**Date:** 2026-07-04 (post S81)  
**Sprint:** S82  
**Authority:** `roadmap-execute-plan-07042026.md` §4 S82, `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md`

## Context
S81 complete (boundary, branch plan, GitNexus re-index, closeout). In-flight validation work from `fix-scenario-publish-cli-wiring` has been integrated or is ready.

S82 focuses on hardening three parallel validation tracks (A+C):

1. Doctrine inheritance (AC-4)
2. Schema conformance (AC-9 / AME-2.6)
3. Save-vs-export gate (AC-12)

All on cloud agents, followed by local closeout.

## Dispatch Rules (superpowers)
- Use dispatching-parallel-agents for the three tracks.
- Each subagent: isolated context, GitNexus preflight on editor symbols + CRITICALs, cite boundary + sprint plan + qa-plan units.
- Prefer extending existing tests/fixtures.
- Run the specific filter before/after changes.
- Work in `.worktrees/stack/sprint82/<track>` 
- TDD where extending (RED then GREEN).
- verification-before-completion on all PASS claims.

## Track Details

**S82-01 Doctrine inheritance (AC-4)**
- Primary: DoctrineInheritanceValidateTests.cs + doctrine-inheritance.json fixture
- Goal: Full coverage of AC-4 via scenario_validate
- Test filter component: DoctrineInheritance

**S82-02 Schema conformance (AC-9/AME-2.6)**
- Primary: ScenarioDocumentSchemaConformanceTests.cs + DerivedOnlyInvariantTests.cs + schema.json
- Goal: All 3 examples + live editor output conform; editorState derived-only
- Test filter component: SchemaConformance|DerivedOnly

**S82-03 Save-vs-export gate (AC-12)**
- Primary: SaveVsExportGateTests.cs + ScenarioValidationExportGate.cs
- Goal: Save succeeds, export blocked for invalid cases
- Test filter component: SaveVsExport

**S82-04 Closeout (local)**
- Aggregate, restack, re-verify full gates + editor filter, GitNexus post, closeout doc.

## Commands
```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "DoctrineInheritance|DerivedOnly|SaveVsExport|SchemaConformance"
```

## Standing Invariants
- Test floor ≥1232
- Replay 6/6, C2 18/18
- Hash preserved
- ZERO DelegationBridge
- Stage Release
- GitNexus discipline

**Kickoff complete.** Dispatch the three validation subagents in parallel.
