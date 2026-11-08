# S31-08 story-done — C2 Manual Sign-Off Refresh

**Story:** `production/epics/sprint-31-presentation-polish/story-031-08-c2-signoff-refresh.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE WITH NOTES (lean mode)

| AC | Evidence | Status |
|----|----------|--------|
| `c2-manual-signoff-*.md` updated post-S31 SHA + verdict | `production/qa/c2-manual-signoff-2026-06-02.md` @ `3406bc4`, **PASS WITH NOTES 16/16** | COVERED |
| Checks 1–13 remain PASS | Headless proxy **61/61** (`PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu`) | COVERED |
| Check 14: Platform import staging | `PlatformImport` **9/9**; `platform-import-staging-s31-baltic-diff.png` | COVERED |
| Check 15: Doctrine ROE override | `Doctrine` **7/7**; `doctrine-panel-s31-roe-override.png` | COVERED |
| Check 16: Begin Execution (Planning) | `C2TopBar` **5/5**; `begin-execution-s31-planning-topbar.png` | COVERED |
| S31-07 evidence linked for 14–16 | `production/qa/sprint-31-presentation-evidence-2026-06-18.md` | COVERED |
| Evidence doc verdict + limitations | `production/qa/sprint-31-c2-signoff-2026-06-18.md` | COVERED |
| Lean PASS WITH NOTES (no Editor) | Headless Linux agent; ADR-010 / PI-006 | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |

Extends S19-01 checklist (13 → 16 rows). Merge authority: headless **61/61** baseline + **21/21** new checks.

## Deliverables

- `production/qa/c2-manual-signoff-2026-06-02.md` — refreshed @ `3406bc4` with checks 14–16
- `production/qa/sprint-31-c2-signoff-2026-06-18.md` — full closeout evidence
- `production/qa/sprint-31-presentation-evidence-2026-06-18.md` — S31-07 dependency (linked, not modified)
- `production/qa/evidence/*-s31-*.png` — checks 14–16 evidence (pre-existing S31-07)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

# Baseline checks 1–13
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu" -v minimal
# 61/61 PASS

# New checks 14–16
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar" -v minimal
# 21/21 PASS

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty

ls production/qa/evidence/*-s31-*.png
# 3 files
```

## Per-filter counts

| Filter | Result |
|--------|--------|
| Baseline `PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu` | **61/61 PASS** |
| New checks `PlatformImport\|Doctrine\|C2TopBar` | **21/21 PASS** |
| `PlatformImport` | **9/9 PASS** |
| `Doctrine` | **7/7 PASS** |
| `C2TopBar` | **5/5 PASS** |

## Advisory (lean mode)

Unity Editor batchmode not run on Linux host for checks 14–16 batch scenarios; headless proxy + S31-07 protocol placeholders are merge authority per S27-10/S31-07 pattern. Live Editor re-capture optional polish.