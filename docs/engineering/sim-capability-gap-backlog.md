# Simulation Capability Gap Backlog

**Created:** 2026-07-27
**Origin:** QA Gauntlet run `gauntlet-20260727-1455`. While expanding gauntlet scenario variability, a systematic sweep established which milsim capabilities the simulation actually models. This document records the ones it does **not**, so they are scheduled deliberately rather than rediscovered each time someone tries to write a scenario that needs them.
**Companion:** [`docs/superpowers/specs/2026-07-27-gauntlet-variability-design.md`](../superpowers/specs/2026-07-27-gauntlet-variability-design.md) — the variability work that *was* achievable within current engine capability.

## How to read this

Every item was verified by source sweep across `src/**/*.cs` (excluding test assemblies), not assumed. Each is classified:

| Class | Meaning |
|---|---|
| **ABSENT** | No implementation anywhere. Adding it is new engine work. |
| **VOCABULARY-ONLY** | The concept exists as an authoring label or catalog string, but **no simulation behaviour consumes it**. More dangerous than ABSENT: a scenario using it will validate and run, and silently do nothing. |
| **PARTIAL** | Some real modelling exists, but a commonly-expected aspect is missing. |

Nothing here is a defect in the sense of "broken code". These are unbuilt capabilities. They are recorded because the QA ladder cannot test what the engine does not model, and because several are easy to *assume* exist.

---

## VOCABULARY-ONLY — highest priority to resolve

These are the traps. A scenario author can select them, validation passes, and nothing happens.

### GAP-01 — Support mission roles (Tanker / AEW / EW) have no simulation behaviour

- **Where the vocabulary lives:** `src/ProjectAegis.MissionEditor.Cli/MissionAddSupportCommand.cs` (`AllowedRoles = ["Tanker", "AEW", "EW"]`), `src/ProjectAegis.Data/Scenario/Authoring/MissionTemplateCatalog.cs` (`tpl-support-tanker`), `ScenarioDocumentDto.Role`.
- **What's missing:** nothing in `ProjectAegis.Sim` or `ProjectAegis.Delegation.UnityAdapter` references tanker, refuelling, or AEW. Verified: zero hits.
- **Consequence:** `mission_add_support --role Tanker` produces a valid scenario in which no aircraft is refuelled, no endurance is extended, and no early-warning coverage is provided. The mission is inert.
- **Why it matters for QA:** the gauntlet cannot test "refuelling missions" — requested during this session and declined for exactly this reason. The nearest honest substitute is the `logistics` block (fuel burn + joker/bingo), which models fuel *pressure* but never fuel *transfer*.
- **Suggested resolution:** either implement support-mission behaviour in the sim, or make the authoring layer reject/flag roles that have no runtime effect so the gap is visible at authoring time rather than discovered downstream.

### GAP-02 — `missionCode` is free-form and never acted upon

- **Where:** `ScenarioMissionContactTrigger.MissionCode` is passed into `MissionTransitionRecord` for the order log (`BalticReplayHarness.cs:416`) and nowhere else. No validation, no enum, no behaviour.
- **Consequence:** a scenario can declare `missionCode: "ASW"` while configuring a purely surface engagement. The label is documentation with no enforcement.
- **Mitigation already adopted:** the gauntlet variability spec requires mission *behaviour* to come from `CombatDomain` pairings, with `missionCode` used only as human-readable annotation.
- **Suggested resolution:** either promote `missionCode` to a validated enum tied to domain/behaviour expectations, or document it explicitly as a non-semantic label.

---

## ABSENT — environmental and physical modelling

### GAP-03 — Weather and sea state

- **Verified:** no `Weather`, `SeaState`, `Precipitation`, `CloudCover`, or `Visibility` modelling anywhere in `src/`.
- **What it would affect:** sensor performance (radar/EO/IR degradation), sea-state effects on small-craft mobility and weapon employment, weather windows as a tactical/planning dimension.
- **Note on `envMask`:** the `envMask` field on detection entries looks like environmental masking but is **RCS-signature-driven** (`PhaseBCatalogDetectionModifier.ApplyEnvMask` scales it by `signature.RcsBandDbsm`). It is not a weather model, and should not be repurposed as one without renaming.
- **Cost signal:** new sim subsystem + determinism guarantees + likely replay-golden movement + catalog data describing per-platform weather sensitivity.

### GAP-04 — Terrain, bathymetry, and littoral geography

- **Verified:** no terrain/bathymetry/coastline/land-mask modelling. The single `terrain` occurrence in `src/` is a code comment in `ValidationRules.cs:373` referencing a research document, not an implementation.
- **What it would affect:** line-of-sight and radar horizon, terrain masking, littoral vs open-ocean behaviour, land-attack geometry, submarine bottom/depth constraints.
- **Related absence:** **radar horizon / line-of-sight / occlusion** is likewise ABSENT — detection is currently range-and-probability based with no geometric occlusion.

### GAP-05 — Acoustic environment (ASW realism)

- **Verified ABSENT:** thermocline, convergence zones, sound-speed profiles, acoustic propagation layers.
- **What it would affect:** the credibility of ASW play generally. Subsurface engagements currently resolve on the same range/Pd basis as surface ones, with no layer exploitation, no shadow zones, and no depth-dependent detection.
- **QA impact:** the ladder introduces subsurface units at T3, but "ASW" testing is currently domain-tagging rather than genuine acoustic modelling. Worth being honest about in any coverage claim.

### GAP-06 — Day/night and illumination

- **Verified ABSENT:** no time-of-day, daylight, or illumination modelling.
- **What it would affect:** EO/IR sensor performance, visual identification timing, night-attack profiles.

---

## ABSENT — tactical systems

### GAP-07 — Soft-kill countermeasures (chaff, flares, decoys)

- **Verified ABSENT:** no chaff, flare, decoy, or general soft-kill modelling.
- **What it would affect:** the entire soft-kill layer of air/missile defence. Currently defence is hard-kill only via `pkIntercept`.
- **Note:** `jammers` and `spoofTracks` exist and provide *electronic* deception, so EW is partially represented — but expendable countermeasures are not.

### GAP-08 — Torpedo defence

- **Verified ABSENT:** no anti-torpedo countermeasures, towed decoys, or torpedo-evasion modelling.
- **Interacts with:** GAP-05 — meaningful torpedo defence is hard to model credibly without acoustic environment.

### GAP-09 — Weapon kinematics and time of flight

- **Verified ABSENT:** no flight time, time-of-flight, trajectory, or seeker-lock modelling.
- **Current behaviour:** engagements resolve within the tick they are launched — `Apply` → `ApplyInterceptOnHit` → `ApplyKillOnHit` execute in sequence inside `MvpEngagementResolver.Resolve`.
- **What it would affect:** in-flight engagement of incoming weapons, shoot-look-shoot doctrine, salvo timing and layered defence, and the tactical meaning of range (long shots currently cost no time).
- **Assessment:** probably the single highest-leverage absence for combat realism, and correspondingly invasive — it changes engagement from an instantaneous resolution to a multi-tick lifecycle, with direct determinism and replay-golden consequences.

### GAP-10 — Carrier / amphibious / deck operations

- **Verified ABSENT:** no flight-deck cycle, sortie generation, or amphibious operation modelling. The one `Amphibious` hit is a word-boundary comment in the catalog importer, not behaviour.
- **What it would affect:** sortie-rate limits as a constraint on air operations; air units are currently available without deck-cycle cost.

---

## PARTIAL — modelled, but with notable gaps

### GAP-11 — Catalog EMCON data absent (behaviour exists)

- **Status:** the scenario-level `emcon` block **is** implemented and consumed (`{"units": {"<id>": {"radar": "Active|Passive"}}}`), used by 7 non-gauntlet scenarios.
- **The gap:** the catalog's `platform_emcon` and `catalog_staging_emcon` tables are **both empty (0 rows)** despite correct schema. So there are no per-platform emissions profiles — only scenario-level overrides.
- **Consequence:** the qa-gauntlet skill's instruction to set "EMCON postures consistent with each platform's `CatalogEmcon` profile" is unsatisfiable as written.
- **Tracked separately as:** `production/qa/bugs/BUG-catalog-emcon-tables-empty.md`.
- **Resolution:** catalog content work via the `CatalogWriteGate` propose/approve path with sourced emitter/posture data — not something to invent during a QA run.

### GAP-12 — Damage modelling depth

- **Status:** damage **is** modelled — `CatalogDamage`, damage-withdraw engage gating (`CatalogDamageWithdrawEngageGate`), and a `catalogWithdraw` policy block exist across ~31 files.
- **The gap:** no damage-control, flooding, or repair-over-time modelling. Damage appears to be a state affecting engagement eligibility and withdrawal, rather than a progressive survivability system.
- **Assessment:** lower priority than the above — what exists is coherent; this is depth, not absence.

---

## Suggested prioritisation

Ordered by *QA value per unit of engine work*, not by realism ambition:

1. **GAP-01 / GAP-02 (vocabulary-only)** — cheapest to resolve and highest risk of silent misuse. Either implement the behaviour or make the authoring layer honest. These actively mislead today.
2. **GAP-11 (catalog EMCON data)** — content work, no engine change, unblocks genuine per-platform EMCON testing.
3. **GAP-09 (weapon time of flight)** — highest combat-realism leverage, but invasive; schedule deliberately with determinism/replay planning.
4. **GAP-03 / GAP-04 (weather, terrain)** — large new subsystems. High realism value, but they also unlock the most new QA dimensions, so they pay back in test coverage as well as fidelity.
5. **GAP-05 (acoustic environment)** — required before ASW testing can claim genuine fidelity.
6. **GAP-07 / GAP-08 (soft-kill, torpedo defence)**, **GAP-06 (day/night)**, **GAP-10 (deck ops)** — valuable, but each is additive rather than foundational.

## Method note

Findings were produced by pattern sweep over `src/**/*.cs` excluding test assemblies, followed by manual inspection of every hit to separate real implementations from incidental matches (for example, `Fire` matching `FireControl`/`FireOrder`, and `land` matching `Landing`). Where a capability appeared to exist, its consumption path was traced before classifying it — this is what distinguished VOCABULARY-ONLY from PARTIAL for the support-mission roles.

This sweep was scoped to capabilities relevant to gauntlet scenario variability. It is not an exhaustive audit of the simulation.
