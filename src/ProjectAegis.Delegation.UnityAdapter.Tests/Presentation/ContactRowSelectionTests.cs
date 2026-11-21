namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

/// <summary>UAT D23: ListView selection index resolves to a contact id (keyboard/gamepad path).</summary>
[TestFixture]
public sealed class ContactRowSelectionTests
{
    private static readonly SensorC2ContactRow[] Rows =
    {
        new("c1", "Classified", "t1", "C1 line"),
        new("c2", "Hostile", "t2", "C2 line"),
    };

    [TestCase(0, true, "c1")]
    [TestCase(1, true, "c2")]
    [TestCase(-1, false, null)]
    [TestCase(2, false, null)]
    public void TryResolveContactId_maps_index(int index, bool ok, string? expected)
    {
        Assert.That(ContactRowSelection.TryResolveContactId(Rows, index, out var id), Is.EqualTo(ok));
        Assert.That(id, Is.EqualTo(expected));
    }
}
