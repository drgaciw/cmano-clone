---
name: qa-gauntlet-ui
description: >
  Game UI Smoke/Pressure track for QA Gauntlet: headless PlayMode + C2/Presentation
  filters, ReplayGolden, Unity Editor C2 Play Mode signoffs (×5), artifact AAR.
  Failures route to /qa-gauntlet-remediation + UCA. Manual UAT stays on /team-qa /
  /smoke-check. Use when /qa-gauntlet-ui, /team-qa-gauntlet --mode ui|ui-smoke,
  or the user asks for "gauntlet UI", "C2 Play Mode pressure", or "UI smoke gauntlet".
argument-hint: "[--run-id <id>] [--unity-version 6000.3.22f1] [--skip-signoff]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, Task
---

# QA Gauntlet UI — Smoke / Pressure (not UAT)

**Owns:** automated game-UI gates for C2 chrome / presentation Surfaces.  
**Does not own:** headless Demo ladder oracles (`/qa-gauntlet`), forge/calibrate/stress
sim axes, or **manual UAT** (`/team-qa`, `/smoke-check` human batches).

**Manual UAT → `/team-qa`** (and `/smoke-check` for sprint hand-off). Do not invent a
second human click-through loop here.

## When to invoke

| Trigger | Action |
|---------|--------|
| `/team-qa-gauntlet --mode ui` or `ui-smoke` | This skill (full package) |
| `/qa-gauntlet-ui` | This skill |
| Presentation defect mid-ladder | Still `/qa-gauntlet-remediation` + UCA — this skill is proactive gates |

## Hard invariants

- **ZERO-touch** `DelegationBridge.cs`; Baltic v2 hash `17144800277401907079` preserved.
- Prefer headless `dotnet test` before Editor Play Mode; UI is a **client** (ADR-010).
- Script-first: LLM never overrides a failed signoff PASS line or `dotnet` non-zero exit.
- On any red gate → dispatch **`/qa-gauntlet-remediation`** (UCA / pr-finish required for
  UnityAdapter / Presentation / C2 Surfaces). Cite ADR-010 §2–3, ADR-007, ADR-001.

## Run identity

```text
RUN_ID = <arg> | gauntlet-$(date +%Y%m%d-%H%M)-ui
RUN_DIR = production/qa/gauntlet/<RUN_ID>/
```

Create at start: `ui/`, `ui/signoff/`, `run-id.txt`, `git-sha.txt`, `branch.txt`,
`manifest.yaml` (fill verdict at end), `AAR.md` (from template below).

## Package (hard-coded — do not freestyle)

### 1) Headless UI / C2 / Presentation suite (“118-style” filter)

```bash
export PATH="${HOME}/.dotnet:${PATH}"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~PlayModeSmoke|FullyQualifiedName~Presentation|FullyQualifiedName~C2|FullyQualifiedName~MapPlaceholder|FullyQualifiedName~MapCanvas|FullyQualifiedName~UnityCsharpScriptHygiene|FullyQualifiedName~Panel|FullyQualifiedName~MessageLog|FullyQualifiedName~SensorC2" \
  -v minimal --nologo | tee "${RUN_DIR}/ui/dotnet-ui-suite.log"
```

Floor: **0 failures**. Record passed count (reference run: 118). PlayModeSmoke is
included in the filter; if suite is green, PlayModeSmoke family is green.

### 2) ReplayGolden family

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter ReplayGolden -v minimal --nologo | tee "${RUN_DIR}/ui/replay-golden.log"
```

Floor: **0 failures** (reference: 17 including ReplayGolden family; AGENTS.md still
requires ReplayGolden **6/6** subset — both must stay green).

### 3) Unity Editor C2 Play Mode signoffs (×5)

Editor: Hub `6000.3.x` matching `unity/ProjectAegis/ProjectSettings/ProjectVersion.txt`
(default path Linux: `$HOME/Unity/Hub/Editor/<ver>/Editor/Unity`).

```bash
UNITY="${UNITY:-$HOME/Unity/Hub/Editor/6000.3.22f1/Editor/Unity}"
PROJ="unity/ProjectAegis"
for METHOD in RunBatch RunClassifyBatch RunDoctrineBatch RunImportBatch RunBeginExecutionBatch; do
  timeout 300s "$UNITY" -batchmode -nographics \
    -projectPath "$PROJ" \
    -executeMethod "ProjectAegis.Unity.Editor.C2PlayModeSignoffBatchRunner.${METHOD}" \
    -logFile "${RUN_DIR}/ui/signoff/${METHOD}.log"
  grep -q 'C2PlayModeSignoffBatchRunner PASS:' "${RUN_DIR}/ui/signoff/${METHOD}.log" || exit 1
done
```

| Method | Scenario (signoff) |
|--------|-------------------|
| `RunBatch` | `baltic-patrol-comms` |
| `RunClassifyBatch` | `baltic-patrol-classify` |
| `RunDoctrineBatch` | `baltic-patrol-mission-roe` |
| `RunImportBatch` | `baltic-patrol-classify` |
| `RunBeginExecutionBatch` | `baltic-patrol-classify` |

`--skip-signoff` only when Editor unavailable — mark AAR **BLOCKED (signoff)** and
do not claim full UI PASS.

### 4) Invariants snippet → `ui/invariants.txt`

- `grep -r 17144800277401907079 tests/ data/` (hits > 0)
- Confirm no intentional `DelegationBridge.cs` edits in the working tree for this run

### 5) Failure routing

| Failure | Route |
|---------|--------|
| `dotnet` UI suite / ReplayGolden red | `/qa-gauntlet-remediation` (TDD); UCA if Surface is presentation |
| Signoff `FAIL` / `SIGNOFF_ERROR:` / missing PASS | Same; prefer reproduction via failing headless test first |
| Compile / plugin DLL | Refresh `tools/copy-delegation-assemblies.sh`, re-run; still remediation if code fix |
| Manual visual/feel gaps | **Stop** — hand off to `/team-qa` / `/smoke-check` (not this skill) |

## AAR template

Write `production/qa/gauntlet/<RUN_ID>/AAR.md`:

```markdown
# Gauntlet UI-track AAR — <RUN_ID>

**Team:** `/team-qa-gauntlet --mode ui` → `/qa-gauntlet-ui`
**Verdict:** PASS | FAIL | BLOCKED

| Gate | Result |
|------|--------|
| Headless UI/C2/Presentation suite | N/N |
| ReplayGolden filter | N/N |
| C2 signoff ×5 | PASS/FAIL each |
| Hash / DelegationBridge | OK / note |
| Remediation / UCA | N/A \| dispatched `<ids>` |

**Manual UAT:** not in scope — use `/team-qa` / `/smoke-check`.
**Ladder:** not run — use `/qa-gauntlet` / `--mode ladder`.
```

Also write `manifest.yaml` with `track: game-ui`, gate exits, and `remediation_required`.

## Success

- All automated gates green **or** defects filed + `/qa-gauntlet-remediation` invoked.
- Artifacts under `production/qa/gauntlet/<RUN_ID>/`.
- No claim of manual UAT completion.
- No edits to `/qa-gauntlet` ladder contract for UI purposes.

## See also

- `/team-qa-gauntlet` — orchestrator (`--mode ui` / `ui-smoke`)
- `/qa-gauntlet-remediation` — Phase D + UCA
- `/qa-gauntlet` — headless sim ladder (separate)
- `/team-qa`, `/smoke-check` — **manual UAT / sprint smoke**
- Reference run: `production/qa/gauntlet/gauntlet-20260817-1626-ui/`
