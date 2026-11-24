using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// UAT D24 / C8: controller-level proof through the real call site (<c>ResolveUnitDetail</c>, the path
/// <c>DelegationBridgeHost</c> feeds to the right unit panel). Uses only the controller's public
/// surface so it also compiles against the pre-fix parent.
/// </summary>
[TestFixture]
public sealed class C2PresentationControllerUnitDetailSelectionTests
{
    [Test]
    public void ResolveUnitDetail_after_hostile_contact_selected_disables_all_attack_options_with_NO_SELECTION()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();
        var snapshot = new SimWorldSnapshotStub(contactCount: 1, hasFireControlTrackOnPrimaryContact: true);
        var controller = new C2PresentationController();

        controller.SelectFriendlyUnit("u1");
        var withUnit = controller.ResolveUnitDetail(snapshot, bridge.Registry, bridge);
        controller.SelectHostileContact(
            "c1",
            [new ContactPictureEntry("c1", "t1", "u1", "CLASSIFIED", 5, 5.0)]);
        var afterContact = controller.ResolveUnitDetail(snapshot, bridge.Registry, bridge);

        Assert.That(withUnit, Is.Not.Null);
        Assert.That(withUnit!.AttackMenu.Any(o => o.Enabled), Is.True, "precondition: a selected unit has a live option");
        Assert.That(controller.SelectedUnitId, Is.Null);
        Assert.That(afterContact, Is.Not.Null);
        Assert.That(afterContact!.AttackMenu, Is.Not.Empty);
        Assert.That(afterContact.AttackMenu.Select(o => o.Enabled), Has.All.False);
        Assert.That(afterContact.AttackMenu.Select(o => o.DisabledReason), Has.All.EqualTo("NO_SELECTION"));
    }
}
