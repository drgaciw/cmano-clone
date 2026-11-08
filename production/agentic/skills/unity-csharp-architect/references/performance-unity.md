# Unity performance budgets

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md) §5 / finish checklist  
**Program:** UCA-M2 · **Audience:** agents touching C2 bind paths, map symbols, hosts, hot loops  
**Implements:** DRG-132 (UCA-08 part: performance)

> **Law (one line):** Rebuild projections on the **sim-tick / dirty** boundary; frame path is interpolate + pool apply — **no unexplained per-frame GC**. Prove rich C2 bind headless under the Req 20 budget before Play Mode polish.

**Related:** [`mono-anti-patterns.md`](mono-anti-patterns.md) §3.4 · [`testing-unity.md`](testing-unity.md) · [`presentation-boundary.md`](presentation-boundary.md)

---

## 1. Two clocks → two budgets

| Clock | Work allowed | Budget mindset |
| --- | --- | --- |
| **Sim tick** | Snapshot → `*Projection` / `*Bridge.Build` → panel bind inputs | Headless-measurable; must stay deterministic |
| **Frame** | Interpolate poses, USS, camera, pool sync of **already built** rows | Main-thread smoothness; avoid alloc |

Do **not** run full rich-panel projection rebuild every `Update` “to be safe.” Use dirty flags / tick boundary (composition host).

---

## 2. Documented numeric gates (repo)

| Gate | Source | Rule |
| --- | --- | --- |
| **Req 20 rich C2 panel bind** | `C2PanelPerfBenchTests` | After warmup, **p95 and max < 100 ms** over n=20 samples (headless wall-clock) |
| **Map symbol UI elements** | `MapSymbolPool` + `MapSymbolPoolTests` | **Reuse** `VisualElement`s across syncs — no clear+recreate on identical ids |
| **DelegationBridge hotpath** | Program invariant | **Zero-touch** — no new hotpath logic through Release v1 |

Unity Profiler deep dives are Editor-side; **agents must still land headless benches** for pure bind math when changing projection/bind volume.

```text
// Anchor: src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PanelPerfBenchTests.cs
// Req20PanelBindBudgetMs = 100; Warmup=3; Measured=20
```

---

## 3. Hot-path rules (c-sharp-engineer)

| Prefer | Avoid |
| --- | --- |
| Pre-sized / pooled collections | Per-frame `new List<>`, LINQ `Where/Select/ToList` |
| Struct rows / `IReadOnlyList` bind | Boxing enums/ids in UI loops |
| `StringBuilder` or static labels | `$"..."` per symbol per frame |
| `MapSymbolPool.Sync` style reuse | Destroy/instantiate symbols every tick |
| `NonAlloc` physics / ray queries | Allocating `RaycastAll` in frame path |
| Cache components in `Awake` | `GetComponent` / `Find*` in `Update` |
| Dirty-flag bind | Blind full rebuild every frame |

### Pool catalog (Aegis)

| Pool / reuse surface | Role |
| --- | --- |
| `MapSymbolPool` | UI Toolkit map symbols — id-keyed reuse, in-place move |
| Projection input fixtures in benches | Untimed setup; timed path = bind only |
| VisualElement USS class toggles | Prefer class swap over rebuild |

---

## 4. Profiling workflow (agent)

1. **Classify** change: sim-tick projection vs frame chrome.  
2. **Headless first:** extend or run `C2PanelPerfBenchTests` / targeted `dotnet test` filter when bind volume changes.  
3. **Alloc review:** no new per-frame LINQ/concat in hosts (`mono-anti-patterns`).  
4. **Play Mode** only after headless gate green — Profiler for GPU/UI Toolkit cost, not as the only proof.  
5. **Document** any intentional alloc (e.g. one-shot on selection change) in PR body.

---

## 5. What is *not* a Unity perf problem

| Domain | Where budgets live |
| --- | --- |
| Engagement resolver / sim tick CPU | Sim benchmarks / gauntlet — not this file |
| Agent attention “pool” trimming | Delegation decision pipeline docs |
| SQLite catalog I/O | Data layer — never from UI frame loop (ADR-006) |

This reference owns **presentation + adapter bind** budgets only.

---

## 6. Agent checklist (before Done)

- [ ] Projection rebuild not forced every frame without dirty/tick reason  
- [ ] No new unexplained per-frame allocations on host hot paths  
- [ ] Map/list UI uses pooling / reuse where symbols churn  
- [ ] Req 20 bind path still under **100 ms** p95/max when bind surface grows (bench updated if needed)  
- [ ] `DelegationBridge` hotpath untouched  
- [ ] PR notes perf impact (none / bench numbers / waiver)  

---

## 7. See also

| Doc | Use |
| --- | --- |
| [`mono-anti-patterns.md`](mono-anti-patterns.md) | Alloc anti-patterns |
| [`testing-unity.md`](testing-unity.md) | Where perf tests live |
| `docs/engineering/c2-projection-layer.md` | Projection cost model |
| `src/…/C2PanelPerfBenchTests.cs` | Numeric Req 20 gate |
| `unity/…/MapSymbolPoolTests.cs` | Pool reuse proof |

**UCA-M2 note:** Budgets are pointers to tests — update numbers in code + this table together.
