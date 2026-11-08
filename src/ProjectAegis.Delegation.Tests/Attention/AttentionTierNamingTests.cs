using ProjectAegis.Delegation.Attention;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Attention;

[TestFixture]
public sealed class AttentionTierNamingTests
{
    [Test]
    public void FromDegradation_highest_severity_wins()
    {
        Assert.That(
            AttentionTierNaming.FromDegradation(new AttentionDegradation(true, true, true)),
            Is.EqualTo(AttentionTierName.SimplerDecisions));
        Assert.That(
            AttentionTierNaming.FromDegradation(new AttentionDegradation(true, true, false)),
            Is.EqualTo(AttentionTierName.NarrowedFocus));
        Assert.That(
            AttentionTierNaming.FromDegradation(new AttentionDegradation(true, false, false)),
            Is.EqualTo(AttentionTierName.SlowerReactions));
        Assert.That(
            AttentionTierNaming.FromDegradation(new AttentionDegradation(false, false, false)),
            Is.EqualTo(AttentionTierName.Nominal));
    }

    [Test]
    public void AccessibleLabel_includes_tier_and_load_not_color()
    {
        var label = AttentionTierNaming.AccessibleLabel(AttentionTierName.NarrowedFocus, 26, 20);
        Assert.That(label, Does.Contain("NarrowedFocus"));
        Assert.That(label, Does.Contain("26.0"));
        Assert.That(label, Does.Contain("20.0"));
        Assert.That(label.ToLowerInvariant(), Does.Not.Contain("#"));
    }
}
