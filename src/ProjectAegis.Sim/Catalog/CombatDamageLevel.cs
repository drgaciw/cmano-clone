namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Data.Catalog;

/// <summary>
/// GDD combat-domains-damage §4 bounded P1 slice: damageLevel 0–3 from hit severity × platform resilience.
/// </summary>
public static class CombatDamageLevel
{
    public const int MaxLevel = 3;

    public const double MinResilience = 0.0;

    public const double MaxResilience = 2.0;

    public const double DefaultResilience = 1.0;

    public const double DefaultHitSeverity = 1.0;

    /// <summary>HP% loss per damage level (level 1 = 25%, matches S29-09 bounded Hit slice).</summary>
    public const double HpPctPerLevel = 25.0;

    public static int ComputeLevel(double hitSeverity, double resilience)
    {
        var severity = Math.Clamp(hitSeverity, 0.0, 1.0);
        var res = Math.Clamp(resilience, MinResilience, MaxResilience);
        return Math.Clamp((int)Math.Floor(severity * res), 0, MaxLevel);
    }

    public static double ResolvePlatformResilience(CatalogPlatformDamage damage) =>
        Math.Clamp(damage.Resilience, MinResilience, MaxResilience);

    public static double HitHpDeltaPct(int damageLevel) =>
        damageLevel <= 0 ? 0.0 : damageLevel * HpPctPerLevel;
}