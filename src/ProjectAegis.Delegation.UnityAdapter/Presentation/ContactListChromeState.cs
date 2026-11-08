namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Local contact-list chrome preferences (presentation-only; never written to sim).</summary>
public sealed record ContactListChromeState(
    ContactListSortKey SortKey = ContactListSortKey.ContactId,
    ContactListLifecycleFilter LifecycleFilter = ContactListLifecycleFilter.All,
    string TextFilter = "")
{
    /// <summary>Default drawer/HUD chrome (ordinal contact id, no filters).</summary>
    public static ContactListChromeState Default { get; } = new();
}
