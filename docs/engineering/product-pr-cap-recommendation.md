# Product PR cap recommendation (Jev, 2026-10-29)

> **Status:** Recommendation only — **not adopted**. The standing product PR cap is still **4**. Changing it requires the owner's (Danny's) decision.

## 1. Recommendation

- **Cap of 3 open product PRs while GameCI is broken**, reverting to **4** once automated Unity compile works again.
- Confidence: **~70%** for this recommendation, versus **~25%** for keeping the cap at 4.
- Renovate and docs PRs stay outside the cap.

## 2. Rubric

Each criterion is weighted (out of 100) and scored for how hard it pushes toward a lower cap (1 = little, 5 = strong).

| Criterion | Weight | Push (1–5) | Why |
|---|---:|---:|---|
| Review bandwidth | 30 | 5 | Danny is the only reviewer, merger, Editor compiler and Play Mode walker. |
| Stale-branch risk | 20 | 5 | At 6–8 drafts in October, rebases went stale. |
| Merge throughput | 15 | 3 | The cap should match what Danny merges in 2–3 days. |
| CI load | 10 | 2 | GitHub Actions plus Buildkite can handle 4+. |
| GameCI licence broken | 10 | 4 | Every PR needs a manual Editor compile. |
| Agent concurrency | 10 | 1 | Agents could flood review, which argues for a lower cap, not a higher one. |
| Blast-radius mix | 5 | 3 | PRs near hard-no files cost more to review. |

**Overall weighted pressure: ~3.9 / 5**, leaning toward a tighter cap.

## 3. Suggested rules

1. At most **one** PR touching a hard-no or ReplayGolden file may be open at a time (it counts toward the cap).
2. Drafts with **no pushes for 5+ days** still count toward the cap and get flagged to the owner (rebase or close is the owner's call).
3. New cloud agent launches **wait in a queue** while the cap is full.

## 4. Status as of 2026-10-29

- Recommendation only; not adopted. Cap remains **4**.
- Catch: #770, the GameCI fix, DRG-412 and DRG-404/320 are already in flight. Adopting 3 today means one slice PR (DRG-412 or DRG-404/320) waits. Jev would keep GameCI first, because fixing it is what lets the cap return to 4.
