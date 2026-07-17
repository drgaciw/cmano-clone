# Scenario policy JSON authoring (`*.policy.json`)

Reference for authoring the sim **policy** files under [`data/scenarios/`](../../data/scenarios/).
These files drive the headless simulation: ROE, engagement priming, EMCON, detection trials,
mission timelines, contact-triggered ROE escalation, logistics, comms, and more. They are what
`ScenarioPolicyRepository.TryGet(id)` returns and what the Baltic replay harness / CI replay
golden tests load.

> **Not the same as scenario *documents*.** `data/scenarios/*.policy.json` (this guide, mapped to
> `ScenarioPolicyJsonDto`) is distinct from the ADR-008 authoring documents validated by
> [`scenario-document.schema.json`](../../data/scenarios/scenario-document.schema.json)
> (`ScenarioDocumentDto`) — see [scenario-document-authoring.md](scenario-document-authoring.md).
> Policy files are **sim-runtime** config; documents are **authoring** artifacts. Do not conflate
> the two.

For the short template-ID matrix and per-sprint content notes, see
[`data/scenarios/scenario-policy-ids.md`](../../data/scenarios/scenario-policy-ids.md); this page
is the full field/loader reference.

---

## How policy files are loaded

Source: [`ScenarioPolicyJsonLoader`](../../src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs),
[`ScenarioPolicyRepository`](../../src/ProjectAegis.Sim/Scenario/ScenarioPolicyRepository.cs),
[`ScenarioPolicyJsonIndex`](../../src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonIndex.cs),
[`ScenarioDataPaths`](../../src/ProjectAegis.Data/Scenario/ScenarioDataPaths.cs).

- **Discovery** — the default directory is resolved by walking up from `AppContext.BaseDirectory`
  (up to 10 levels) looking for a `data/scenarios/` folder. Any file matching the glob
  **`*.policy.json`** in that directory is loaded. Call
  `ScenarioPolicyRepository.LoadFromDirectory(path)` to point at a different directory.
- **Keying is by `id`, not filename.** Each file is deserialized and indexed by its `id` field.
  If two files declare the same `id`, the last one enumerated wins — keep `id` unique and, by
  convention, matching the filename stem.
- **JSON overrides built-ins.** `TryGet(id)` returns the JSON profile when one exists for that id,
  otherwise falls back to the built-in catalog. Built-in ids: `baltic-patrol`,
  `baltic-patrol-opp-hold-fire`, `restricted-engagement`, `test-sandbox-dual-side`
  (see [`ScenarioPolicyJsonCatalog`](../../src/ProjectAegis.Data/Scenario/ScenarioPolicyJsonCatalog.cs)).
- **Property names are case-insensitive** (`PropertyNameCaseInsensitive = true`). The examples
  below use `camelCase`, which is the repo convention.
- **The catalog is cached** after first load (`EnsureDefaultJsonLoaded`). A long-lived process
  reloads only via an explicit `LoadFromDirectory` call.

Minimal valid file:

```json
{
  "id": "my-drill",
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "HoldFire"
}
```

---

## Top-level fields

All fields except `id` are optional; omitting one applies the default in the table. Unless noted,
these map 1:1 onto `ScenarioPolicyProfile` via the loader's `To*` parsers.

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `id` | string | `""` | Catalog key. Keep unique; convention = filename stem. |
| `friendlyRoe` | ROE enum | `WeaponsFree` | Side ROE for friendly units. |
| `opposingRoe` | ROE enum | `WeaponsFree` | Side ROE for opposing units. |
| `playerInfoModel` | enum | `FullTransparency` | Fog/transparency model (req 02/03). |
| `personalityEditPolicy` | enum | `Anytime` | When agent personalities may be edited. |
| `allowDualSideControl` | bool | `false` | Permit controlling both sides (sandbox/tests). |
| `unitOverrides` | map `unitId → ROE` | — | Per-unit ROE override; inherits side `maxSalvo`. |
| `engage` | object | — | Engagement priming / DLZ / combat-domain flags (see below). |
| `contacts` | array | — | Scripted contact seeds (`appearAtTick`, `hasFireControlTrack`). |
| `emcon` | object | — | Per-unit radar EMCON state (`units[unitId].radar`). |
| `detection` | array | — | Inline detection trials (`basePd`, `envMask`, `jamStrength`, `eccmFactor`, `requiresActiveRadar`). |
| `catalogDetection` | array | — | Catalog-backed detection (Pd from platform catalog; no inline `basePd`). |
| `catalogWithdraw` | array | — | Per-platform withdraw HP% seeding. |
| `jammers` | array | — | Jammer sources (`targetId`, `jamStrength`, `activeFromTick`, `observerId`). |
| `contactLifecycle` | object | defaults | `staleThresholdTicks` (≥1), `classifyAfterTicks`/`identifyAfterTicks` (0 = disabled). |
| `replay` | object | 300 | `checkpointIntervalTicks` (min 1). |
| `mission` | object | — | `fireOrder`, `events`, `triggers` (see Mission triggers). |
| `missionPolicy` | object | — | Mission-tier ROE override (`roe`, `unitIds`, `maxSalvo`); req 13 inheritance. |
| `delegation` | object | defaults | `usePatrolCandidates`. |
| `comms` | array | — | Timed comms-state transitions (`atTick`, `newState`, `nodeId`, `reason`). |
| `logistics` | object | defaults | Fuel/joker/bingo model. |
| `commsDisplay` | object | defaults | Degraded-comms presentation (lag, ghost offset). |
| `speculative` | object | campaign default | `blackProjectMode`, `maxTechnologyLevel`. |
| `unitReadiness` | map `unitId → {readyForLaunch}` | — | Per-unit launch readiness. |
| `spoofTracks` | array | — | Timed spoof events (`atTick`, `contactId`, `reason`). |
| `telemetry` | object | disabled | Balance-drift detection + per-entity balance trials. |
| `datalink` | object | defaults | `organicOnly` (default `true`), `unitSides`, `shareLagTicks` (≥0). |
| `mineHazard` | object | — | Mine zone + transit schedule (see validation below). |

### `engage` sub-fields

Maps to `ScenarioEngageDefaults`. Defaults shown are the loader/DTO defaults:

| Field | Default | Meaning |
|-------|---------|---------|
| `rangeMeters` | `50000` | Engagement range. |
| `envelopeMinMeters` / `envelopeMaxMeters` | `1000` / `100000` | Weapon envelope. |
| `defaultMagazineRounds` | `2` | Magazine priming for MVP engage. |
| `hasFireControlTrack` | `true` | FC track present by default. |
| `pkBase` / `pkIntercept` / `pkKill` | `0.85` / `0.0` / `1.0` | Probability-of-kill tuning. |
| `salvoSize` | `1` | Rounds per salvo. |
| `maxSalvo` | *(side default 8)* | WRA cap; `> 0` else `EffectivePolicy.DefaultMaxSalvo` (8). Drives side + unit `maxSalvo`. |
| `weaponTechnologyLevel` | `0` | Tech gate. |
| `weaponRequiresBlackProject` | `false` | Requires speculative black-project mode. |
| `dlzPersonality` | — | DLZ personality (parsed by `DlzPersonalityParser`). |
| `combatDomain` | — | Combat domain (parsed by `CombatDomainParser`). |
| `mountOnline` | `true` | Weapon mount availability. |
| `contactIdentified` | `true` | Whether target is treated as identified. |
| `combatDomainsEnabled` | `false` | **ADR-009 registry validators.** Leave `false` on `baltic-patrol-*` variants unless explicitly documented in `scenario-policy-ids.md` (replay-hash sensitive). |

---

## Enum values

| Enum | Accepted values (case-insensitive) | On unknown value |
|------|-------------------------------------|------------------|
| ROE (`friendlyRoe`, `opposingRoe`, `unitOverrides.*`, `missionPolicy.roe`, `mission.triggers[].roe`) | `HoldFire`, `WeaponsTight`, `WeaponsFree` | **Throws** `InvalidDataException`. |
| `playerInfoModel` | `FullTransparency`, `DelegationFog`, `TieredByAutonomy` | Throws (empty → `FullTransparency`). |
| `personalityEditPolicy` | `Anytime`, `PlanningOnly`, `TieredRebrief` | Throws (empty → `Anytime`). |
| `emcon.units.*.radar` | `Off`, `Passive`, `Active` | Throws. |
| `telemetry.balanceTrials[].entityKind` | `BalanceEntityKind` values (e.g. `Platform`) | Throws. |
| `mission.triggers[].targetClass` | `Any`, `Surface`, `Air` | **Falls back** to `Any`. |
| `mission.triggers[].side` | `friendly`, `opposing` | **Falls back** to `friendly`. |

A malformed enum in a *throwing* field fails the whole load with an `InvalidDataException` naming
the bad value — a fast way to catch typos in CI.

---

## Mission contact triggers (contact-triggered ROE escalation)

Baltic v3 uses `mission.triggers` to escalate ROE on first recon detection (e.g. ASuW/AAA →
`WeaponsFree` when a recon unit first detects a contact). Runtime:
[`MissionContactTriggerRuntime`](../../src/ProjectAegis.Delegation/Mission/MissionContactTriggerRuntime.cs)
— see [mission-timeline-runtime.md](mission-timeline-runtime.md) for the full runtime behaviour
(tick ordering, order-log output, `ApplyRoeToUnits`, determinism).

Semantics (verified against the runtime):

- A trigger fires **once**, on a contact transition from `Unknown → Detected` only.
- It fires when `observerId` matches the transitioning observer **and** the contact's target
  class matches `targetClass` (`Any` matches everything; classification via
  `MissionContactTargetClassifier`, which keys off the target id, e.g. `ucav*`).
- Triggers are evaluated in **`id` order** (`StringComparer.Ordinal`) for determinism; keep ids
  stable and sortable.
- On firing it emits the trigger's `missionCode` + `roe` for the listed `side` / `unitIds`.

```json
{
  "id": "baltic-v3-asuw-escalate",
  "friendlyRoe": "WeaponsTight",
  "mission": {
    "triggers": [
      {
        "id": "asuw-weapons-free-on-recon",
        "observerId": "ucav-blue",
        "targetClass": "Surface",
        "side": "friendly",
        "missionCode": "ASUW",
        "roe": "WeaponsFree",
        "unitIds": ["u1"]
      }
    ]
  }
}
```

> **Baltic v3 isolation.** `baltic-v3-*` policies and their replay goldens are independent from
> v2. Never edit v2 goldens or the production hash while authoring v3 content.

---

## Validation errors to expect

The loader throws `InvalidDataException` (fails the load) for:

- Any unknown value in a *throwing* enum field (table above).
- `mineHazard.zoneMinRangeMeters > zoneMaxRangeMeters`.
- `mineHazard.triggerRadiusMeters < 0`.
- `datalink.shareLagTicks < 0`.
- File content that does not deserialize to a `ScenarioPolicyJsonDto` (message names the file).

Silent normalization (no error): `maxSalvo`/`missionPolicy.maxSalvo` ≤ 0 → default `8`;
`replay.checkpointIntervalTicks` < 1 → `1`; `contactLifecycle.staleThresholdTicks` < 1 → `1`;
`commsDisplay.degradedStaleThresholdDivisor` ≤ 0 → `1`; mine lethality clamped to `[0, 1]`.

---

## Determinism & replay constraints

Policy loading is deterministic: parsers sort collections with `StringComparer.Ordinal`, use
`SeededRng` downstream, and read no wall-clock/`DateTime` state. When authoring or editing:

- **Do not change** the production Baltic v2 replay hash `17144800277401907079`. Edits to
  `baltic-patrol` / any fixture feeding the ReplayGolden 6/6 suite must keep the hash — a golden
  change requires an ADR (see [determinism-and-replay.md](determinism-and-replay.md)).
- Keep `combatDomainsEnabled` at its documented value per fixture (see
  [`scenario-policy-ids.md`](../../data/scenarios/scenario-policy-ids.md)).
- Prefer additive new-id policies over editing shared fixtures.

Validate a scenario end-to-end (id resolution, catalog references, determinism) with the
`scenario-audit` workflow and the headless [Mission Editor CLI](mission-editor-cli.md).

---

## Related docs

| Where | What |
|-------|------|
| [`data/scenarios/scenario-policy-ids.md`](../../data/scenarios/scenario-policy-ids.md) | Template-ID matrix + per-sprint content notes. |
| [scenario-document-authoring.md](scenario-document-authoring.md) | The sibling `*.scenario.json` authoring-document format (`ScenarioDocumentDto`). |
| [determinism-and-replay.md](determinism-and-replay.md) | Replay hashing, golden-fixture workflow, the hash invariant. |
| [mission-editor-cli.md](mission-editor-cli.md) | Headless CLI/MCP verbs to author, validate, and simulate scenarios. |
| [`ProjectAegis.Sim` README](../../src/ProjectAegis.Sim/README.md) · [`ProjectAegis.Data` README](../../src/ProjectAegis.Data/README.md) | Sim/Data assemblies that own the loader and index. |
| [`docs/architecture/`](../architecture/) | ADR-002 (policy evaluator), ADR-008 (mission validation), ADR-009 (combat-domain validators). |
| [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) | Hard invariants — replay hash, Baltic v3 isolation. |
