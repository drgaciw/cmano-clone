namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// UAT D21: <c>VisualElement.Insert(index, child)</c> throws when index is outside
/// [0, childCount]. Hosts enabled before their UXML tree exists have zero children.
/// </summary>
public static class PanelInsertGuard
{
    /// <summary>Clamps a desired insert index into the valid range [0, <paramref name="childCount"/>].</summary>
    public static int ClampIndex(int desiredIndex, int childCount)
    {
        if (childCount < 0)
        {
            childCount = 0;
        }

        if (desiredIndex < 0)
        {
            return 0;
        }

        return desiredIndex > childCount ? childCount : desiredIndex;
    }
}
