# Local vs Cloud Agent Routing — 10-Sprint Program (S39–S48)

**Date:** 2026-06-20  
**Authority:** 10-sprint agent program plan + `AGENTS.md` Cloud Agent path  
**Enforcement:** Every parallel kickoff must cite this matrix in track tables.

---

## Assignment matrix

| Track type | Local (Cursor) | Cloud Agent | Rationale |
|------------|----------------|-------------|-----------|
| Baseline + CI + closeout | **Primary** | OK | Headless .NET 8 supported on Cloud VMs |
| C2/Platform code + proxy tests | Primary | **Primary** | Headless proxy is the gate; Unity Editor optional |
| Hygiene / docs / coordination | Primary | **Primary** | No Editor required |
| Perf / replay / determinism | Either | **Primary** | `dotnet test` + `/replay-verify` |
| Live Editor PNG evidence | **Local only** | Defer or proxy-only | Cloud VM lacks Unity Editor 6.3 |
| Art bible / UX docs | Either | **Primary** | Markdown/design files |
| Catalog/Import (S40+) | Mixed | **Careful** | HIGH blast-radius — **single owner per Catalog cluster** |
| B3 refactor (S44) | **Primary** | Parallel sub-tracks | GitNexus `rename`/`impact`; split Decision vs Telemetry |
| B4 DOTS/ECS (S45) | **Primary** | Limited | determinism-engineer review; cloud for test-only sub-tracks |
| B5 launch artifacts (S46) | Either | **Primary** | Docs/checklists/localization specs |
| Release gate (S47–S48) | **Coordinator local** | Cloud for CI verify | Human sign-off on scope gate + release |

---

## Per-sprint agent budget

| Environment | During parallel wave | Notes |
|-------------|---------------------|-------|
| **Local** | Up to **6** concurrent Cursor agents | RAM/CPU + merge bandwidth limited |
| **Cloud** | Up to **5** concurrent Cloud Agents | Drop or localize Editor-evidence track |
| **Combined** | **4–6 effective tracks** | Cloud handles code/test; local owns Editor evidence + coordinator merge |

---

## Dispatch rules

1. **Editor evidence tracks** → always local (S39-07, S40-07, S43 evidence).
2. **Catalog symbol cluster** → one local lead agent; no cloud split (S40-03, S42 content-catalog-platform).
3. **Closeout** → local coordinator merges stacks, writes smoke/retro/sprint-status.
4. **Cloud-eligible** → hygiene, perf, replay, ADR/docs, store/i18n (S41, S46).
5. **S42–S48** → blocked until scope gate; routing applies after gate approval.

---

## Verification before merge (every track)

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal          # ≥ prior baseline
dotnet test ... --filter ReplayGoldenSuiteTests  # 6/6
dotnet test ... --filter PlayModeSmokeHarnessTests  # 18/18+
```

GitNexus: `impact()` before symbol edits; `detect_changes()` before commit.

---

*Routing matrix for S39–S48. Pair with `production/agentic/s39-s48-worktree-manifest.md`.*
