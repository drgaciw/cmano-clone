---
name: qa-gauntlet-combat-ui
description: >
  QA Gauntlet specialist for combat-presentation Smoke/Pressure: headless Engage /
  Kill / CombatDomains / magazine-salvo ReplayGolden filters. Not C2 Play Mode
  (qa-gauntlet-ui) and not Combat UX Slice B (DRG-165–170). Failures route to
  /qa-gauntlet-remediation + UCA. Use when /qa-gauntlet-combat-ui,
  /team-qa-gauntlet --mode combat-ui, or combat HUD/kill-chain chrome is in scope.
argument-hint: "[--run-id <id>]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Bash, Task
---

# QA Gauntlet Combat-UI — Engage/Kill Presentation Gates

**Owns:** automated **combat-presentation** gates (engage/kill/combat-domain chrome).  
**Does not own:** C2 Play Mode signoffs ×5 (`/qa-gauntlet-ui`), headless Demo ladder
(`/qa-gauntlet`), or **Combat UX Slice B (DRG-165–170)** implementation.

Do **not** dual-write Slice B. Do not edit `src/`, `unity/`, catalog DB,
`DelegationBridge.cs`, or `BalticReplayHarness`. Ask before writing outside
`production/qa/gauntlet/<RUN_ID>/`.

## Phase 1 — Identity

```text
RUN_ID = <arg> | gauntlet-$(date +%Y%m%d-%H%M)-combat-ui
RUN_DIR = production/qa/gauntlet/<RUN_ID>/
```

Create `combat-ui/`, `manifest.yaml` (`track: combat-ui`), `AAR.md`.

Hard invariants: ZERO-touch `DelegationBridge.cs`; Baltic v2 hash
`17144800277401907079`; script-first (LLM never overrides `dotnet` non-zero).

## Phase 2 — Headless package

```bash
set -euo pipefail
export PATH="${HOME}/.dotnet:${PATH}"
UA_TEST="src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj"
SIM_TEST="src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj"

require_passed_floor() {
  local log="$1" min="$2" label="$3"
  local passed
  passed="$(grep -Eo 'Passed:[[:space:]]*[0-9]+' "$log" | tail -1 | grep -Eo '[0-9]+' || true)"
  if [ -z "$passed" ]; then
    echo "FATAL: could not parse Passed count from ${log} (${label})" >&2
    exit 1
  fi
  if [ "$passed" -lt "$min" ]; then
    echo "FATAL: ${label} passed=${passed} floor=${min}" >&2
    exit 1
  fi
}

dotnet test "$UA_TEST" \
  --filter "FullyQualifiedName~CombatDomains|FullyQualifiedName~PolicyEngage|FullyQualifiedName~ReplayGoldenBalticEngage|FullyQualifiedName~ReplayGoldenBalticKill|FullyQualifiedName~ReplayGoldenBalticMagazine|FullyQualifiedName~ReplayGoldenBalticSalvo" \
  -v minimal --nologo | tee "${RUN_DIR}/combat-ui/dotnet-combat-ui.log"
require_passed_floor "${RUN_DIR}/combat-ui/dotnet-combat-ui.log" 1 "combat-ui UA filter"

dotnet test "$SIM_TEST" \
  --filter "FullyQualifiedName~CombatDomainValidator" \
  -v minimal --nologo | tee "${RUN_DIR}/combat-ui/dotnet-combat-domain.log"
require_passed_floor "${RUN_DIR}/combat-ui/dotnet-combat-domain.log" 1 "CombatDomainValidator"
```

Zero-match discovery is **FAIL**. Do not add Slice B screens/prefabs to raise the floor.

## Phase 3 — Routing

| Failure | Route |
|---------|--------|
| Headless red | `/qa-gauntlet-remediation`; UCA if Surface is presentation |
| Missing combat chrome / Slice B gaps | **BLOCKED** — hand to Combat UX Slice B owners (DRG-165–170), do not implement here |
| C2 IA / Play Mode ×5 | Wrong skill — `/qa-gauntlet-ui` |
| Manual feel | `/team-qa` / `/smoke-check` |

Verdict: **PASS** \| **FAIL** \| **BLOCKED**.

## Never

- Run or rewrite `/qa-gauntlet-ui` C2 signoff loop.
- Implement DRG-165–170 Combat UX Slice B.
- Touch `BalticReplayHarness` source; tests are **run-only**.

## See also

- `/team-qa-gauntlet --mode combat-ui`
- `/qa-gauntlet-ui` — C2 chrome / ReplayGolden family / Editor signoffs
- `/qa-gauntlet-remediation` — Phase D + UCA
- `/qa-gauntlet` — sim ladder
