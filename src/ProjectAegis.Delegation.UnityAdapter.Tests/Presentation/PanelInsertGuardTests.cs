namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

/// <summary>UAT D21: VisualElement.Insert must never throw ArgumentOutOfRange on an empty root.</summary>
[TestFixture]
public sealed class PanelInsertGuardTests
{
    [TestCase(1, 0, 0)]
    [TestCase(1, 1, 1)]
    [TestCase(1, 5, 1)]
    [TestCase(-1, 3, 0)]
    [TestCase(0, 0, 0)]
    public void ClampIndex_stays_within_zero_and_childCount(int desired, int childCount, int expected)
    {
        Assert.That(PanelInsertGuard.ClampIndex(desired, childCount), Is.EqualTo(expected));
    }
}
