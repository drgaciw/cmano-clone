namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Req 20 P0 Phase 0: registry entry for unit/map context-menu actions.
/// Tracks (T3/T4/T6) register actions; menu shell (T2) only renders and invokes.
/// ADR-010: eligibility is pure over projections; invoke yields a logged intent factory result
/// for the host to submit via the bridge — never a direct sim mutation.
/// </summary>
public interface IContextActionProvider
{
    /// <summary>Stable action id (e.g. <c>attack_options</c>, <c>assign_mission</c>).</summary>
    string ActionId { get; }

    /// <summary>Human-readable menu label.</summary>
    string Label { get; }

    /// <summary>
    /// Whether the action is eligible for the current selection/projection snapshot.
    /// Must be pure (no side effects).
    /// </summary>
    bool IsEligible(ContextActionEvaluationContext context);

    /// <summary>
    /// Builds the intent payload for a confirmed action. Host logs/submits via bridge.
    /// Returns null when the action is not applicable (caller should no-op).
    /// </summary>
    ContextActionIntent? CreateIntent(ContextActionEvaluationContext context);
}

/// <summary>Read-only evaluation inputs for context-menu eligibility (presentation state only).</summary>
public sealed class ContextActionEvaluationContext
{
    public ContextActionEvaluationContext(
        IReadOnlyList<string> selectedUnitIds,
        bool isMapSurface,
        bool isReplay,
        string? primaryUnitId = null)
    {
        SelectedUnitIds = selectedUnitIds ?? Array.Empty<string>();
        IsMapSurface = isMapSurface;
        IsReplay = isReplay;
        PrimaryUnitId = primaryUnitId;
    }

    public IReadOnlyList<string> SelectedUnitIds { get; }

    public bool IsMapSurface { get; }

    public bool IsReplay { get; }

    public string? PrimaryUnitId { get; }
}

/// <summary>Intent factory result — host maps <see cref="IntentKind"/> to bridge APIs.</summary>
public sealed record ContextActionIntent(
    string ActionId,
    string IntentKind,
    IReadOnlyList<string> TargetUnitIds,
    string Detail = "");

/// <summary>
/// Ordered registry of <see cref="IContextActionProvider"/> instances.
/// Menu shells enumerate providers; tracks only register — they do not edit shell files.
/// </summary>
public sealed class ContextActionRegistry
{
    private readonly List<IContextActionProvider> _providers = new();

    public void Register(IContextActionProvider provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(provider.ActionId))
        {
            throw new ArgumentException("ActionId is required.", nameof(provider));
        }

        if (_providers.Any(p => string.Equals(p.ActionId, provider.ActionId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Context action '{provider.ActionId}' is already registered.");
        }

        _providers.Add(provider);
    }

    public IReadOnlyList<IContextActionProvider> Providers => _providers;

    /// <summary>Eligible actions for the given context, in registration order.</summary>
    public IReadOnlyList<IContextActionProvider> Eligible(ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.IsReplay)
        {
            return Array.Empty<IContextActionProvider>();
        }

        return _providers.Where(p => p.IsEligible(context)).ToArray();
    }
}
