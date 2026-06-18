# Deterministic replay & world-hash verification — developer reference

Developer/operator reference for the **determinism toolchain** that proves Project Aegis
reproduces a given `(scenario, seed)` byte-for-byte on every run. It ties together the
seeded RNG primitives, the layered world-state hash, the order-log fingerprint, the headless
**Baltic replay harness**, and the pinned golden regression suite that gates every PR.

The core invariant (req-03 / req-04, ADR-004) is: **controllers and the simulation are pure
functions of `(observed state, traits, seed)`.** A build whose replay diverges is not
reproducible, and a non-reproducible sim is broken — even if every other unit test passes.
The aspirational gate is documented in the [`/replay-verify`](../../.claude/skills/replay-verify/SKILL.md)
skill; *this* doc describes the harness and goldens that actually implement it in-repo.

| Question | Answer |
|----------|--------|
| What proves reproducibility? | Two fresh runs of the same `(seed, scenario, ticks)` must produce an identical order-log fingerprint and identical `WorldHash` / `DetectionWorldHash`. Pinned goldens additionally lock those values against drift. |
| Where does the code live? | RNG + hashing: `src/ProjectAegis.Sim/Core/` and `src/ProjectAegis.Sim/Sensors/DetectionWorldHash.cs`. Order log: `src/ProjectAegis.Delegation/Decision/DecisionLog.cs` + `Replay/OrderLogReplayFingerprint.cs`. Harness: `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs`. |
| How do I run it? | Headlessly via `dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed N --scenario ID --ticks N`, or via the golden suite `dotnet test … --filter FullyQualifiedName~ReplayGoldenSuiteTests`. |
| What gates CI? | `ReplayGoldenSuiteTests` (req-17) runs on every PR via Buildkite; `main` also runs a dedicated post-merge Baltic replay step. |
| Where are the goldens? | [`tests/regression/replay-golden-*.txt`](../../tests/regression/), cataloged in `ReplayGoldenRegressionCatalog.cs`. |
| What does a FAIL mean? | **A != B** (two runs differ) = internal non-determinism, release-blocking. **A != golden** but A == B = behavior drift, needs a human decision (re-record vs regression). |

## Why it exists

The delegation framework lets AI controllers and the simulation drive units autonomously. For
the sim to be debuggable, fair, and replayable (after-action review, golden regression, save/replay),
the same inputs must always yield the same outputs. .NET makes this easy to break silently — most
notably randomized per-process `string.GetHashCode` (see [DET-001](#det-001-string-gethashcode-is-per-process-random)) —
so the project pins determinism with explicit seeded RNG, deterministic hashing, and a
hash/fingerprint comparison that runs in CI.

## The two seeded RNG primitives

There are **two distinct `SeededRng` types** with different shapes; do not confuse them.

### `ProjectAegis.Sim.Core.SeededRng` — stateless, addressable

A static, pure function used by simulation subsystems (detection, engagement). Every draw is
addressed by its coordinates so order-of-evaluation does not matter:

```7:15:src/ProjectAegis.Sim/Core/SeededRng.cs
    public static double UnitFloat(
        SimSeed seed,
        RngDomain domain,
        ulong entityId,
        ulong simTick,
        int drawIndex)
    {
        ulong hash = Mix(seed.Value, (ulong)domain, entityId, simTick, (ulong)(uint)drawIndex);
        return (hash & 0xFFFF_FFFF) / (double)uint.MaxValue;
    }
```

`RngDomain` partitions the stream so independent subsystems never alias each other's draws:
`Detection = 0, Engage = 1, AgentDecision = 2, Logistics = 3, Combat = 4`
(`src/ProjectAegis.Sim/Core/RngDomain.cs`). The returned unit float is `(hash & 0xFFFF_FFFF) / uint.MaxValue`.

### `ProjectAegis.Delegation.Decision.SeededRng` — stateful per-agent stream

An instance xorshift stream used on the agent-decision path. It is seeded from the global seed
XORed with a **per-agent salt**, and yields a 24-bit unit float (`& 0xFFFFFF / 2^24`):

```7:22:src/ProjectAegis.Delegation/Decision/SeededRng.cs
    public SeededRng(int globalSeed, int agentSalt)
    {
        _state = (ulong)(globalSeed ^ (agentSalt * 0x9E3779B9));
        if (_state == 0)
        {
            _state = 1;
        }
    }

    public double NextUnit()
    {
        _state ^= _state << 13;
        _state ^= _state >> 7;
        _state ^= _state << 17;
        return (_state & 0xFFFFFF) / (double)0x1000000;
    }
```

Because this stream *is* stateful, draw order matters: the agent decision pipeline must consume
draws in a fixed, tick-ordered sequence.

### `DeterministicHash.OrdinalHash` — the salt source (DET-001)

The per-agent salt is derived with `DeterministicHash.OrdinalHash(agentName)` — an FNV-1a hash
over UTF-16, **not** `string.GetHashCode`:

```31:46:src/ProjectAegis.Delegation/Core/DeterministicHash.cs
    public static int OrdinalHash(string value)
    {
        unchecked
        {
            var hash = Fnv1aOffsetBasis;
            foreach (var c in value)
            {
                hash ^= (byte)(c & 0xFF);
                hash *= Fnv1aPrime;
                hash ^= (byte)((c >> 8) & 0xFF);
                hash *= Fnv1aPrime;
            }

            return (int)hash;
        }
    }
```

`ReplayGoldenTests.OrdinalHash_is_stable_golden` pins `OrdinalHash("a1") == 1012613629`, a constant
computed independently of any process run. It only holds if the salt is cross-process-stable, so the
test fails immediately if `string.GetHashCode` is reintroduced.

## The layered world-state hash

The world hash (ADR-004) is built in **layers** so each tick step contributes a tagged sub-hash,
letting a divergence be localised to a phase (core → detection → engage → combat outcome):

```6:18:src/ProjectAegis.Sim/Core/SimWorldHash.cs
    public const byte LayerCore = 1;
    public const byte LayerDetection = 2;
    public const byte LayerEngage = 3;
    public const byte LayerCombatOutcome = 4;

    public static ulong MixLayer(ulong composite, ulong layer, byte tag) =>
        Fold(composite ^ Fold(layer ^ ((ulong)tag << 56)));

    public static ulong Combine(ulong coreHash, ulong detectionHash, ulong engageMix) =>
        MixLayer(MixLayer(coreHash, detectionHash, LayerDetection), engageMix, LayerEngage);

    public static ulong Combine(ulong coreHash, ulong detectionHash, ulong engageMix, ulong killMix) =>
        MixLayer(Combine(coreHash, detectionHash, engageMix), killMix, LayerCombatOutcome);
```

- **Core layer** — `SimTickRunner.TickOnce` folds `seed ^ tick ^ previous` after `Clock.AdvanceOneTick()`.
- **Detection layer** — `DetectionWorldHash.MixTick` mixes each detection roll's
  `(observer, sensor, target)` id, the `Detected` flag, and quantized `Pd`/`Draw` values
  (`src/ProjectAegis.Sim/Sensors/DetectionWorldHash.cs`).
- **Engage + combat-outcome layers** — `SimTickPipeline` resolves queued engagements (ADR-004 step 8),
  mixes launched engagement ids and outcome codes, folds in the killed-target registry hash, and
  recomputes `LastWorldHash`:

```61:66:src/ProjectAegis.Sim/Core/SimTickPipeline.cs
    private void RecomputeWorldHash()
    {
        var engageMix = MixEngagements(_lastResults);
        var killMix = _engagement is MvpEngagementResolver mvp ? mvp.KilledTargets.MixHash() : 0UL;
        LastWorldHash = SimWorldHash.Combine(_core.LastWorldHash, DetectionSubhash, engageMix, killMix);
    }
```

`Fold` and the per-domain `Mix` use the same MurmurHash3-style finalizer constants
(`0xff51afd7ed558ccd`, `0xc4ceb9fe1a85ec53`), which are integer ops only — no float/transcendental
math — so the hash is bit-stable across platforms.

## The order-log fingerprint

The order log (the chronological record of every intent/decision/engagement) has its own
canonical text fingerprint, independent of the numeric world hash. `DecisionLog.ComputeFingerprint`
emits one newline-delimited row per entry, sorted by `SequenceId`, as `Kind|SequenceId|SimTime|payload`:

```296:312:src/ProjectAegis.Delegation/Decision/DecisionLog.cs
    public string ComputeFingerprint()
    {
        var sb = new StringBuilder();
        foreach (var entry in ChronologicalEntries())
        {
            sb.Append(entry.Kind);
            sb.Append('|');
            sb.Append(entry.SequenceId);
            sb.Append('|');
            sb.Append(entry.SimTime.ToString("R"));
            sb.Append('|');
            sb.Append(FormatPayload(entry));
            sb.Append('\n');
        }

        return sb.ToString();
    }
```

Floating-point fields are always formatted with the round-trip `"R"` specifier (and
`CultureInfo.InvariantCulture` at the call sites) so the text is identical regardless of locale.
`OrderLogReplayFingerprint.ComputeSha256Hex(log)` collapses that canonical text to a 64-char SHA-256
hex digest for compact pinning.

## The Baltic replay harness

`BalticReplayHarness.Run` is the headless entry point that wires the full deterministic path
(scenario load → detection → policy → delegation → engagement → order log) and returns everything
the gate compares:

```42:50:src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs
    public static Result Run(
        int seed,
        string scenarioPolicyId,
        int ticks,
        bool mvpEngagement = true,
        ICatalogReader? catalog = null,
        IReadOnlyDictionary<string, bool>? unitReadiness = null,
        IReadOnlyList<ScenarioNearFutureUnitRequest>? nearFutureUnits = null,
        int maxTechnologyLevel = 2)
```

`Result` exposes `Fingerprint`, `FingerprintSha256`, `WorldHash`, `DetectionWorldHash`,
`EngagementCount`, periodic `Checkpoints` (per-tick `WorldHash` snapshots, controlled by the
scenario's `ReplaySettings.CheckpointIntervalTicks`), plus message/sensor/scoring projections.

## Running it

### Single scenario (prints the comparable fields)

```bash
dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed 42 --scenario baltic-patrol --ticks 4
```

Output (the three pinned lines plus context):

```
SEED=42 SCENARIO=baltic-patrol TICKS=4 ENGAGEMENTS=1
FINGERPRINT=EngagementOutcome|5|1|1|1|hostile-1|Kill|0.0065019661575793...
FINGERPRINT_SHA256=080a4cbf18f620043a4e6401ac1f60749e9604760b91d2adfc770de816649917
DETECTION_WORLD_HASH=15600
WORLD_HASH=17144800277401907079
```

Flags: `--no-engage` runs with `mvpEngagement: false` (classification/comms scenarios), `--csv`
appends the scoring CSV row. A batch mode (`--batch --scenarios … --seeds … [--all-scenarios]`)
sweeps multiple cases.

### Golden regression suite (the CI gate)

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~ReplayGoldenSuiteTests
```

For each pinned case, `ReplayGoldenSuiteTests` runs the harness **twice** (A vs B intra-run check),
asserts the two fingerprints/world-hashes match, then asserts the run matches the pinned golden file
and contains required fingerprint fragments:

```17:35:src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenSuiteTests.cs
        var a = BalticReplayHarness.Run(
            testCase.Seed,
            testCase.PolicyId,
            testCase.Ticks,
            mvpEngagement: testCase.MvpEngagement);
        var b = BalticReplayHarness.Run(
            testCase.Seed,
            testCase.PolicyId,
            testCase.Ticks,
            mvpEngagement: testCase.MvpEngagement);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        ReplayGoldenAssertions.AssertPinnedHashes(a, golden);

        foreach (var fragment in testCase.FingerprintMustContain)
        {
            Assert.That(a.Fingerprint, Does.Contain(fragment));
        }
```

## The golden files

Each pinned case lives in [`tests/regression/replay-golden-*.txt`](../../tests/regression/) and stores
`WORLD_HASH=`, `DETECTION_WORLD_HASH=`, and optionally `FINGERPRINT_SHA256=`, with a header comment
recording the seed/scenario/ticks used to regenerate it:

```
# Golden fingerprint — baltic-patrol seed=42 ticks=4 (kill persists, no re-detect)
WORLD_HASH=17144800277401907079
DETECTION_WORLD_HASH=15600
FINGERPRINT_SHA256=080a4cbf18f620043a4e6401ac1f60749e9604760b91d2adfc770de816649917
```

The seed/scenario/ticks and required-fragment assertions for each file are declared in
`ReplayGoldenRegressionCatalog.cs` (`baltic-patrol`, `-comms`, `-classify`, `-stale`, `-spoof`,
`-readiness`). Per-tick checkpoint goldens are covered separately by `ReplayGoldenBalticCheckpointTests`.

### Regenerating a golden (intentional change only)

A golden is a deliberate baseline; regenerate it **only** when you have confirmed a behavior change
is correct, and note the reason in the commit:

1. Run the demo with the case's exact `--seed`/`--scenario`/`--ticks` (from the catalog).
2. Copy `WORLD_HASH`, `DETECTION_WORLD_HASH`, and `FINGERPRINT_SHA256` into the matching
   `replay-golden-*.txt`.
3. Re-run `ReplayGoldenSuiteTests` to confirm green.

Silently re-recording a golden hides regressions — drift always goes through a human decision (see
the `/replay-verify` skill's drift handling).

## What a failure means

| Symptom | Meaning | Action |
|---------|---------|--------|
| A != B (two runs differ) | **Internal non-determinism** — release-blocking | Bisect to the first diverging tick/checkpoint, run [`/determinism-audit`](../../.claude/skills/determinism-audit/SKILL.md) on the implicated namespace |
| A == B but A != golden | **Behavior drift** since the baseline | Decide: intentional → regenerate the golden; unintended → fix/revert |
| `WORLD_HASH` matches, `FINGERPRINT` differs | Same end-state, different intent ordering | Inspect order-log emission order (a stateful-RNG draw-order bug) |
| Goldens differ only on `main` post-merge | A merge interaction not caught on the PR | Inspect the post-merge Baltic replay step |

## Common pitfalls

### DET-001: `string.GetHashCode` is per-process random
.NET randomizes string hashing per launch. Never seed RNG, salt, or order keys from
`string.GetHashCode` — use `DeterministicHash.OrdinalHash`. This is the single most common way to
break replay while passing within a single process.

- **Unordered iteration.** Iterating a `Dictionary`/`HashSet` has no stable order. Sort by a stable
  key (the order log sorts by `SequenceId`) before hashing or emitting.
- **Float formatting.** Always use `"R"` + `InvariantCulture` for any float that enters a fingerprint;
  quantize floats to integers before XOR-mixing into a world hash (as `DetectionWorldHash` does).
- **Wall-clock / `Guid.NewGuid` / `DateTime.Now`** in sim or controller paths — all non-deterministic.
- **Two `SeededRng` types.** Pick the right one: stateless/addressable (`Sim.Core`) for sim subsystems,
  stateful per-agent (`Delegation.Decision`) for the decision stream.
- **Hindsight recall/reflect inside `Tick()`** — forbidden; it makes the tick depend on external state.

## Code paths covered

| Concern | Path |
|---------|------|
| RNG domains | `src/ProjectAegis.Sim/Core/RngDomain.cs` |
| Stateless seeded RNG | `src/ProjectAegis.Sim/Core/SeededRng.cs` |
| Stateful per-agent RNG | `src/ProjectAegis.Delegation/Decision/SeededRng.cs` |
| Cross-process salt hash | `src/ProjectAegis.Delegation/Core/DeterministicHash.cs` |
| World-hash layering | `src/ProjectAegis.Sim/Core/SimWorldHash.cs`, `SimTickRunner.cs`, `SimTickPipeline.cs` |
| Detection sub-hash | `src/ProjectAegis.Sim/Sensors/DetectionWorldHash.cs` |
| Order-log fingerprint | `src/ProjectAegis.Delegation/Decision/DecisionLog.cs`, `Replay/OrderLogReplayFingerprint.cs` |
| Headless harness | `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` |
| Demo CLI | `src/ProjectAegis.Delegation.Demo/Program.cs` |
| Golden suite + catalog | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenSuiteTests.cs`, `ReplayGoldenRegressionCatalog.cs`, `ReplayGoldenAssertions.cs` |
| In-code replay golden | `src/ProjectAegis.Delegation.Tests/Decision/ReplayGoldenTests.cs`, `OrderLogReplayFingerprintSha256Tests.cs`, `DeterminismTests.cs` |
| Pinned goldens | [`tests/regression/`](../../tests/regression/) |

## Related

- [ADR-004 — Deterministic tick pipeline ordering](../architecture/adr-004-tick-pipeline-order.md)
- [ADR-003 — Order-log schema](../architecture/adr-003-order-log-schema.md)
- [`/replay-verify` skill](../../.claude/skills/replay-verify/SKILL.md) — the PASS/FAIL reproducibility gate
- [`/determinism-audit` skill](../../.claude/skills/determinism-audit/SKILL.md) — static scan for non-deterministic patterns
- [Buildkite CI](buildkite-ci.md) and [CI / branch protection](ci-and-branch-protection.md) — where the gate runs
