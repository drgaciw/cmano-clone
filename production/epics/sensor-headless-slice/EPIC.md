# Epic: Sensor Headless Slice

> **Status:** Ready for stories  
> **Created:** 2026-06-01  
> **Priority:** MVP  
> **Layer:** Core (sim tick step 4)  
> **Depends on:** [baltic-headless-slice](../baltic-headless-slice/EPIC.md) (Complete)

## Goal

Replace harness-only contacts with a **deterministic, logged** contact feed: scenario-seeded contacts evolve on tick 4, emit `ContactChange` in the order log, and drive `ObservedState` / engage aborts without C2 UI.

## Scope (in)

- `ContactChange` record + `OrderLogEntryKind.ContactChange` in fingerprint
- Headless contact FSM MVP: `Unknown → Detected → Lost` (classify P1)
- Scenario JSON contact seeds for `baltic-patrol` (stable sorted iteration)
- `ISimWorldSnapshot` fed from contact service (not hardcoded `hostile-1`)
- Tests: same seed → identical contact list + fingerprint at tick T (GDD AC-1, AC-2)

## Scope (out)

- Full Platform DB `basePd` tables
- EW noise jam (TR-sensor-003) — follow-on story batch
- Side picture / datalink sharing (TR-sensor-004)
- C2 contact UI, covariance ellipses
- Cyber/comms delayed sharing

## Governing docs

| Doc | TR |
|-----|-----|
| [sensor-detection-ew.md](../../design/gdd/sensor-detection-ew.md) | TR-sensor-001..004 |
| [order-log-replay.md](../../design/gdd/order-log-replay.md) | ContactChange schema |
| [policy-roe-emcon-wra.md](../../design/gdd/policy-roe-emcon-wra.md) | EMCON gate (Active radar) |
| [engagement-fire-control.md](../../design/gdd/engagement-fire-control.md) | Track quality → engage |

## ADRs

- ADR-003 order-log schema (extend union)
- ADR-004 tick pipeline order (step 4 contacts before engage)

## Acceptance (epic-level)

1. Two runs, same seed → identical contact ids + states at tick T.
2. Every contact state transition produces a `ContactChange` row in fingerprint.
3. Engage `NoFireControlTrack` when EMCON/snapshot denies track (not harness default).
4. `dotnet test ProjectAegis.sln` green; replay CLI includes `ContactChange|` lines.

## Proposed stories (run `/create-stories sensor-headless-slice`)

| # | Slug (draft) | Focus |
|---|--------------|-------|
| 001 | contact-change-order-log | Schema + fingerprint |
| 002 | scenario-contact-seed | JSON seeds + sorted loop |
| 003 | observed-state-from-contacts | Bridge wiring |

## Engine risk

**Medium** — new sim subsystem; keep hot path allocation-free; no SQLite per tick.

## Blockers

- GDD **In Review** — recommend `/design-review` → Approved before story 002 implementation.

## Untraced / greenfield

GitNexus: no sensor symbols yet; new namespace `ProjectAegis.Sim.Sensors` (proposed).