# Linear Usage Contract

**Created:** 2026-07-26, from failure modes observed while running a full Linear + repo + CI session
**Companion:** [`linear-parallel-dispatch-playbook.md`](linear-parallel-dispatch-playbook.md) — reconciliation, `Surface` rule, WIP limits, dispatch loop
**Companion:** [`parallel-research-dispatch-template.md`](parallel-research-dispatch-template.md) — read-only research fan-out

The playbook covers *how work is dispatched*. This covers *how Linear is kept truthful*. They are separate because dispatch discipline held up well; record-keeping is where the drift happened.

---

## 1. The one rule

> **A Linear issue is a pointer plus a state. It is never the content.**

Git holds files. Notion holds design. Linear's unique job is **what's in flight, what's blocked, what's next**.

Every time content was embedded in Linear, it drifted within days:

| Case | What happened |
|---|---|
| **DRG-51** | Carried a full ADR body in the description, *plus* a Linear document, *plus* a Notion page — while the file existed in **no branch**. Three copies, zero source of truth |
| **DRG-52** | A duplicate of DRG-51 that had its *own* second copy of that document, independently searchable in Linear |
| **DRG-49** | Its description asserted the wrong *reason* a bug was latent. Following it would have **activated** the bug against two golden-backed fixtures |

An issue body should carry: the git path, the current state, the blocking reason, and a link. If a reader needs the decision itself, they open the file.

**Corollary:** if an issue names a `Git source of truth` path, that path must exist in a branch. DRG-51 named one that did not — the same failure shape as the stranded artifacts in DRG-42 and PR #324.

---

## 2. States — use `In Review`

The team has seven states. **`In Review` (type: `started`) was completely unused**; every PR-backed issue sat in `In Progress`, so "being written" was indistinguishable from "waiting on a human".

| State | Means |
|---|---|
| `Backlog` | Not scheduled |
| `Todo` | Scheduled, not started |
| `In Progress` | Being worked |
| **`In Review`** | **PR open, work complete, waiting on review or a gate** |
| `Done` | Merged to `main` |
| `Canceled` / `Duplicate` | Terminal, not done |

**`Done` means merged.** Not "PR opened", not "branch pushed". An issue whose work sits in an unmerged PR is `In Review`.

---

## 3. Automate the transitions — do not hand-maintain them

Roughly eight issue states were flipped by hand in one session. Hand-maintained state is the root cause of most drift here.

- **Magic words work** — `Closes DRG-54` in a PR body auto-attached the PR to the issue
- **Use Linear's generated branch name.** Every issue exposes `gitBranchName` (e.g. `drgamtd/drg-54-scenario-policy-loaders-...`). Branching from it makes linkage automatic and bidirectional — which matters more here than usual, because `gt` stacking already obscures branch↔PR↔issue mapping
- **Wire the GitHub integration** so PR opened → `In Review` and PR merged → `Done`

Anything a human or agent must remember to update will eventually be wrong.

---

## 4. Red gates become issues automatically

**The most consequential blocker in the project's history had no Linear issue.**

Buildkite failed at 0s on every build for seven days, blocking `main` and 18 PRs. It was tracked nowhere until DRG-53 was opened on 2026-07-26 — a week in, and only because someone went looking.

**Rule:** any required check red on `main` for more than one build opens an issue at `Urgent`. CI health is delivery state, and delivery state belongs in the tracker.

---

## 5. PR-mirror issues: automate or retire

`pr-mirror` labelled issues (DRG-31…38) shadow live PRs. The label's own description says *"Transient… Closes on merge."* In practice they do not close, and they go stale in a way that actively misleads:

> **DRG-35** sat at **Urgent** long after its actual reason — four stranded governance artifacts — had been resolved by a different PR (#340). Nothing updated it for days.

Either let the GitHub integration provide PR linkage and drop the mirrors, or close them automatically on merge. A mirror that outlives its PR is worse than no mirror.

---

## 6. Priorities are claims about *now*

`Urgent` should mean *someone should stop what they are doing*. When the reason for a priority is resolved elsewhere, the priority is stale and must move. Re-read `Urgent` and `High` items whenever their blocking reason changes — not on a schedule.

---

## 7. Milestones, not cycles

Keep the outcome-keyed milestones (**H1–H7**). **Do not adopt Cycles.**

At peak this project runs ~3 sprints/day. That cadence is what made the S94–S97 roadmap obsolete before it was committed — it proposed sprints that had already run. Time-boxed cycles would go stale the same way. Milestones keyed to outcomes and triggers survive variable cadence; date-boxes do not.

Same reasoning applies to reopen conditions: **DRG-46**'s trigger is *"a 50+-shooter scenario entering the corpus"*, not a date.

---

## 8. Deliberately not adopted

| | Why |
|---|---|
| **Backfilling history** | The waterline decision stands — 10 of the original 12 issues described already-shipped work. Backfilling ~350 items produces an archive nobody reads |
| **Estimates** | One owner plus agents. Estimates cost more than they inform. Revisit when a second human joins |
| **Cycles** | See §7 |

---

## 9. Hygiene checks

Cheap, and each one catches a failure that actually occurred:

- [ ] Does every issue citing a `Git source of truth` path have that file in a branch? *(DRG-51 did not)*
- [ ] Does any issue duplicate content that lives in git or Notion? *(DRG-51/52 did)*
- [ ] Is any `pr-mirror` issue older than its PR's merge? *(DRG-35 was)*
- [ ] Is any `Urgent` item's blocking reason already resolved? *(DRG-35 was)*
- [ ] Is any red required check on `main` untracked? *(Buildkite was, for 7 days)*
- [ ] Does any ADR/document number appear twice? *(ADR-018 did — DRG-43 vs DRG-51)*

---

## 10. Smallest high-value next step

**Wire the GitHub integration and start using `In Review`.**

That alone removes the manual state-flipping that produced most of the drift documented here — and it is configuration rather than discipline, so it cannot be forgotten.

---

*Derived from the 2026-07-24 → 2026-07-26 Linear reconciliation, H7 requirement-gap research, and CI outage. Every failure mode cited is a real occurrence in this project, not a hypothetical.*
