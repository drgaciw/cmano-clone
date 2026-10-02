using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Headless contract for C2 keyboard-discovery focus rings (DRG-270 / REQ-20 CMD-12).
/// Presentation hosts apply <see cref="FocusRingTokens"/> via USS; row labels use
/// <see cref="KeyboardDiscoverySurfaces"/> for Tab order into OOB and message log lists.
/// </summary>
public static class FocusRingTokens
{
    /// <summary>Primary focus ring color token in <c>AegisTokens.uss</c>.</summary>
    public const string RingColorVariable = "--focus-ring";

    /// <summary>Focus ring stroke width token in <c>AegisTokens.uss</c>.</summary>
    public const string RingWidthVariable = "--focus-ring-width";

    /// <summary>PE / catalog alias consumed by some panels.</summary>
    public const string RingColorAlias = "--aegis-focus-ring";

    /// <summary>PE / catalog alias for ring width.</summary>
    public const string RingWidthAlias = "--aegis-focus-ring-width";

    /// <summary>USS snippet hosts must reference (not <c>--selected-ring</c>).</summary>
    public const string RingColorReference = "var(--focus-ring)";

    /// <summary>USS snippet for ring width.</summary>
    public const string RingWidthReference = "var(--focus-ring-width)";
}

/// <summary>Named C2 surfaces that participate in baseline keyboard discovery.</summary>
public static class KeyboardDiscoverySurfaces
{
    public const string OobListElementName = "oob-list";
    public const string MessageLogListElementName = "message-list";

    /// <summary>OOB row base class; focus ring is <c>.oob-row:focus</c> in drawer / OOB USS.</summary>
    public const string OobRowBaseClass = "oob-row";

    /// <summary>Message log selectable row class (pairs with <see cref="MessageLogCategoryClassMap.SelectableRowClass"/>).</summary>
    public const string MessageLogSelectableClass = MessageLogCategoryClassMap.SelectableRowClass;
}

/// <summary>USS selectors that must reference <see cref="FocusRingTokens"/> (grep oracle).</summary>
public static class FocusRingUssSelectors
{
    public const string OobRowFocused = ".oob-row:focus";
    public const string MessageLogSelectableFocused = ".message-log-row--selectable:focus";
    public const string C2DrawerRowFocused = ".c2-drawer-row:focus";
}
