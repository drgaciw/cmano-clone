namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-319 disclosure chrome. Fail-closed: a missing or false <c>jevDecide</c> flag shows the banner.
/// </summary>
public static class JevDisclosureBinder
{
    /// <summary>Project disclosure from the skill-availability flag. Does not call a model.</summary>
    public static JevDisclosurePresentation Project(bool? jevDecide) =>
        jevDecide == true ? JevDisclosurePresentation.Hidden : JevDisclosurePresentation.Shown;
}
