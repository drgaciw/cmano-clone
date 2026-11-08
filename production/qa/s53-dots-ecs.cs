// S53 DOTS spawn + MASS tier full (Req09 E3).
// After S52 DOTS expand skeleton. Isolated fixtures first. Determinism engineer notes: Burst det ready.
// Cites: production/release-enablement-scope-boundary-2026-06-20.md (Req09 post; "isolated-fixture pilot only"; no hash w/o ADR), 
// production/gate-checks/scope-expansion-decision-2026-06-20.md, docs/reports/future-sprint-roadpmap.md §10 S53 + S52 DOTS, 
// §7 invariants (Baltic hash immutable 17144800277401907079, replay 6/6, GitNexus impact before symbols, detect before commit), 
// §0 parallel (git worktrees + dispatching), ADR-005, dots-ecs-notes.md (BurstCompile FloatMode.Deterministic), 
// implementation-tracker-2026-06-04.md, progress.md.
// Additive ONLY: never called from SimTickPipeline / SimulationSession (CRITICAL 179 impacted per GitNexus) / BalticReplayHarness / TargetRegistry.
// Dedicated Dots* test filters only. Pre-edit impacts run on SimulationSession, ISimWorldSnapshot (CRITICAL), SensorHotPath (notfound), DOTS symbols.
// No production hash impact. No DelegationBridge changes. Isolated from golden replay. Parallel dispatch via sibling mass-tier wt.

using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

/// <summary>
/// Mock ECS entity (blittable-friendly struct for future IComponentData + Burst).
/// Lifecycle state: Alive driven from snapshot or explicit destroy.
/// </summary>
public struct DotsSpawnedEntity
{
    public int Id;
    public DotsBlittableSpawn SpawnData;
    public bool Alive;
}

/// <summary>
/// S53 full DOTS spawn: ECS systems for spawn + entity lifecycle from snapshot.
/// Isolated fixture: pure C# simulation of ECS World spawn/destroy/update (no Unity.Entities dep).
/// Integration additive: callers use via DotsNearFutureSpawnIntegrationSkeleton or direct in DOTS harness fixture.
/// Supports: batch spawn from blittables (from NearFuture plans), lifecycle update from ISimWorldSnapshot, destroy by id.
/// Ordinal stable, deterministic within fixture seed.
/// </summary>
public sealed class DotsEcsSpawnSystem
{
    private List<DotsSpawnedEntity> _entities;
    private int _nextId = 1;
    private readonly string _fixtureContext;

    public DotsEcsSpawnSystem(string fixtureContext = "s53-dots-isolated", int initialCapacity = 0)
    {
        // S53 full MASS: pre-alloc for up to 5000 (avoids re-alloc in hot sim; zero alloc intent per engine rules)
        _entities = (initialCapacity > 0) ? new List<DotsSpawnedEntity>(initialCapacity) : new List<DotsSpawnedEntity>();
        _fixtureContext = fixtureContext;
    }

    /// <summary>Active (alive) entities only. Snapshot of current ECS-like world state.</summary>
    public IReadOnlyList<DotsSpawnedEntity> ActiveEntities =>
        _entities.Where(e => e.Alive).ToList().AsReadOnly();

    public int ActiveCount => ActiveEntities.Count;

    public string FixtureContext => _fixtureContext;

    /// <summary>
    /// Spawn batch from blittables (post NearFutureArchetypeRuntime.PlanSpawns + DotsSpawnSkeleton).
    /// Preserves input order (ordinal stable for determinism).
    /// S53 full: MASS capacity aware (uses MaxSwarmEntities from first or hint).
    /// </summary>
    public void Spawn(IReadOnlyList<DotsBlittableSpawn> spawns)
    {
        if (spawns == null || spawns.Count == 0) return;
        int capHint = spawns.Count > 0 ? spawns[0].MaxSwarmEntities : 0;
        if (capHint > _entities.Capacity) _entities.Capacity = System.Math.Max(capHint, spawns.Count);
        foreach (var s in spawns)
        {
            _entities.Add(new DotsSpawnedEntity
            {
                Id = _nextId++,
                SpawnData = s,
                Alive = true
            });
        }
    }

    /// <summary>
    /// Full lifecycle update driven from sim snapshot (entity state sync).
    /// For isolated fixture: uses snapshot counts to gate alive status (demo of snapshot->ECS).
    /// Real DOTS: would query entity by component hash/id, apply health/pos deltas, mark destroyed.
    /// Additive: does not mutate any shared sim state.
    /// </summary>
    public void UpdateLifecycleFromSnapshot(ISimWorldSnapshot? snapshot)
    {
        if (snapshot == null)
            return;

        // Deterministic rule for fixture: if snapshot shows no contacts/engagements, allow attrition simulation for some.
        // But keep stable: preserve Alive unless explicit zero-world and we opt to cull (here we keep for replay compat in fixture).
        bool hasActivity = snapshot.ContactCount > 0 || snapshot.ActiveEngagementCount > 0;

        for (int i = 0; i < _entities.Count; i++)
        {
            var e = _entities[i];
            // Lifecycle example: if no activity in snapshot, mark oldest half as test-destroyed (controlled, seeded by count).
            if (!hasActivity && e.Alive && (e.Id % 2 == 0))
            {
                e.Alive = false;
            }
            else if (hasActivity && !e.Alive)
            {
                // revive policy for demo (not production)
                e.Alive = true;
            }
            _entities[i] = e;
        }
    }

    /// <summary>Explicit destroy for lifecycle test (maps to ECS entity destroy / killed in snapshot).</summary>
    public void Destroy(int entityId)
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            if (_entities[i].Id == entityId)
            {
                var e = _entities[i];
                e.Alive = false;
                _entities[i] = e;
                return;
            }
        }
    }

    /// <summary>Reset for isolated test fixtures (pure, no side effects outside).</summary>
    public void ResetForFixture()
    {
        _entities.Clear();
        _nextId = 1;
    }

    /// <summary>
    /// S53 full: Simulate DOTS SensorHotPath for active entities (isolated pilot).
    /// Deterministic: uses EntitySeed + SensorHotPath* from blittable. No RNG/wall.
    /// For MASS tier: scales with ActiveCount (prealloc'd). Returns aggregate det hash (for fixture assert).
    /// Burst det note: maps to DOTS IJobEntity + [BurstCompile(FloatMode=FloatMode.Deterministic)] consuming SensorHotPathPd/Range.
    /// Never touches real sim sensor code.
    /// </summary>
    public int SimulateSensorHotPath()
    {
        int aggregate = 0;
        foreach (var e in _entities)
        {
            if (!e.Alive) continue;
            var s = e.SpawnData;
            // det calc: seed mix + pd + range / tier factor (Mass uses full range)
            int tierFactor = (s.SwarmTier == (int)SwarmTier.Mass) ? 3 : 1;
            int local = (int)((s.EntitySeed & 0xFFFF) ^ (uint)s.SensorHotPathPd ^ (uint)(s.SensorHotPathRange * tierFactor));
            aggregate ^= local;
            // In real DOTS: would write to component, query in hotpath system.
        }
        return aggregate;
    }

    /// <summary>
    /// S53: Spawn with MASS capacity awareness (callers may pass count from plans).
    /// </summary>
    public void Spawn(IReadOnlyList<DotsBlittableSpawn> spawns, int hintCapacity = 0)
    {
        if (spawns == null || spawns.Count == 0) return;
        if (hintCapacity > _entities.Capacity) _entities.Capacity = hintCapacity;
        foreach (var s in spawns)
        {
            _entities.Add(new DotsSpawnedEntity
            {
                Id = _nextId++,
                SpawnData = s,
                Alive = true
            });
        }
    }
}
