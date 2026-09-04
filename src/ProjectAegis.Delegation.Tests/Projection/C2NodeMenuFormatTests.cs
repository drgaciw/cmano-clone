using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class C2NodeMenuFormatTests
{
    [Test]
    public void FormatLabel_includes_role_unit_and_availability()
    {
        var element = Sample("elem-c2-1", "u1", C2NodeRole.C2, C2NodeAvailability.Available);
        Assert.That(C2NodeMenuFormat.FormatLabel(element), Is.EqualTo("C2 u1 (Available)"));
    }

    [Test]
    public void FormatLabel_renders_last_known_and_unavailable()
    {
        var lastKnown = Sample("elem-relay-1", "u3", C2NodeRole.Relay, C2NodeAvailability.LastKnown);
        var unavailable = Sample("elem-shooter-1", "u2", C2NodeRole.Shooter, C2NodeAvailability.Unavailable);

        Assert.That(C2NodeMenuFormat.FormatLabel(lastKnown), Is.EqualTo("Relay u3 (LastKnown)"));
        Assert.That(C2NodeMenuFormat.FormatLabel(unavailable), Is.EqualTo("Shooter u2 (Unavailable)"));
    }

    [Test]
    public void ItemId_prefixes_element_id()
    {
        Assert.That(C2NodeMenuFormat.ItemId("elem-c2-1"), Is.EqualTo("c2-node-elem-c2-1"));
    }

    private static C2NodeElement Sample(
        string elementId,
        string platformUnitId,
        C2NodeRole role,
        C2NodeAvailability availability) =>
        new(
            elementId,
            platformUnitId,
            role,
            availability,
            new C2NodeMembership("pkg-asuw-1", "Baltic ASuW Package", C2NodeMembershipKind.Package),
            "package-test",
            false,
            0,
            0,
            null,
            Array.Empty<string>());
}
