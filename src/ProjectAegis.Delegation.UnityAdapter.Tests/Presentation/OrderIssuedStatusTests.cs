namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

/// <summary>UAT D25: successful toolbar orders show a visible confirmation.</summary>
[TestFixture]
public sealed class OrderIssuedStatusTests
{
    [Test]
    public void FormatIssued_includes_command_and_unit()
    {
        Assert.That(OrderIssuedStatus.FormatIssued("hold", "u1"), Is.EqualTo("ISSUED: hold → u1"));
    }

    [Test]
    public void FormatIssued_without_unit_still_confirms()
    {
        Assert.That(OrderIssuedStatus.FormatIssued("rtb", null), Is.EqualTo("ISSUED: rtb"));
    }
}
