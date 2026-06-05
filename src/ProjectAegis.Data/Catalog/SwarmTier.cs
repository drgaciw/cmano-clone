namespace ProjectAegis.Data.Catalog;

/// <summary>Swarm size tiers from req 09 (MEDIUM cap for MVP).</summary>
public enum SwarmTier
{
    Micro = 0,
    Medium = 1,
    Mass = 2,
}

public static class SwarmTierLimits
{
    public const int MicroMaxEntities = 50;
    public const int MediumMaxEntities = 500;
    public const int MassMaxEntities = 5000;

    public static int MaxEntitiesFor(SwarmTier tier) =>
        tier switch
        {
            SwarmTier.Micro => MicroMaxEntities,
            SwarmTier.Medium => MediumMaxEntities,
            SwarmTier.Mass => MassMaxEntities,
            _ => MicroMaxEntities,
        };

    public static bool TryParse(string value, out SwarmTier tier)
    {
        if (Enum.TryParse<SwarmTier>(value, ignoreCase: true, out tier))
        {
            return true;
        }

        tier = SwarmTier.Micro;
        return false;
    }
}