namespace ProjectAegis.Sim.Swarm;

/// <summary>
/// SWARM-25 / DRG-91: independent logical vs render caps for swarm platforms.
/// Phase A combat is aggregate (O(swarm units) per pulse) — logical count never expands engagement work.
/// Distinct from <see cref="ProjectAegis.Data.Catalog.SwarmTierLimits"/> (req-09 near-future entity tiers).
/// </summary>
public static class SwarmPerformanceCaps
{
    /// <summary>
    /// Logical integrity ceiling per swarm unit (combat SoT / <c>maxDrones</c>).
    /// Aligns with <see cref="ProjectAegis.Data.Catalog.CatalogSwarmPlatformDefaults.GenericMaxDrones"/>.
    /// </summary>
    public const int LogicalMaxDronesPerSwarm = 40;

    /// <summary>
    /// Cosmetic member sprites/billboards per swarm (presentation LOD only).
    /// Combat and detection must not iterate this set as authority.
    /// </summary>
    public const int RenderMaxMembersPerSwarm = 12;

    /// <summary>Design-max concurrent first-class swarm platforms in a Phase A scenario.</summary>
    public const int DesignMaxConcurrentSwarms = 16;

    /// <summary>
    /// Upper bound on logical drones under design-max load
    /// (<see cref="DesignMaxConcurrentSwarms"/> × <see cref="LogicalMaxDronesPerSwarm"/>).
    /// </summary>
    public const int DesignMaxLogicalDrones =
        DesignMaxConcurrentSwarms * LogicalMaxDronesPerSwarm;

    /// <summary>Stress fixture tick count (headless integrity + centroid pulse).</summary>
    public const int StressScenarioTicks = 60;

    /// <summary>
    /// Headless wall-clock budget (ms) for design-max stress: one integrity apply + Tick per swarm per tick.
    /// Generous for CI hosts; algorithmic O(swarms×ticks) is the hard gate in tests.
    /// </summary>
    public const int StressPulseBudgetMs = 2000;

    /// <summary>True when logical count exceeds the Phase A ceiling (spawn/register should clamp).</summary>
    public static bool ExceedsLogicalCap(int maxDrones) => maxDrones > LogicalMaxDronesPerSwarm;

    /// <summary>Clamp max drones to the logical ceiling.</summary>
    public static int ClampLogicalMaxDrones(int maxDrones) =>
        maxDrones <= 0 ? 0 : Math.Min(maxDrones, LogicalMaxDronesPerSwarm);

    /// <summary>Clamp living count into [0, logical max].</summary>
    public static int ClampDroneCount(int droneCount, int maxDrones)
    {
        var max = ClampLogicalMaxDrones(maxDrones);
        if (droneCount <= 0)
        {
            return 0;
        }

        return droneCount > max ? max : droneCount;
    }

    /// <summary>
    /// Members to render for a given living integrity — never exceeds render LOD or living count.
    /// </summary>
    public static int RenderMemberCount(int droneCount) =>
        droneCount <= 0 ? 0 : Math.Min(droneCount, RenderMaxMembersPerSwarm);

    /// <summary>
    /// Engagement / integrity work units per pulse for N swarm platforms (aggregate SoT).
    /// Independent of logical drone totals — SWARM-25 acceptance.
    /// </summary>
    public static int EngagementWorkUnitsPerPulse(int concurrentSwarmUnits) =>
        concurrentSwarmUnits < 0 ? 0 : concurrentSwarmUnits;
}
