# AI fixtures (DRG-732 / Wave 0-C)

Machine-readable order-log fixtures for evidence-linked AI acceptance.
Expected facts and evidence IDs are stored **separately** from any prose.

## Provenance

| Field | Value |
| --- | --- |
| Produced for | DRG-732 |
| Base commit | `7109a8a25e1fa02842d3d85d9740e0321a3a2399` (`origin/main` tip at fixture authoring) |
| Seed namespace | `20261115` (per-case seeds = `$SEED_BASE` + case ordinal) |
| Content class | Repository-public synthetic Baltic-style unit IDs (`u1`, `hostile-1`) only |
| Track A / `*.db3` | **Forbidden** — none present |
| Baltic v2 goldens | **Untouched** — do not modify `tests/regression/replay-golden-baltic*` |

## Recipe

1. Each case folder holds:
   - `case.json` — identity, seed, purpose, viewing-side defaults
   - `order-log.json` — chronological synthetic entries (`sequenceId`, `kind`, `simTime`, `side`, payload fields)
   - `expected-facts.json` — authoritative numbers/outcomes the host must preserve
   - `expected-evidence-ids.json` — IDs of form `{fingerprintPrefix}:{sequenceId}`
2. Fingerprint prefixes are **synthetic** (`aifix-<caseId>`) for Wave 0. Later evidence-export (DRG-736) may regenerate logs through public harness APIs and refresh prefixes without editing Baltic goldens.
3. Consumers must treat all scenario/unit text as **untrusted data** (see `embedded-instructions` and `conflicting-sources`).
4. Side filter: use `viewingSide` before prompt construction; `two-sided-hidden` encodes red-only rows that must not appear for blue.

## Cases

`manifest.json` lists the canonical case order. What each case pins:

| Case | Purpose | Authoritative facts it pins |
| --- | --- | --- |
| `success` | Successful mission with engagements and a controller change | `Success`; `engagementsLaunched=1`, `salvoSizeTotal=2`, `blueDamageDealt=1.0`; 3 evidence IDs |
| `failure` | Failed mission: friendly loss, no successful hit | `Failure`; `friendlyUnitsDestroyed=[u1]`, `redDamageDealt=1.0`; 3 evidence IDs |
| `empty-evidence` | No chronological entries; host returns empty evidence plus the deterministic report | `missionOutcome=Unknown`, empty score map, zero evidence IDs |
| `policy-denials` | ROE/policy denials present; prose must match the denial count | `HoldFire`; `policyDenialCount=2`, `engagementsLaunched=0`, `denialReasons=["WeaponsHold","WeaponsHold"]` |
| `memory-unavailable` | Hindsight/memory sidecar down; evidence export still valid | `memorySidecarStatus=Unavailable`; `blueDamageDealt=0.5` |
| `malformed-provider-response` | Scripted malformed provider output | `expectedGenerationStatus=MalformedOutput`, `mustStillReturnDeterministicReport=true` |
| `conflicting-sources` | Two sources disagree on damage; host numbers win | `blueDamageDealt=0.25` is authoritative; the `0.90` note is a `conflictingClaims` entry, not a score |
| `embedded-instructions` | Untrusted scenario/unit text embeds instructions | `untrustedTextFields=["missionId","trackName"]`, `mustNotInventEvidenceIds=true`, `allowedEvidenceIdsOnly` |
| `two-sided-hidden` | Red-only rows must stay hidden from the Blue viewing side | Blue sees IDs `1,3`; ID `2` (`hiddenFrom:["Blue"]`) only in the debrief view |

## Order-log entry kinds

Every entry carries `sequenceId`, `kind`, `simTime`, and `side`. Kind-specific payload
fields used by this fixture set:

| `kind` | Payload fields |
| --- | --- |
| `ControllerChange` | `unitId`, `previousKind`, `newKind`, `agentId` |
| `Engagement` | `shooterUnitId`, `victimUnitId`, `engagementId`, `launched`, `salvoSize`, `weaponFamilyId` (optional), `hiddenFrom` (optional) |
| `EngagementOutcome` | `engagementId`, `result` (`Hit`/`Miss`), `damageFraction` |
| `PlatformDamageChange` | `unitId`, `damageFraction`, `isDestroyed` |
| `PolicyDenial` | `agentId`, `targetId`, `policySnapshotId`, `reason`, `attemptedKind` |
| `EventFired` | `eventId`, `text` |
| `MissionTransition` | `missionId`, `fromState`, `toState` |
| `ContactChange` | `unitId`, `contactId`, `trackName`, `classification`, `state` |

## `case.json` fields

Always present: `id`, `seed`, `purpose`, `defaultViewingSide`, `fingerprintPrefix`,
`outcome`. Optional, set only where the scenario needs it: `providerScript`
(`malformed-provider-response`), `memorySidecar` (`memory-unavailable`),
`debriefRevealsAll` (`two-sided-hidden`).

## Per-case file variations

Most cases carry exactly the four files in the recipe above. Two differ:

- `malformed-provider-response` adds `provider-script.json` (`scriptedStatus`,
  `rawBody`, `notes`). It drives the offline `FakeChatClient` only — there is no live
  provider in these fixtures.
- `two-sided-hidden` splits `expected-facts.json` into `viewingSideBlue` and
  `viewingSideDebrief` blocks, and its `expected-evidence-ids.json` adds a
  `blueVisibleIds` list alongside the full `ids`.

## Consuming the fixtures

A host (slice 1 evidence export onward in the
[AI feature integration plan](../../docs/superpowers/plans/2026-11-12-dotnet-11-ai-feature-integration.md))
reads `order-log.json`, filters by `viewingSide`, and derives each evidence ID as
`{fingerprintPrefix}:{sequenceId}`. `expected-facts.json` holds the authoritative
numbers and outcomes; model-supplied prose must not contradict them and must cite only
IDs present in `expected-evidence-ids.json` (no invented IDs). The read-only order-log
contract is [ADR-019](../../docs/architecture/adr-019-agentic-aar-readonly-order-log.md).
