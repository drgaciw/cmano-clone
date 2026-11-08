using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>Projects transient effects from one combat frame and its displayed contact poses.</summary>
public static class CombatVfxFrameBridge
{
    /// <summary>Resolves contact-to-target aliases without changing the supplied symbol picture.</summary>
    public static CombatVfxFrame Build(CombatPresentationFrame frame, IReadOnlyList<MapSymbolEntry> symbols)
    {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        if (symbols is null) throw new ArgumentNullException(nameof(symbols));

        var combatSymbols = CombatMapSymbolBridge.Build(symbols, frame.Contacts.Contacts);
        return CombatVfxProjection.Project(frame.Events, combatSymbols, frame.SimTime);
    }
}
