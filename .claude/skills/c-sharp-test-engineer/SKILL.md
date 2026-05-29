---
name: c-sharp-test-engineer
description: "Author automated tests for Unity C# (or plain .NET) code using the Unity Test Framework (NUnit): EditMode and PlayMode tests, fixtures, and mocks. Classifies test mode, maps coverage to acceptance criteria, writes to tests/, and runs the suite."
argument-hint: "[path-to-code-under-test or story]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Edit, Write, AskUserQuestion
model: sonnet
agent: c-sharp-test-engineer
---

## Phase 1: Load Context

Read:
- The code under test (the argument) and its acceptance criteria / `## QA Test Cases` (if a story).
- `.claude/docs/coding-standards.md` → **Testing Standards** and **Automated Test Rules**.
- `docs/engine-reference/dotnet/README.md` — [unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices), [TimeProvider testing](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) for clock seams.
- Existing tests under `src/*Tests/` and `tests/` to match conventions (NUnit vs xUnit per project) and reuse helpers (`tests/helpers/` if present).

---

## Phase 2: Classify & Plan

For each behavior, decide:
- **EditMode** (pure logic, no scene/frame) — preferred. Formulas, state machines, AI decisions.
- **PlayMode** (needs game loop, physics, coroutines, scene lifecycle) — use only when required.

List the test cases as `scenario → expected`, and map each to an acceptance
criterion. Flag any criterion that is **untestable as written** (logic locked in
MonoBehaviour lifecycle, statics with no injection) — request a seam from
`c-sharp-engineer`/`c-sharp-architect` before writing brittle tests.

---

## Phase 3: Approval Gate

Present the test plan and target file paths (mirroring source layout under
`tests/unit/[system]/` or `tests/integration/[system]/`, files named
`[System]_[Feature]Tests.cs`). Ask **"May I write these tests to [paths]?"**

---

## Phase 4: Write Tests

Follow the rules strictly:
- Methods named `[Scenario]_[Expected]`; Arrange-Act-Assert.
- **Deterministic**: inject clock/RNG; no wall-clock or order dependence.
- **Isolated**: per-test setup/teardown; no shared mutable statics.
- **No real I/O / network / DB**: use injected fakes/mocks.
- Maintain the test `.asmdef` (reference `nunit.framework` + systems under test) for Unity tests.

---

## Phase 5: Run & Report

Execute the suite and report pass/fail with output — never claim green without running:
- Unity: Test Runner, or CI `game-ci/unity-test-runner@v4` (EditMode/PlayMode).
- Plain .NET (`src/`): `dotnet test`.

If tests fail, report the failure verbatim and diagnose. Do **not** skip/disable
tests to go green. Offer next step: fix in `c-sharp-engineer`, or hand to
`c-sharp-devops-engineer` to wire into CI.
