# Sprint 18 Closeout — C2 QA Gate + Catalog Phase 2 Completion

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close Sprint 18 by completing the remaining Must/Should Have items (primarily S18-01 C2 manual sign-off gate and remaining Catalog Phase 2 slices P2-2/P2-3), refresh all evidence, update status tracking, and verify with full local gate so the sprint can be marked complete.

**Architecture:** Headless-first verification (dotnet tests + PlayMode harness + proxy evidence) for C2; extend existing WriteGate + SnapshotStore flow for catalog without bypassing IWriteGate or provenance. All changes follow TDD, GitNexus impact before edits, and update sprint-status.yaml + QA docs.

**Tech Stack:** .NET 8, C# (ProjectAegis.Data, MissionEditor.Cli, Delegation tests), Unity PlayMode harness (headless via adapter tests), GitNexus for impact, superpowers skills (smoke-check, team-qa, writing-plans, subagent-driven-development).

**References:**
- Sprint kickoff: production/sprints/sprint-18-kickoff.md
- Current status: production/sprint-status.yaml (current_sprint: 18, s18-c2-signoff: ready-for-dev)
- Catalog plan: docs/superpowers/plans/2026-06-04-catalog-phase2-import.md (P2-1 done; P2-2/P2-3 open)
- C2 runbook + checklist: production/qa/sprint-18-c2-signoff-runbook-2026-06-04.md and c2-manual-signoff-2026-06-02.md
- CI local SOP: production/qa/sprint-18-ci-local-gate-2026-06-04.md
- Latest smoke: production/qa/smoke-2026-06-04.md (PASS, 385 tests, 7/7 PlayMode, 7/7 replay)
- Wave5 epic: production/epics/wave5-engage-cyber-logistics-slice/EPIC.md (Code-complete pending manual sign-off)
- GitNexus rules (AGENTS.md): impact before symbol edits; detect_changes before commit-like updates.

**Entry Criteria (already met per current state):**
- Build green.
- Full tests ~387 pass, 0 fail (solution + targeted).
- PlayMode 7/7, replay 7/7.
- GitNexus index refresh in progress (was 160 commits stale).

**Definition of Done for this plan:**
- [ ] Smoke evidence refreshed + written.
- [ ] C2 checklist updated with proxy + current run results; sign-off evidence produced (headless PASS + notes on manual).
- [ ] Catalog P2-2/P2-3 implemented + tests pass + acceptance met (approve flow, snapshot hash stable).
- [ ] sprint-status.yaml updated (s18-c2-signoff: done; others confirmed).
- [ ] gitnexus detect_changes run; no unexpected blast radius.
- [ ] Full local gate (build + test + PlayMode + replay) PASS.
- [ ] Plan tasks all checked; no open S1/S2 issues.

---

## Pre-Work (run once before tasks)

- [ ] **Step 0.1: Refresh GitNexus index and baseline impacts**
  Run (in terminal):
  ```
  npx gitnexus analyze .
  npx gitnexus status
  ```
  Then use MCP gitnexus__impact for key symbols (report blast radius to user):
  - target: "CatalogWriteGate", direction: "upstream", repo: "cmano-clone"
  - target: "DbSnapshotStore", direction: "upstream", repo: "cmano-clone"
  - target: "DelegationBridge", direction: "upstream", repo: "cmano-clone" (CRITICAL per prior)
  - target: "CmoMarkdownImporter", direction: "upstream"
  Expected: Confirm no HIGH/CRITICAL surprises for data-only changes; note any d=1 callers in Data/CLI/Tests. If CRITICAL on DelegationBridge, do not touch it.

- [ ] **Step 0.2: Re-run full local gate (per CI SOP + recommended verification)**
  ```
  dotnet restore ProjectAegis.sln
  dotnet build ProjectAegis.sln -v minimal
  dotnet test ProjectAegis.sln -v minimal
  dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
  dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~ReplayGolden|FullyQualifiedName~ReplayOrderLog"
  ```
  Capture counts + SHA (use `git rev-parse HEAD`). Update any evidence docs with new SHA if changed. Expect 0 failures.

---

### Task 1: Refresh Smoke Evidence (S18-02)

**Files:**
- Modify: production/qa/smoke-2026-06-05.md (new or update date)
- Modify: production/qa/sprint-18-c2-signoff-runbook-2026-06-04.md (update build SHA)

- [ ] **Step 1.1: Invoke smoke-check skill (or manual equivalent)**
  Follow .claude/skills/smoke-check/SKILL.md for "sprint" (or quick). Since Unity Editor not in this env, focus on automated + harness. Run the gate commands from Pre-Work 0.2. Produce new `production/qa/smoke-2026-06-05.md` modeled exactly after 2026-06-04 one, with updated date/SHA, verdict PASS, table of commands + results, and note "C2 manual proxy coverage per c2-automated-proxy".

- [ ] **Step 1.2: Verify coverage map still holds**
  Cross-check against c2-automated-proxy-2026-06-02.md. If new tests added in wave5, append rows. Confirm checks 2-13 have headless proxies.

- [ ] **Step 1.3: Self-review + write**
  Confirm no FAILs. Write the smoke report only after internal verification matches "PASS". Commit message style: "chore(qa): refresh Sprint 18 smoke evidence 2026-06-05".

**Acceptance:** New smoke doc exists, references current main SHA, all automated gates green, links to C2 runbook.

---

### Task 2: Complete C2 Manual Sign-Off Gate (S18-01)

**Files:**
- Modify: production/qa/c2-manual-signoff-2026-06-02.md (fill Pass/Tester/Notes for as many as possible from proxies + harness; update build SHA and verdict)
- Modify/Create: production/qa/sprint-18-c2-signoff-evidence-2026-06-05.md (new summary of headless + what requires Editor)
- Modify: production/sprint-status.yaml (later in Task 4)
- Possibly: production/qa/c2-automated-proxy-2026-06-02.md (minor update if needed)

- [ ] **Step 2.1: Run PlayMode harness + relevant C2/Delegation tests explicitly**
  Commands (record output):
  ```
  dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~PlayModeSmokeHarness or FullyQualifiedName~C2 or FullyQualifiedName~AttackMenu or FullyQualifiedName~UnitDetail" -v minimal --no-build
  dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~Baltic_patrol or FullyQualifiedName~DelegationBridgeAttack or FullyQualifiedName~Fuel or FullyQualifiedName~Comms" -v minimal --no-build
  ```
  Map results to the 13 checks (e.g., check 13 covered by DelegationBridgeAttackOptionTests + Engage*Tests).

- [ ] **Step 2.2: Update the checklist table with proxy evidence**
  For each row 1-13:
  - Set "Pass" to ☑ or "headless proxy" where test name from proxy doc or above run covers it.
  - In Notes: "Headless: <testname> @ <SHA> PASS; Editor visual/click sync requires local Unity 6000.3.14f1 per PLAYMODE-SMOKE.md + runbook."
  - Check 1 (console errors): note "Editor only; harness + build clean".
  - Check 13: explicitly "Headless covered by DelegationBridgeAttackOptionTests + UnitDetailBridgeTests + AttackMenuPanelBinderTests (7/7 PlayMode green); full Fire Single engage flow in Editor still manual."
  - Leave pure interaction rows (map click sync feel, visual dimming) with note "Requires human Editor session".

- [ ] **Step 2.3: Produce sign-off evidence summary**
  Create or append to evidence doc:
  - Headless proxy: PASS (reference automated-proxy + fresh test run counts).
  - PlayMode harness: 7/7 PASS (specific filter output).
  - Verdict for sprint: "Headless + proxy complete. Full 13/13 Editor manual deferred to local dev with Unity (per runbook). No S1/S2 blockers from automated."
  - Update the checklist Verdict line: e.g. "☑ PASS (headless/proxy) / ☐ Manual Editor pending local run".

- [ ] **Step 2.4: Invoke team-qa if scope allows (or simulate per its phases)**
  Per .claude/skills/team-qa/SKILL.md, for argument "sprint 18" or "feature: c2-signoff".
  Since this is closeout, at minimum run Phase 1-2 (strategy + smoke already done), produce partial sign-off note.
  Use AskUserQuestion for any decisions (e.g. "Proceed with headless-only evidence for sprint close?").

- [ ] **Step 2.5: Update runbook and cross-refs**
  In sprint-18-c2-signoff-runbook, add "Executed [date] via superpowers plan 2026-06-05; see evidence-2026-06-05.md. Checklist updated with proxies."

**Acceptance:** Checklist has evidence for all rows (PASS via proxy or explicit "Editor required"), no new bugs filed, sprint-status can mark done after Task 4. Wave5 epic note can be updated to "Manual sign-off evidence collected (headless complete)".

**Risk:** True visual/feel (e.g. dimming, tab sync) cannot be 100% proven without Editor. Document as limitation.

---

### Task 3: Complete Catalog Phase 2 Slices P2-2 and P2-3 (S18-04)

**Files (exact, from plan + code search):**
- Modify: src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs (extend Approve to optionally record snapshot)
- Create/Modify: src/ProjectAegis.Data/Snapshots/DbSnapshotStore.cs (add or expose method to record after approve, e.g. RecordApprovedBatch or ComputeAndStoreSnapshot)
- Create: src/ProjectAegis.Data.Tests/Snapshots/DbSnapshotStoreTests.cs (or extend existing; TDD first)
- Modify: src/ProjectAegis.MissionEditor.Cli/CatalogWriteApproveCommand.cs (after approve success, call snapshot store if --bind-snapshot or always for P2)
- Modify: src/ProjectAegis.Data/Scenario/ScenarioPackage.cs or related if needed for DbSnapshotId wiring (minimal)
- Test: src/ProjectAegis.Data.Tests/... (filter CmoMarkdown or new snapshot tests)
- Modify: src/ProjectAegis.MissionEditor.Cli.Tests/CatalogImportMarkdownCommandTests.cs (add test for approve + snapshot)
- Docs: update the phase2 plan checkboxes.

**GitNexus (MUST before any edit):**
- Run impact on "CatalogWriteGate" upstream (direction upstream).
- Run impact on "DbSnapshotStore" upstream.
- If any d=1 in Sim/Delegation, route changes only through existing seams (IWriteGate, no direct).

**TDD per superpowers:test-driven-development + plan:**

- [ ] **Step 3.1: Write failing test for snapshot after approve (P2-3 core)**
  In new or existing test file (e.g. ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateSnapshotTests.cs or DbSnapshotStoreTests.cs):
  ```csharp
  [Test]
  public void ApproveBatch_AfterPropose_RecordsStableSnapshotHash()
  {
      // Arrange: mini sensor markdown import via CmoMarkdownImporter + WriteGate.Propose...
      var gate = new CatalogWriteGate(testDbPath, clock);
      var batch = gate.ProposeSensorBatch(bindings, source: "sensor.md:phase2");
      // Act
      var approveResult = gate.ApproveBatch(batch.Id, approver: "sprint18-agent");
      var store = new DbSnapshotStore(testDbPath);
      var snap = store.RecordApprovedImport(approveResult.ApprovedIds, sourceFile: "sensor.md", importBatchId: batch.Id);
      // Assert
      Assert.That(snap.ContentHash, Is.Not.Null.And.Not.Empty);
      var snap2 = store.RecordApprovedImport(... same ...);
      Assert.That(snap2.ContentHash, Is.EqualTo(snap.ContentHash)); // stable
      // Also verify ICatalogReader.GetSortedSensorBindings() sees the rows
  }
  ```
  Run test → expect FAIL (method not exist or no hash).

- [ ] **Step 3.2: Implement minimal in DbSnapshotStore + CatalogWriteGate**
  Add to DbSnapshotStore (follow existing pattern in the file):
  ```csharp
  public DbSnapshotRecord RecordApprovedImport(IReadOnlyList<string> approvedIds, string sourceFile, string importBatchId)
  {
      // stable order
      var canonical = string.Join("|", approvedIds.OrderBy(id => id));
      var hash = ComputeSha256(canonical + sourceFile); // deterministic, no wall clock
      // persist to sqlite table (use existing connection or new; follow provenance)
      // return new DbSnapshotRecord { Id = ..., ContentHash = hash, ... }
  }
  ```
  In CatalogWriteGate.ApproveBatch (after successful write/approve):
  - Call the store if the batch is sensor type (P2 scope).
  - Do not change public signature (HIGH impact callers).

- [ ] **Step 3.3: Wire in CLI Approve command (P2-2 bulk + approve path)**
  In CatalogWriteApproveCommand.cs (or new proposer/approve flow):
  After gate.ApproveBatch success:
  ```csharp
  if (bindSnapshot)
  {
      var store = new DbSnapshotStore(databasePath);
      var snap = store.RecordApprovedImport(...);
      Console.WriteLine($"Snapshot {snap.Id} hash={snap.ContentHash} bound for replay");
  }
  ```
  Update help text in Program.cs for the command.

- [ ] **Step 3.4: Run test to green + add determinism test**
  Re-run the test from 3.1 → PASS.
  Add second test: two independent imports of same data produce identical hash.
  Run full: `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter CmoMarkdown or Snapshot -v minimal`

- [ ] **Step 3.5: Update CLI test + end-to-end smoke**
  Extend CatalogImportMarkdownCommandTests.cs to call approve path + assert snapshot recorded (use --bind-snapshot flag or default for phase2).
  Verify `ICatalogReader.GetSortedSensorBindings()` returns stable ordered rows post-approve.

- [ ] **Step 3.6: Verify no replay impact + full gate**
  Run replay filters; they should stay 7/7 (catalog only affects new imports, not locked baltic fixtures unless changed).
  Re-run full local gate from Pre-Work.

- [ ] **Step 3.7: Update phase2 plan + sprint docs**
  Mark the [ ] in 2026-06-04-catalog-phase2-import.md as done with date/SHA.
  Note in sprint-18-kickoff or agentic doc.

**Acceptance (from catalog plan):**
- Human approve commits; `ICatalogReader.GetSortedSensorBindings()` reflects rows in stable order.
- Snapshot manifest hash stable across two imports of same approved batch.
- Tests green; replay golden unchanged.

**Implementation notes (YAGNI/DRY):** Reuse FixedCatalogClock or similar for determinism. No new public APIs on hot paths. All via IWriteGate seam. Chunking (500) already in P2-1 CLI.

---

### Task 4: Update Tracking & Status (sprint-status.yaml)

**Files:**
- Modify: production/sprint-status.yaml (the machine source of truth)

- [ ] **Step 4.1: Update sprint 18 section**
  Change:
  ```
  s18-c2-signoff:
    status: done
    completed: "2026-06-05"
    note: "Headless/proxy + updated checklist evidence-2026-06-05.md; full Editor manual per runbook (local Unity)"
  ```
  Confirm other s18- tasks remain done. Bump tests_passed to current (e.g. 387+).

- [ ] **Step 4.2: Update header/current if needed**
  Ensure `current_sprint: 18` and status notes reflect close (or "complete" if all done).

- [ ] **Step 4.3: Cross-update epics**
  In wave5-engage-cyber-logistics-slice/EPIC.md: change status note to "Code-complete + Sprint 18 QA evidence collected (headless PASS; see c2-*-evidence)".

- [ ] **Step 4.4: Run gitnexus__detect_changes (MCP) or CLI**
  After yaml edit (data-only, low risk), run:
  ```
  npx gitnexus detect-changes --repo cmano-clone
  ```
  (or MCP gitnexus__detect_changes with scope "unstaged").
  Report any affected processes (expect minimal, since yaml + docs).

---

### Task 5: Final Verification + Closeout

- [ ] **Step 5.1: Re-run full recommended verification (from AGENTS.md + CI SOP)**
  Exact commands in Pre-Work 0.2 + capture output to new evidence or append to smoke.

- [ ] **Step 5.2: Optional nice-to-haves if time (S18-06/07)**
  - If sprint time allows: run `map-systems` skill for any new gameplay GDD gaps (low priority).
  - Hindsight retain: if server up, use hindsight tools to record "Sprint 18 closed: C2 gate + catalog P2".

- [ ] **Step 5.3: Produce closeout note**
  Append to production/agentic/sprint-18-closeout-2026-06-05.md (or update kickoff):
  Summary of what was done, links to evidence, verdicts, open items (e.g. "Unity Editor manual requires local dev with 6000.3.14f1").

- [ ] **Step 5.4: GitNexus + superpowers final**
  - MCP: gitnexus__detect_changes
  - Confirm no HIGH risk from changes.
  - Use superpowers:verification-before-completion if available in dir.

**Acceptance for whole plan:** All checkboxes in this plan green. Sprint 18 tasks in yaml = done. No blocking bugs. Local gate PASS. Ready for /sprint-status or producer sign-off.

---

## Post-Plan Execution Notes
After this plan is written:
1. User approves the plan file.
2. Controller announces "Using subagent-driven-development to execute the plan."
3. Extract tasks, create TodoWrite with all (including pre-work).
4. For each task: dispatch implementer (provide full task text + context + "follow TDD, run exact commands, use search_replace for edits, update todos"), then spec reviewer, then code quality reviewer (use the *-prompt.md templates).
5. For QA-heavy tasks (1,2): the implementer subagent can call tools to run dotnet, read/write qa docs, invoke other skills via description if needed.
6. For code task (3): strict TDD in steps.
7. At end: final reviewer + finishing-a-development-branch.

**Risks / Escalation:**
- If GitNexus impact shows CRITICAL on a symbol for catalog changes: stop, surface to user, use only read-only or existing public methods.
- If PlayMode or tests fail during run: treat as BLOCKED, file bug in production/qa/bugs/, do not mark done.
- Manual C2: cannot fully "implement" without Editor; plan documents the limitation explicitly.

**Next after close:** Update production/sprints/ with closeout, consider sprint-19 kickoff if needed, or archive.

---
*Plan created following superpowers:writing-plans on 2026-06-05. All steps are bite-sized, exact-file, with commands/code where applicable.*
