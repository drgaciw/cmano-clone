namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.PlatformAssistant;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// Thin Unity/CLI host bridge for the Platform Design Assistant.
/// Delegates to <see cref="PlatformDesignAssistant"/> only — no direct SQLite, no gate bypass.
/// </summary>
public static class PlatformDesignAssistantBridge
{
    private static readonly PlatformDesignAssistant Assistant = new();

    public static PlatformDesignProposal Draft(ICatalogReader catalog, PlatformDesignBrief brief) =>
        Assistant.Draft(catalog, brief);

    public static PlatformDesignProposal DraftFromDatabase(
        string databasePath,
        PlatformDesignBrief brief,
        string layerVersion = "platform-design-assistant")
    {
        using var catalog = new SqliteCatalogReader(databasePath, layerVersion);
        return Assistant.Draft(catalog, brief);
    }

    public static PlatformDesignProposeResult Propose(
        string databasePath,
        PlatformDesignBrief brief,
        long clockTicks = 0,
        string actorType = "agent",
        string actorId = "platform-design-assistant",
        string? rationale = null)
    {
        using var catalog = new SqliteCatalogReader(databasePath, "platform-design-assistant");
        return Assistant.Propose(
            databasePath,
            catalog,
            brief,
            new FixedCatalogClock(clockTicks),
            actorType,
            actorId,
            rationale);
    }
}
