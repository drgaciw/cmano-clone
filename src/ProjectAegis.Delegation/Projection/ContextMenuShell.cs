namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One renderable row in a unit/map context menu (presentation binding only).
/// </summary>
public sealed record ContextMenuEntry(string ActionId, string Label);

/// <summary>
/// Req 20 P0 T2: headless context-menu shell over <see cref="ContextActionRegistry"/>.
/// Enumerates eligible providers and invokes them to produce logged intents (ADR-010 —
/// no direct sim mutation; host submits via bridge).
/// </summary>
/// <remarks>
/// Track isolation: T2 owns this shell. T3/T4/T6 register actions only — they must not
/// edit shell files. Replay yields an empty menu (registry gate).
/// </remarks>
public sealed class ContextMenuShell
{
    private readonly ContextActionRegistry _registry;

    public ContextMenuShell(ContextActionRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>Underlying registry (tracks may register additional providers).</summary>
    public ContextActionRegistry Registry => _registry;

    /// <summary>
    /// Eligible menu rows in registration order for the given evaluation context.
    /// Empty when replay, or when no provider is eligible (e.g. empty selection off-map).
    /// </summary>
    public IReadOnlyList<ContextMenuEntry> Enumerate(ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var eligible = _registry.Eligible(context);
        if (eligible.Count == 0)
        {
            return Array.Empty<ContextMenuEntry>();
        }

        var rows = new ContextMenuEntry[eligible.Count];
        for (var i = 0; i < eligible.Count; i++)
        {
            var p = eligible[i];
            rows[i] = new ContextMenuEntry(p.ActionId, p.Label);
        }

        return rows;
    }

    /// <summary>
    /// Invokes a registered action by id when it is currently eligible.
    /// Returns the intent payload for host logging/submit, or null when not found,
    /// not eligible, or the provider declines.
    /// </summary>
    public ContextActionIntent? Invoke(string actionId, ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrWhiteSpace(actionId) || context.IsReplay)
        {
            return null;
        }

        // Iterate providers directly — avoid Eligible() ToArray() allocation on hot Invoke path.
        foreach (var provider in _registry.Providers)
        {
            if (!string.Equals(provider.ActionId, actionId, StringComparison.Ordinal))
            {
                continue;
            }

            return provider.IsEligible(context) ? provider.CreateIntent(context) : null;
        }

        return null;
    }
}
