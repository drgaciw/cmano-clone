namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// ADR-009 bounded mine transit hazard hot-tick — seeded placement + deterministic hazard rolls.
/// No mine-laying missions; no danger-area map layer.
/// </summary>
public static class MineTransitHazardHotTickApplier
{
    public static bool IsEnabled(bool combatDomainsEnabled, ScenarioMineHazardSettings? hazard) =>
        combatDomainsEnabled && hazard is { HasTransit: true };

    public static IReadOnlyList<CatalogDamageHotTickApplier.DamageChange> ApplyTransitHazardTick(
        SimSeed seed,
        ulong simTick,
        PlatformHpLedger ledger,
        ICatalogReader catalog,
        ScenarioMineHazardSettings hazard)
    {
        if (!hazard.HasTransit)
        {
            return Array.Empty<CatalogDamageHotTickApplier.DamageChange>();
        }

        var tickIndex = simTick == 0 ? 0 : (int)simTick - 1;
        var changes = new List<CatalogDamageHotTickApplier.DamageChange>();
        var drawIndex = 0;
        foreach (var transit in hazard.Transit.OrderBy(t => t.PlatformId, StringComparer.Ordinal))
        {
            if (!TryResolveTransitRange(transit, tickIndex, out var platformRange))
            {
                continue;
            }

            if (!hazard.IsRangeInsideZone(platformRange))
            {
                continue;
            }

            if (!ledger.TryGetHpPct(transit.PlatformId, out var previousHp) || previousHp <= 0)
            {
                continue;
            }

            foreach (var mine in hazard.Mines.OrderBy(m => m.MineId, StringComparer.Ordinal))
            {
                if (Math.Abs(platformRange - mine.RangeMeters) > hazard.TriggerRadiusMeters)
                {
                    continue;
                }

                var hitProbability = Math.Clamp(mine.Lethality * hazard.HazardSeverity, 0.0, 1.0);
                if (hitProbability <= 0)
                {
                    continue;
                }

                var entityId = MineTransitHazardEntityId.FromTrial(transit.PlatformId, mine.MineId);
                var draw = SeededRng.UnitFloat(seed, RngDomain.MineHazard, entityId, simTick, drawIndex++);
                if (draw >= hitProbability)
                {
                    continue;
                }

                if (!catalog.TryGetPlatformDamage(transit.PlatformId, out var damage))
                {
                    continue;
                }

                var damageLevel = CombatDamageLevel.ComputeLevel(
                    mine.Lethality,
                    CombatDamageLevel.ResolvePlatformResilience(damage));
                var delta = CombatDamageLevel.HitHpDeltaPct(damageLevel);
                if (delta <= 0)
                {
                    continue;
                }

                var newHp = Math.Max(0.0, previousHp - delta);
                if (Math.Abs(newHp - previousHp) < 1e-9)
                {
                    continue;
                }

                ledger.SetHpPct(transit.PlatformId, newHp);
                changes.Add(new CatalogDamageHotTickApplier.DamageChange(
                    transit.PlatformId,
                    previousHp,
                    newHp,
                    PlatformDamageChangeReasonCodes.MineTransitHazard,
                    damageLevel));
                previousHp = newHp;
            }
        }

        return changes;
    }

    private static bool TryResolveTransitRange(
        ScenarioMineTransitSchedule transit,
        int tickIndex,
        out double rangeMeters)
    {
        rangeMeters = 0;
        if (transit.RangesMeters.Count == 0)
        {
            return false;
        }

        var index = Math.Clamp(tickIndex, 0, transit.RangesMeters.Count - 1);
        rangeMeters = transit.RangesMeters[index];
        return true;
    }
}