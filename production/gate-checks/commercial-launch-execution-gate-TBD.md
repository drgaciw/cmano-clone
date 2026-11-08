# Commercial Launch Execution Gate — TBD (Future Phase)

**Date:** 2026-06-25  
**Status:** **STUB — NOT EXECUTABLE** (explicitly deferred post–S72 prep)  
**Authority:** `production/commercial-launch-scope-boundary-2026-06-25.md` + `docs/reports/future-sprint-roadpmap-062526.01.md` §3 (E9 Baltic v3 lead; E7 execution separate)  
**Stage requirement:** `production/stage.txt` must read **Launch** (currently **Release**)

---

## Purpose

Documents prerequisites for **commercial launch execution** (store submission, revenue, locale production, marketing spend). S69–S72 completed **prep only**; S73–S80 is **Baltic v3 content** (E9). This gate is a placeholder until the user expands scope beyond internal engineering + content trains.

**Do not execute** any item below without explicit human ack and a new scope boundary superseding this stub.

---

## Prerequisites (all required before gate opens)

### 1. Stage & governance

- [ ] Human ack to advance `production/stage.txt` from **Release** → **Launch**
- [ ] New scope boundary published (supersedes commercial-launch prep boundary for execution phase)
- [ ] `release-checklist-v3.md` E7 prep sections marked complete with evidence index links

### 2. Asset pipeline (>5% production threshold)

- [ ] `design/assets/asset-manifest.md` exists and is indexed from art bible + store checklist
- [ ] Capsule, screenshots, trailer placeholders replaced with reviewed assets per `production/release/store/asset-checklist.md`
- [ ] Addressables / remote content strategy documented (if applicable)

### 3. i18n production (not prep-only)

- [ ] P0 `en-US` string lock + extraction CI green
- [ ] At least one additional locale production pass (not spec-only) per i18n pipeline spec
- [ ] Localization QA plan executed (PlayMode or headless locale smoke)

### 4. Store & platform accounts

- [ ] Steam / platform developer accounts active and payment/tax profiles complete
- [ ] Store page live-draft reviewed (not internal draft only)
- [ ] Platform certification requirements checklist per target SKU
- [ ] Privacy, EULA, and support URLs published

### 5. CI / release ops

- [ ] Buildkite (or successor) production promotion pipeline distinct from preflight
- [ ] Branch protection + required checks enforced on trunk
- [ ] Rollback and hotfix runbook tested once

### 6. Standing invariants (carry forward)

- [ ] Test baseline ≥1232/0f monotonic
- [ ] ReplayGolden 6/6; C2 18/18
- [ ] Production Baltic v2 hash `17144800277401907079` preserved unless ADR
- [ ] ZERO `DelegationBridge.cs` edits
- [ ] GitNexus pre on CRITICALs (178/97/127/52 exact)

---

## Gate commands (when authorized)

```bash
export PATH="$HOME/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

# GitNexus pre (MCP list/detect/impact on CRITICALs)
dotnet build ProjectAegis.sln --no-restore -v minimal
dotnet test ProjectAegis.sln -v minimal --no-build
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --no-build --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --no-build --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"
grep -r "17144800277401907079" tests/regression/replay-golden-baltic-v2-*.txt | head -3
grep -r "DelegationBridge" --include="*.cs" . | grep -vE 'adapter|UnityAdapter|Bridge' | wc -l
```

**Pass criteria:** 0e build; ≥1232/0f; 6/6 replay; 18/18 C2; hash preserved; ZERO hotpath; all prerequisite checkboxes above satisfied.

---

## Human sign-off (required at execution time)

> "Commercial launch execution authorized" — explicit user ack; records date + checklist version.

---

## Related documents

- `production/release/release-checklist-v3.md` — future phase section link
- `production/commercial-launch-scope-boundary-2026-06-25.md` — prep scope (S69–S72 COMPLETE)
- `production/baltic-v3-scope-boundary-2026-06-25.md` — active program (S73–S80; no E7 execution)
- `docs/architecture/architecture-review-2026-06-25-post-s72-v3.md` — CONCERNS; asset + Launch gaps

**STOP:** Await explicit user scope expansion before executing this gate.
