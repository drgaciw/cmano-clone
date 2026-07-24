# Parallel Research Dispatch — Template & Failure Remediation

**Created:** 2026-07-24, after a 6-agent research wave in which **2 agents silently returned nothing**
**Companion:** [`linear-parallel-dispatch-playbook.md`](linear-parallel-dispatch-playbook.md) — §4 Surface rule, §6 WIP limits, §8 dispatch loop
**Skill:** `superpowers:dispatching-parallel-agents`

This template covers the **research** fan-out — read-only investigation across independent domains, synthesised by the coordinator. For implementation fan-out (agents that write code), use the playbook's worktree + Surface rules instead.

---

## 1. Why this exists

A 6-agent wave on the H7 requirement gaps (DRG-43…48) produced 4 usable briefs and 2 silent failures. The failures cost roughly **200k tokens of completed research** and were only caught because the coordinator read every result before synthesising.

The root cause was **a flaw in the dispatch prompt, not in the agents**.

Every agent was told:

> *"Do NOT create or modify any file. No Write, no Edit."*

The intent was "don't touch the repo." The effect was to remove the agents' only means of persisting work. When a run was cut off mid-loop, everything it had learned existed solely in volatile context and was lost.

**One constraint turned a partial truncation into total loss.**

---

## 2. What the transcripts showed

From `~/.claude/projects/<project>/<session>/subagents/agent-*.jsonl`:

| Agent | tool_use turns | Tokens | Outcome |
|---|---|---|---|
| DRG-47 | 9 | 94k | ✅ |
| DRG-45 | 11 | 125k | ✅ |
| DRG-46 | 16 | 103k | ✅ |
| **DRG-48** | **20** | **139k** | ❌ nothing returned |
| DRG-43 | 29 | 92k | ✅ |
| **DRG-44** | **34** | **65k** | ❌ fragment only |

Every agent file contains exactly **one `end_turn`**. For both failures that single `end_turn` came from the *resume* — their original runs ended while `stop_reason` was still `tool_use`, i.e. **cut off mid-loop, never reaching the write-the-answer step**.

### Root cause is NOT established

All agents are configured `maxTurns: 20`. DRG-48 landing on exactly 20 is suggestive — but **DRG-43 ran 29 turns and completed normally**, so a hard stop at 20 does not explain the data. Raising `maxTurns` may well be a no-op.

What is solid: the two failures were the two heaviest runs, and both died mid-loop. Treat the mechanism as **unknown** and design for robustness rather than betting on one cause.

---

## 3. The five rules

### R1 — Read-only means *repo*-read-only, never *write-nothing*

> ❌ "Do NOT create or modify any file."
> ✅ "Do not modify anything in the repository. Write your brief incrementally to `<scratchpad>/<issue>-brief.md` as you go — do not wait until the end."

Verify the intent held with `git status --porcelain` after the wave. That is the actual guarantee; a blanket write ban is not.

### R2 — Give an explicit turn budget

> "You have roughly 20 tool calls. If you approach that, **stop investigating and write up what you have.** A short honest brief beats a truncated perfect one."

Free, needs no config change, and works whatever the underlying limit turns out to be.

### R3 — Require a verified/unverified split

> "Mark anything you could not confirm as **not verified** rather than inferring it."

DRG-44's "Not verified" list was among the most valuable output of the whole wave — and spot-checking it found one item **wrong in a load-bearing way** (`RemainingFuelKg` was described as possibly dead code; it is live in `FuelStateProjection`, meaning two parallel fuel computations both ship).

### R4 — Never trust `status: completed`

Both failures reported completed. One returned a fragment, one returned nothing.

After every wave, before synthesising:
1. Confirm each agent's brief file exists and is non-trivial in length
2. Confirm each returned result is actually present, not an empty string
3. Treat missing/short as **failed** regardless of reported status

### R5 — Resume before re-dispatching

`SendMessage` to the agent id resumes from its transcript with context intact. In this wave, DRG-48's resume produced a complete brief using **zero** additional tool calls — the research was all still there.

Resume cost ~68k and ~116k tokens against originals of ~65k and ~139k. A re-dispatch would have discarded finished work and re-hit the filesystem for answers the agent already held.

---

## 4. Pre-dispatch checklist

- [ ] **Surface-disjoint?** Per playbook §4 — if agents would write to the same paths, do not fan out writes. Research fan-out is naturally disjoint because it is read-only
- [ ] **Within WIP limit?** 4–6 effective tracks per `local-cloud-agent-routing.md`
- [ ] **Scope split for heavy subjects?** An agent asked to *inventory + analyse + draft + risk-assess* is a truncation candidate. Split into sequential lighter agents
- [ ] **Scratchpad path in every prompt?** (R1)
- [ ] **Turn budget in every prompt?** (R2)
- [ ] **Output structure specified?** Numbered sections, so a partial brief is still parseable

---

## 5. Prompt skeleton

```
READ-ONLY RESEARCH TASK. Repo: <abs repo path>

Produce a decision brief. **Do not modify anything in the repository.**
Write your brief incrementally to <scratchpad>/<id>-brief.md as you go —
do not wait until the end. Partial output there is far better than nothing.

You have roughly 20 tool calls. If you approach that, stop investigating and
write up what you have. A short honest brief beats a truncated perfect one.
Mark anything you could not confirm as "not verified" rather than inferring it.

## Subject
<issue id / requirement / traceability row / current status>

## Context you need
<GDD path, traceability path, key source paths, 2-3 existing ADRs for house format>
<standing invariants: suite floor, ReplayGolden, Baltic hash, ZERO-hotpath rules>

## GitNexus
Available via Bash. You MUST pass the explicit absolute repo path or it fails
with "Multiple repositories indexed" (two checkouts are registered):
  gitnexus impact  --repo <abs path> --target <Symbol> --direction upstream
  gitnexus context --repo <abs path> <Symbol>
  gitnexus query   --repo <abs path> "<concept>"

## Deliver exactly this structure
1. What exists today
2. The precise gap
3. Decisions required — options + a recommendation each
4. Determinism / invariant risk
5. Blast radius — GitNexus numbers; flag CRITICAL hubs
6. Effort
7. Verdict — engineering work, or does it need a product decision first?

Cite file paths. Flag ambiguity rather than guessing.
```

---

## 6. Post-wave protocol

1. **Verify** every brief per R4 — before reading any of them for content
2. **Resume** any that failed, per R5, asking explicitly for "what you established, with unverified items marked"
3. **Spot-check** load-bearing claims yourself. The skill's own guidance: *"agents can make systematic errors."* In this wave, spot-checking a "not verified" item found a live two-implementations divergence, and verifying a claimed bug confirmed a **real latent defect** (DRG-49 — same-tick mission events collide on order-log `sequenceId`, corrupting the replay fingerprint)
4. **Confirm the repo is untouched** — `git status --porcelain`
5. **Synthesise**, and record findings where the work lives (Linear comments, not just chat)

---

## 7. Deliberately not changed

**`maxTurns: 20` in `.claude/agents/*.md` was left alone.** DRG-43 completed a 29-turn run under that same setting, which is direct evidence the value is not enforced as a hard stop here. Raising it would most likely be a no-op, and it would churn committed config on a guess.

If someone later wants to test it: raise it on **one** agent file, re-run a known-heavy subject, and compare turn counts. Treat it as an experiment, not a fix.

---

*Derived from the 2026-07-24 H7 research wave. Transcript evidence: `~/.claude/projects/<project>/<session>/subagents/agent-*.jsonl`.*
